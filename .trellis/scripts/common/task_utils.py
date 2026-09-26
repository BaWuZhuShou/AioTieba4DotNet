#!/usr/bin/env python3
"""
任务工具函数。

提供功能：
    is_within_tasks_dir - 检查解析后的路径是否为 tasks/ 的直接子任务
    find_task_by_name   - 按名称查找任务目录
    resolve_task_dir    - 从名称、相对路径或绝对路径解析任务目录
    archive_destination_for - 获取任务的归档目标路径
    archive_task_dir    - 将任务归档到按月划分的目录
    run_task_hooks      - 运行任务事件的生命周期钩子
"""

from __future__ import annotations

import shutil
import sys
from datetime import datetime
from pathlib import Path
from typing import TYPE_CHECKING

from .paths import get_repo_root, get_tasks_dir

if TYPE_CHECKING:
    import subprocess


# =============================================================================
# 路径安全
# =============================================================================

def is_within_tasks_dir(task_dir_abs: Path, repo_root: Path | None = None) -> bool:
    """检查解析后的任务目录是否确实位于任务根目录下。

    实际任务直接位于 ``.trellis/tasks/<name>``。
    仅当 ``task_dir_abs`` 是任务根目录的直接子目录时返回 True。

    针对归档收紧 ``resolve_task_dir`` 的范围限制：该统一入口接受任务根目录下
    的任意路径，包括已在 ``archive/<YYYY-MM>/`` 中的任务。
    此处拒绝这些路径及任务根目录本身，避免 ``shutil.move``
    把已归档任务再次归档成嵌套副本。
    """
    if repo_root is None:
        repo_root = get_repo_root()
    try:
        resolved = task_dir_abs.resolve()
        tasks_resolved = get_tasks_dir(repo_root).resolve()
    except (OSError, RuntimeError):
        return False
    if resolved.parent != tasks_resolved:
        return False
    return resolved.name != "archive"


# =============================================================================
# 任务查找
# =============================================================================

def find_task_by_name(task_name: str, tasks_dir: Path) -> Path | None:
    """按名称查找任务目录，支持精确匹配和后缀匹配。

    任务名是 ``tasks_dir`` 下的单个目录名，不能是路径。
    拼接前拒绝包含分隔符或点路径段的名称，避免 ``".."`` 返回任务根目录的父目录。

    后缀有歧义时（不同日期创建了相同 slug 的两个任务）应失败，不能随机选择：
    ``iterdir()`` 遵循文件系统顺序，静默选择首个匹配项会在不同机器上选中不同任务。

    参数：
        task_name: 要查找的任务名。
        tasks_dir: 任务根目录路径。

    返回值：
        任务目录的绝对路径；未找到或存在歧义时返回 None。
    """
    if not task_name or not tasks_dir or not tasks_dir.is_dir():
        return None

    if "/" in task_name or "\\" in task_name or task_name in (".", ".."):
        print(f"错误：无效任务名： {task_name}", file=sys.stderr)
        return None

    # 先尝试精确匹配
    exact_match = tasks_dir / task_name
    if exact_match.is_dir():
        return exact_match

    # 再尝试后缀匹配（例如 "my-task" 匹配 "01-21-my-task"）
    matches = sorted(
        d for d in tasks_dir.iterdir()
        if d.is_dir() and d.name.endswith(f"-{task_name}")
    )
    if len(matches) == 1:
        return matches[0]
    if matches:
        print(f"错误：任务名 '{task_name}' 有歧义，匹配：", file=sys.stderr)
        for match in matches:
            print(f"  - {match.name}", file=sys.stderr)
        print("请传入完整任务目录名。", file=sys.stderr)

    return None


# =============================================================================
# 归档操作
# =============================================================================

def archive_destination_for(task_dir_abs: Path) -> Path:
    """任务的归档目标路径： <tasks>/archive/<YYYY-MM>/<name>."""
    tasks_dir = task_dir_abs.parent
    year_month = datetime.now().strftime("%Y-%m")
    return tasks_dir / "archive" / year_month / task_dir_abs.name


