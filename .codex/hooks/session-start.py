#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Codex 会话启动 hook，将 Trellis 上下文注入 Codex 会话。

输出遵循 Codex hook 协议：
  stdout JSON → { hookSpecificOutput: { hookEventName: "SessionStart", additionalContext: "..." } }"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import warnings
from io import StringIO
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


def _normalize_windows_shell_path(path_str: str) -> str:
    """将 Unix 风格的 Shell 路径规范化为实际 Windows 路径。

    Windows 上的 Git Bash / MSYS2 / Cygwin 可能报告 `/d/Users/...` 或
    `/cygdrive/d/Users/...`。在 D: 盘上，`Path.resolve()` 会将这些路径误解为
    `D:/d/Users...` 等形式，破坏仓库根目录检测。

    本函数采用保守策略，只重写能够明确表示盘符挂载的模式。"""
    if not isinstance(path_str, str) or not path_str:
        return path_str

    # 只适用于 Windows；其他平台原样返回。
    if not sys.platform.startswith("win"):
        return path_str

    p = path_str.strip()

    # 已经是 Windows 盘符路径（C:\... 或 C:/...）
    if re.match(r"^[A-Za-z]:[\/]", p):
        return p

    # MSYS/Git-Bash 风格：/c/Users/... 或 /d/Work/...
    m = re.match(r"^/([A-Za-z])/(.*)", p)
    if m:
        drive, rest = m.group(1).upper(), m.group(2)
        rest = rest.replace('/', '\\')
        return f"{drive}:\\{rest}"

    # Cygwin 风格：/cygdrive/c/Users/...
    m = re.match(r"^/cygdrive/([A-Za-z])/(.*)", p)
    if m:
        drive, rest = m.group(1).upper(), m.group(2)
        rest = rest.replace('/', '\\')
        return f"{drive}:\\{rest}"

    # WSL 挂载盘符（有时进入环境变量）：/mnt/c/Users/...
    m = re.match(r"^/mnt/([A-Za-z])/(.*)", p)
    if m:
        drive, rest = m.group(1).upper(), m.group(2)
        rest = rest.replace('/', '\\')
        return f"{drive}:\\{rest}"

    return path_str


warnings.filterwarnings("ignore")

FIRST_REPLY_NOTICE = """<first-reply-notice>
在本次会话第一条可见助手回复中，简短说明 Trellis SessionStart 上下文已加载。
按以下顺序选择说明所用语言：
1. 使用触发本次回复的用户请求语言。
2. 若该请求没有明确自然语言，使用已明确约定的项目交流语言。
3. 若两者均未确定语言，原样输出不区分语言的回退文本：`Trellis SessionStart ✓`。
说明之后直接继续处理用户请求，不改变回复其余部分的语言。
此提示仅执行一次，第一条可见助手回复之后不要重复。
</first-reply-notice>"""


def should_skip_injection() -> bool:
    if os.environ.get("TRELLIS_HOOKS") == "0":
        return True
    if os.environ.get("TRELLIS_DISABLE_HOOKS") == "1":
        return True
    return os.environ.get("CODEX_NON_INTERACTIVE") == "1"


def configure_project_encoding(project_dir: Path) -> None:
    """输出 JSON 前复用 Trellis 共享的 Windows 标准流编码助手。"""
    scripts_dir = project_dir / ".trellis" / "scripts"
    if str(scripts_dir) not in sys.path:
        sys.path.insert(0, str(scripts_dir))

    try:
        from common import configure_encoding  # type: ignore[import-not-found]

        configure_encoding()
    except Exception:
        pass  # 编码助手可选，失败时仍可使用宿主默认值。


def _has_curated_jsonl_entry(jsonl_path: Path) -> bool:
    """仅当 jsonl 至少存在一行带 ``file`` 字段的记录时返回 True。

    新建 jsonl 为空，旧任务可能仍包含没有 ``file`` 的 ``{"_example": ...}``
    占位行，两者都不算就绪。至少一个整理后的条目才满足就绪条件；此契约
    与 ``inject-subagent-context.py`` 一致。"""
    try:
        for line in jsonl_path.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if not line:
                continue
            try:
                row = json.loads(line)
            except json.JSONDecodeError:
                continue
            if isinstance(row, dict) and row.get("file"):
                return True
    except (OSError, UnicodeDecodeError):
        return False
    return False


def read_file(path: Path, fallback: str = "") -> str:
    try:
        return path.read_text(encoding="utf-8")
    except (FileNotFoundError, PermissionError):
        return fallback


