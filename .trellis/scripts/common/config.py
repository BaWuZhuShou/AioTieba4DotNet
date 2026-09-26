#!/usr/bin/env python3
"""Trellis 配置读取器。

从 .trellis/config.yaml 读取设置，并提供合理的默认值。
"""

from __future__ import annotations

import sys
from pathlib import Path

from .paths import DIR_WORKFLOW, get_repo_root
from .trellis_config import parse_simple_yaml

# YAML 子集解析器位于 trellis_config.py；它不导入本包其他模块，
# 钩子可将它作为单文件独立加载。保留两个内容一致的副本会带来漂移风险。


# 默认值
DEFAULT_SESSION_COMMIT_MESSAGE = "chore: 记录开发日志"
DEFAULT_MAX_JOURNAL_LINES = 2000
DEFAULT_SESSION_AUTO_COMMIT = True
DEFAULT_CODEX_DISPATCH_MODE = "auto"

CONFIG_FILE = "config.yaml"


TRUE_CONFIG_VALUES = ("true", "yes", "1", "on")
FALSE_CONFIG_VALUES = ("false", "no", "0", "off")


def coerce_config_bool(
    value: object,
    default: bool,
    label: str,
) -> bool:
    """将配置值转换为布尔值，对无法识别的值发出警告。

    解析器将所有值保存为字符串，因此 ``git: yes`` 会变为 ``"yes"``。
    所有布尔配置键都经过本函数：若各处接受规则不同，用户写入合理的 YAML 布尔值后
    可能静默进入相反分支。

    参数：
        value: 解析所得的原始配置值。
        default: 无法识别时返回的默认值。
        label: 配置键名，用于警告。
    """
    if isinstance(value, bool):
        return value
    s = str(value).strip().lower()
    if s in TRUE_CONFIG_VALUES:
        return True
    if s in FALSE_CONFIG_VALUES:
        return False
    print(
        f"[WARN] {label} 的值无效：{value!r}；使用默认值 {str(default).lower()}",
        file=sys.stderr,
    )
    return default


def _is_true_config_value(value: object, label: str = "配置开关") -> bool:
    """配置值表示启用时返回 True。"""
    if value is None:
        return False
    return coerce_config_bool(value, False, label)


def _get_config_path(repo_root: Path | None = None) -> Path:
    """获取 config.yaml 的路径。"""
    root = repo_root or get_repo_root()
    return root / DIR_WORKFLOW / CONFIG_FILE


def _load_config(repo_root: Path | None = None) -> dict:
    """加载并解析 config.yaml，任何错误均返回空字典。

    与 ``trellis_config.read_trellis_config`` 一致，失败时允许继续：格式错误的配置
    不应使 ``task.py create`` 中断。解析失败会向 stderr 报告一次，避免静默忽略。
    """
    config_file = _get_config_path(repo_root)
    try:
        content = config_file.read_text(encoding="utf-8")
    except (OSError, IOError):
        return {}
    try:
        parsed = parse_simple_yaml(content, source=str(config_file))
    except Exception as e:
        print(
            f"[WARN] 无法解析 {config_file}：{type(e).__name__}: {e}；"
            "使用默认配置",
            file=sys.stderr,
        )
        return {}
    return parsed if isinstance(parsed, dict) else {}


def get_session_commit_message(repo_root: Path | None = None) -> str:
    """获取自动提交会话记录使用的提交说明。"""
    config = _load_config(repo_root)
    return config.get("session_commit_message", DEFAULT_SESSION_COMMIT_MESSAGE)


def get_max_journal_lines(repo_root: Path | None = None) -> int:
    """获取每个日志文件的最大行数。"""
    config = _load_config(repo_root)
    value = config.get("max_journal_lines", DEFAULT_MAX_JOURNAL_LINES)
    try:
        return int(value)
    except (ValueError, TypeError):
        return DEFAULT_MAX_JOURNAL_LINES


def get_session_auto_commit(repo_root: Path | None = None) -> bool:
    """判断脚本是否应自动暂存并提交会话/任务变更。

    同时控制 ``add_session.py:_auto_commit_workspace`` 与
    ``task_store.py:_auto_commit_archive``。

    默认 ``True``（保持自动暂存并提交的既有行为）。在 ``.trellis/config.yaml``
    中设置 ``session_auto_commit: false`` 可完全跳过自动暂存；日志/归档文件仍写入磁盘，
    由用户自行执行 ``git add`` / ``git commit``。

    接受 YAML 原生布尔值（``true`` / ``false``）及字符串别名
    ``true / false / yes / no / 1 / 0 / on / off``（不区分大小写）。
    无效值回退为 ``True``，并向 stderr 发出警告。
    """
    config = _load_config(repo_root)
    raw = config.get("session_auto_commit", DEFAULT_SESSION_AUTO_COMMIT)
    return coerce_config_bool(
        raw, DEFAULT_SESSION_AUTO_COMMIT, "session_auto_commit"
    )


