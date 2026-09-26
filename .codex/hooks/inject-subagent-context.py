#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""跨平台子代理上下文注入 hook。

创建实施、检查、研究子代理时注入任务专属上下文。

核心设计：
- hook 负责完整上下文注入，子代理根据充分信息自主工作。
- 各代理使用专属 jsonl 定义上下文。
- 无需恢复或分段，行为由代码控制，不依赖提示词控制。

触发：PreToolUse（Task 工具调用前）；Codex 原生入口为 SubagentStart。

上下文来源：Trellis 活动任务解析器指向的任务目录。
- implement.jsonl：实施代理专属上下文
- check.jsonl：检查代理专属上下文
- prd.md：需求文档
- design.md：复杂任务技术设计
- implement.md：复杂任务执行计划
- codex-review-output.txt：代码审查结果"""
from __future__ import annotations

# 注意：首先屏蔽所有警告
import warnings
warnings.filterwarnings("ignore")

import json
import os
import sys
from pathlib import Path
from typing import Any

# 无论进程区域设置如何，hook 宿主均发送 UTF-8 JSON。
_stdin_reconfigure = getattr(sys.stdin, "reconfigure", None)
if callable(_stdin_reconfigure):
    try:
        _stdin_reconfigure(encoding="utf-8", errors="replace")
    except (OSError, ValueError):
        pass

# 注意：Windows 上强制标准输出使用 UTF-8
# 避免输出非 ASCII 字符时抛出 UnicodeEncodeError
if sys.platform.startswith("win"):
    import io as _io
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # type: ignore[union-attr]
    elif hasattr(sys.stdout, "detach"):
        sys.stdout = _io.TextIOWrapper(sys.stdout.detach(), encoding="utf-8", errors="replace")  # type: ignore[union-attr]


# =============================================================================
# 路径常量（重命名目录时在此修改）
# =============================================================================

DIR_WORKFLOW = ".trellis"
DIR_SPEC = "spec"
FILE_TASK_JSON = "task.json"

# =============================================================================
# 子代理常量（重命名代理类型时在此修改）
# =============================================================================

AGENT_IMPLEMENT = "trellis-implement"
AGENT_CHECK = "trellis-check"
AGENT_RESEARCH = "trellis-research"

# 需要任务目录的代理
AGENTS_REQUIRE_TASK = (AGENT_IMPLEMENT, AGENT_CHECK)
# 所有支持的代理
AGENTS_ALL = (AGENT_IMPLEMENT, AGENT_CHECK, AGENT_RESEARCH)


def find_repo_root(start_path: str) -> str | None:
    """从 start_path 向上查找 Git 仓库根目录。

    返回：
        仓库根路径，未找到时返回 None。"""
    current = Path(start_path).resolve()
    while current != current.parent:
        if (current / ".git").exists():
            return str(current)
        current = current.parent
    return None


def _detect_platform(input_data: dict) -> str | None:
    if _hook_event_name(input_data) == "SubagentStart":
        return "codex"
    if isinstance(input_data.get("cursor_version"), str):
        return "cursor"
    # CLAUDE_PROJECT_DIR 是多个宿主会设置的兼容别名；
    # CodeBuddy、ZCode、Trae 会同时设置它与自身变量，因此必须
    # 最后检查它，否则这些宿主都会被识别为 claude，
    # 上下文键会变成 `claude_<their-session-id>`，与
    # `task.py start` 以真实宿主名写入的会话文件不匹配，
    # 导致指针实际存在于磁盘上，子代理启动却没有任务上下文。
    # 修复方式同 inject-workflow-state.py 和 session-start.py；
    # 前两份修正时曾遗漏此处的第三份实现。
    env_map = {
        "ZCODE_PROJECT_DIR": "zcode",
        "CURSOR_PROJECT_DIR": "cursor",
        "CODEBUDDY_PROJECT_DIR": "codebuddy",
        "FACTORY_PROJECT_DIR": "droid",
        "GEMINI_PROJECT_DIR": "gemini",
        "QODER_PROJECT_DIR": "qoder",
        "KIRO_PROJECT_DIR": "kiro",
        "COPILOT_PROJECT_DIR": "copilot",
        "TRAE_PROJECT_DIR": "trae",
        # 最后检查共享别名；仅未匹配任何厂商键时才有效。
        "CLAUDE_PROJECT_DIR": "claude",
    }
    for env_name, platform in env_map.items():
        if os.environ.get(env_name):
            return platform
    script_parts = set(Path(sys.argv[0]).parts)
    if ".claude" in script_parts:
        return "claude"
    if ".cursor" in script_parts:
        return "cursor"
    if ".gemini" in script_parts:
        return "gemini"
    if ".qoder" in script_parts:
        return "qoder"
    if ".codebuddy" in script_parts:
        return "codebuddy"
    if ".factory" in script_parts:
        return "droid"
    if ".kiro" in script_parts:
        return "kiro"
    if ".zcode" in script_parts:
        return "zcode"
    return None


def get_current_task(
    repo_root: str,
    input_data: dict,
    *,
    platform: str | None = None,
    allow_single_session_fallback: bool = True,
    allow_environment_context: bool = True,
    require_existing: bool = False,
) -> str | None:
    """通过统一活动任务解析器确定当前任务目录。"""
    scripts_dir = Path(repo_root) / DIR_WORKFLOW / "scripts"
    if str(scripts_dir) not in sys.path:
        sys.path.insert(0, str(scripts_dir))
    try:
        from common.active_task import resolve_active_task  # type: ignore[import-not-found]
    except Exception:
        return None

    active = resolve_active_task(
        Path(repo_root),
        input_data,
        platform=platform or _detect_platform(input_data),
        allow_single_session_fallback=allow_single_session_fallback,
        allow_environment_context=allow_environment_context,
    )
    if require_existing and active.stale:
        return None
    return active.task_path


# =============================================================================
# 上下文注入限制（issue #441）
#
# 上游 Pi TS 扩展逐字节镜像提示与行为；本项目只定制本地 hook。
# 上游路径：templates/pi/extensions/trellis/index.ts.txt。若在上游修改文案，
# 也必须同步该文件；本项目没有该上游模板树。
# =============================================================================

DEFAULT_MAX_FILE_BYTES = 32768
DEFAULT_MAX_ARTIFACT_BYTES = 65536
DEFAULT_MAX_TOTAL_BYTES = 131072

DEFAULT_LIMITS: dict[str, int] = {
    "max_file_bytes": DEFAULT_MAX_FILE_BYTES,
    "max_artifact_bytes": DEFAULT_MAX_ARTIFACT_BYTES,
    "max_total_bytes": DEFAULT_MAX_TOTAL_BYTES,
}


def _get_limits(repo_root: str) -> dict[str, int]:
    """从 config.yaml 加载上下文注入的字节限制，失败时安全回退。"""
    scripts_dir = Path(repo_root) / DIR_WORKFLOW / "scripts"
    if str(scripts_dir) not in sys.path:
        sys.path.insert(0, str(scripts_dir))
    try:
        from common.config import get_context_injection_limits  # type: ignore[import-not-found]

        return get_context_injection_limits(Path(repo_root))
    except Exception:
        return dict(DEFAULT_LIMITS)


def truncate_utf8(data: bytes, cap: int) -> bytes:
    """将 ``data`` 截断到最多 ``cap`` 字节，不拆开 UTF-8 多字节序列。

    ``cap <= 0`` 表示不限，原样返回 ``data``。"""
    if cap <= 0 or len(data) <= cap:
        return data

    truncated = data[:cap]
    i = len(truncated)
    # 从续字节（10xxxxxx）向前回退，找到首字节。
    while i > 0 and (truncated[i - 1] & 0xC0) == 0x80:
        i -= 1
    if i == 0:
        return b""

    lead = truncated[i - 1]
    if lead & 0x80:
        if (lead & 0xE0) == 0xC0:
            seq_len = 2
        elif (lead & 0xF0) == 0xE0:
            seq_len = 3
        elif (lead & 0xF8) == 0xF0:
            seq_len = 4
        else:
            seq_len = 1
        # 完整序列放不下时，也移除首字节。
        if (i - 1) + seq_len > len(truncated):
            return truncated[:i - 1]

    return truncated


class _Budget:
    """累计跟踪已输出到子代理上下文的字节数。"""

    def __init__(self, max_total_bytes: int) -> None:
        self.max_total_bytes = max_total_bytes
        self.used = 0

    def has_room(self, size: int) -> bool:
        if self.max_total_bytes <= 0:
            return True
        return self.used + size <= self.max_total_bytes

    def add(self, size: int) -> None:
        self.used += size


def _real_path_contained(base_real: str, target_real: str) -> bool:
    """检查已解析实际路径的目标是否位于已解析实际路径的基目录内。

    Windows 上两者盘符不同时会抛出 ValueError，此时目标显然在基目录外，故拒绝。"""
    try:
        return os.path.commonpath([base_real, target_real]) == base_real
    except ValueError:
        return False


def _read_file_bytes(base_path: str, file_path: str) -> bytes | None:
    """读取文件原始字节；文件不存在时返回 None。"""
    full_path = os.path.join(base_path, file_path)
    try:
        root_real = os.path.realpath(base_path)
        # `.trellis` 本身可能是指向仓库外存储的符号链接
        # （#567）；其实际位置是第二个合法的包含基目录。
        workflow_real = os.path.realpath(os.path.join(base_path, ".trellis"))
        full_real = os.path.realpath(full_path)
        if not _real_path_contained(root_real, full_real) and not (
            _real_path_contained(workflow_real, full_real)
        ):
            return None
    except OSError:
        return None
    if os.path.exists(full_path) and os.path.isfile(full_path):
        try:
            with open(full_path, "rb") as f:
                return f.read()
        except Exception:
            return None
    return None


def _truncate_notice(path: str, cap: int) -> str:
    return f"\n[Trellis：已按 {cap} 字节上限截断；请阅读 {path} 获取完整内容]"


def _is_binary_content(data: bytes) -> bool:
    """原始字节不应被解码到模型上下文时返回 True。"""
    if b"\x00" in data:
        return True
    try:
        data.decode("utf-8", errors="strict")
    except UnicodeDecodeError:
        return True
    return False


def _binary_notice(path: str, size: int, reason: str) -> str:
    return (
        f"[Trellis：未内联（二进制文件）；"
        f"{path} （{size} 字节）：{reason}]"
    )


def _index_notice(path: str, size: int, reason: str) -> str:
    return (
        f"[Trellis：未内联（已达上下文总量上限）；"
        f"{path} （{size} 字节）：{reason}]"
    )


def _budgeted_block(
    budget: _Budget,
    header: str,
    plain_path: str,
    content: str,
    reason: str,
    size_for_index: int,
) -> str:
    """返回内联的 ``=== header ===`` 区块；总上下文预算耗尽时改为索引提示。"""
    block = f"=== {header} ===\n{content}"
    block_bytes = len(block.encode("utf-8"))
    if not budget.has_room(block_bytes):
        notice = _index_notice(plain_path, size_for_index, reason)
        budget.add(len(notice.encode("utf-8")))
        return notice
    budget.add(block_bytes)
    return block


def _materialize_file(
    base_path: str,
    file_path: str,
    reason: str,
    limits: dict[str, int],
    budget: _Budget,
) -> str | None:
    """读取 JSONL 引用的文件，应用单文件上限，再计入总预算。"""
    data = _read_file_bytes(base_path, file_path)
    if data is None:
        return None

    size = len(data)
    if _is_binary_content(data):
        notice = _binary_notice(file_path, size, reason)
        budget.add(len(notice.encode("utf-8")))
        return notice

    cap = limits["max_file_bytes"]
    truncated_bytes = truncate_utf8(data, cap)
    content = truncated_bytes.decode("utf-8", errors="replace")
    if len(truncated_bytes) < size:
        content += _truncate_notice(file_path, cap)

    return _budgeted_block(budget, file_path, file_path, content, reason, size)


def _materialize_directory(
    base_path: str,
    dir_path: str,
    reason: str,
    limits: dict[str, int],
    budget: _Budget,
    max_files: int = 20,
) -> list[str]:
    """读取目录中的所有 .md 文件，应用与单文件 JSONL 条目相同的单文件和总量限制。"""
    full_path = os.path.join(base_path, dir_path)
    if not os.path.exists(full_path) or not os.path.isdir(full_path):
        return []

    blocks: list[str] = []
    try:
        md_files = sorted(
            f
            for f in os.listdir(full_path)
            if f.endswith(".md") and os.path.isfile(os.path.join(full_path, f))
        )
        for filename in md_files[:max_files]:
            relative_path = os.path.join(dir_path, filename)
            block = _materialize_file(base_path, relative_path, reason, limits, budget)
            if block:
                blocks.append(block)
    except Exception:
        pass

    return blocks


def read_jsonl_entries(base_path: str, jsonl_path: str) -> list[dict]:
    """解析 jsonl 上下文文件引用的所有文件和目录条目。

    格式：
        {"file": "path/to/file.md", "reason": "说明"}
        {"file": "path/to/dir/", "type": "directory", "reason": "说明"}
        {"_example": "示例"}  # 旧占位行，没有 `file` 字段，跳过

    没有 ``file`` 字段的行（如旧版 Trellis 在 ``task.py create`` 时写入的占位行）
    静默跳过。最终条目列表为空时在标准错误输出警告，便于排查上下文缺失。

    返回：
        [{"file": path, "type": "file" | "directory", "reason": reason}, ...]"""
    full_path = os.path.join(base_path, jsonl_path)
    if not os.path.exists(full_path):
        print(
            f"[inject-subagent-context] 警告：未找到 {jsonl_path}；"
            f"子代理只会收到任务文件",
            file=sys.stderr,
        )
        return []

    entries: list[dict] = []
    saw_real_entry = False
    try:
        with open(full_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    item = json.loads(line)
                    file_path = item.get("file") or item.get("path")

                    if not file_path:
                        # 占位或注释行，静默跳过
                        continue

                    saw_real_entry = True
                    entries.append(
                        {
                            "file": file_path,
                            "type": item.get("type", "file"),
                            "reason": item.get("reason") or "-",
                        }
                    )
                except json.JSONDecodeError:
                    continue
    except Exception:
        pass

    if not saw_real_entry:
        print(
            f"[inject-subagent-context] 警告：{jsonl_path} 没有整理后的条目"
            f"（仅有占位内容或为空）；子代理只会收到"
            f"任务文件。请查阅 workflow.md 的规划文件指引。",
            file=sys.stderr,
        )

    return entries


def _materialize_jsonl_entries(
    base_path: str, jsonl_path: str, limits: dict[str, int], budget: _Budget
) -> list[str]:
    """将 jsonl 中的每个条目读入上下文区块，应用单文件和总预算限制。"""
    blocks: list[str] = []
    for entry in read_jsonl_entries(base_path, jsonl_path):
        if entry["type"] == "directory":
            blocks.extend(
                _materialize_directory(
                    base_path, entry["file"], entry["reason"], limits, budget
                )
            )
        else:
            block = _materialize_file(
                base_path, entry["file"], entry["reason"], limits, budget
            )
            if block:
                blocks.append(block)
    return blocks


def get_agent_context(
    repo_root: str,
    task_dir: str,
    agent_type: str,
    limits: dict[str, int],
    budget: _Budget,
) -> str:
    """从 {agent_type}.jsonl 获取指定代理的上下文。

    只读取任务系统创建的两个 JSONL 文件：implement.jsonl 或 check.jsonl。"""
    agent_jsonl = f"{task_dir}/{agent_type}.jsonl"
    blocks = _materialize_jsonl_entries(repo_root, agent_jsonl, limits, budget)
    if not blocks:
        # 否则没有任何整理后的上下文也会静默传给模型；
        # 上面的标准错误警告不会进入任何会话（#573）。因此将情况写入
        # 提示本身，让子代理自行补齐，不会错误地认为
        # 已获得完整规范上下文。
        return (
            f"[Trellis] {agent_jsonl} 没有整理后的条目，因此未注入规范或研究上下文。"
            "开始工作前，请阅读 .trellis/spec/ 下与待修改代码相关的指南，"
            "并将下方任务文件视为仅有的预备上下文。"
        )
    return "\n\n".join(blocks)


def _materialize_artifact(
    base_path: str,
    file_path: str,
    header_label: str,
    reason: str,
    limits: dict[str, int],
    budget: _Budget,
) -> str | None:
    """读取任务文件（prd/design/implement.md），应用单文件产物上限，再计入总预算。"""
    data = _read_file_bytes(base_path, file_path)
    if data is None:
        return None

    size = len(data)
    cap = limits["max_artifact_bytes"]
    truncated_bytes = truncate_utf8(data, cap)
    content = truncated_bytes.decode("utf-8", errors="replace")
    if len(truncated_bytes) < size:
        content += _truncate_notice(file_path, cap)

    return _budgeted_block(budget, header_label, file_path, content, reason, size)


def get_implement_context(repo_root: str, task_dir: str) -> str:
    """实施代理的完整上下文。

    读取顺序：
    1. implement.jsonl 引用的全部文件（规范和研究清单）
    2. prd.md（需求）
    3. design.md（若存在，技术设计）
    4. implement.md（若存在，执行计划）"""
    limits = _get_limits(repo_root)
    budget = _Budget(limits["max_total_bytes"])
    context_parts = []

    # 1. 读取 implement.jsonl
    base_context = get_agent_context(repo_root, task_dir, "implement", limits, budget)
    if base_context:
        context_parts.append(base_context)

    # 2. 需求文档
    prd_block = _materialize_artifact(
        repo_root,
        f"{task_dir}/prd.md",
        f"{task_dir}/prd.md （需求）",
        "需求文档",
        limits,
        budget,
    )
    if prd_block:
        context_parts.append(prd_block)

    # 3. 复杂任务的技术设计
    design_block = _materialize_artifact(
        repo_root,
        f"{task_dir}/design.md",
        f"{task_dir}/design.md （技术设计）",
        "技术设计文档",
        limits,
        budget,
    )
    if design_block:
        context_parts.append(design_block)

    # 4. 复杂任务的执行计划
    implement_plan_block = _materialize_artifact(
        repo_root,
        f"{task_dir}/implement.md",
        f"{task_dir}/implement.md （执行计划）",
        "执行计划文档",
        limits,
        budget,
    )
    if implement_plan_block:
        context_parts.append(implement_plan_block)

    return "\n\n".join(context_parts)


def get_check_context(repo_root: str, task_dir: str) -> str:
    """检查代理上下文：check.jsonl 与任务文件。"""
    limits = _get_limits(repo_root)
    budget = _Budget(limits["max_total_bytes"])
    context_parts = []

    base_context = get_agent_context(repo_root, task_dir, "check", limits, budget)
    if base_context:
        context_parts.append(base_context)

    prd_block = _materialize_artifact(
        repo_root,
        f"{task_dir}/prd.md",
        f"{task_dir}/prd.md （需求）",
        "需求文档",
        limits,
        budget,
    )
    if prd_block:
        context_parts.append(prd_block)

    design_block = _materialize_artifact(
        repo_root,
        f"{task_dir}/design.md",
        f"{task_dir}/design.md （技术设计）",
        "技术设计文档",
        limits,
        budget,
    )
    if design_block:
        context_parts.append(design_block)

    implement_plan_block = _materialize_artifact(
        repo_root,
        f"{task_dir}/implement.md",
        f"{task_dir}/implement.md （执行计划）",
        "执行计划文档",
        limits,
        budget,
    )
    if implement_plan_block:
        context_parts.append(implement_plan_block)

    return "\n\n".join(context_parts)


def get_finish_context(repo_root: str, task_dir: str) -> str:
    """收尾阶段上下文：复用检查上下文及任务文件。

    收尾属于最终检查，因此采用相同上下文来源。"""
    return get_check_context(repo_root, task_dir)



def build_implement_prompt(original_prompt: str, context: str) -> str:
    """构建实施代理的完整提示。"""
    return f"""<!-- trellis-hook-injected -->