def _resolve_context_key(project_dir: Path, hook_input: dict) -> str | None:
    scripts_dir = project_dir / ".trellis" / "scripts"
    if str(scripts_dir) not in sys.path:
        sys.path.insert(0, str(scripts_dir))
    try:
        from common.active_task import resolve_context_key  # type: ignore[import-not-found]
    except Exception:
        return None
    return resolve_context_key(hook_input, platform="codex")


def _resolve_active_task(trellis_dir: Path, hook_input: dict):
    scripts_dir = trellis_dir / "scripts"
    if str(scripts_dir) not in sys.path:
        sys.path.insert(0, str(scripts_dir))
    from common.active_task import resolve_active_task  # type: ignore[import-not-found]

    return resolve_active_task(trellis_dir.parent, hook_input, platform="codex")


def run_script(script_path: Path, context_key: str | None = None) -> str:
    try:
        env = os.environ.copy()
        env["PYTHONIOENCODING"] = "utf-8"
        if context_key:
            env["TRELLIS_CONTEXT_ID"] = context_key
        cmd = [sys.executable, "-W", "ignore", str(script_path)]
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=5,
            cwd=str(script_path.parent.parent.parent),
            env=env,
        )
        return result.stdout if result.returncode == 0 else "暂无可用上下文"
    except (subprocess.TimeoutExpired, FileNotFoundError, PermissionError):
        return "暂无可用上下文"


def _normalize_task_ref(task_ref: str) -> str:
    normalized = task_ref.strip()
    if not normalized:
        return ""

    path_obj = Path(normalized)
    if path_obj.is_absolute():
        return str(path_obj)

    normalized = normalized.replace("\\", "/")
    while normalized.startswith("./"):
        normalized = normalized[2:]

    if normalized.startswith("tasks/"):
        return f".trellis/{normalized}"

    return normalized


def _resolve_task_dir(trellis_dir: Path, task_ref: str) -> Path:
    normalized = _normalize_task_ref(task_ref)
    path_obj = Path(normalized)
    if path_obj.is_absolute():
        return path_obj
    if normalized.startswith(".trellis/"):
        return trellis_dir.parent / path_obj
    return trellis_dir / "tasks" / path_obj


def _get_task_status(trellis_dir: Path, hook_input: dict) -> str:
    active = _resolve_active_task(trellis_dir, hook_input)
    if not active.task_path:
        return (
            "状态：无活动任务\n"
            "下一步：先判断本轮请求类型，并在创建 Trellis 任务前"
            "征得用户同意。"
        )

    task_ref = active.task_path
    task_dir = _resolve_task_dir(trellis_dir, task_ref)
    if active.stale or not task_dir.is_dir():
        return (
            f"状态：指针已失效\n任务： {task_ref}\n"
            "下一步：未找到任务目录。请运行： python3 ./.trellis/scripts/task.py finish"
        )

    task_json_path = task_dir / "task.json"
    task_data: dict = {}
    if task_json_path.is_file():
        try:
            task_data = json.loads(task_json_path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, PermissionError):
            pass  # 任务元数据可选，失败时回退到通用状态。

    task_title = task_data.get("title", task_ref)
    task_status = task_data.get("status", "unknown")

    if task_status == "completed":
        return (
            f"状态：已完成\n任务： {task_title}\n"
            f"下一步：使用以下命令归档： `python3 ./.trellis/scripts/task.py archive {task_dir.name}` "
            "或开始新任务。"
        )

    has_prd = (task_dir / "prd.md").is_file()
    has_design = (task_dir / "design.md").is_file()
    has_implement = (task_dir / "implement.md").is_file()
    present = [
        name
        for name in ("prd.md", "design.md", "implement.md", "implement.jsonl", "check.jsonl")
        if (task_dir / name).is_file()
    ]
    present_line = ", ".join(present) if present else "无"

    if not has_prd:
        return (
            f"状态：规划中\n任务：{task_title}\n已有文件： {present_line}\n"
            "下一步：加载 trellis-brainstorm 并编写 prd.md，保持规划阶段。"
        )

    if task_status == "planning":
        if has_design and has_implement:
            next_action = "执行 `task.py start` 前与用户评审规划文件。"
        else:
            next_action = (
                "轻量任务可只凭 PRD 请求开始评审；"
                "复杂任务必须在 `task.py start` 前补齐 design.md 和 implement.md。"
            )
        return (
            f"状态：规划中\n任务：{task_title}\n已有文件： {present_line}\n"
            f"下一步：{next_action}"
        )

    return (
        f"状态：{task_status.upper()}\n任务：{task_title}\n已有文件： {present_line}\n"
        "下一步：遵循对应的逐轮 workflow-state。上下文读取顺序为 jsonl 条目、"
        "prd.md、design.md（若存在）、implement.md（若存在）。"
    )


