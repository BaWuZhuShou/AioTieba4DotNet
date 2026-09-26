#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
任务管理脚本.

用法：
    python3 task.py create "<title>" --description "<desc>" [--slug <name>] [--assignee <dev>] [--priority P0|P1|P2|P3] [--parent <dir>] [--package <pkg>] [--no-start] [--force]
    python3 task.py add-context <dir> <file> <path> [reason] # 添加 JSONL 条目
    python3 task.py validate <dir>              # 校验 JSONL 文件
    python3 task.py list-context <dir>          # 列出 JSONL 条目
    python3 task.py start <dir>                 # 设置活跃任务并记录当前分支
    python3 task.py current [--source] [--json] # 显示活跃任务
    python3 task.py finish                      # 清除活跃任务
    python3 task.py set-branch <dir> <branch>   # 设置 Git 分支
    python3 task.py set-base-branch <dir> <branch>  # 设置 PR 目标分支
    python3 task.py set-scope <dir> <scope>     # 设置 PR 标题的范围
    python3 task.py set-meta <dir> <key> <value>  # 设置任务元数据键
    python3 task.py rename <dir> <new-slug> [--dry-run]  # 重命名任务及引用
    python3 task.py archive <task-dir> [--skip-branch-validation]  # 归档已完成任务
    python3 task.py list                        # 列出活跃任务
    python3 task.py list-archive [month]        # 列出归档任务
    python3 task.py add-subtask <parent-dir> <child-dir>     # 关联子任务与父任务
    python3 task.py remove-subtask <parent-dir> <child-dir>  # 解除子任务与父任务的关联
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from common.log import Colors, colored
from common.paths import (
    DEVELOPER_HINT,
    DIR_WORKFLOW,
    DIR_TASKS,
    FILE_TASK_JSON,
    get_repo_root,
    get_developer,
    get_tasks_dir,
    get_current_task,
)
from common.active_task import (
    clear_active_task,
    resolve_active_task,
    resolve_context_key,
    set_active_task,
)
from common.git import current_branch_name
from common.io import (
    describe_json_read_failure,
    read_json_checked,
    write_json,
)
from common.task_utils import resolve_task_dir, run_task_hooks
from common.tasks import iter_active_tasks, children_progress

# 从拆分模块导入命令处理器，同时重新导出以兼容 plan.py。
from common.task_store import (
    cmd_create,
    cmd_rename,
    cmd_archive,
    cmd_set_branch,
    cmd_set_base_branch,
    cmd_set_scope,
    cmd_set_meta,
    cmd_add_subtask,
    cmd_remove_subtask,
)
from common.task_context import (
    cmd_add_context,
    cmd_validate,
    cmd_list_context,
    curated_entry_count,
)


# =============================================================================
# 命令： start / finish
# =============================================================================

def _record_start_state(
    task_json_path: Path,
    repo_root: Path,
    label: str = "",
) -> None:
    """将刚启动的任务转为 in_progress，并记录分支。

    两个更新共用一次读写：从 planning 切换状态，以及在 `branch`
    仍为空时记录当前检出的分支。启动时记录能保证归档时 `branch` 可信；
    仅依赖手动设置分支的任务，往往直到归档时仍是 `branch: null`。

    这里有意容错：损坏的 task.json 不会让 `start` 失败，因为命令主要
    负责会话指针。但本次读取后会重写文件，因此不能静默失败；
    缺少提示时，未输出状态行会让人误以为任务原本就不在 planning。
    """
    data, reason = read_json_checked(task_json_path)
    if data is None:
        problem, hint = describe_json_read_failure(task_json_path, reason)
        print(
            colored(f"警告：{problem}；未更新 task.json。", Colors.YELLOW),
            file=sys.stderr,
        )
        print(hint, file=sys.stderr)
        return

    applied: list[str] = []

    if data.get("status") == "planning":
        data["status"] = "in_progress"
        applied.append(f"✓ 状态： planning → in_progress{label}")

    # 仅填写空字段：后续 `start` 必须保留显式 `set-branch` 设置的值；
    # 切换检出分支后重新启动任务是正常操作。
    base_branch_conflict: str | None = None
    if not data.get("branch"):
        branch = current_branch_name(repo_root)
        if branch:
            data["branch"] = branch
            applied.append(f"✓ 已记录分支： {branch}{label}")
            if branch == data.get("base_branch"):
                base_branch_conflict = branch
        else:
            print(
                colored(
                    "说明：没有已检出的分支（游离 HEAD，或不是 Git"
                    "仓库）；未记录任务分支。",
                    Colors.YELLOW,
                ),
                file=sys.stderr,
            )

    if not applied:
        return

    if not write_json(task_json_path, data):
        print(
            colored(
                f"警告：无法写入 {task_json_path}；"
                "状态和分支保持不变。",
                Colors.YELLOW,
            ),
            file=sys.stderr,
        )
        return

    for line in applied:
        print(colored(line, Colors.GREEN))

    if base_branch_conflict:
        # 仍然记录，因为此值真实，只是不能描述 PR。
        # 归档会拒绝这种情况，因此现在就说明，避免到门禁处才发现。
        print(
            colored(
                f"警告：'{base_branch_conflict}' 同时也是此任务的 base_branch；"
                "PR 不能指向自身分支，归档会拒绝此配置。",
                Colors.YELLOW,
            ),
            file=sys.stderr,
        )
        print(
            f"创建分支后运行： python3 {DIR_WORKFLOW}/scripts/task.py "
            "set-branch <task> <feature-branch>",
            file=sys.stderr,
        )