# 实施代理任务

你是多代理流程中的实施代理。

## 上下文

工作所需信息已准备如下：

{context}

---

## 任务

{original_prompt}

---

## 工作流程

1. **理解规范**：阅读上方注入的全部开发规范。
2. **理解任务文件**：阅读需求，以及存在的技术设计和执行计划。
3. **实施功能**：遵循规范和任务文件实施。
4. **自检**：依据检查规范保证代码质量。

## 重要约束

- 禁止执行 git commit，只负责代码修改；由主会话统一提交。
- 遵循上方注入的全部开发规范。
- 完成后报告修改和新建的文件清单。"""


def build_check_prompt(original_prompt: str, context: str) -> str:
    """构建检查代理的完整提示。"""
    return f"""<!-- trellis-hook-injected -->
# 检查代理任务

你是多代理流程中的检查代理，负责代码与跨层检查。

## 上下文

所需检查规范和开发规范如下：

{context}

---

## 任务

{original_prompt}

---

## 工作流程

1. **获取变更**：运行 `git diff --name-only` 和 `git diff` 查看代码差异。
2. **按规范检查**：逐项对照上方规范。
3. **自行修复**：直接修复问题，不只报告。
4. **执行验证**：运行项目的 lint 和类型检查命令。

## 重要约束