def archive_task_dir(task_dir_abs: Path, repo_root: Path | None = None) -> Path | None:
    """将任务目录归档到 archive/{YYYY-MM}/。

    参数：
        task_dir_abs: 任务目录的绝对路径。
        repo_root: 仓库根路径，默认自动检测。

    返回值：
        归档后的目录路径；发生错误时返回 None。
    """
    if not task_dir_abs.is_dir():
        print(f"错误：找不到任务目录： {task_dir_abs}", file=sys.stderr)
        return None

    dest = archive_destination_for(task_dir_abs)
    month_dir = dest.parent

    # 创建归档目录
    try:
        month_dir.mkdir(parents=True, exist_ok=True)
    except (OSError, IOError) as e:
        print(f"错误：无法创建归档目录： {e}", file=sys.stderr)
        return None

    # shutil.move 的目标目录若已存在，会把源目录移入其内部，
    # 形成 archive/<month>/<task>/<task>/，返回路径与实际落点不同。
    # 这个错误路径随后会进入显示结果、after_archive 钩子的 TASK_JSON_PATH
    # 及自动提交的暂存逻辑，因此必须在移动前拒绝。
    if dest.exists():
        print(
            f"错误：拒绝归档 {task_dir_abs}： "
            f"归档目标已存在： {dest}",
            file=sys.stderr,
        )
        print(
            "请先移动或重命名已有归档任务，再重试。",
            file=sys.stderr,
        )
        return None

    try:
        shutil.move(str(task_dir_abs), str(dest))
    except (OSError, IOError, shutil.Error) as e:
        print(f"错误：无法将任务移入归档： {e}", file=sys.stderr)
        return None

    return dest


def archive_task_complete(
    task_dir_abs: Path,
    repo_root: Path | None = None
) -> dict[str, str]:
    """完成归档流程：归档目录。

    参数：
        task_dir_abs: 任务目录的绝对路径。
        repo_root: 仓库根路径，默认自动检测。

    返回值：
        包含归档结果信息的字典。
    """
    if not task_dir_abs.is_dir():
        print(f"错误：找不到任务目录： {task_dir_abs}", file=sys.stderr)
        return {}

    archive_dest = archive_task_dir(task_dir_abs, repo_root)
    if archive_dest:
        return {"archived_to": str(archive_dest)}

    return {}


# =============================================================================
# 任务目录解析
# =============================================================================

def resolve_task_dir(target_dir: str, repo_root: Path) -> Path | None:
    """将任务目录解析为任务根目录内的绝对路径。

    支持：
    - 绝对路径：/path/to/task
    - 相对路径：.trellis/tasks/01-31-my-task
    - 任务名：my-task（通过 find_task_by_name 查找）

    这是所有接受任务目录参数的命令的统一范围检查入口。
    解析候选路径（跟随符号链接）后，必须严格位于 ``.trellis/tasks/`` 下；
    ``archive/<YYYY-MM>/`` 下的归档任务也符合条件。
    此处拒绝路径穿越（``../victim``）、仓库外绝对路径、指向任务树外的任务
    符号链接，以及任务根目录本身，调用者无需重复检查。
    范围检查以任务根目录的真实位置为基准，因此 ``.trellis`` 本身
    链接到共享存储的情形（#567）仍可使用。

    参数：
        target_dir: 任务目录描述。
        repo_root: 仓库根路径。

    返回值：
        经由仓库自身任务目录表示的绝对路径（保留仓库内的词法形式）；
        若位置不在任务根目录内，返回 None，并向 stderr 输出包含路径的错误。
    """
    if not target_dir:
        print("错误：必须提供任务目录", file=sys.stderr)
        return None

    normalized = target_dir.replace("\\", "/")
    while normalized.startswith("./"):
        normalized = normalized[2:]

    tasks_dir = get_tasks_dir(repo_root)

    if Path(target_dir).is_absolute():
        candidate = Path(target_dir)
    elif "/" in normalized or normalized.startswith(".trellis"):
        # 相对路径（包含路径分隔符或以 .trellis 开头）
        candidate = repo_root / Path(normalized)
    else:
        # 任务名必须解析到任务根目录内。旧逻辑回退到 repo_root/<name>
        # 只会产生被下方检查拒绝的路径，因此查找失败时在此结束。
        candidate = find_task_by_name(target_dir, tasks_dir)
        if candidate is None:
            # find_task_by_name 自行报告无效名称和歧义。
            print(
                f"错误：无法在 {tasks_dir} 下解析任务 '{target_dir}'",
                file=sys.stderr,
            )
            return None

    try:
        resolved = candidate.resolve()
        tasks_lexical = get_tasks_dir(repo_root.resolve())
        tasks_resolved = tasks_lexical.resolve()
    except (OSError, RuntimeError) as e:
        print(f"错误：无法解析任务目录 '{target_dir}'： {e}", file=sys.stderr)
        return None

    if resolved == tasks_resolved:
        print(
            f"错误：拒绝使用 '{target_dir}'：{tasks_resolved} 是任务根"
            "目录本身，不是任务",
            file=sys.stderr,
        )
        return None

    if tasks_resolved not in resolved.parents:
        print(
            f"错误：拒绝使用 '{target_dir}'：{resolved} 位于 {tasks_resolved} 之外",
            file=sys.stderr,
        )
        return None

    # 返回经由仓库自身任务目录表示的路径，而非 `resolved`：
    # `.trellis` 可能链接到仓库外的存储（#567），此时任务有效，
    # 但 `resolved` 位于仓库外，调用者将其转成仓库相对引用
    # 保存时会拒绝该路径。
    return tasks_lexical / resolved.relative_to(tasks_resolved)


