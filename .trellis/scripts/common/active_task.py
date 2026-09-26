#!/usr/bin/env python3
"""在会话范围内解析活动任务。

面向用户的概念是单一的“活动任务”。Trellis 在 `.trellis/.runtime/sessions/` 中
按 AI 会话/窗口保存指针；没有稳定的会话键，就没有活动任务。
"""

from __future__ import annotations

import hashlib
import os
import re
import sys
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from .io import read_json as _io_read_json, write_json as _io_write_json

DIR_WORKFLOW = ".trellis"
DIR_TASKS = "tasks"
DIR_RUNTIME = ".runtime"
DIR_SESSIONS = "sessions"
DIR_SHELL_TICKETS = "shell-tickets"
# 0.6.13 之前仅支持 Cursor 桥接时使用的名称。继续读取但不再写入，避免升级时
# 正在执行命令的会话静默降级。票据仅存活 30 秒，旧目录会自然失效；无需迁移，
# 只需对通常不存在的目录执行 glob。若直接忽略，现有可用平台就会丢失那条命令。
DIR_LEGACY_CURSOR_SHELL_TICKETS = "cursor-shell"
SHELL_TICKET_TTL_SECONDS = 30
TASK_SESSION_COMMANDS = {"start", "current", "finish"}

_SESSION_KEYS = ("session_id", "sessionId", "sessionID")
_CONVERSATION_KEYS = ("conversation_id", "conversationId", "conversationID")
_TRANSCRIPT_KEYS = ("transcript_path", "transcriptPath", "transcript")
_NESTED_KEYS = ("input", "properties", "event", "hook_input", "hookInput")
_KNOWN_PLATFORMS = {
    "claude",
    "codex",
    "cursor",
    "opencode",
    "gemini",
    "droid",
    "qoder",
    "codebuddy",
    "kiro",
    "copilot",
    "pi",
    "trae",
    "grok",
    "kimi",
    "zcode",
    "snow",
    "dsh",
}