- 自行修复问题，不只报告。
- 必须执行检查规范中的完整清单。
- 特别关注影响范围分析（L1-L5）。
- 禁止执行 git commit，由主会话统一提交。"""


def build_finish_prompt(original_prompt: str, context: str) -> str:
    """构建收尾代理的完整提示（创建 PR 前的最终检查）。"""
    return f"""<!-- trellis-hook-injected -->
# 收尾代理任务

你负责创建 PR 前的最终检查。

## 上下文

收尾检查清单与需求如下：

{context}

---

## 任务

{original_prompt}

---

## 工作流程

1. **审阅变更**：运行 `git diff --name-only` 查看全部变更文件。
2. **核对任务文件**：检查 prd.md 需求及存在的 design.md / implement.md。
3. **同步规范**：分析变更是否引入新模式、契约或约定。
   - 发现新模式或约定：先读目标规范 → 更新规范 → 必要时更新 index.md。
   - 基础设施或跨层变更：遵循 update-spec.md 的七节必填模板。
   - 仅为代码修复且无新模式：跳过此步。
4. **最终检查**：执行 lint 和类型检查。
5. **确认就绪**：确保代码已具备创建 PR 的条件。

## 重要约束

- 发现规范缺口时可以更新规范文件，参考 update-spec.md。
- 编辑前必须先读目标规范，避免重复已有内容。
- 拼写、格式或明显修正等琐碎变更无需更新规范。
- 发现严重代码问题时明确报告；此阶段修复规范，不修改代码。
- 验证 prd.md 中全部验收标准已满足。
- design.md 和 implement.md 存在时，核对其中约束。
- 禁止执行 git commit，由主会话统一提交。"""



def get_research_context(repo_root: str, task_dir: str | None) -> str:
    """研究代理上下文：项目规范目录的结构概览。

    保留 `task_dir` 参数以与 get_implement_context / get_check_context 的签名
    一致，便于派发器统一调用。"""
    _ = task_dir
    context_parts = []

    # 1. 项目结构概览（动态发现规范目录）
    spec_path = f"{DIR_WORKFLOW}/{DIR_SPEC}"
    spec_root = Path(repo_root) / DIR_WORKFLOW / DIR_SPEC

    # 动态构建规范目录树
    tree_lines = [f"{spec_path}/"]
    if spec_root.is_dir():
        pkg_dirs = sorted(d for d in spec_root.iterdir() if d.is_dir())
        for i, pkg_dir in enumerate(pkg_dirs):
            is_last = i == len(pkg_dirs) - 1
            prefix = "└── " if is_last else "├── "
            layers = sorted(d.name for d in pkg_dir.iterdir() if d.is_dir())
            layer_info = f" ({', '.join(layers)})" if layers else ""
            tree_lines.append(f"{prefix}{pkg_dir.name}/{layer_info}")

    spec_tree = "\n".join(tree_lines)

    project_structure = f"""## 项目规范目录结构