def _run_git(repo_root: Path, args: list[str]) -> str:
    try:
        result = subprocess.run(
            ["git", *args],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=3,
            cwd=str(repo_root),
        )
    except (subprocess.TimeoutExpired, FileNotFoundError, PermissionError):
        return ""
    if result.returncode != 0:
        return ""
    return result.stdout.strip()


def _format_git_state(repo_root: Path) -> str:
    branch = _run_git(repo_root, ["branch", "--show-current"]) or "（分离 HEAD）"
    dirty_lines = [
        line for line in _run_git(repo_root, ["status", "--porcelain"]).splitlines()
        if line.strip()
    ]
    dirty_text = "干净" if not dirty_lines else f"{len(dirty_lines)} 个路径有未提交变更"
    return f"Git：分支 {branch}；{dirty_text}。"


def _repo_relative(repo_root: Path, path: Path) -> str:
    try:
        return path.relative_to(repo_root).as_posix()
    except ValueError:
        return str(path)


def _collect_spec_index_paths(trellis_dir: Path) -> list[str]:
    paths: list[str] = []
    guides_index = trellis_dir / "spec" / "guides" / "index.md"
    if guides_index.is_file():
        paths.append(".trellis/spec/guides/index.md")

    spec_dir = trellis_dir / "spec"
    if not spec_dir.is_dir():
        return paths

    for sub in sorted(spec_dir.iterdir()):
        if not sub.is_dir() or sub.name.startswith(".") or sub.name == "guides":
            continue
        index_file = sub / "index.md"
        if index_file.is_file():
            paths.append(f".trellis/spec/{sub.name}/index.md")
            continue
        for nested in sorted(sub.iterdir()):
            if not nested.is_dir():
                continue
            nested_index = nested / "index.md"
            if nested_index.is_file():
                paths.append(f".trellis/spec/{sub.name}/{nested.name}/index.md")

    return paths


def _build_compact_current_state(
    trellis_dir: Path,
    hook_input: dict,
    spec_index_paths: list[str],
) -> str:
    repo_root = trellis_dir.parent
    lines: list[str] = []

    try:
        from common.paths import get_active_journal_file, get_developer, get_tasks_dir, count_lines  # type: ignore[import-not-found]
        from common.tasks import iter_active_tasks  # type: ignore[import-not-found]
    except Exception:
        get_active_journal_file = None  # type: ignore[assignment]
        get_developer = None  # type: ignore[assignment]
        get_tasks_dir = None  # type: ignore[assignment]
        count_lines = None  # type: ignore[assignment]
        iter_active_tasks = None  # type: ignore[assignment]

    developer = get_developer(repo_root) if get_developer else None
    lines.append(f"开发者：{developer or '（尚未初始化）'}")
    lines.append(_format_git_state(repo_root))

    active = _resolve_active_task(trellis_dir, hook_input)
    if active.task_path:
        task_dir = _resolve_task_dir(trellis_dir, active.task_path)
        status = "unknown"
        task_json = task_dir / "task.json"
        if task_json.is_file():
            try:
                data = json.loads(task_json.read_text(encoding="utf-8"))
                if isinstance(data, dict):
                    status = str(data.get("status") or "unknown")
            except (json.JSONDecodeError, OSError):
                pass  # 任务元数据可选，失败时回退到通用状态。
        lines.append(f"当前任务：{_repo_relative(repo_root, task_dir)}；状态={status}。")
    else:
        lines.append("当前任务：无。")

    if get_tasks_dir and iter_active_tasks:
        try:
            task_count = sum(1 for _ in iter_active_tasks(get_tasks_dir(repo_root)))
            lines.append(
                f"活动任务：共 {task_count} 个。仅在需要时运行 `python3 ./.trellis/scripts/task.py list --mine`。"
            )
        except Exception:
            pass  # 任务摘要可选，失败时仍保留精简状态。

    if get_active_journal_file and count_lines:
        journal = get_active_journal_file(repo_root)
        if journal:
            lines.append(
                f"日志：{_repo_relative(repo_root, journal)}，{count_lines(journal)} / 2000 行。"
            )

    if spec_index_paths:
        lines.append(f"可用规范索引：{len(spec_index_paths)} 个。")

    return "\n".join(lines)