# 以下每个名称都记录了验证方式，禁止类比相邻条目添加。2026-08-05 对全部
# 21 个平台的审计发现，已声明的 21 个名称中有 12 个从未存在：它们仅按没有厂商
# 认可的 `<PLATFORM>_SESSION_ID` 形式猜测，命名一致性是唯一“证据”。
# 未验证名称的平台不应进入任何表，应通过 TRELLIS_CONTEXT_ID 或钩子/插件桥接解析。
_ENV_SESSION_KEYS: tuple[tuple[str, tuple[str, ...]], ...] = (
    # 已证实：@SajoLuo 于 2026-08-13 在 DSH 0.1.0-rc.6 实际运行中报告，
    # DSH 将 DSH_SESSION_ID 和 DSH_SHELL=1 导出到托管 shell。
    # 必须保持第一项：DSH 会话可能继承外层宿主身份，例如从 Codex 启动时仍携带
    # CODEX_THREAD_ID。下方未限定平台的查找按表顺序执行，排在它之前的条目可能
    # 抢占身份，为 DSH 工作写入错误的 `codex_<thread>` 指针。
    # 这里仅 DSH_SESSION_ID 不会由其他厂商设置，因此置首不会误认非 DSH 会话。
    ("dsh", ("DSH_SESSION_ID",)),
    # 已证实但未文档化：2026-08-05 在真实 Claude Code 2.1.221 的 bash 子进程中验证；
    # code.claude.com/docs/en/env-vars 未列出。CLAUDE_SESSION_ID 已移除，
    # 同一真实环境中确认不存在该变量。
    ("claude", ("CLAUDE_CODE_SESSION_ID",)),
    # 已证实但未文档化：2026-08-05 验证由 codex-cli 0.146.0 注入 shell 子进程，
    # 父环境中不存在；见 openai/codex#19937。CODEX_SESSION_ID 已移除，
    # 真实 `codex exec` 环境中不存在该变量。
    ("codex", ("CODEX_THREAD_ID",)),
    # 已证实但仅在钩子范围内：2026-08-05 验证由 Gemini 的 hookRunner.ts 设置。
    # shell 工具在 shellExecutionService.ts 构建子环境，仅添加
    # GEMINI_CLI/TERM/PAGER/GIT_PAGER，因此该变量不会进入 bash 子进程，
    # 只能在钩子进程中解析。
    ("gemini", ("GEMINI_SESSION_ID",)),
    # 已证实但仅在钩子范围内：2026-08-05 核验 docs.qoder.com/zh/extensions/hooks，
    # 文档说明由 Qoder IDE 插件在钩子执行时注入。Qoder CLI 钩子文档和 Lingma
    # 中均不存在此项。
    ("qoder", ("QODER_SESSION_ID",)),
    # 未验证（2026-08-05）：kiro.dev/docs/hooks/ 未列出，但 Dynatrace dtctl、
    # oh-my-agent、gastown 均以它识别代理，其中一处说明交互及 --no-interactive
    # 模式都会设置。保留是因为缺少证据不等于不存在。要确认，应在装有 Kiro 的机器上，
    # 通过 Kiro shell 工具调用执行 `env | grep KIRO`。
    ("kiro", ("KIRO_SESSION_ID",)),
    # 未验证（2026-08-05）：docs.github.com/en/copilot/reference/hooks-reference
    # 及 CLI 编程参考中均未列出。要确认，应执行 `copilot help environment`
    # （文档指定的权威列表）；此处未安装 CLI，且 copilot-cli 不提供源码，无法执行。
    ("copilot", ("COPILOT_SESSION_ID", "COPILOT_SESSIONID")),
    # 有推断依据但未验证（2026-08-05）：ZCode 闭源且此处无法安装。它在其他位置
    # 沿用 Claude 命名，文档中包含 CLAUDE_PLUGIN_ROOT / CLAUDE_PLUGIN_DATA 兼容别名。
    # 此前声明的 CLAUDE_SESSION_ID 在 Claude Code 中也不存在，因此 ZCode 实际复用的
    # 应是 CLAUDE_CODE_SESSION_ID。先尝试它，保留历史名称作为回退；两者均不存在时
    # 行为不变。查找限定平台范围（_iter_env_keys 按平台名过滤），仅检测到 "zcode"
    # 时触发，不会与上方 claude 条目冲突。
    ("zcode", ("CLAUDE_CODE_SESSION_ID", "CLAUDE_SESSION_ID")),
    # 按厂商设计已证实（2026-08-05）：Snow 的 sessionIdentityEnv.ts 将
    # SNOW_SESSION_ID 导出到钩子、终端和子代理进程，源码头部也提到了 Trellis。
    # TRELLIS_CONTEXT_ID 仍为优先覆盖项，Snow 也设置它。
    ("snow", ("SNOW_SESSION_ID",)),
)
_ENV_CONVERSATION_KEYS: tuple[tuple[str, tuple[str, ...]], ...] = (
    # cursor-agent（CLI）中已证实但未文档化：2026-08-05 验证该值匹配
    # ~/.cursor/chats/<ws>/<id>。Cursor IDE 尚未验证，2026-05 的论坛请求无工作人员回应。
    # 虚构的 CURSOR_SESSION_ID 已从会话表移除，真实 cursor-agent shell 中为空。
    # Cursor 还可走下方的 shell 票据路径（_lookup_shell_ticket_context_key），
    # 该路径不限定 Cursor。
    ("cursor", ("CURSOR_CONVERSATION_ID", "CURSOR_CONVERSATIONID")),
)
_ENV_TRANSCRIPT_KEYS: tuple[tuple[str, tuple[str, ...]], ...] = (
    # 已证实但仅在钩子范围内（2026-08-05）：Cursor 钩子脚本文档有说明，
    # 代理自身的 shell 环境中为空。
    ("cursor", ("CURSOR_TRANSCRIPT_PATH",)),
    # 未验证，尚未研究。2026-08-05 审计仅覆盖会话表，不能据此判断这些名称真实
    # 或虚构。CLAUDE_/CODEX_TRANSCRIPT_PATH 被移除，是因为这两项已核验，
    # 文档和真实环境均不存在。逐项确认时，应分别在钩子和 shell 工具调用中执行
    # `env | grep _TRANSCRIPT_PATH`。
    ("gemini", ("GEMINI_TRANSCRIPT_PATH",)),
    ("droid", ("FACTORY_TRANSCRIPT_PATH", "DROID_TRANSCRIPT_PATH")),
    ("qoder", ("QODER_TRANSCRIPT_PATH",)),
    ("codebuddy", ("CODEBUDDY_TRANSCRIPT_PATH",)),
)
_ENV_PLATFORM_ALIASES = {
    "claude-code": "claude",
    "factory": "droid",
    "factory-ai": "droid",
    "github-copilot": "copilot",
}
# ZCode 有意复用 Claude 的会话环境变量名。钩子知道宿主是 ZCode，后续 shell
# 命令却只能看到共享变量名，并通过 claude 条目解析。两条路径规范化为同一运行时文件名。
_CONTEXT_KEY_PLATFORM_ALIASES = {
    "zcode": "claude",
    # Factory Droid 配置目录为 `.factory/`，按安装目录命名平台的钩子会报告
    # "factory"，同级钩子则报告 "droid"。两者使用同一运行时文件名。
    "factory": "droid",
}