```
{spec_tree}
```

获取结构化包信息，请运行： `python3 ./{DIR_WORKFLOW}/scripts/get_context.py --mode packages`

## 搜索提示

- 规范文件： `{spec_path}/**/*.md`
- 代码搜索：使用 Glob 和 Grep 工具
- 技术方案：使用 mcp__exa__web_search_exa 或 mcp__exa__get_code_context_exa"""

    context_parts.append(project_structure)

    return "\n\n".join(context_parts)


def build_research_prompt(original_prompt: str, context: str) -> str:
    """构建研究代理的完整提示。"""
    return f"""# 研究代理任务

你是多代理流程中的研究代理，负责搜索研究。

## 核心原则

**专注于查找和解释信息。**

职责是记录信息，不进行审查。

## 项目信息

{context}

---

## 任务

{original_prompt}

---

## 工作流程

1. **理解问题**：确定搜索类型（内部/外部）和范围。
2. **规划搜索**：复杂问题先列出搜索步骤。
3. **执行搜索**：并行执行多个独立搜索。
4. **整理结果**：输出结构化报告。

## 搜索工具

| 工具 | 用途 |
|------|------|
| Glob | 按文件名模式搜索 |
| Grep | 按内容搜索 |
| Read | 读取文件内容 |
| mcp__exa__web_search_exa | 外部网页搜索 |
| mcp__exa__get_code_context_exa | 外部代码或文档搜索 |