# =============================================================================
# 生命周期钩子
# =============================================================================

HOOK_TIMEOUT_SECONDS = 60
HOOK_KILL_GRACE_SECONDS = 5
HOOK_OUTPUT_LIMIT = 2000


def _kill_hook_tree(proc: subprocess.Popen[str]) -> None:
    """终止超时钩子的整个进程树，包括 shell 及其后代进程。

    ``shell=True`` 时直接子进程是 shell，实际工作由它的子进程执行。
    只终止 shell 会让孙进程继续占用继承的 stdout/stderr 管道，
    后续收集输出会一直阻塞到孤儿进程退出，重新出现超时机制要防止的
    “命令永不返回”问题。

    POSIX：钩子以 ``start_new_session=True`` 启动，shell 和所有后代
    共享新的进程组；对整个组发送 SIGKILL。
    Windows：通过 ``taskkill /F /T`` 尽力遍历并终止整个进程树。
    两个平台均以 ``proc.kill()`` 作为后备方式。

    限制：钩子自行调用 ``setsid`` 后会离开进程组，因而可能存活。
    这是已接受且不在本功能范围内的限制：钩子可执行任意代码，
    超时用于保证命令最终返回，并非隔离边界。
    """
    import os
    import signal
    import subprocess

    if os.name == "posix":
        try:
            os.killpg(os.getpgid(proc.pid), signal.SIGKILL)
            return
        except (ProcessLookupError, PermissionError, OSError):
            pass
    else:
        try:
            subprocess.run(
                ["taskkill", "/F", "/T", "/PID", str(proc.pid)],
                capture_output=True,
            )
        except (OSError, ValueError):
            pass

    try:
        proc.kill()
    except OSError:
        pass


def _release_hook_process(proc: subprocess.Popen[str]) -> None:
    """关闭本端管道，并在时限内回收已结束的钩子进程。

    ``Popen`` 用作上下文管理器时，在退出代码块时会无超时地调用 ``wait()``。
    这会使命令仍有可能无法返回：若 ``_kill_hook_tree`` 完全失败，
    即终止进程组和直接终止进程均抛出异常，生命周期命令就会一直阻塞，
    重新出现超时机制要防止的挂起。
    """
    import subprocess

    for stream in (proc.stdout, proc.stderr, proc.stdin):
        if stream is not None:
            try:
                stream.close()
            except OSError:
                pass
    try:
        proc.wait(timeout=HOOK_KILL_GRACE_SECONDS)
    except (subprocess.TimeoutExpired, OSError, ValueError):
        # 僵尸进程会保留到本进程退出。容错并在时限内返回，
        # 可避免任务命令永远无法结束。
        pass


def _decode_hook_output(raw: object) -> str:
    """TimeoutExpired 携带 bytes 或 str，取决于平台。"""
    if raw is None:
        return ""
    if isinstance(raw, bytes):
        return raw.decode("utf-8", errors="replace")
    return str(raw)


def _print_hook_stream(name: str, text: str) -> None:
    """将捕获的钩子输出流截断后打印到 stderr。"""
    text = text.strip()
    if not text:
        return
    if len(text) > HOOK_OUTPUT_LIMIT:
        text = (
            text[:HOOK_OUTPUT_LIMIT]
            + f"\n… ({len(text) - HOOK_OUTPUT_LIMIT} 个字符已截断)"
        )
    print(f"  {name}:", file=sys.stderr)
    for line in text.splitlines():
        print(f"    {line}", file=sys.stderr)