@dataclass(frozen=True)
class ActiveTask:
    """解析后的活动任务状态。"""

    task_path: str | None
    source_type: str
    context_key: str | None = None
    stale: bool = False

    @property
    def source(self) -> str:
        """人类可读的来源标签。"""
        if self.source_type == "session" and self.context_key:
            return f"session:{self.context_key}"
        if self.source_type == "session-fallback" and self.context_key:
            return f"session-fallback:{self.context_key}"
        return self.source_type


def normalize_task_ref(task_ref: str) -> str:
    """规范化任务引用，以便稳定地存储和比较。"""
    normalized = task_ref.strip()
    if not normalized:
        return ""

    path_obj = Path(normalized)
    if path_obj.is_absolute():
        return str(path_obj)

    normalized = normalized.replace("\\", "/")
    while normalized.startswith("./"):
        normalized = normalized[2:]

    if normalized.startswith(f"{DIR_TASKS}/"):
        return f"{DIR_WORKFLOW}/{normalized}"

    return normalized


def resolve_task_ref(task_ref: str, repo_root: Path) -> Path | None:
    """将任务引用解析为仓库内任务目录的绝对路径。

    与 `paths.resolve_task_ref` 使用相同的范围校验。此处复制实现而非导入，是因为本模块
    可被独立加载：钩子直接将它加入 `sys.path`，因此有意不依赖相对导入。
    """
    normalized = normalize_task_ref(task_ref)
    if not normalized:
        return None

    try:
        root = repo_root.resolve()
    except OSError:
        return None

    path_obj = Path(normalized)
    if path_obj.is_absolute():
        candidate = path_obj
    elif normalized.startswith(f"{DIR_WORKFLOW}/"):
        candidate = root / path_obj
    else:
        candidate = root / DIR_WORKFLOW / DIR_TASKS / path_obj

    # 两端均需解析，因为 repo_root 本身可能经过符号链接（如 macOS 的 /tmp）。
    # resolve() 会折叠 `..`，避免仅按字面判断的 relative_to() 放行。
    try:
        resolved = candidate.resolve()
        workflow_real = (root / DIR_WORKFLOW).resolve()
    except OSError:
        return None

    try:
        resolved.relative_to(root)
        return resolved
    except ValueError:
        pass

    # `.trellis` 本身可能指向仓库外的存储（#567）。工作流目录的真实位置成为
    # 第二个合法范围基准；逃出两个基准的引用仍被拒绝。映射回仓库内的字面路径，
    # 使调用者保存仓库相对引用。
    try:
        rel = resolved.relative_to(workflow_real)
    except ValueError:
        return None

    return root / DIR_WORKFLOW / rel