## 严格边界

**只允许**：说明已有内容、所在位置及工作方式。

**禁止**（除非明确要求）：
- 提出改进建议。
- 评价实现缺点。
- 推荐重构。
- 修改任何文件。

## 报告格式

提供结构化搜索结果，包括：
- 找到的文件清单及路径。
- 代码模式分析（如适用）。
- 相关规范文档。
- 外部引用（如有）。"""


def _string_value(value: Any) -> str:
    if isinstance(value, str):
        stripped = value.strip()
        return stripped
    return ""


def _hook_event_name(input_data: dict) -> str:
    """从约定的蛇形或驼峰字段返回 hook 事件名。"""
    return _string_value(
        input_data.get("hook_event_name") or input_data.get("hookEventName")
    )


def _codex_subagent_type(input_data: dict) -> str:
    """仅为原生启动事件返回 Trellis Codex 代理类型。"""
    if _hook_event_name(input_data) != "SubagentStart":
        return ""
    agent_type = _string_value(
        input_data.get("agent_type") or input_data.get("agentType")
    )
    return agent_type if agent_type in AGENTS_ALL else ""


def build_codex_subagent_context(
    subagent_type: str,
    task_dir: str,
    context: str,
) -> str:
    """为已派发的 Codex 原生角色构建开发者上下文。"""
    role = subagent_type.removeprefix("trellis-")
    return f"""<!-- trellis-hook-injected -->