def cmd_start(args: argparse.Namespace) -> int:
    """设置活跃任务."""
    repo_root = get_repo_root()
    task_input = args.dir

    if not task_input:
        print(colored("错误：必须提供任务目录或名称", Colors.RED))
        return 1

    # 解析任务目录（支持任务名、相对路径或绝对路径）
    full_path = resolve_task_dir(task_input, repo_root)

    if full_path is None:
        # resolve_task_dir 已向 stderr 输出确切原因。
        # 若在 stdout 再补一行泛化提示，会将同一个诊断拆到两个输出流，
        # 并掩盖具体信息。
        return 1

    if not full_path.is_dir():
        print(colored(f"错误：找不到任务： {task_input}", Colors.RED))
        print("提示：请使用任务名（例如 'my-task'）或完整路径（例如 '.trellis/tasks/01-31-my-task'）")
        return 1

    # 上下文清单门禁（#573）：已创建但未整理的 implement/check 清单，
    # 会让该任务的所有子代理都没有规范上下文，后续也不会向主会话报告。
    # 缺失的清单不设门禁：create 仅在支持子代理的平台生成这些文件，
    # 缺失意味着没有子代理读取。
    if not getattr(args, "allow_empty_context", False):
        empty_manifests = [
            name
            for name in ("implement.jsonl", "check.jsonl")
            if curated_entry_count(full_path / name) == 0
        ]
        if empty_manifests:
            print(colored(
                f"错误：{'、'.join(empty_manifests)} 没有已整理条目",
                Colors.RED,
            ))
            print("子代理（implement/check）将没有任何规范上下文。")
            print(f"  整理：  python3 .trellis/scripts/task.py add-context {task_input} implement <path> \"<why>\"")
            print(f"  校验：  python3 .trellis/scripts/task.py validate {task_input}")
            print("  若有意留空，请加 --allow-empty-context 重新运行 start")
            return 1

    # 转为相对路径保存。full_path 已解析（resolve_task_dir 仅返回
    # 解析后根目录内的路径），因此 repo_root 也必须解析。
    # 否则遇到符号链接（例如 macOS 的 /tmp）会不匹配，误拒绝正常任务。
    try:
        task_dir = full_path.relative_to(repo_root.resolve()).as_posix()
    except ValueError:
        # resolve_task_dir 已拒绝仓库外路径，实际不会到达此处。
        # 此处仍拒绝，不回退到 str(full_path)：词法 relative_to() 配合
        # 绝对路径回退的模式，正是修复前让 `..` 引用逃逸并被存储的原因。
        print(colored(f"错误：找不到任务： {task_input}", Colors.RED))
        print("提示：请使用任务名（例如 'my-task'）或完整路径（例如 '.trellis/tasks/01-31-my-task'）")
        return 1

    task_json_path = full_path / FILE_TASK_JSON

    if not resolve_context_key():
        # 降级模式：无法取得会话身份。
        # 钩子没有注入 TRELLIS_CONTEXT_ID（常见于 Windows + Claude Code、
        # --continue 恢复、派生发行版或禁用钩子等）。跳过会话指针写入，
        # AI 继续依据对话上下文工作。
        print(colored(
            "ℹ 无法取得会话身份；本会话不保存活跃任务指针"
            "（降级模式）。AI 继续依据对话上下文工作。",
            Colors.YELLOW,
        ))
        print(colored(
            "提示：请在提供会话身份的 AI IDE 或会话中运行，"
            "或在运行 task.py start 前设置 TRELLIS_CONTEXT_ID。",
            Colors.YELLOW,
        ))

        # 仍将 task.json 状态由 planning 改为 in_progress，使后续阶段继续。
        if task_json_path.is_file():
            _record_start_state(task_json_path, repo_root, "（降级）")
            run_task_hooks("after_start", task_json_path, repo_root)
        return 0

    active = set_active_task(task_dir, repo_root)
    if active:
        print(colored(f"✓ 当前任务已设为： {task_dir}", Colors.GREEN))
        print(f"来源： {active.source}")

        if task_json_path.is_file():
            _record_start_state(task_json_path, repo_root)

        print()
        print(colored("钩子现在会从此任务的 JSONL 文件注入上下文。", Colors.BLUE))

        run_task_hooks("after_start", task_json_path, repo_root)
        return 0
    else:
        print(colored("错误：无法设置当前任务", Colors.RED))
        return 1