def run_task_hooks(event: str, task_json_path: Path, repo_root: Path) -> None:
    """运行任务事件的生命周期钩子。

    钩子是从 ``.trellis/config.yaml`` 读取的 shell 命令，在仓库根目录下
    以 ``shell=True`` 执行。信任边界说明原属 Trellis 上游规范
    ``.trellis/spec/cli/backend/script-conventions.md``，本项目未提供该文件。

    设计上采用容错继续策略：钩子出错时告警，生命周期命令继续。
    告警包含事件、命令、退出状态及捕获的两个输出流，避免故障仅表现为
    “什么都没发生”而无法调试。挂起的钩子受 ``HOOK_TIMEOUT_SECONDS`` 限制；
    因为输出被捕获，若没有超时，用户只会看到任务命令既不返回也没有输出。
    超时时终止整个进程树（见 ``_kill_hook_tree``），并在有限宽限期内
    收集输出，避免仍占用管道的存活孙进程再次造成挂起。
    清理过程同样设时限，见 ``_release_hook_process``。

    参数：
        event: 事件名，例如 "after_create"。
        task_json_path: 任务 task.json 的绝对路径。
        repo_root: 用作 cwd 和配置查找起点的仓库根目录。
    """
    import os
    import subprocess

    from .config import get_hooks
    from .log import Colors, colored

    commands = get_hooks(event, repo_root)
    if not commands:
        return

    env = {**os.environ, "TASK_JSON_PATH": str(task_json_path)}

    # 仅 POSIX：新会话使 shell 及其全部后代处于同一进程组，
    # 这样超时处理才能终止整棵进程树。
    popen_kwargs: dict = {}
    if os.name == "posix":
        popen_kwargs["start_new_session"] = True

    for cmd in commands:
        proc = None
        try:
            proc = subprocess.Popen(
                cmd,
                shell=True,
                cwd=repo_root,
                env=env,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                encoding="utf-8",
                errors="replace",
                **popen_kwargs,
            )
            try:
                stdout, stderr = proc.communicate(timeout=HOOK_TIMEOUT_SECONDS)
            except subprocess.TimeoutExpired as e:
                _kill_hook_tree(proc)
                stdout = _decode_hook_output(e.stdout)
                stderr = _decode_hook_output(e.stderr)
                try:
                    # 限制等待时间：逃过终止的孤儿进程仍持有管道，
                    # 在此无限等待会重新造成超时机制要防止的挂起。
                    rest_out, rest_err = proc.communicate(
                        timeout=HOOK_KILL_GRACE_SECONDS
                    )
                    stdout = rest_out or stdout
                    stderr = rest_err or stderr
                except (subprocess.TimeoutExpired, OSError, ValueError):
                    pass
                print(
                    colored(
                        f"[WARN] 钩子超时（{event}），已等待"
                        f"{HOOK_TIMEOUT_SECONDS} 秒：{cmd}",
                        Colors.YELLOW,
                    ),
                    file=sys.stderr,
                )
                print(f"  cwd: {repo_root}", file=sys.stderr)
                _print_hook_stream("stdout", stdout)
                _print_hook_stream("stderr", stderr)
                continue

            if proc.returncode != 0:
                print(
                    colored(
                        f"[WARN] 钩子失败（{event}）：退出码 {proc.returncode}： {cmd}",
                        Colors.YELLOW,
                    ),
                    file=sys.stderr,
                )
                print(f"  cwd: {repo_root}", file=sys.stderr)
                _print_hook_stream("stdout", stdout or "")
                _print_hook_stream("stderr", stderr or "")
        except Exception as e:
            print(
                colored(
                    f"[WARN] 钩子错误（{event}）： {cmd} — {type(e).__name__}: {e}",
                    Colors.YELLOW,
                ),
                file=sys.stderr,
            )


# =============================================================================
# 主入口（供测试使用）
# =============================================================================

if __name__ == "__main__":
    repo = get_repo_root()
    tasks = get_tasks_dir(repo)

    print(f"任务根目录： {tasks}")
    print(f"resolve_task_dir('.trellis/tasks/test'): {resolve_task_dir('.trellis/tasks/test', repo)}")
    print(f"resolve_task_dir('../test'): {resolve_task_dir('../test', repo)}")