def _runtime_sessions_dir(repo_root: Path) -> Path:
    return repo_root / DIR_WORKFLOW / DIR_RUNTIME / DIR_SESSIONS


def _sanitize_key(raw: str) -> str:
    safe = re.sub(r"[^A-Za-z0-9._-]+", "_", raw.strip())
    safe = safe.strip("._-")
    return safe[:160] if safe else ""


def _hash_value(raw: str) -> str:
    return hashlib.sha256(raw.encode("utf-8")).hexdigest()[:24]


def _as_dict(value: Any) -> dict[str, Any] | None:
    return value if isinstance(value, dict) else None


def _string_value(value: Any) -> str | None:
    if isinstance(value, str):
        stripped = value.strip()
        return stripped or None
    return None


def _lookup_string(data: dict[str, Any], keys: tuple[str, ...]) -> str | None:
    for key in keys:
        value = _string_value(data.get(key))
        if value:
            return value

    for nested_key in _NESTED_KEYS:
        nested = _as_dict(data.get(nested_key))
        if not nested:
            continue
        value = _lookup_string(nested, keys)
        if value:
            return value

    return None


def _detect_platform(platform_input: dict[str, Any] | None, platform: str | None) -> str:
    if platform:
        return _sanitize_key(platform) or "session"
    if platform_input:
        for key in ("_trellis_platform", "trellis_platform", "platform", "source"):
            value = _string_value(platform_input.get(key))
            if value:
                return _sanitize_key(value) or "session"
        if _string_value(platform_input.get("cursor_version")):
            return "cursor"
    return "session"


def _context_key(platform_name: str, kind: str, value: str) -> str:
    platform_name = _CONTEXT_KEY_PLATFORM_ALIASES.get(platform_name, platform_name)
    if kind == "transcript":
        return f"{platform_name}_transcript_{_hash_value(value)}"
    safe_value = _sanitize_key(value)
    if safe_value:
        return f"{platform_name}_{safe_value}"
    return f"{platform_name}_{_hash_value(value)}"


def _iter_env_keys(
    env_keys: tuple[tuple[str, tuple[str, ...]], ...],
    platform_name: str | None,
) -> tuple[tuple[str, tuple[str, ...]], ...]:
    """将环境变量键表收窄到指定平台，或返回完整表。

    平台没有条目时返回空元组，调用者的 `for` 循环不会执行。这是正常情况：
    未验证环境变量名称的平台，有意不加入这些表。
    """
    if not platform_name:
        return env_keys
    matched = tuple((name, keys) for name, keys in env_keys if name == platform_name)
    return matched


def _env_platform_name(platform_name: str | None) -> str | None:
    if not platform_name or platform_name == "session":
        return None
    return _ENV_PLATFORM_ALIASES.get(platform_name, platform_name)


def _lookup_env_context_key(platform_name: str | None) -> str | None:
    """从平台提供的环境变量解析上下文键。

    钩子会将 `TRELLIS_CONTEXT_ID` 传给所启动的子进程，但 AI 执行的 shell 命令只有
    在宿主平台将会话身份导出到命令环境时才能看到它。这些名称是尽力而为的适配；
    均不存在时，就没有会话范围内的活动任务。
    """
    env_platform_name = _env_platform_name(platform_name)

    for name, keys in _iter_env_keys(_ENV_SESSION_KEYS, env_platform_name):
        for key in keys:
            value = _string_value(os.environ.get(key))
            if value:
                return _context_key(name, "session", value)

    for name, keys in _iter_env_keys(_ENV_CONVERSATION_KEYS, env_platform_name):
        for key in keys:
            value = _string_value(os.environ.get(key))
            if value:
                return _context_key(name, "conversation", value)

    for name, keys in _iter_env_keys(_ENV_TRANSCRIPT_KEYS, env_platform_name):
        for key in keys:
            value = _string_value(os.environ.get(key))
            if value:
                return _context_key(name, "transcript", value)

    return None


