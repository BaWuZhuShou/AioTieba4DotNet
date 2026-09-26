#!/usr/bin/env python3
"""Trellis 逐轮状态提示 hook（UserPromptSubmit / BeforeAgent）。

每次用户提示触发时，使用 Trellis 支持会话的活动任务解析器确定任务，
输出简短的 <workflow-state> 区块，提醒主代理当前任务及预期流程。

``hookEventName`` 根据平台选择：多数宿主使用 ``UserPromptSubmit``
（Claude Code 命名，也被 Cursor / Qoder / CodeBuddy / Droid / Codex /
Copilot 接线接受）；Gemini CLI 0.40.x 改用 ``BeforeAgent``，其 schema
校验器拒绝旧名称。``_detect_platform`` 在运行时选择对应值。

状态文案只来自 workflow.md 的 [workflow-state:STATUS] 区块，
workflow.md 是唯一真源。此脚本没有回退字典；文件或标签缺失时仅提示
“请查阅 workflow.md 确认当前步骤。”，让用户发现并修复异常，而非静默掩盖。

注册平台由上游 templates/shared-hooks/index.ts 的
SHARED_HOOKS_BY_PLATFORM 决定，当前包括 Claude、Codex、Gemini、Qoder、
Copilot、CodeBuddy、Droid、Kiro、Trae 和 ZCode。各平台的
collect<Platform>Templates() 经 collectSharedHooks() 将本文件纳入模板映射，
初始化时由统一写入器落盘。Kiro 通过 CLI 自定义代理的
``hooks.userPromptSubmit`` 及 IDE ``.kiro.hook`` 的 ``promptSubmit`` 事件接线，
其输出为纯文本（Kiro 将 hook 标准输出直接加入对话上下文）。

未发现 .trellis/（不是 Trellis 项目）时静默退出 0。

会话指向的任务目录若缺少 task.json、内容损坏或没有可用状态，输出
task_error 提示，不将其误报为无活动任务。"""
from __future__ import annotations

import json
import os
import re
import sys
import queue
import threading
from pathlib import Path

# Windows 上强制标准输入、输出和错误流使用 UTF-8。默认代码页通常是
# cp936、cp1252 等；非 ASCII 内容（中文任务名、PRD 片段）无论出现在
# 标准输入（宿主 CLI 的 hook 载荷）还是标准输出（注入区块），都可能导致
# UnicodeDecodeError / UnicodeEncodeError。效果相当于 `python -X utf8`，
# 但逐流设置，避免依赖宿主 CLI 的命令接线方式。
if sys.platform.startswith("win"):
    import io as _io
    for _stream_name in ("stdin", "stdout", "stderr"):
        _stream = getattr(sys, _stream_name, None)
        if _stream is None:
            continue
        if hasattr(_stream, "reconfigure"):
            try:
                _stream.reconfigure(encoding="utf-8", errors="replace")  # type: ignore[union-attr]
            except Exception:
                pass  # 可选的 Windows 流设置，失败时不阻断 hook 启动。
        elif hasattr(_stream, "detach"):
            try:
                setattr(sys, _stream_name, _io.TextIOWrapper(_stream.detach(), encoding="utf-8", errors="replace"))
            except Exception:
                pass  # 可选的 Windows 流设置，失败时不阻断 hook 启动。
from typing import Optional


# 会话没有活动任务时向 Codex 注入启动提示。Codex 不会收到
# 完整 SessionStart 概览，因此用简短提示让主会话
# 阅读一次启动技能，同时保持逐轮状态区块简短。
CODEX_NO_TASK_BOOTSTRAP_NOTICE = """<trellis-bootstrap>
如果本次会话尚未加载 Trellis 上下文，请先阅读一次 `trellis-start` 技能。
</trellis-bootstrap>"""


# ---------------------------------------------------------------------------
# 可容忍工作目录变化的 Trellis 根目录发现（修复 hook 路径稳健性）
# ---------------------------------------------------------------------------

def find_trellis_root(start: Path) -> Optional[Path]:
    """从 start 向上查找包含 .trellis/ 的目录。

    处理从子目录、多包仓库等位置启动造成的工作目录偏移。
    找不到 .trellis/ 时返回 None，静默跳过。"""
    cur = start.resolve()
    while cur != cur.parent:
        if (cur / ".trellis").is_dir():
            return cur
        cur = cur.parent
    return None


# ---------------------------------------------------------------------------
# 活动任务发现
# ---------------------------------------------------------------------------