def cmd_finish(args: argparse.Namespace) -> int:
    """清除活跃任务."""
    repo_root = get_repo_root()
    active = clear_active_task(repo_root)
    current = active.task_path

    if not current:
        print(colored("尚未设置当前任务", Colors.YELLOW))
        return 0

    # 清除前解析 task.json 路径
    task_json_path = repo_root / current / FILE_TASK_JSON

    print(colored(f"✓ 已清除当前任务（原为：{current}）", Colors.GREEN))
    print(f"来源： {active.source}")

    if task_json_path.is_file():
        run_task_hooks("after_finish", task_json_path, repo_root)
    return 0


def cmd_current(args: argparse.Namespace) -> int:
    """显示活跃任务."""
    repo_root = get_repo_root()
    active = resolve_active_task(repo_root)

    if getattr(args, "json", False):
        task_obj = None
        read_error = None
        if active.task_path:
            task_json_path = repo_root / active.task_path / FILE_TASK_JSON
            data, reason = read_json_checked(task_json_path)
            if data is None:
                # 若没有这段处理，损坏的 task.json 会将每个字段都输出为 null，
                # 无法与字段确实为 null 的任务区分。
                problem, hint = describe_json_read_failure(task_json_path, reason)
                read_error = {
                    "file": str(task_json_path),
                    "reason": reason,
                    "message": f"{problem}. {hint}",
                }
                data = {}
            task_obj = {
                "dir": active.task_path,
                "id": data.get("id") or data.get("name"),
                "title": data.get("title"),
                "status": data.get("status"),
                "parent": data.get("parent"),
                "children": data.get("children", []),
                "branch": data.get("branch"),
                "base_branch": data.get("base_branch"),
            }
        payload = {
            "current_task": task_obj,
            "source": active.source,
            "stale": active.stale,
        }
        # 仅在读取失败时出现，保持正常输出结构不变。
        if read_error:
            payload["error"] = read_error
        print(json.dumps(payload, ensure_ascii=False))
        return 0 if active.task_path else 1

    if args.source:
        print(f"当前任务： {active.task_path or '（无）'}")
        print(f"来源： {active.source}")
        if active.stale:
            print("状态：已过期")
        return 0 if active.task_path else 1

    if active.task_path:
        print(active.task_path)
        return 0

    return 1


# =============================================================================
# 命令： list
# =============================================================================