def _find_repo_root_from_cwd() -> Path | None:
    current = Path.cwd().resolve()
    while True:
        if (current / DIR_WORKFLOW).is_dir():
            return current
        if current == current.parent:
            return None
        current = current.parent


def _shell_ticket_dirs(repo_root: Path) -> tuple[Path, ...]:
    runtime_dir = repo_root / DIR_WORKFLOW / DIR_RUNTIME
    return (
        runtime_dir / DIR_SHELL_TICKETS,
        runtime_dir / DIR_LEGACY_CURSOR_SHELL_TICKETS,
    )


def _remove_file(path: Path) -> bool:
    try:
        path.unlink()
        return True
    except OSError:
        return False


def _task_refs_match(left: str | None, right: str | None, repo_root: Path) -> bool:
    if not left or not right:
        return False
    left_path = resolve_task_ref(left, repo_root)
    right_path = resolve_task_ref(right, repo_root)
    if left_path is not None and right_path is not None:
        return left_path == right_path
    return normalize_task_ref(left) == normalize_task_ref(right)


def _pending_ticket_matches_args(ticket: dict[str, Any], repo_root: Path) -> bool:
    if Path(sys.argv[0]).name != "task.py":
        return False
    args = tuple(sys.argv[1:])
    if not args:
        return False

    command_name = args[0]
    if command_name not in TASK_SESSION_COMMANDS:
        return False

    subcommands = ticket.get("subcommands")
    if not isinstance(subcommands, list):
        return False

    for subcommand in subcommands:
        if not isinstance(subcommand, dict):
            continue
        if _string_value(subcommand.get("name")) != command_name:
            continue
        if command_name != "start":
            return True
        task_ref = args[1] if len(args) > 1 else None
        if _task_refs_match(_string_value(subcommand.get("task_ref")), task_ref, repo_root):
            return True

    return False


def _ticket_is_fresh(ticket: dict[str, Any], ticket_path: Path, now: float) -> bool:
    expires_at = ticket.get("expires_at_epoch")
    if isinstance(expires_at, (int, float)) and expires_at < now:
        _remove_file(ticket_path)
        return False

    created_at = ticket.get("created_at_epoch")
    if isinstance(created_at, (int, float)):
        if now - created_at <= SHELL_TICKET_TTL_SECONDS:
            return True
        _remove_file(ticket_path)
        return False
    return True


def _ticket_cwd_matches_repo(ticket: dict[str, Any], repo_root: Path) -> bool:
    cwd = _string_value(ticket.get("cwd"))
    if not cwd:
        return True
    try:
        Path(cwd).resolve().relative_to(repo_root)
    except ValueError:
        return False
    return True


def _matching_ticket_context_key(
    ticket_path: Path,
    repo_root: Path,
    now: float,
) -> str | None:
    """按票据本身是否有效来接受，不按写入它的平台判断。

    票据中的 `platform` 仅用于调试；过去按它限制访问，导致 Cursor 之外的平台
    无法使用这条桥接路径。
    """
    ticket = _read_json(ticket_path)
    if ticket is None:
        return None
    if not _ticket_is_fresh(ticket, ticket_path, now):
        return None
    if not _ticket_cwd_matches_repo(ticket, repo_root):
        return None
    if not _pending_ticket_matches_args(ticket, repo_root):
        return None
    return _string_value(ticket.get("context_key"))


