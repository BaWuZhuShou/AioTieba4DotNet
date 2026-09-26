#!/usr/bin/env python3
"""Trellis 工作流的公共路径工具。

提供：
    get_repo_root          - 获取仓库根目录
    get_developer          - 获取开发者名称
    get_workspace_dir      - 获取开发者工作区目录
    get_tasks_dir          - 获取任务目录
    get_active_journal_file - 获取当前日志文件
"""

from __future__ import annotations

import os
import re
from datetime import datetime
from pathlib import Path

from .git import main_worktree_root


# =============================================================================
# 路径常量（重命名目录时在此修改）
# =============================================================================

# 目录名
DIR_WORKFLOW = ".trellis"
DIR_WORKSPACE = "workspace"
DIR_TASKS = "tasks"
DIR_ARCHIVE = "archive"
DIR_SPEC = "spec"
DIR_SCRIPTS = "scripts"

# 文件名
FILE_DEVELOPER = ".developer"
FILE_CURRENT_TASK = ".current-task"
FILE_TASK_JSON = "task.json"
FILE_JOURNAL_PREFIX = "journal-"

# 开发者身份的环境变量覆盖项，优先于 .developer 文件。
ENV_DEVELOPER = "TRELLIS_DEVELOPER"

# 附加到每条“未设置开发者”的错误中，让用户从错误本身发现这两个不直观的来源。
DEVELOPER_HINT = (
    f"  或在环境中设置 {ENV_DEVELOPER}=<你的名称>。\n"
    f"  关联 Git worktree 会从主检出目录继承 {DIR_WORKFLOW}/{FILE_DEVELOPER}；"
    f"在主检出目录运行 init_developer.py 即可供所有 worktree 使用。"
)


# =============================================================================
# 仓库根目录
# =============================================================================

def get_repo_root(start_path: Path | None = None) -> Path:
    """查找最近的包含 .trellis/ 文件夹的目录。

    可正确处理嵌套 Git 仓库，例如另一仓库内的测试项目。

    参数：
        start_path: 搜索起点，默认为当前目录。

    返回：
        仓库根路径；未找到 .trellis/ 时返回当前目录。
    """
    current = (start_path or Path.cwd()).resolve()

    while current != current.parent:
        if (current / DIR_WORKFLOW).is_dir():
            return current
        current = current.parent

    # 未找到 .trellis/ 时回退到当前目录
    return Path.cwd().resolve()


# =============================================================================
# 开发者
# =============================================================================

def _read_developer_file(dev_file: Path) -> str | None:
    """读取 .developer 文件的 `name=` 字段，未找到时返回 None。"""
    if not dev_file.is_file():
        return None

    try:
        content = dev_file.read_text(encoding="utf-8")
    except (OSError, IOError):
        return None

    for line in content.splitlines():
        if line.startswith("name="):
            return line.split("=", 1)[1].strip() or None

    return None


def get_developer(repo_root: Path | None = None) -> str | None:
    """获取当前检出目录的开发者名称。

    按以下顺序解析，首次命中即返回（CLI `--assignee` 在调用本函数之前覆盖全部来源）：

        1. ``TRELLIS_DEVELOPER`` 环境变量。
        2. 当前检出目录的 ``.trellis/.developer``。
        3. 当前目录是关联 Git worktree 时，主检出目录的 ``.trellis/.developer``。

    第 3 步是因为 `.developer` 有意被 Git 忽略：它保存个人身份，不应纳入跟踪文件。
    新建的 `git worktree add` 因而没有自己的身份文件，过去需要逐个 worktree 重新执行
    init_developer.py，否则 task.py 命令都会失败。这里只读取主检出目录的文件，不复制它；
    副本会过时，并遮蔽主检出目录中的后续更改。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        开发者名称，未初始化时返回 None。
    """
    env_name = os.environ.get(ENV_DEVELOPER, "").strip()
    if env_name:
        return env_name

    if repo_root is None:
        repo_root = get_repo_root()

    local = _read_developer_file(repo_root / DIR_WORKFLOW / FILE_DEVELOPER)
    if local:
        return local

    main_root = main_worktree_root(repo_root)
    if main_root is None:
        return None

    return _read_developer_file(main_root / DIR_WORKFLOW / FILE_DEVELOPER)


def check_developer(repo_root: Path | None = None) -> bool:
    """检查开发者是否已初始化。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        开发者已初始化时返回 True。
    """
    return get_developer(repo_root) is not None