def get_codex_dispatch_mode(repo_root: Path | None = None) -> str:
    """返回 Codex 派发模式。

    默认 ``auto``，派发 Trellis 子代理，优先使用原生上下文注入，并提供子代理侧加载回退。
    ``inline`` 表示明确不使用派发。``sub-agent`` 保留为 ``auto`` 的向后兼容别名。

    显式配置无效时回退到 ``inline``，避免意外派发子代理。仅面向 CLI 的本解析器会
    报告无效值；钩子读取器安全回退，不在每一轮重复产生警告。
    """
    config = _load_config(repo_root)
    codex = config.get("codex")
    if codex is None:
        return DEFAULT_CODEX_DISPATCH_MODE
    if not isinstance(codex, dict):
        print(
            f"[WARN] codex 配置无效：{codex!r}；使用 inline",
            file=sys.stderr,
        )
        return "inline"

    raw = codex.get("dispatch_mode", DEFAULT_CODEX_DISPATCH_MODE)
    mode = str(raw).strip().lower()
    if mode in ("auto", "inline"):
        return mode
    if mode == "sub-agent":
        return "auto"
    print(
        f"[WARN] codex.dispatch_mode 的值无效：{raw!r}；使用 inline",
        file=sys.stderr,
    )
    return "inline"


DEFAULT_CONTEXT_INJECTION_MAX_FILE_BYTES = 32768
DEFAULT_CONTEXT_INJECTION_MAX_ARTIFACT_BYTES = 65536
DEFAULT_CONTEXT_INJECTION_MAX_TOTAL_BYTES = 131072


def get_context_injection_limits(repo_root: Path | None = None) -> dict[str, int]:
    """返回子代理上下文注入的字节限制。

    读取 ``.trellis/config.yaml`` 的 ``context_injection:`` 部分：

        context_injection:
          max_file_bytes: 32768
          max_artifact_bytes: 65536
          max_total_bytes: 131072

    ``0`` 禁用对应限制。缺失键使用默认值；无效值（非整数或负数）回退到该键默认值，
    并向 stderr 发出警告。
    """
    defaults = {
        "max_file_bytes": DEFAULT_CONTEXT_INJECTION_MAX_FILE_BYTES,
        "max_artifact_bytes": DEFAULT_CONTEXT_INJECTION_MAX_ARTIFACT_BYTES,
        "max_total_bytes": DEFAULT_CONTEXT_INJECTION_MAX_TOTAL_BYTES,
    }

    config = _load_config(repo_root)
    section = config.get("context_injection")
    if not isinstance(section, dict):
        return defaults

    result = dict(defaults)
    for key, default_value in defaults.items():
        if key not in section:
            continue
        raw = section[key]
        try:
            value = int(raw)
        except (TypeError, ValueError):
            print(
                f"[WARN] context_injection.{key} 的值无效：{raw!r}；"
                f"使用默认值 {default_value}",
                file=sys.stderr,
            )
            continue
        if value < 0:
            print(
                f"[WARN] context_injection.{key} 的值无效：{raw!r}；"
                f"使用默认值 {default_value}",
                file=sys.stderr,
            )
            continue
        result[key] = value

    return result


DEFAULT_PROMPT_INJECTION_SKIP_KEYWORD = "no-trellis"


def get_prompt_injection_config(repo_root: Path | None = None) -> dict[str, str]:
    """返回每轮提示注入配置。

    读取 ``.trellis/config.yaml`` 的 ``prompt_injection:`` 部分：

        prompt_injection:
          skip_keyword: "no-trellis"   # "" 完全禁用跳过机制

    ``skip_keyword`` 按单词边界匹配且不区分大小写；用户提示中出现该关键字时，
    本轮工作流状态注入不输出内容。默认 ``"no-trellis"``。非字符串值回退到默认值。
    """
    defaults = {"skip_keyword": DEFAULT_PROMPT_INJECTION_SKIP_KEYWORD}

    config = _load_config(repo_root)
    section = config.get("prompt_injection")
    if not isinstance(section, dict):
        return defaults

    result = dict(defaults)
    raw = section.get("skip_keyword", DEFAULT_PROMPT_INJECTION_SKIP_KEYWORD)
    if isinstance(raw, str):
        result["skip_keyword"] = raw
    return result


def get_hooks(event: str, repo_root: Path | None = None) -> list[str]:
    """获取生命周期事件对应的钩子命令。

    参数：
        event: 事件名称，如 "after_create"、"after_archive"。
        repo_root: 仓库根路径。

    用户以为钩子已安装，实际却从未执行，是本功能最差的结果；因此已声明但形状不可用的
    配置会发出警告，不会静默返回空列表。

    返回：
        要执行的 shell 命令列表，未配置时为空。
    """
    config = _load_config(repo_root)
    hooks = config.get("hooks")
    if hooks is None:
        return []
    if not isinstance(hooks, dict):
        print(
            f"[WARN] 忽略 config.yaml 中的 `hooks`：预期为"
            f"事件到命令列表的映射，实际为 {hooks!r}",
            file=sys.stderr,
        )
        return []
    commands = hooks.get(event)
    if commands is None:
        return []
    if isinstance(commands, list):
        return [str(c) for c in commands]
    # 使用 `after_create: echo hi` 而非 `- ` 列表时，虽然能解析，但不会注册命令。
    print(
        f"[WARN] 忽略 config.yaml 中的钩子 `{event}`：预期为"
        f"命令列表，实际为 {commands!r}。请改为：\n"
        f"  hooks:\n    {event}:\n      - {commands}",
        file=sys.stderr,
    )
    return []