def _lookup_shell_ticket_context_key() -> str | None:
    """从短时有效的 shell 票据解析会话身份。

    此前研究的平台都不向 shell 子进程导出会话 ID，但支持钩子的平台都会把 ID 传给钩子。
    因此 shell 命令执行前的钩子写入票据，由这里读回。仅接受新鲜、属于当前仓库且匹配
    当前 `task.py` 子命令的票据，并且必须恰好只有一个新鲜上下文键匹配。
    这样两个并发窗口都会回退，而不会让其中一个继承另一个的指针。
    """
    repo_root = _find_repo_root_from_cwd()
    if repo_root is None:
        return None

    now = time.time()
    candidates: set[str] = set()
    for ticket_dir in _shell_ticket_dirs(repo_root):
        if not ticket_dir.is_dir():
            continue
        for ticket_path in ticket_dir.glob("*.json"):
            context_key = _matching_ticket_context_key(ticket_path, repo_root, now)
            if context_key:
                candidates.add(context_key)

    if len(candidates) == 1:
        return next(iter(candidates))
    return None


def resolve_context_key(
    platform_input: dict[str, Any] | None = None,
    platform: str | None = None,
    *,
    allow_environment_context: bool = True,
) -> str | None:
    """存在可用值时，解析稳定的会话/窗口上下文键。

    `TRELLIS_CONTEXT_ID` 是 CLI 脚本及子进程使用的显式上下文键覆盖项，不保存任务本身。
    """
    if allow_environment_context:
        override = _string_value(os.environ.get("TRELLIS_CONTEXT_ID"))
        if override:
            return _sanitize_key(override) or _hash_value(override)

    data = _as_dict(platform_input)
    platform_name = _detect_platform(data, platform) if data or platform else None

    if data:
        session_id = _lookup_string(data, _SESSION_KEYS)
        if session_id:
            return _context_key(platform_name or "session", "session", session_id)

        conversation_id = _lookup_string(data, _CONVERSATION_KEYS)
        if conversation_id:
            return _context_key(platform_name or "session", "conversation", conversation_id)

        transcript_path = _lookup_string(data, _TRANSCRIPT_KEYS)
        if transcript_path:
            return _context_key(platform_name or "session", "transcript", transcript_path)

    if allow_environment_context:
        env_context_key = _lookup_env_context_key(platform_name)
        if env_context_key:
            return env_context_key

    # 有意放在解析链末尾：平台真实导出到 shell 的身份优先于票据，查找不按平台名限制。
    if allow_environment_context:
        return _lookup_shell_ticket_context_key()
    return None


def _read_json(path: Path) -> dict[str, Any] | None:
    """宽容读取会话运行时文件，包括非对象内容。"""
    data = _io_read_json(path)
    return data if isinstance(data, dict) else None


def _write_json(path: Path, data: dict[str, Any]) -> bool:
    """原子写入会话运行时文件，并创建运行时目录。

    通过 io.write_json，使会话指针与 task.json 一样采用临时文件再重命名的方式（#429）。
    普通 write_text 会先截断目标，写入中途崩溃会使会话文件被读成无活动任务。
    """
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
    except OSError:
        return False
    return _io_write_json(path, data)


def _canonical_task_ref(task_path: str, repo_root: Path) -> str | None:
    normalized = normalize_task_ref(task_path)
    if not normalized:
        return None
    full_path = resolve_task_ref(normalized, repo_root)
    if full_path is None or not full_path.is_dir():
        return None
    try:
        return full_path.relative_to(repo_root.resolve()).as_posix()
    except ValueError:
        # resolve_task_ref 已拒绝仓库外路径，因此此处不可达。仍然拒绝，不能回退到绝对
        # 路径；过去正是这种回退让仓库外引用进入会话指针，并在后续每轮重放。
        return None