# =============================================================================
# 任务目录
# =============================================================================

def get_tasks_dir(repo_root: Path | None = None) -> Path:
    """获取任务目录路径。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        任务目录路径。
    """
    if repo_root is None:
        repo_root = get_repo_root()
    return repo_root / DIR_WORKFLOW / DIR_TASKS


# =============================================================================
# 工作区目录
# =============================================================================

def get_workspace_dir(repo_root: Path | None = None) -> Path | None:
    """获取开发者工作区目录。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        工作区目录路径，未设置开发者时返回 None。
    """
    if repo_root is None:
        repo_root = get_repo_root()

    developer = get_developer(repo_root)
    if developer:
        return repo_root / DIR_WORKFLOW / DIR_WORKSPACE / developer
    return None


# =============================================================================
# 日志文件
# =============================================================================

def get_active_journal_file(repo_root: Path | None = None) -> Path | None:
    """获取当前使用的日志文件。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        当前日志文件路径，未找到时返回 None。
    """
    if repo_root is None:
        repo_root = get_repo_root()

    workspace_dir = get_workspace_dir(repo_root)
    if workspace_dir is None or not workspace_dir.is_dir():
        return None

    latest: Path | None = None
    highest = 0

    for f in workspace_dir.glob(f"{FILE_JOURNAL_PREFIX}*.md"):
        if not f.is_file():
            continue

        # 从文件名提取编号
        name = f.stem  # 例如 "journal-1"
        match = re.search(r"(\d+)$", name)
        if match:
            num = int(match.group(1))
            if num > highest:
                highest = num
                latest = f

    return latest


def count_lines(file_path: Path) -> int:
    """统计文件行数。

    参数：
        file_path: 文件路径。

    返回：
        行数，文件不存在时返回 0。
    """
    if not file_path.is_file():
        return 0

    try:
        return len(file_path.read_text(encoding="utf-8").splitlines())
    except (OSError, IOError):
        return 0


# =============================================================================
# 当前任务管理
# =============================================================================

def normalize_task_ref(task_ref: str) -> str:
    """规范化任务引用，以便稳定地保存运行时状态。

    即使在 Windows 上，也应优先保存 `.trellis/tasks/03-27-my-task` 这样的仓库相对
    POSIX 路径。绝对路径会保留，除非调用者后续能将其转换为仓库相对形式。
    """
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


def resolve_task_ref(task_ref: str, repo_root: Path | None = None) -> Path | None:
    """将任务引用解析为仓库内任务目录的绝对路径。

    引用解析到 `repo_root` 之外时返回 None。活动任务的所有读取者（`task.py`、共享钩子、
    平台扩展）均通过这里，在统一入口约束路径范围，避免每个调用点重复实现。

    引用不一定由用户直接输入，它会经过 `.trellis/.runtime/sessions/` 下的会话指针。
    过去 `..` 段会原样往返：`_canonical_task_ref` 按字面比较，字面上的 `relative_to`
    会接受 `<root>/.trellis/tasks/../../../elsewhere`，因为字符串确实以根路径开头。
    引用随后原样存储，并在每轮重放，使 `task.py start .trellis/tasks/../../../elsewhere`
    既改写该目录的 `task.json`，又将其中的文件输入模型。

    在这里解析也会规范化路径，因此调用者取得的存储引用不含 `..`。
    """
    if repo_root is None:
        repo_root = get_repo_root()

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

    # resolve() 会折叠 `..` 并跟随符号链接，因此指向仓库外的任务目录也会被拒绝。
    # 两端均需解析，因为 repo_root 本身也可能经过符号链接（如 macOS 的 /tmp）。
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

    # `.trellis` 本身可能是指向仓库外存储的符号链接（#567）。
    # 工作流目录的真实位置因此成为第二个合法范围基准：通过该链接的引用并未离开
    # 工作流目录树。逃出两个基准的引用（路径穿越、其他位置的绝对路径、
    # 指向树外的任务目录符号链接）仍会被拒绝。
    try:
        rel = resolved.relative_to(workflow_real)
    except ValueError:
        return None

    # 映射回仓库内的字面路径，使调用者保存与非符号链接布局相同的仓库相对引用。
    return root / DIR_WORKFLOW / rel