def _detect_platform(input_data: dict) -> str | None:
    if isinstance(input_data.get("cursor_version"), str):
        return "cursor"
    # CLAUDE_PROJECT_DIR 是多个宿主会设置的兼容别名；
    # CodeBuddy、ZCode、Trae 会同时设置它与自身变量，因此必须
    # 最后检查它，否则这些宿主都会被识别为 claude，
    # 上下文键会变成 `claude_<their-session-id>`，与
    # `task.py start` 以真实宿主名写入的会话文件不匹配，
    # 导致磁盘上已有指针，每一轮却仍报告 no_task。
    # 曾在 CodeBuddy IDE 4.10.4 观察到：会话文件 `codebuddy_ae54840e….json`
    # 与标记 `update-check-claude_ae54840e….marker` 使用同一 ID。
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
    if ".codex" in script_parts:
        return "codex"
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
    if ".trae" in script_parts:
        return "trae"
    if ".zcode" in script_parts:
        return "zcode"
    return None


def _resolve_active_task(root: Path, input_data: dict):
    scripts_dir = root / ".trellis" / "scripts"
    if str(scripts_dir) not in sys.path:
        sys.path.insert(0, str(scripts_dir))
    from common.active_task import resolve_active_task  # type: ignore[import-not-found]

    return resolve_active_task(root, input_data, platform=_detect_platform(input_data))