def _relative_task_ref(task_path: str, repo_root: Path) -> str:
    """为不必存在的任务路径生成仓库相对 POSIX 引用。

    `_canonical_task_ref` 通过文件系统解析，会拒绝已移动的任务目录。
    重命名需要同时引用移动前后两个位置，其中一个必定不存在。
    """
    normalized = normalize_task_ref(task_path)
    if not normalized:
        return ""
    candidate = Path(normalized)
    if not candidate.is_absolute():
        return normalized
    try:
        resolved = candidate.resolve()
        root = repo_root.resolve()
        workflow_real = (root / DIR_WORKFLOW).resolve()
    except OSError:
        return ""
    try:
        return resolved.relative_to(root).as_posix()
    except ValueError:
        pass
    # 与 resolve_task_ref 使用相同的双基准范围检查：经符号链接 `.trellis`（#567）
    # 访问的路径映射回仓库内形式；两个基准之外的路径会被拒绝，不保存为绝对指针。
    try:
        rel = resolved.relative_to(workflow_real)
    except ValueError:
        return ""
    return (Path(DIR_WORKFLOW) / rel).as_posix()


def _active_from_ref(
    task_ref: str | None,
    repo_root: Path,
    source_type: str,
    context_key: str | None = None,
) -> ActiveTask | None:
    if not task_ref:
        return None
    resolved = resolve_task_ref(task_ref, repo_root)
    stale = resolved is None or not resolved.is_dir()
    return ActiveTask(task_ref, source_type, context_key, stale)


def _context_path(repo_root: Path, context_key: str) -> Path:
    return _runtime_sessions_dir(repo_root) / f"{context_key}.json"


def resolve_active_task(
    repo_root: Path,
    platform_input: dict[str, Any] | None = None,
    platform: str | None = None,
    *,
    allow_single_session_fallback: bool = False,
    allow_environment_context: bool = True,
) -> ActiveTask:
    """仅从会话运行时状态解析活动任务。

    陈旧的会话任务按陈旧状态返回。缺失或不匹配的会话身份，不会根据会话文件数量
    推断任务归属。无法继承父会话身份的主动拉取型子代理调用者，必须显式启用兼容回退。
    """
    context_key = resolve_context_key(
        platform_input,
        platform,
        allow_environment_context=allow_environment_context,
    )
    if context_key:
        context = _read_json(_context_path(repo_root, context_key)) or {}
        task_ref = _string_value(context.get("current_task"))
        active = _active_from_ref(task_ref, repo_root, "session", context_key)
        if active:
            return active

    if allow_single_session_fallback:
        fallback = _resolve_single_session_fallback(repo_root)
        if fallback is not None:
            return fallback

    return ActiveTask(None, "none", context_key)


def _resolve_single_session_fallback(repo_root: Path) -> ActiveTask | None:
    """恰好存在一个会话文件时，返回其指向的任务。

    用于上下文键解析失败时（常见于第 2 类平台的子代理）。会话文件为 0 或 ≥2 个时返回
    None，拒绝跨窗口选择，以保持 04-21 的多会话隔离契约。
    """
    sessions_dir = _runtime_sessions_dir(repo_root)
    if not sessions_dir.is_dir():
        return None

    session_files = sorted(sessions_dir.glob("*.json"))
    if len(session_files) != 1:
        return None

    session_file = session_files[0]
    context = _read_json(session_file) or {}
    task_ref = _string_value(context.get("current_task"))
    if not task_ref:
        return None

    fallback_key = session_file.stem
    return _active_from_ref(task_ref, repo_root, "session-fallback", fallback_key)


def _utc_now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def _context_metadata(
    platform_input: dict[str, Any] | None,
    platform: str | None,
    context_key: str | None = None,
) -> dict[str, Any]:
    data = _as_dict(platform_input) or {}
    platform_name = _detect_platform(data, platform)
    if platform_name == "session" and context_key:
        prefix = context_key.split("_", 1)[0]
        if prefix in _KNOWN_PLATFORMS:
            platform_name = prefix
    metadata: dict[str, Any] = {
        "platform": platform_name,
        "last_seen_at": _utc_now(),
    }
    for key in (*_SESSION_KEYS, *_CONVERSATION_KEYS, *_TRANSCRIPT_KEYS):
        value = _lookup_string(data, (key,))
        if value:
            metadata[key] = value
    return metadata