def _display_status(t, all_statuses: dict) -> str:
    """返回 `list` 输出中任务的状态标签。

    除非直接对父任务执行 `task.py start`，其保存的状态一直是 planning，
    即使子任务正在推进。这会误导浏览列表的人（#399 第 3 项）。
    只要至少一个子任务已离开规划，就显示 active；不修改保存的状态值。
    """
    if t.status == "planning" and t.children:
        child_in_flight = any(
            all_statuses.get(c) not in (None, "planning") for c in t.children
        )
        if child_in_flight:
            return "active"
    return t.status


def cmd_list(args: argparse.Namespace) -> int:
    """列出活跃任务."""
    repo_root = get_repo_root()
    tasks_dir = get_tasks_dir(repo_root)
    current_task = get_current_task(repo_root)
    developer = get_developer(repo_root)
    filter_mine = args.mine
    filter_status = args.status
    as_json = getattr(args, "json", False)

    # 通过共享迭代器一次性收集所有任务
    all_tasks = {t.dir_name: t for t in iter_active_tasks(tasks_dir)}
    all_statuses = {name: t.status for name, t in all_tasks.items()}

    if as_json:
        if filter_mine and not developer:
            print(
                json.dumps({"error": "尚未设置开发者", "hint": DEVELOPER_HINT}),
                file=sys.stderr,
            )
            return 1

        items = []
        for dir_name in sorted(all_tasks.keys()):
            t = all_tasks[dir_name]
            if filter_mine and (t.assignee or "-") != developer:
                continue
            if filter_status and t.status != filter_status:
                continue
            items.append({
                "dir": f"{DIR_WORKFLOW}/{DIR_TASKS}/{dir_name}",
                "id": t.raw.get("id") or dir_name,
                "title": t.title,
                "status": t.status,
                "display_status": _display_status(t, all_statuses),
                "priority": t.priority,
                "assignee": t.assignee or None,
                "parent": t.parent,
                "children": list(t.children),
                "package": t.package,
            })
        print(json.dumps({"tasks": items}, ensure_ascii=False))
        return 0

    if filter_mine:
        if not developer:
            print(colored("错误：尚未设置开发者。请先运行 init_developer.py", Colors.RED), file=sys.stderr)
            print(DEVELOPER_HINT, file=sys.stderr)
            return 1
        print(colored(f"我的任务（负责人：{developer}）：", Colors.BLUE))
    else:
        print(colored("全部活跃任务：", Colors.BLUE))
    print()

    # 分层显示任务
    count = 0

    def _print_task(dir_name: str, indent: int = 0) -> None:
        nonlocal count
        t = all_tasks[dir_name]

        # 应用 --mine 筛选
        if filter_mine and (t.assignee or "-") != developer:
            return

        # 应用 --status 筛选
        if filter_status and t.status != filter_status:
            return

        relative_path = f"{DIR_WORKFLOW}/{DIR_TASKS}/{dir_name}"
        marker = ""
        if relative_path == current_task:
            marker = f" {colored('<- 当前', Colors.GREEN)}"

        # 子任务进度
        progress = children_progress(t.children, all_statuses)
        status_label = _display_status(t, all_statuses)

        # 包标签
        pkg_tag = f" @{t.package}" if t.package else ""

        prefix = "  " * indent + "  - "

        if filter_mine:
            print(f"{prefix}{dir_name}/ ({status_label}){pkg_tag}{progress}{marker}")
        else:
            print(f"{prefix}{dir_name}/ ({status_label}){pkg_tag}{progress} [{colored(t.assignee or '-', Colors.CYAN)}]{marker}")
        count += 1

        # 缩进显示子任务
        for child_name in t.children:
            if child_name in all_tasks:
                _print_task(child_name, indent + 1)

    # 仅显示顶层任务：没有父任务的任务，以及其父任务不在或已离开
    # 活跃集合的孤立任务。悬空父引用应平铺显示，不能让任务消失。
    for dir_name in sorted(all_tasks.keys()):
        parent = all_tasks[dir_name].parent
        if not parent or parent not in all_tasks:
            _print_task(dir_name)

    if count == 0:
        if filter_mine:
            print("  （没有分配给你的任务）")
        else:
            print("  （没有活跃任务）")

    print()
    print(f"合计：{count} 个任务")
    return 0


# =============================================================================
# 命令： list-archive
# =============================================================================

