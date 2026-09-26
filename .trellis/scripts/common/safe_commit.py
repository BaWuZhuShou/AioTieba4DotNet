"""针对 Trellis 自有路径的安全 git-add 工具。

本模块的缘由
------------
曾有真实用户事件：项目的 `.gitignore` 因公司模板或个人习惯而忽略 `.trellis/`。
`add_session.py` 和 `task.py archive` 自动提交时，`git add` 报出
`ignored by .gitignore`，驱动工作流的 AI 用 `git add -f .trellis/` 重试“修复”，
结果把所有被忽略的子树（`.trellis/.backup-*/`、`.trellis/worktrees/`、
`.trellis/.template-hashes.json`、`.trellis/.runtime/`）一并纳入，提交了
548 个文件、83474 行缓存与备份。

设计
----
- 脚本只暂存明确的产物路径（日志文件、index.md、当前任务目录、归档目录），
  绝不暂存整个 `.trellis/` 树。
- 普通 `git add <specific>` 因 "ignored by" 失败时，禁止用 ``-f`` 重试。
  `.gitignore` 中的 `.trellis/` 表达用户希望仅在本地保留它的意图。
  脚本警告并跳过自动提交；需要自动暂存的用户可修正 `.gitignore`，或设置
  ``session_auto_commit: false`` 后自行管理 Git。
- 警告包含反例：“禁止使用 `git add -f .trellis/`”，避免读取日志的 AI 重犯错误。

历史说明：0.5.10 曾对明确路径自动用 ``git add -f`` 重试，0.5.11 已撤回。
即使路径列表很窄，强行加入用户已忽略的目录树仍违背用户意图。
粗粒度的禁用命令仍然禁止，细粒度的自动 ``-f`` 也已移除。
"""

from __future__ import annotations

import sys
from pathlib import Path

from .git import run_git, run_git_retry_index_lock
from .paths import (
    DIR_ARCHIVE,
    DIR_TASKS,
    DIR_WORKFLOW,
    DIR_WORKSPACE,
    FILE_JOURNAL_PREFIX,
    FILE_TASK_JSON,
    get_developer,
)


# 绝不能自动暂存的 .trellis/ 子路径。列在此处，便于警告提示用户逐项忽略
# 具体子路径，而非忽略整个 `.trellis/` 目录树。
TRELLIS_IGNORED_SUBPATHS = (
    ".trellis/.backup-*",
    ".trellis/worktrees/",
    ".trellis/.template-hashes.json",
    ".trellis/.runtime/",
    ".trellis/.cache/",
)