def get_current_task(
    repo_root: Path | None = None,
    platform_input: dict | None = None,
    platform: str | None = None,
) -> str | None:
    """获取当前任务目录路径（相对于 repo_root）。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        当前任务目录的相对路径，或 None。
    """
    if repo_root is None:
        repo_root = get_repo_root()

    from .active_task import resolve_active_task

    return resolve_active_task(repo_root, platform_input, platform).task_path


def get_current_task_abs(
    repo_root: Path | None = None,
    platform_input: dict | None = None,
    platform: str | None = None,
) -> Path | None:
    """获取当前任务目录的绝对路径。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        当前任务目录的绝对路径，或 None。
    """
    if repo_root is None:
        repo_root = get_repo_root()

    relative = get_current_task(repo_root, platform_input, platform)
    if relative:
        return resolve_task_ref(relative, repo_root)
    return None


def get_current_task_source(
    repo_root: Path | None = None,
    platform_input: dict | None = None,
    platform: str | None = None,
) -> tuple[str, str | None, str | None]:
    """返回活动任务来源，形式为 (`source`, `context_key`, `task_path`)。"""
    if repo_root is None:
        repo_root = get_repo_root()

    from .active_task import get_current_task_source as _get_source

    return _get_source(repo_root, platform_input, platform)


def set_current_task(
    task_path: str,
    repo_root: Path | None = None,
    platform_input: dict | None = None,
    platform: str | None = None,
) -> bool:
    """在会话范围内设置当前任务。

    参数：
        task_path: 任务目录路径（相对于 repo_root）。
        repo_root: 仓库根路径，默认自动检测。

    返回：
        成功返回 True，出错返回 False。
    """
    if repo_root is None:
        repo_root = get_repo_root()

    from .active_task import set_active_task

    return set_active_task(
        task_path,
        repo_root,
        platform_input=platform_input,
        platform=platform,
    ) is not None


def clear_current_task(
    repo_root: Path | None = None,
    platform_input: dict | None = None,
    platform: str | None = None,
) -> bool:
    """在会话范围内清除当前任务。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        成功返回 True。
    """
    if repo_root is None:
        repo_root = get_repo_root()

    from .active_task import clear_active_task

    clear_active_task(
        repo_root,
        platform_input=platform_input,
        platform=platform,
    )
    return True


def has_current_task(repo_root: Path | None = None) -> bool:
    """检查是否设置了当前任务。

    参数：
        repo_root: 仓库根路径，默认自动检测。

    返回：
        已设置当前任务时返回 True。
    """
    return get_current_task(repo_root) is not None


# =============================================================================
# 任务 ID 生成
# =============================================================================

def generate_task_date_prefix() -> str:
    """按日期生成任务 ID 前缀（MM-DD 格式）。

    返回：
        日期前缀字符串，如 "01-21"。
    """
    return datetime.now().strftime("%m-%d")


# =============================================================================
# 单体仓库 / 包路径
# =============================================================================


def get_spec_dir(package: str | None = None, repo_root: Path | None = None) -> Path:
    """获取规范目录路径。

    单仓库：.trellis/spec
    单体仓库且指定包：.trellis/spec/<package>

    使用延迟导入，避免与 config.py 循环依赖。
    """
    if repo_root is None:
        repo_root = get_repo_root()

    from .config import get_spec_base

    base = get_spec_base(package, repo_root)
    return repo_root / DIR_WORKFLOW / base


def get_package_path(package: str, repo_root: Path | None = None) -> Path | None:
    """从配置获取包源目录的绝对路径。

    返回：
        包目录的绝对路径，未找到时返回 None。
    """
    if repo_root is None:
        repo_root = get_repo_root()

    from .config import get_packages

    packages = get_packages(repo_root)
    if not packages or package not in packages:
        return None

    info = packages[package]
    if isinstance(info, dict):
        rel_path = info.get("path", package)
    else:
        rel_path = str(info)

    return repo_root / rel_path


# =============================================================================
# 主入口（用于测试）
# =============================================================================

if __name__ == "__main__":
    repo = get_repo_root()
    print(f"仓库根目录：{repo}")
    print(f"开发者：{get_developer(repo)}")
    print(f"任务目录：{get_tasks_dir(repo)}")
    print(f"工作区目录：{get_workspace_dir(repo)}")
    print(f"日志文件：{get_active_journal_file(repo)}")
    print(f"当前任务：{get_current_task(repo)}")