def cmd_list_archive(args: argparse.Namespace) -> int:
    """列出归档任务."""
    repo_root = get_repo_root()
    tasks_dir = get_tasks_dir(repo_root)
    archive_dir = tasks_dir / "archive"
    month = args.month

    print(colored("归档任务：", Colors.BLUE))
    print()

    if month:
        month_dir = archive_dir / month
        if month_dir.is_dir():
            print(f"[{month}]")
            for d in sorted(month_dir.iterdir()):
                if d.is_dir():
                    print(f"  - {d.name}/")
        else:
            print(f"  {month} 没有归档")
    else:
        if archive_dir.is_dir():
            for month_dir in sorted(archive_dir.iterdir()):
                if month_dir.is_dir():
                    month_name = month_dir.name
                    count = sum(1 for d in month_dir.iterdir() if d.is_dir())
                    print(f"[{month_name}] - {count} 个任务")

    return 0


# =============================================================================
# 帮助
# =============================================================================

def show_usage() -> None:
    """显示用法帮助。"""
    print("""任务管理脚本

用法：
  python3 task.py create <title> --description <desc>  创建新任务目录（标题和描述均必填、非空）
  python3 task.py create <title> --description <desc> --package <pkg>   为指定包创建任务
  python3 task.py create <title> --description <desc> --parent <dir>    创建父任务的子任务
  python3 task.py create <title> --description <desc> --no-start        创建任务但不在本会话激活
  python3 task.py add-context <dir> <jsonl> <path> [reason]  添加 JSONL 条目
  python3 task.py validate <dir>                     校验 JSONL 文件
  python3 task.py list-context <dir>                 列出 JSONL 条目
  python3 task.py start <dir>                        设置活跃任务；分支未设置时记录当前检出的分支
  python3 task.py current [--source]                 显示活跃任务
  python3 task.py finish                             清除活跃任务
  python3 task.py set-branch <dir> <branch>          设置 Git 分支
  python3 task.py set-base-branch <dir> <branch>     设置 PR 目标分支
  python3 task.py set-scope <dir> <scope>            设置 PR 标题的范围
  python3 task.py set-meta <dir> <key> <value>       设置或覆盖任务元数据键
  python3 task.py rename <dir> <new-slug>            重命名任务、身份字段及引用
  python3 task.py archive <task-dir>                 归档已完成任务
  python3 task.py add-subtask <parent> <child>       关联子任务与父任务
  python3 task.py remove-subtask <parent> <child>    解除子任务与父任务的关联
  python3 task.py list [--mine] [--status <status>] [--json]  列出任务
  python3 task.py list-archive [YYYY-MM]             列出归档任务

多包仓库选项：
  --package <pkg>      包名（依据 config.yaml 的 packages 校验）

重命名选项：
  --dry-run            仅显示变更清单，不写入内容

归档选项：
  --no-commit                跳过归档后的 Git 自动提交
  --skip-branch-validation   即使分支元数据缺失或指向自身也归档。
                             通常情况下，任务存在 base_branch、仓库存在远程但缺少 branch，
                             或 branch == base_branch 时，归档会拒绝执行。
                             应通过 set-branch / set-base-branch 修复。
                             此标志仅用于从未对应 PR 的任务。
                             已记录分支在合并后被删除时只告警，无需此标志。

列表选项：
  --mine, -m           仅显示分配给当前开发者的任务
  --status, -s <s>     按状态筛选（planning、in_progress、review、completed）
  --json               输出机器可读的 JSON（current 也支持）

示例：
  python3 task.py create "添加登录功能" --description "邮箱和密码登录" --slug add-login
  python3 task.py create "添加登录功能" --description "邮箱和密码登录" --slug add-login --package cli
  python3 task.py create "添加登录功能" --description "邮箱和密码登录" --slug add-login --meta linear=ENG-123 --meta epic=auth
  python3 task.py create "子任务" --description "处理会话 cookie" --slug child --parent .trellis/tasks/01-21-parent
  python3 task.py add-context <dir> implement .trellis/spec/backend/error-handling.md "错误处理规范"
  python3 task.py set-branch <dir> task/add-login
  python3 task.py start .trellis/tasks/01-21-add-login
  python3 task.py current --source
  python3 task.py finish
  python3 task.py rename add-login add-sso --dry-run  # 预览变更清单
  python3 task.py rename add-login add-sso
  python3 task.py archive add-login
  python3 task.py archive add-login --skip-branch-validation  # 任务从未有独立分支
  python3 task.py add-subtask parent-task child-task  # 关联已有任务
  python3 task.py remove-subtask parent-task child-task
  python3 task.py list                               # 列出全部活跃任务
  python3 task.py list --mine                        # 仅列出我的任务
  python3 task.py list --mine --status in_progress   # 列出我进行中的任务
""")