def safe_trellis_paths_to_add(
    repo_root: Path,
    task_name: str | None = None,
) -> list[str]:
    """返回自动提交应暂存的仓库相对路径列表。

    只包含磁盘上存在的路径，避免向 Git 传入不存在的参数。
    调用者负责随后使用 `git diff --cached` 检查。

    包含：
      - .trellis/workspace/<developer>/journal-*.md
      - .trellis/workspace/<developer>/index.md
      - .trellis/tasks/<task_name>/（传入 ``task_name`` 时仅当前任务目录；
        若任务已在 archive/ 下，也包含归档位置）

    有意排除（不得暂存）：
      - .trellis/.backup-*、.trellis/worktrees/、
        .trellis/.template-hashes.json、.trellis/.runtime/、.trellis/.cache/

    范围契约（见 #303 / break-loop 分析）：传入 ``task_name`` 时，任务部分只暂存该目录，
    绝不用 ``tasks_dir.iterdir()`` 遍历全部活动任务。这与 :func:`safe_archive_paths_to_add`
    一致，避免其他并行窗口任务的未提交修改混入会话自动提交。

    向后兼容：未传 ``task_name`` 时，保留旧版宽范围行为，遍历全部活动任务目录及归档子树。
    新调用者应始终传入 ``task_name``。
    """
    paths: list[str] = []

    # 工作区日志文件与 index.md
    developer = get_developer(repo_root)
    if developer:
        ws = repo_root / DIR_WORKFLOW / DIR_WORKSPACE / developer
        if ws.is_dir():
            for f in sorted(ws.glob(f"{FILE_JOURNAL_PREFIX}*.md")):
                if f.is_file():
                    paths.append(
                        f"{DIR_WORKFLOW}/{DIR_WORKSPACE}/{developer}/{f.name}"
                    )
            index_md = ws / "index.md"
            if index_md.is_file():
                paths.append(
                    f"{DIR_WORKFLOW}/{DIR_WORKSPACE}/{developer}/index.md"
                )

    tasks_dir = repo_root / DIR_WORKFLOW / DIR_TASKS
    if not tasks_dir.is_dir():
        return paths

    if task_name is not None:
        # 范围限制：仅当前任务目录（活动或已归档）。
        # 绝不使用 iterdir() 遍历所有任务，避免其他并行窗口的未提交任务变更
        # 混入会话自动提交。
        active_task = tasks_dir / task_name
        if active_task.is_dir():
            paths.append(f"{DIR_WORKFLOW}/{DIR_TASKS}/{task_name}")
        archived_task = tasks_dir / DIR_ARCHIVE / task_name
        if archived_task.is_dir():
            paths.append(
                f"{DIR_WORKFLOW}/{DIR_TASKS}/{DIR_ARCHIVE}/{task_name}"
            )
        return paths

    # 旧版宽范围（未传 task_name）：tasks/ 下除归档根目录外的每个直接子目录，
    # 再加上整个归档子树。
    for child in sorted(tasks_dir.iterdir()):
        if not child.is_dir():
            continue
        if child.name == DIR_ARCHIVE:
            continue
        paths.append(f"{DIR_WORKFLOW}/{DIR_TASKS}/{child.name}")

    archive_dir = tasks_dir / DIR_ARCHIVE
    if archive_dir.is_dir():
        paths.append(f"{DIR_WORKFLOW}/{DIR_TASKS}/{DIR_ARCHIVE}")

    return paths


def safe_archive_paths_to_add(
    repo_root: Path,
    modified_children: list[str] | None = None,
    *,
    archive_dest: Path,
) -> list[str]:
    """返回 `task.py archive` 后应暂存的路径。

    仅涉及归档操作实际触及的路径：

      - 归档子树（刚移动的任务所在位置）。
      - 源任务目录（用于源端删除；调用者配合 `git rm --cached`，因为源路径已不在
        工作树中时，`git add` 不会暂存其删除）。
      - 为移除已归档父任务而修改的子任务 `task.json`（更新父子关系）。

    这一范围限制避免把其他活动任务目录中并行窗口产生的未提交修改混入归档提交。
    调用者分别处理各类变更的提交边界。

    调用方必须传入移动操作返回的精确 ``archive_dest``，不能仅根据任务名
    推断月份或回退为整个归档树。
    """
    paths: list[str] = []
    tasks_dir = repo_root / DIR_WORKFLOW / DIR_TASKS
    if not tasks_dir.is_dir():
        return paths

    # 只纳入移动后的精确目标，源端删除由调用者单独处理。
    if archive_dest.is_dir():
        paths.append(archive_dest.relative_to(repo_root).as_posix())
    for child_name in modified_children or []:
        child_json = tasks_dir / child_name / FILE_TASK_JSON
        if child_json.is_file():
            paths.append(child_json.relative_to(repo_root).as_posix())
    return paths


def _stderr_indicates_ignored(stderr: str) -> bool:
    """判断 git add 错误是否表示路径被 .gitignore 排除。"""
    if not stderr:
        return False
    lowered = stderr.lower()
    return "ignored by" in lowered