def get_active_task(
    root: Path, input_data: dict
) -> tuple[str, str, str] | None:
    """返回活动任务数据、任务记录错误或无任务指针。

    ``(task_id, "task_error", source)`` 不同于 ``None``：任务记录缺失或不可读
    时，会话指针仍可能存在；此时需要诊断提示，而非常规 ``no_task`` 提示。"""
    active = _resolve_active_task(root, input_data)
    if not active.task_path:
        return None

    task_dir = Path(active.task_path)
    if not task_dir.is_absolute():
        task_dir = root / task_dir
    if active.stale:
        return task_dir.name, f"stale_{active.source_type}", active.source

    task_json = task_dir / "task.json"
    if not task_json.is_file():
        return task_dir.name, "task_error", active.source
    try:
        data = json.loads(task_json.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return task_dir.name, "task_error", active.source
    if not isinstance(data, dict):
        return task_dir.name, "task_error", active.source

    task_id = data.get("id") or task_dir.name
    status = data.get("status", "")
    if not isinstance(status, str) or not status:
        return task_dir.name, "task_error", active.source
    return task_id, status, active.source


# ---------------------------------------------------------------------------
# 状态提示加载：解析 workflow.md；缺失时仅使用通用查阅提示
# ---------------------------------------------------------------------------

# STATUS 支持字母、数字、下划线和连字符
# （因此 "in-review"、"blocked-by-team" 和 "in_progress" 都可使用）。
_TAG_RE = re.compile(
    r"\[workflow-state:([A-Za-z0-9_-]+)\]\s*\n(.*?)\n\s*\[/workflow-state:\1\]",
    re.DOTALL,
)

def load_breadcrumbs(root: Path) -> dict[str, str]:
    """解析 workflow.md 中的 [workflow-state:STATUS] 区块。

    返回 {status: body_text}。workflow.md 是唯一真源，此脚本没有回退字典。
    标签缺失或 workflow.md 缺失、不可读时，build_breadcrumb 使用通用提示，
    让用户看到异常并修复 workflow.md，不静默掩盖问题。"""
    workflow = root / ".trellis" / "workflow.md"
    if not workflow.is_file():
        return {}
    try:
        content = workflow.read_text(encoding="utf-8")
    except OSError:
        return {}

    result: dict[str, str] = {}
    for match in _TAG_RE.finditer(content):
        status = match.group(1)
        body = match.group(2).strip()
        if body:
            result[status] = body
    return result


def _read_trellis_config(root: Path) -> dict:
    """使用捆绑的 trellis_config 助手加载 .trellis/config.yaml。

    助手位于 .trellis/scripts/common，而 hook 位于脚本树之外，导入前扩展 sys.path。"""
    scripts_dir = root / ".trellis" / "scripts"
    if str(scripts_dir) not in sys.path:
        sys.path.insert(0, str(scripts_dir))
    try:
        from common.trellis_config import read_trellis_config  # type: ignore[import-not-found]
    except Exception:
        return {}
    try:
        return read_trellis_config(root)
    except Exception:
        return {}


DEFAULT_PROMPT_INJECTION_SKIP_KEYWORD = "no-trellis"


def _resolve_skip_keyword(config: dict) -> str:
    """从解析后的 .trellis/config.yaml 读取 `prompt_injection.skip_keyword`。

    与 `common.config.get_prompt_injection_config()` 一致，默认为 "no-trellis"；
    空字符串完全禁用跳过入口，非字符串值回退为默认值。"""
    if isinstance(config, dict):
        section = config.get("prompt_injection")
        if isinstance(section, dict):
            raw = section.get("skip_keyword", DEFAULT_PROMPT_INJECTION_SKIP_KEYWORD)
            if isinstance(raw, str):
                return raw
    return DEFAULT_PROMPT_INJECTION_SKIP_KEYWORD


def prompt_has_skip_keyword(prompt: str, keyword: str) -> bool:
    """在 `prompt` 中忽略大小写、按词边界匹配 `keyword`。

    连字符视为词字符，因此 "no-trellisx"、"xno-trellis" 和 "foo-no-trellis"
    不匹配；标点和空白边界可以匹配。空关键词永不匹配（禁用跳过入口）。"""
    if not keyword or not isinstance(prompt, str):
        return False
    pattern = r"(?<![\w-])" + re.escape(keyword) + r"(?![\w-])"
    return re.search(pattern, prompt, re.IGNORECASE) is not None


def _resolve_codex_dispatch_mode(config: dict) -> str:
    """将 .trellis/config.yaml 的 `codex.dispatch_mode` 规范化为 "auto" 或 "inline"。

    默认为 `auto`，旧值 `sub-agent` 是其别名。其他显式值（包括无效值）回退
    为 `inline`，不逐轮警告。`_codex_mode_banner` 和 `resolve_breadcrumb_key`
    共用此函数，使逐轮模式说明与状态标签始终一致。"""
    mode = "auto"
    if isinstance(config, dict):
        codex_cfg = config.get("codex")
        if isinstance(codex_cfg, dict):
            cfg_mode = str(codex_cfg.get("dispatch_mode", mode)).strip().lower()
            if cfg_mode == "inline":
                mode = "inline"
            elif cfg_mode in ("auto", "sub-agent"):
                mode = "auto"
            else:
                mode = "inline"
    return mode


def _codex_mode_banner(config: dict) -> str:
    """为 additionalContext 输出 `<codex-mode>` 说明。

    读取 .trellis/config.yaml 的 `codex.dispatch_mode`，默认 `auto`：使用 Codex
    原生注入派发 Trellis 子代理，子代理自行加载作为回退。不依赖继承父对话：
    `fork_turns` 仍由调用者控制，全新历史的子代理也会收到明确的委派任务和
    继承的会话配置。`inline` 显式禁用派发；旧值 `sub-agent` 是 `auto` 的别名。
    无效显式值回退为 `inline`，不逐轮警告。模式说明告诉 AI 应采用的派发协议，
    与告知当前步骤的逐状态 workflow-state 正文互补。"""
    mode = _resolve_codex_dispatch_mode(config)
    if mode == "auto":
        meaning = (
            "auto：实施/检查默认交给 Trellis 子代理；优先使用 Codex 原生上下文注入，"
            "子代理自行加载作为回退。"
            "主会话继续负责协调、澄清、更新规范、提交和收尾。"
        )
    else:
        meaning = (
            "inline：主会话直接实施和检查；"
            "不派发实施/检查子代理。"
        )
    return f"<codex-mode>{meaning}</codex-mode>"


def resolve_breadcrumb_key(
    status: str, platform: str | None, config: dict
) -> str:
    """根据 Codex dispatch_mode 选择状态标签键。

    Codex 默认 ``auto``，使用普通 ``<status>`` 提示，支持原生 SubagentStart
    派发和子代理加载回退，不依赖继承的父对话。``inline`` 选择平行的
    ``<status>-inline`` 标签；``sub-agent`` 仍是 ``auto`` 的别名。显式无效值
    回退为 inline，不逐轮警告。其他平台原样返回状态。"""
    if platform == "codex":
        mode = _resolve_codex_dispatch_mode(config)
        return f"{status}-inline" if mode == "inline" else status
    return status


def build_breadcrumb(
    task_id: Optional[str],
    status: str,
    templates: dict[str, str],
    source: str | None = None,
    breadcrumb_key: str | None = None,
) -> str:
    """构建 <workflow-state>...</workflow-state> 区块。

    - 已知状态（workflow.md 有标签）：使用详细模板正文。
    - 未知状态（标签或文件缺失）：提示查阅 workflow.md 确认当前步骤。
    - `no_task` 伪状态（task_id 为 None）：标题省略任务信息。"""
    lookup_key = breadcrumb_key or status
    body = templates.get(lookup_key)
    if body is None and lookup_key != status:
        body = templates.get(status)
    if body is None:
        body = "请查阅 workflow.md 确认当前步骤。"
    header = f"状态：{status}" if task_id is None else f"任务：{task_id}（{status}）"
    return f"<workflow-state>\n{header}\n{body}\n</workflow-state>"


# ---------------------------------------------------------------------------
# 入口
# ---------------------------------------------------------------------------

def _load_hook_input() -> dict:
    """读取 hook JSON，不依赖宿主一定关闭标准输入。

    Kiro IDE 的 `runCommand` 等运行器可能保持标准输入打开，却不发送载荷，
    直接 `json.load(sys.stdin)` 会一直阻塞。正常运行器会写入完整 JSON 并关闭
    输入，因此短时守护线程读取保留正常路径，并对没有管道输入的宿主回退为 `{}`。"""
    result_queue: "queue.Queue[str | Exception]" = queue.Queue(maxsize=1)

    def _read() -> None:
        try:
            result_queue.put(sys.stdin.read())
        except Exception as exc:
            result_queue.put(exc)

    reader = threading.Thread(target=_read, daemon=True)
    reader.start()
    try:
        raw = result_queue.get(timeout=0.2)
    except queue.Empty:
        return {}

    if isinstance(raw, Exception):
        return {}
    try:
        data = json.loads(raw) if raw.strip() else {}
    except (json.JSONDecodeError, ValueError):
        return {}
    return data if isinstance(data, dict) else {}


def main() -> int:
    if os.environ.get("TRELLIS_HOOKS") == "0" or os.environ.get("TRELLIS_DISABLE_HOOKS") == "1":
        return 0

    data = _load_hook_input()

    cwd_str = data.get("cwd") or os.getcwd()
    cwd = Path(cwd_str)

    root = find_trellis_root(cwd)
    if root is None:
        return 0  # 不是 Trellis 项目

    config = _read_trellis_config(root)
    if prompt_has_skip_keyword(data.get("prompt", ""), _resolve_skip_keyword(config)):
        return 0  # 用户选择本轮跳过状态提示

    templates = load_breadcrumbs(root)
    platform = _detect_platform(data)
    task = get_active_task(root, data)
    if task is None:
        # 没有活动任务时仍输出状态提示；用户描述实际工作时，
        # 引导 AI 使用 trellis-brainstorm 和 task.py create。
        no_task_key = resolve_breadcrumb_key("no_task", platform, config)
        breadcrumb = build_breadcrumb(
            None, "no_task", templates, breadcrumb_key=no_task_key
        )
    else:
        task_id, status, source = task
        status_key = resolve_breadcrumb_key(status, platform, config)
        source_for_breadcrumb = None if platform == "codex" else source
        breadcrumb = build_breadcrumb(
            task_id, status, templates, source_for_breadcrumb, breadcrumb_key=status_key
        )
    if platform == "codex":
        parts: list[str] = []
        if task is None:
            parts.append(CODEX_NO_TASK_BOOTSTRAP_NOTICE)
        parts.append(_codex_mode_banner(config))
        parts.append(breadcrumb)
        breadcrumb = "\n\n".join(parts)

    # Kiro（CLI userPromptSubmit / IDE promptSubmit）将 hook 标准输出
    # 直接加入对话上下文，不使用 JSON 外层结构，因此输出纯文本
    # 状态提示。分支独立，其他平台继续使用下方
    # 原有 hookSpecificOutput JSON 输出路径。
    if platform == "kiro":
        print(breadcrumb)
        return 0

    # Gemini CLI 0.40.x 拒绝 "UserPromptSubmit"，其逐轮事件名是
    # "BeforeAgent"。其他平台（Claude/Cursor/Qoder/CodeBuddy/
    # Droid/Codex/Copilot）接受原有 Claude 风格名称。
    hook_event_name = (
        "BeforeAgent" if platform == "gemini" else "UserPromptSubmit"
    )

    output = {
        "hookSpecificOutput": {
            "hookEventName": hook_event_name,
            "additionalContext": breadcrumb,
        }
    }
    print(json.dumps(output))
    return 0


if __name__ == "__main__":
    sys.exit(main())