# =============================================================================
# 主入口
# =============================================================================

def main() -> int:
    """CLI 入口。"""
    # 弃用保护：`init-context` 已在 v0.5.0-beta.12 移除。
    # 提前检测，避免 argparse 用笼统的 "invalid choice" 错误掩盖真实原因。
    if len(sys.argv) >= 2 and sys.argv[1] == "init-context":
        print(
            colored(
                "错误：`task.py init-context` 已在 v0.5.0-beta.12 移除。",
                Colors.RED,
            ),
            file=sys.stderr,
        )
        print(
            "现在 `task.py create` 会为支持子代理的平台创建 implement.jsonl / check.jsonl，",
            file=sys.stderr,
        )
        print(
            "并在需要时由 AI 在规划阶段整理。",
            file=sys.stderr,
        )
        print("请参阅 .trellis/workflow.md 的规划产物指引，或运行：", file=sys.stderr)
        print(
            "  python3 ./.trellis/scripts/get_context.py --mode phase --step 1",
            file=sys.stderr,
        )
        print(
            "使用 `task.py add-context <dir> implement|check <path> <reason>` 添加条目。",
            file=sys.stderr,
        )
        return 2

    parser = argparse.ArgumentParser(
        description="任务管理脚本",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    subparsers = parser.add_subparsers(dest="command", help="命令")

    # create：创建任务
    p_create = subparsers.add_parser("create", help="创建新任务")
    p_create.add_argument("title", help="任务标题（必填，非空）")
    p_create.add_argument("--slug", "-s", help="任务英文 slug，不包含 MM-DD 日期前缀")
    p_create.add_argument("--assignee", "-a", help="负责人名称")
    p_create.add_argument("--priority", "-p", default="P2", help="优先级（P0-P3）")
    p_create.add_argument(
        "--description",
        "-d",
        help="任务描述（必填，非空；描述为空时无法归档）",
    )
    p_create.add_argument("--parent", help="父任务目录（建立子任务关联）")
    p_create.add_argument("--package", help="多包仓库项目的包名")
    p_create.add_argument(
        "--base-branch",
        help="PR 目标分支（覆盖 origin/HEAD 检测结果及当前分支回退值）",
    )
    p_create.add_argument(
        "--meta",
        action="append",
        help="任务元数据 key=value（可重复指定）",
    )
    p_create.add_argument(
        "--no-start",
        action="store_true",
        help="创建任务，但不将其设为本会话活跃任务",
    )
    p_create.add_argument(
        "--force",
        action="store_true",
        help="任务目录已存在时覆盖 task.json",
    )

    # add-context：添加上下文
    p_add = subparsers.add_parser("add-context", help="添加上下文条目")
    p_add.add_argument("dir", help="任务目录")
    p_add.add_argument("file", help="JSONL 文件（implement|check）")
    p_add.add_argument("path", help="要添加的文件路径")
    p_add.add_argument("reason", nargs="?", help="添加原因")

    # validate：校验上下文
    p_validate = subparsers.add_parser("validate", help="校验上下文文件")
    p_validate.add_argument("dir", help="任务目录")

    # list-context：列出上下文
    p_listctx = subparsers.add_parser("list-context", help="列出上下文条目")
    p_listctx.add_argument("dir", help="任务目录")

    # start：启动任务
    p_start = subparsers.add_parser("start", help="设置活跃任务")
    p_start.add_argument("dir", help="任务目录")
    p_start.add_argument(
        "--allow-empty-context",
        action="store_true",
        help="即使 implement.jsonl / check.jsonl 尚未整理条目，也启动任务",
    )

    # current：显示当前任务
    p_current = subparsers.add_parser("current", help="显示活跃任务")
    p_current.add_argument("--source", action="store_true",
                           help="显示活跃任务来源")
    p_current.add_argument("--json", action="store_true",
                           help="输出机器可读的 JSON")

    # finish：结束当前任务
    subparsers.add_parser("finish", help="清除活跃任务")

    # set-branch：设置分支
    p_branch = subparsers.add_parser("set-branch", help="设置 Git 分支")
    p_branch.add_argument("dir", help="任务目录")
    p_branch.add_argument("branch", help="分支名")

    # set-base-branch：设置基准分支
    p_base = subparsers.add_parser("set-base-branch", help="设置 PR 目标分支")
    p_base.add_argument("dir", help="任务目录")
    p_base.add_argument("base_branch", help="基准分支名（PR 目标）")

    # set-scope：设置范围
    p_scope = subparsers.add_parser("set-scope", help="设置范围")
    p_scope.add_argument("dir", help="任务目录")
    p_scope.add_argument("scope", help="范围名称")

    # set-meta：设置元数据
    p_setmeta = subparsers.add_parser("set-meta", help="设置或覆盖任务元数据键")
    p_setmeta.add_argument("dir", help="任务目录")
    p_setmeta.add_argument("key", help="元数据键")
    p_setmeta.add_argument("value", help="元数据值")

    # rename：重命名任务
    p_rename = subparsers.add_parser("rename", help="重命名任务及其引用")
    p_rename.add_argument("name", help="任务目录或名称")
    p_rename.add_argument("new_slug", help="新 slug，不包含 MM-DD 日期前缀")
    p_rename.add_argument(
        "--dry-run",
        action="store_true",
        help="仅显示变更清单，不写入内容",
    )

    # archive：归档任务
    p_archive = subparsers.add_parser("archive", help="归档任务")
    p_archive.add_argument("name", help="任务目录或名称")
    p_archive.add_argument("--no-commit", action="store_true", help="跳过归档后的 Git 自动提交")
    p_archive.add_argument(
        "--skip-branch-validation",
        action="store_true",
        help=(
            "即使分支元数据缺失或指向自身也归档"
            "（用于从未对应 PR 的任务）"
        ),
    )

    # list：列出任务
    p_list = subparsers.add_parser("list", help="列出任务")
    p_list.add_argument("--mine", "-m", action="store_true", help="仅列出我的任务")
    p_list.add_argument("--status", "-s", help="按状态筛选")
    p_list.add_argument("--json", action="store_true", help="输出机器可读的 JSON")

    # add-subtask：关联子任务
    p_addsub = subparsers.add_parser("add-subtask", help="关联子任务与父任务")
    p_addsub.add_argument("parent_dir", help="父任务目录")
    p_addsub.add_argument("child_dir", help="子任务目录")

    # remove-subtask：解除子任务关联
    p_rmsub = subparsers.add_parser("remove-subtask", help="解除子任务与父任务的关联")
    p_rmsub.add_argument("parent_dir", help="父任务目录")
    p_rmsub.add_argument("child_dir", help="子任务目录")

    # list-archive：列出归档
    p_listarch = subparsers.add_parser("list-archive", help="列出归档任务")
    p_listarch.add_argument("month", nargs="?", help="月份（YYYY-MM）")

    args = parser.parse_args()

    if not args.command:
        show_usage()
        return 1

    commands = {
        "create": cmd_create,
        "add-context": cmd_add_context,
        "validate": cmd_validate,
        "list-context": cmd_list_context,
        "start": cmd_start,
        "current": cmd_current,
        "finish": cmd_finish,
        "set-branch": cmd_set_branch,
        "set-base-branch": cmd_set_base_branch,
        "set-scope": cmd_set_scope,
        "set-meta": cmd_set_meta,
        "rename": cmd_rename,
        "archive": cmd_archive,
        "add-subtask": cmd_add_subtask,
        "remove-subtask": cmd_remove_subtask,
        "list": cmd_list,
        "list-archive": cmd_list_archive,
    }

    if args.command in commands:
        return commands[args.command](args)
    else:
        show_usage()
        return 1


if __name__ == "__main__":
    sys.exit(main())