def safe_git_add(
    paths: list[str], repo_root: Path, retry_on_index_lock: bool = False
) -> tuple[bool, bool, str]:
    """对指定路径运行 `git add`，绝不使用 -f 重试。

    返回 ``(success, used_force, stderr)``。为保持与 0.5.10 的签名兼容，保留
    ``used_force`` 字段，但始终为 ``False``，不会自动强制暂存。

    行为：
      - 未传路径 → 成功、未强制、stderr 为空。
      - 普通 ``git add -- <paths>`` 成功 → 返回成功。
      - 普通暂存失败（被忽略或任何其他原因）→ 返回失败及 stderr。
        调用者应检查 stderr（见 :func:`print_gitignore_warning`）并跳过自动提交。

    ``retry_on_index_lock`` 可启用 `.git/index.lock` 被占用时的有界退避重试
    （见 :func:`~.git.run_git_retry_index_lock`）。默认关闭：仅归档路径需要等待短暂锁，
    因为它在暂存时已将任务目录移动到新位置。
    """
    if not paths:
        return True, False, ""

    runner = run_git_retry_index_lock if retry_on_index_lock else run_git
    rc, _, err = runner(["add", "--", *paths], cwd=repo_root)
    if rc == 0:
        return True, False, ""
    return False, False, err


def print_gitignore_warning(paths: list[str]) -> None:
    """向用户以及读取日志的 AI 说明处理方式。

    关键：包含“禁止使用 `git add -f .trellis/`”这一反例。
    已知代理会自行想到该命令，进而纳入被忽略的缓存和备份。
    """
    print(
        "[WARN] git add 失败，因为 .trellis/ 路径被你的 .gitignore 忽略。",
        file=sys.stderr,
    )
    print(
        "[WARN] 已跳过自动提交。日志/任务文件仍已写入磁盘；",
        file=sys.stderr,
    )
    print(
        "[WARN] 尚未创建提交；git add 失败前可能已暂存部分路径，请检查 git status。",
        file=sys.stderr,
    )
    print("[WARN]", file=sys.stderr)
    print(
        "[WARN] Trellis 管理以下具体路径，建议将其纳入版本跟踪：",
        file=sys.stderr,
    )
    if paths:
        for p in paths:
            print(f"[WARN]   {p}", file=sys.stderr)
    else:
        print(
            "[WARN]   .trellis/workspace/<developer>/{journal-*.md,index.md}",
            file=sys.stderr,
        )
        print(
            "[WARN]   .trellis/tasks/<task-dir>/",
            file=sys.stderr,
        )
        print(
            "[WARN]   .trellis/tasks/archive/",
            file=sys.stderr,
        )
    print("[WARN]", file=sys.stderr)
    print(
        "[WARN] 建议在 .gitignore 中将 `.trellis/` 改为具体的",
        file=sys.stderr,
    )
    print(
        "[WARN] 应继续忽略的子路径，例如：",
        file=sys.stderr,
    )
    for sub in TRELLIS_IGNORED_SUBPATHS:
        print(f"[WARN]   {sub}", file=sys.stderr)
    print("[WARN]", file=sys.stderr)
    print(
        "[WARN] 若你有意仅在本地保留 .trellis/，请在以下文件中设置：",
        file=sys.stderr,
    )
    print(
        "[WARN] .trellis/config.yaml:",
        file=sys.stderr,
    )
    print(
        "[WARN]   session_auto_commit: false",
        file=sys.stderr,
    )
    print(
        "[WARN] 这样脚本将完全跳过 Git 操作，你可以使用",
        file=sys.stderr,
    )
    print(
        "[WARN] `git status` / `git add` / `git commit` 手动审阅和提交。",
        file=sys.stderr,
    )
    print("[WARN]", file=sys.stderr)
    print(
        "[WARN] 禁止使用 `git add -f .trellis/`，它会纳入备份、worktree",
        file=sys.stderr,
    )
    print(
        "[WARN] 以及绝不应提交的运行时缓存。",
        file=sys.stderr,
    )