# =============================================================================
# 单体仓库 / 包
# =============================================================================


def get_packages(repo_root: Path | None = None) -> dict[str, dict] | None:
    """获取单体仓库的包声明。

    返回：
        包名到配置（path、type 等）的字典；未配置时返回 None（单仓库模式）。

    返回示例：
        {"cli": {"path": "packages/cli"}, "docs-site": {"path": "docs-site", "type": "submodule"}}
    """
    config = _load_config(repo_root)
    packages = config.get("packages")
    if not isinstance(packages, dict):
        return None
    # 确保每个值都是字典（过滤标量条目）
    filtered = {k: v for k, v in packages.items() if isinstance(v, dict)}
    if not filtered:
        return None
    return filtered


def get_default_package(repo_root: Path | None = None) -> str | None:
    """从配置获取默认包名。

    返回：
        包名字符串，未配置时返回 None。
    """
    config = _load_config(repo_root)
    value = config.get("default_package")
    return str(value) if value else None


def get_submodule_packages(repo_root: Path | None = None) -> dict[str, str]:
    """获取属于 Git 子模块的包。

    返回：
        子模块类型的包名到路径的字典；未配置时返回空字典。

    返回示例：
        {"docs-site": "docs-site"}
    """
    packages = get_packages(repo_root)
    if packages is None:
        return {}
    return {
        name: cfg.get("path", name)
        for name, cfg in packages.items()
        if cfg.get("type") == "submodule"
    }


def get_git_packages(repo_root: Path | None = None) -> dict[str, str]:
    """获取拥有独立 Git 仓库的包。

    这些子目录拥有自己的 .git（不是子模块），并在 config.yaml 中标记 ``git: true``。

    返回：
        独立 Git 仓库的包名到路径的字典；未配置时返回空字典。

    配置示例::

        packages:
          backend:
            path: iqs
            git: true

    返回示例::

        {"backend": "iqs"}
    """
    packages = get_packages(repo_root)
    if packages is None:
        return {}
    return {
        name: cfg.get("path", name)
        for name, cfg in packages.items()
        if _is_true_config_value(cfg.get("git"), f"packages.{name}.git")
    }


def is_monorepo(repo_root: Path | None = None) -> bool:
    """检查项目是否配置为单体仓库（配置中存在 packages）。"""
    return get_packages(repo_root) is not None


def get_spec_base(package: str | None = None, repo_root: Path | None = None) -> str:
    """获取相对于 .trellis/ 的规范目录基础路径。

    单仓库：返回 "spec"。
    单体仓库且指定包：返回 "spec/<package>"。
    单体仓库但未指定包：返回 "spec"（调用者应指定包）。
    """
    if package and is_monorepo(repo_root):
        return f"spec/{package}"
    return "spec"


def validate_package(package: str, repo_root: Path | None = None) -> bool:
    """检查包名在本项目中是否有效。

    单仓库（未配置 packages）：始终返回 True。
    单体仓库：仅当包存在于 config.yaml 的 packages 中时返回 True。
    """
    packages = get_packages(repo_root)
    if packages is None:
        return True  # 单仓库，无需校验
    return package in packages


def resolve_package(
    task_package: str | None = None,
    repo_root: Path | None = None,
) -> str | None:
    """从推断来源解析包名并校验。

    依次检查 task_package → default_package。
    推断值无效时向 stderr 发出警告并跳过。

    返回：
        解析后的包名；未找到有效包时返回 None。

    说明：
        CLI --package 应由调用者单独校验；出错时立即失败并列出可用包。
    """
    packages = get_packages(repo_root)
    if packages is None:
        return None  # 单仓库，无需包名

    # 尝试 task_package（防范格式错误的 JSON 提供非字符串值）
    if task_package and isinstance(task_package, str):
        if task_package in packages:
            return task_package
        print(
            f"警告：配置中未找到 task.json 的包 '{task_package}'，已跳过",
            file=sys.stderr,
        )

    # 尝试 default_package
    default = get_default_package(repo_root)
    if default:
        if default in packages:
            return default
        print(
            f"警告：配置中未找到 default_package '{default}'，已跳过",
            file=sys.stderr,
        )

    return None


def get_spec_scope(repo_root: Path | None = None) -> list[str] | str | None:
    """获取 session.spec_scope 配置。

    返回：
        list[str]: 规范扫描应包含的包名。
        str: "active_task" 表示使用当前任务的包。
        None: 未配置范围（扫描所有包）。
    """
    config = _load_config(repo_root)
    session = config.get("session")
    if not isinstance(session, dict):
        return None

    scope = session.get("spec_scope")
    if scope is None:
        return None
    if isinstance(scope, str):
        return scope  # 例如 "active_task"
    if isinstance(scope, list):
        return [str(s) for s in scope]
    return None