# Trellis 原生 {role} 子代理

你已作为 `{subagent_type}` 角色派发到此任务，请直接履行该角色。
不要执行仅供主会话使用的派发或等待指令，也不要再创建 Trellis 子代理。

Active task: {task_dir}

## 已整理的上下文

{context}"""


def _handle_codex_subagent_start(input_data: dict) -> None:
    """为识别到的原生 Trellis 子代理输出 Codex 开发者上下文。

    事件提供父会话 ID。此处必须禁用通用单会话回退：父会话 ID 缺失或失效时，
    原生启动绝不能借用其他 Codex 窗口的任务。"""
    subagent_type = _codex_subagent_type(input_data)
    parent_session_id = _string_value(input_data.get("session_id"))
    if not subagent_type or not parent_session_id:
        return

    # 先使用载荷中的 cwd，再尝试本进程目录；有些宿主（如 CodeBuddy IDE 4.10.4）
    # 所有 hook 事件都报告 "/"。参见 inject-workflow-state.py。
    repo_root = None
    for candidate in (_string_value(input_data.get("cwd")), os.getcwd()):
        if not candidate:
            continue
        repo_root = find_repo_root(candidate)
        if repo_root:
            break
    if not repo_root:
        return

    task_dir = get_current_task(
        repo_root,
        {"session_id": parent_session_id},
        platform="codex",
        allow_single_session_fallback=False,
        allow_environment_context=False,
        require_existing=True,
    )
    if not task_dir:
        return

    if subagent_type in AGENTS_REQUIRE_TASK:
        task_dir_full = Path(repo_root) / task_dir
        if not task_dir_full.is_dir():
            return

    if subagent_type == AGENT_IMPLEMENT:
        context = get_implement_context(repo_root, task_dir)
    elif subagent_type == AGENT_CHECK:
        context = get_check_context(repo_root, task_dir)
    else:
        context = get_research_context(repo_root, task_dir)

    if not context:
        return

    output = {
        "hookSpecificOutput": {
            "hookEventName": "SubagentStart",
            "additionalContext": build_codex_subagent_context(
                subagent_type, task_dir, context
            ),
        }
    }
    print(json.dumps(output, ensure_ascii=False))


def _extract_subagent_name(value: Any) -> str:
    """从常见平台编码中提取子代理名称。

    Cursor 原生 Task 参数以 protobuf oneof 编码自定义子代理，hook JSON 中
    可能表现为 ``{"custom": {"name": "..."}}`` 或
    ``{"type": {"case": "custom", "value": {"name": "..."}}}``。"""
    direct = _string_value(value)
    if direct:
        return direct

    if not isinstance(value, dict):
        return ""

    for key in ("name", "subagent_type_name", "subagentTypeName"):
        direct = _string_value(value.get(key))
        if direct:
            return direct

    custom = value.get("custom")
    if isinstance(custom, dict):
        custom_name = _string_value(custom.get("name"))
        if custom_name:
            return custom_name

    oneof = value.get("type")
    if isinstance(oneof, dict):
        case_name = _string_value(oneof.get("case"))
        if case_name == "custom":
            nested_value = oneof.get("value")
            if isinstance(nested_value, dict):
                custom_name = _string_value(nested_value.get("name"))
                if custom_name:
                    return custom_name
        if case_name:
            return case_name

    case_name = _string_value(value.get("case"))
    if case_name == "custom":
        nested_value = value.get("value")
        if isinstance(nested_value, dict):
            custom_name = _string_value(nested_value.get("name"))
            if custom_name:
                return custom_name
    if case_name:
        return case_name

    for agent_name in AGENTS_ALL:
        if agent_name in value:
            return agent_name

    return ""


def _extract_subagent_type(tool_input: dict) -> str:
    for key in (
        "subagent_type",
        "subagentType",
        "subagent_type_name",
        "subagentTypeName",
        "subagent_name",
        "subagentName",
        "agent_type",
        "agentType",
        "name",
    ):
        agent_name = _extract_subagent_name(tool_input.get(key))
        if agent_name:
            return agent_name
    return ""


def _parse_hook_input(input_data: dict) -> tuple[str, str, dict]:
    """解析各平台格式的 hook 输入。

    返回 (subagent_type, original_prompt, tool_input)。支持：
    - Claude Code / Qoder / Droid：tool_name=Task|Agent，tool_input.subagent_type
    - CodeBuddy：tool_name=task（IDE）或 Task（CLI），tool_input.subagent_name
    - Cursor：tool_name=Task|Subagent，tool_input.subagent_type
    - Copilot CLI：toolName=task（驼峰键、小写值）
    - ZCode：toolName=Agent，toolInput/tool_input.subagent_type
    - Gemini CLI：tool_name 即代理名（BeforeTool 匹配器已过滤）
    - Kiro：agentSpawn hook，顶层 agent_name 字段"""
    tool_input = input_data.get("tool_input", {})
    if not isinstance(tool_input, dict):
        tool_input = input_data.get("toolInput", {})
    if not isinstance(tool_input, dict):
        tool_input = {}

    # 标准格式：携带 subagent_type 的 Task/Agent 工具
    tool_name = input_data.get("tool_name", "") or input_data.get("toolName", "")
    if tool_name.lower() in ("task", "agent", "subagent"):
        return (
            _extract_subagent_type(tool_input),
            tool_input.get("prompt", ""),
            tool_input,
        )

    # Kiro：agentSpawn hook 在顶层传入 agent_name
    agent_name = input_data.get("agent_name", "")
    if agent_name:
        return agent_name, tool_input.get("prompt", input_data.get("prompt", "")), tool_input

    # Gemini CLI：BeforeTool 的 tool_name 就是代理名
    # （匹配器已确保它属于我们的代理）
    if tool_name in AGENTS_ALL:
        return tool_name, tool_input.get("prompt", ""), tool_input

    # Copilot CLI：驼峰字段 toolName，其值可能是代理名
    tool_name_camel = input_data.get("toolName", "")
    if tool_name_camel in AGENTS_ALL:
        return tool_name_camel, input_data.get("toolArgs", ""), tool_input

    return "", "", tool_input


def main():
    if os.environ.get("TRELLIS_HOOKS") == "0" or os.environ.get("TRELLIS_DISABLE_HOOKS") == "1":
        sys.exit(0)

    try:
        input_data = json.load(sys.stdin)
    except json.JSONDecodeError:
        sys.exit(0)
    if not isinstance(input_data, dict):
        sys.exit(0)

    if _hook_event_name(input_data) == "SubagentStart":
        try:
            _handle_codex_subagent_start(input_data)
        except Exception:
            # 原生上下文 hook 在运行状态不可用或失效时，
            # 绝不能阻止 Codex 创建请求的子代理。
            pass
        sys.exit(0)

    subagent_type, original_prompt, tool_input = _parse_hook_input(input_data)
    cwd = input_data.get("cwd", os.getcwd())

    # 仅处理支持的子代理类型
    if subagent_type not in AGENTS_ALL:
        sys.exit(0)

    # 查找仓库根目录
    repo_root = find_repo_root(cwd)
    if not repo_root:
        sys.exit(0)

    # 获取当前任务目录（研究角色不要求此目录）
    task_dir = get_current_task(
        repo_root,
        input_data,
        allow_single_session_fallback=True,
    )

    # 实施和检查需要任务目录
    if subagent_type in AGENTS_REQUIRE_TASK:
        if not task_dir:
            sys.exit(0)
        # 通过指针读取任何内容前，先校验其范围。虽然 `task.py` 已经
        # 拒绝存储越出仓库的引用，修复前写入的会话文件却可能
        # 仍持有这种指针；而 `trellis update`
        # 不重写会话文件，所以有害指针可能在写入端修复升级后继续存在。
        # 这里是任务 prd.md/design.md 到达模型提示前的
        # 最后一道边界，因此再次检查。
        try:
            root_real = os.path.realpath(repo_root)
            # `.trellis` 本身可能是指向仓库外存储的符号链接
            # （#567）；其实际位置是第二个合法基目录。
            workflow_real = os.path.realpath(os.path.join(repo_root, ".trellis"))
            task_dir_full = os.path.realpath(os.path.join(repo_root, task_dir))
            if not _real_path_contained(root_real, task_dir_full) and not (
                _real_path_contained(workflow_real, task_dir_full)
            ):
                sys.exit(0)
        except OSError:
            sys.exit(0)
        if not os.path.exists(task_dir_full):
            sys.exit(0)

    # 检测提示中的 [finish] 标记（检查代理使用收尾上下文）
    is_finish_phase = "[finish]" in original_prompt.lower()

    # 按子代理类型获取上下文并构建提示
    if subagent_type == AGENT_IMPLEMENT:
        assert task_dir is not None  # 已在上方校验
        context = get_implement_context(repo_root, task_dir)
        new_prompt = build_implement_prompt(original_prompt, context)
    elif subagent_type == AGENT_CHECK:
        assert task_dir is not None  # 已在上方校验
        if is_finish_phase:
            # 收尾阶段：使用收尾上下文，聚焦最终验证
            context = get_finish_context(repo_root, task_dir)
            new_prompt = build_finish_prompt(original_prompt, context)
        else:
            # 常规检查：使用检查上下文，以完整规范执行自修循环
            context = get_check_context(repo_root, task_dir)
            new_prompt = build_check_prompt(original_prompt, context)
    elif subagent_type == AGENT_RESEARCH:
        # 研究可以在没有任务目录的情况下进行
        context = get_research_context(repo_root, task_dir)
        new_prompt = build_research_prompt(original_prompt, context)
    else:
        sys.exit(0)

    if not context:
        sys.exit(0)

    # 返回更新后的输入。多数平台忽略无法识别的字段，因此同时
    # 包含多种格式。ZCode 较严格；此前实际探测确认下方
    # 兼容 Claude 的嵌套结构能够进入子代理提示。
    updated = {**tool_input, "prompt": new_prompt}
    if _detect_platform(input_data) == "zcode":
        output = {
            "hookSpecificOutput": {
                "hookEventName": "PreToolUse",
                "permissionDecision": "allow",
                "updatedInput": updated,
            }
        }
    else:
        output = {
            # Claude Code / Qoder / CodeBuddy / Droid 格式
            "hookSpecificOutput": {
                "hookEventName": "PreToolUse",
                "permissionDecision": "allow",
                "updatedInput": updated,
            },
            # Cursor 格式
            "permission": "allow",
            "updated_input": updated,
            # Gemini 格式
            "updatedInput": updated,
        }

    print(json.dumps(output, ensure_ascii=False))
    sys.exit(0)


if __name__ == "__main__":
    main()