def _extract_range(content: str, start_header: str, end_header: str) -> str:
    """提取从 `## start_header` 开始、到 `## end_header` 之前的行，兼容中英文阶段标题。"""
    lines = content.splitlines()
    start: "int | None" = None
    end: int = len(lines)
    # 两处阶段提取器均兼容旧英文模板与项目的中文标题。
    aliases = {
        "Phase Index": ("Phase Index", "阶段索引"),
        "阶段索引": ("Phase Index", "阶段索引"),
        "Phase 1: Plan": ("Phase 1: Plan", "阶段 1：规划"),
        "阶段 1：规划": ("Phase 1: Plan", "阶段 1：规划"),
    }
    start_matches = {f"## {name}" for name in aliases.get(start_header, (start_header,))}
    end_matches = {f"## {name}" for name in aliases.get(end_header, (end_header,))}
    for i, line in enumerate(lines):
        stripped = line.strip()
        if start is None and stripped in start_matches:
            start = i
            continue
        if start is not None and stripped in end_matches:
            end = i
            break
    if start is None:
        return ""
    return "\n".join(lines[start:end]).rstrip()


_BREADCRUMB_TAG_RE = re.compile(
    r"\[workflow-state:([A-Za-z0-9_-]+)\]\s*\n.*?\n\s*\[/workflow-state:\1\]",
    re.DOTALL,
)


def _strip_breadcrumb_tag_blocks(content: str) -> str:
    stripped = _BREADCRUMB_TAG_RE.sub("", content)
    stripped = re.sub(r"<!--.*?-->", "", stripped, flags=re.DOTALL)
    stripped = re.sub(r"^\[(?!/?workflow-state:)/?[^\]\n]+\]\s*\n?", "", stripped, flags=re.MULTILINE)
    return re.sub(r"\n{3,}", "\n\n", stripped).strip()


def _build_workflow_toc(workflow_path: Path) -> str:
    """为 SessionStart 只注入简要阶段索引。"""
    content = read_file(workflow_path)
    if not content:
        return "未找到 workflow.md"

    out_lines = [
        "# 开发工作流：会话摘要",
        "完整指南：.trellis/workflow.md。步骤详情： `python3 ./.trellis/scripts/get_context.py --mode phase --step <X.Y>`.",
        "",
    ]

    phases = _extract_range(content, "Phase Index", "Phase 1: Plan")
    if phases:
        out_lines.append(_strip_breadcrumb_tag_blocks(phases).rstrip())

    return "\n".join(out_lines).rstrip()


def main() -> None:
    if should_skip_injection():
        sys.exit(0)

    # 从标准输入读取 hook 载荷
    try:
        hook_input = json.loads(sys.stdin.read())
        if not isinstance(hook_input, dict):
            hook_input = {}
        project_dir = Path(_normalize_windows_shell_path(hook_input.get("cwd", "."))).resolve()
    except (json.JSONDecodeError, KeyError):
        hook_input = {}
        project_dir = Path(".").resolve()

    configure_project_encoding(project_dir)

    trellis_dir = project_dir / ".trellis"
    spec_index_paths = _collect_spec_index_paths(trellis_dir)

    output = StringIO()

    output.write("""<session-context>
Trellis 精简 SessionStart 上下文，用于确定会话方向；详细内容按需加载。
</session-context>

""")
    output.write(FIRST_REPLY_NOTICE)
    output.write("\n\n")

    output.write("<current-state>\n")
    output.write(_build_compact_current_state(trellis_dir, hook_input, spec_index_paths))
    output.write("\n</current-state>\n\n")

    output.write("<trellis-workflow>\n")
    output.write(_build_workflow_toc(trellis_dir / "workflow.md"))
    output.write("\n</trellis-workflow>\n\n")

    output.write("<guidelines>\n")
    output.write(
        "实施/检查的任务上下文读取顺序：jsonl 条目 -> `prd.md` -> "
        "`design.md`（若存在）-> `implement.md`（若存在）。轻量任务可跳过"
        "缺失的可选文件。\n\n"
    )

    if spec_index_paths:
        output.write("## 可用索引（按需阅读）\n")
        for p in spec_index_paths:
            output.write(f"- {p}\n")
        output.write("\n")

    output.write(
        "进一步发现： "
        "`python3 ./.trellis/scripts/get_context.py --mode packages`\n"
    )
    output.write("</guidelines>\n\n")

    task_status = _get_task_status(trellis_dir, hook_input)
    output.write(f"<task-status>\n{task_status}\n</task-status>\n\n")

    output.write("""<ready>
上下文已加载。遵循 <task-status>，只在需要时加载工作流、规范和任务详情。
</ready>""")

    context = output.getvalue()
    result = {
        "suppressOutput": True,
        "systemMessage": f"Trellis 上下文已注入（{len(context)} 个字符）",
        "hookSpecificOutput": {
            "hookEventName": "SessionStart",
            "additionalContext": context,
        },
    }

    print(json.dumps(result, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