def set_active_task(
    task_path: str,
    repo_root: Path,
    platform_input: dict[str, Any] | None = None,
    platform: str | None = None,
) -> ActiveTask | None:
    """在会话范围内设置活动任务。

    没有可用上下文键时返回 None；调用者应向用户报告错误，说明如何提供会话身份。
    """
    canonical = _canonical_task_ref(task_path, repo_root)
    if canonical is None:
        return None

    context_key = resolve_context_key(platform_input, platform)
    if not context_key:
        return None

    context_path = _context_path(repo_root, context_key)
    context = _read_json(context_path) or {}
    context.update(_context_metadata(platform_input, platform, context_key))
    context["current_task"] = canonical
    context.setdefault("current_run", None)
    if not _write_json(context_path, context):
        return None
    return ActiveTask(canonical, "session", context_key)


def clear_active_task(
    repo_root: Path,
    platform_input: dict[str, Any] | None = None,
    platform: str | None = None,
) -> ActiveTask:
    """删除解析出的会话上下文文件，以清除活动任务。"""
    context_key = resolve_context_key(platform_input, platform)
    if not context_key:
        return ActiveTask(None, "none")

    previous = resolve_active_task(repo_root, platform_input, platform)
    if not previous.task_path or not previous.context_key:
        return previous

    context_path = _context_path(repo_root, previous.context_key)
    if context_path.is_file():
        _remove_file(context_path)
    return previous


def clear_task_from_sessions(task_path: str, repo_root: Path) -> int:
    """删除所有指向指定任务的会话运行时文件。"""
    target = _canonical_task_ref(task_path, repo_root) or normalize_task_ref(task_path)
    if not target:
        return 0

    cleared = 0
    sessions_dir = _runtime_sessions_dir(repo_root)
    if not sessions_dir.is_dir():
        return cleared

    for session_path in sessions_dir.glob("*.json"):
        context = _read_json(session_path) or {}
        current = _string_value(context.get("current_task"))
        if not current:
            continue
        current_ref = _canonical_task_ref(current, repo_root) or normalize_task_ref(current)
        if current_ref != target:
            continue
        if session_path.is_file() and _remove_file(session_path):
            cleared += 1

    return cleared


def repoint_task_in_sessions(old_path: str, new_path: str, repo_root: Path) -> int:
    """将所有会话指针从 `old_path` 改指向 `new_path`。

    重命名是任务以不同名称继续存在的生命周期步骤，因此不能像归档一样清空指针：
    否则用户会静默丢失活动任务，必须重新执行 `task.py start` 才能恢复上下文注入。
    重定向可让会话在重命名后继续有效。
    """
    # 不用 `_canonical_task_ref`：调用者在移动目录后重定向，此时 `old_path`
    # 已不存在，而规范化要求目录存在，会恰好对需要匹配的引用返回 None。
    target = _relative_task_ref(old_path, repo_root)
    replacement = _relative_task_ref(new_path, repo_root)
    if not target or not replacement:
        return 0

    moved = 0
    sessions_dir = _runtime_sessions_dir(repo_root)
    if not sessions_dir.is_dir():
        return moved

    for session_path in sorted(sessions_dir.glob("*.json")):
        context = _read_json(session_path)
        if not context:
            continue
        current = _string_value(context.get("current_task"))
        if not current:
            continue
        current_ref = _canonical_task_ref(current, repo_root) or _relative_task_ref(
            current, repo_root
        )
        if current_ref != target:
            continue
        context["current_task"] = replacement
        if _write_json(session_path, context):
            moved += 1

    return moved


def get_current_task_source(
    repo_root: Path,
    platform_input: dict[str, Any] | None = None,
    platform: str | None = None,
) -> tuple[str, str | None, str | None]:
    """为兼容返回 (`source_type`, `context_key`, `task_path`)。"""
    active = resolve_active_task(repo_root, platform_input, platform)
    return active.source_type, active.context_key, active.task_path
