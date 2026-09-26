"""Git 命令执行工具。

所有 Trellis 脚本执行 Git 命令的统一入口。
"""

from __future__ import annotations

import subprocess
import time
from pathlib import Path


# 对短暂的 `.git/index.lock` 竞争进行有界重试。其他进程（IDE 的 Git 集成、
# 状态守护进程、并发 Trellis 会话）可能短暂持锁；约 1.5 秒内尝试三次，
# 足以等待短暂竞争，又不会因真正卡死的锁一直阻塞命令。
# 每次重试前等待一次，因此尝试次数由退避元组决定。
INDEX_LOCK_RETRY_BACKOFF = (0.5, 1.0)
INDEX_LOCK_RETRY_ATTEMPTS = len(INDEX_LOCK_RETRY_BACKOFF) + 1

# 脚本运行期间，检出目录是否为关联 worktree 不会改变，而查询需要两个子进程。
# 本地 `.developer` 缺失时，每个命令会多次解析开发者身份，因此缓存结果。
_CACHE_MISS = object()
_MAIN_WORKTREE_CACHE: dict[Path, Path | None] = {}


def run_git(
    args: list[str],
    cwd: Path | None = None,
    timeout: float | None = None,
) -> tuple[int, str, str]:
    """运行 Git 命令并返回 (returncode, stdout, stderr)。

    使用 UTF-8 编码与 -c i18n.logOutputEncoding=UTF-8，确保 Windows、macOS、Linux
    上的输出一致。调用者可为尽力而为的探测指定超时；常规 Git 操作默认不设超时。
    """
    try:
        git_args = ["git", "-c", "i18n.logOutputEncoding=UTF-8"] + args
        result = subprocess.run(
            git_args,
            cwd=cwd,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=timeout,
        )
        return result.returncode, result.stdout, result.stderr
    except Exception as e:
        return 1, "", str(e)


def stderr_indicates_index_lock(stderr: str) -> bool:
    """判断 Git 是否因其他进程持有 `.git/index.lock` 而失败。"""
    if not stderr:
        return False
    return "index.lock" in stderr.lower()


def run_git_retry_index_lock(
    args: list[str],
    cwd: Path | None = None,
    timeout: float | None = None,
) -> tuple[int, str, str]:
    """运行 Git 命令，仅在 `.git/index.lock` 被占用时重试。

    其他非零退出立即返回：对真实失败（错误路径、无内容可提交、钩子拒绝）重试只会
    延迟报错。返回最后一次的 (returncode, stdout, stderr)。
    """
    rc, out, err = run_git(args, cwd=cwd, timeout=timeout)
    attempt = 1
    while (
        rc != 0
        and attempt < INDEX_LOCK_RETRY_ATTEMPTS
        and stderr_indicates_index_lock(err)
    ):
        time.sleep(INDEX_LOCK_RETRY_BACKOFF[attempt - 1])
        attempt += 1
        rc, out, err = run_git(args, cwd=cwd, timeout=timeout)
    return rc, out, err


def index_lock_path(repo_root: Path) -> str:
    """返回 Git 正在争用的锁文件路径，用于诊断。

    向 Git 查询真实路径，兼容 worktree 与 `GIT_DIR` 设置，避免猜测并不存在的 `.git/` 路径。
    """
    rc, out, _ = run_git(["rev-parse", "--git-path", "index.lock"], cwd=repo_root)
    if rc == 0 and out.strip():
        return out.strip()
    return ".git/index.lock"


def resolve_default_branch(repo_root: Path) -> str | None:
    """解析仓库默认分支（origin/HEAD 的目标）。

    先尝试本地 `refs/remotes/origin/HEAD` 符号引用（不访问网络），再回退到
    `git remote show origin`（可能访问网络，也可修复缺失或陈旧的符号引用）。
    均无法解析时返回 None，供调用者保留原有回退行为。
    """
    rc, out, _ = run_git(["symbolic-ref", "refs/remotes/origin/HEAD"], cwd=repo_root)
    if rc == 0 and out.strip():
        return out.strip().rsplit("/", 1)[-1]

    rc, out, _ = run_git(["remote", "show", "origin"], cwd=repo_root)
    if rc == 0:
        for line in out.splitlines():
            line = line.strip()
            if line.startswith("HEAD branch:"):
                branch = line.split(":", 1)[1].strip()
                if branch and branch != "(unknown)":
                    return branch

    return None


def current_branch_name(repo_root: Path) -> str | None:
    """返回当前检出的分支名；不存在时返回 None。

    空输出同时涵盖分离 HEAD 和非 Git 仓库两种情况，调用者都视为没有可记录的分支。
    """
    rc, out, _ = run_git(["branch", "--show-current"], cwd=repo_root)
    if rc != 0:
        return None
    return out.strip() or None


def has_git_remote(repo_root: Path) -> bool:
    """判断仓库是否至少配置了一个远程。"""
    rc, out, _ = run_git(["remote"], cwd=repo_root)
    return rc == 0 and bool(out.strip())


def main_worktree_root(repo_root: Path) -> Path | None:
    """当 `repo_root` 是关联 worktree 时，返回主工作树的根路径。

    在主工作树本身、Git 仓库之外，或裸仓库的关联 worktree 中返回 None
    （裸仓库没有可指向的主检出目录）。

    `git worktree list --porcelain` 的首条记录是主工作树，因此由 Git 识别它，
    而不是从 `.git` 布局推导。取 `--git-common-dir` 的父目录这一推导方式会误判
    恰好位于无关仓库内的裸仓库（例如 `~/repos/project.git`，而 `~/repos` 自身
    也是仓库）：父目录是带真实 `.developer` 的检出目录，误判与命中无法区分，
    开发者身份便会跨仓库泄漏。
    """
    cached = _MAIN_WORKTREE_CACHE.get(repo_root, _CACHE_MISS)
    if cached is not _CACHE_MISS:
        return cached  # type: ignore[return-value]

    result = _probe_main_worktree_root(repo_root)
    _MAIN_WORKTREE_CACHE[repo_root] = result
    return result


def _probe_main_worktree_root(repo_root: Path) -> Path | None:
    rc_list, listing, _ = run_git(["worktree", "list", "--porcelain"], cwd=repo_root)
    rc_top, toplevel, _ = run_git(["rev-parse", "--show-toplevel"], cwd=repo_root)
    if rc_list != 0 or rc_top != 0:
        return None

    lines = listing.splitlines()
    if not lines or not lines[0].startswith("worktree "):
        return None

    # 记录以空行分隔；首条记录含 `bare` 属性，表示“主工作树”是裸仓库，
    # 没有可继承的检出目录。
    for line in lines[1:]:
        if not line.strip():
            break
        if line.strip() == "bare":
            return None

    try:
        main_root = Path(lines[0][len("worktree ") :].strip()).resolve()
        current_root = Path(toplevel.strip()).resolve()
    except (OSError, ValueError):
        return None

    if main_root == current_root:
        return None
    return main_root


def branch_exists_locally(branch: str, repo_root: Path) -> bool:
    """检查仓库中是否存在指定的本地分支引用。"""
    if not branch:
        return False
    rc, _, _ = run_git(
        ["rev-parse", "--verify", "--quiet", f"refs/heads/{branch}"],
        cwd=repo_root,
    )
    return rc == 0
