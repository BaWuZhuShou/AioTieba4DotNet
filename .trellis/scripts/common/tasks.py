"""
任务数据访问层。

统一负责加载和遍历任务目录，
替代分散在九个以上文件中的 task.json 解析逻辑。

提供功能：
    load_task          — 按目录路径加载单个任务
    iter_active_tasks  — 按顺序遍历所有未归档任务
    get_all_statuses   — 获取子任务进度所需的 {dir_name: status} 映射
"""

from __future__ import annotations

import sys
from collections.abc import Iterator
from pathlib import Path

from .io import describe_json_read_failure, read_json_checked
from .paths import FILE_TASK_JSON
from .types import TaskInfo


def load_task(task_dir: Path) -> TaskInfo | None:
    """从包含 task.json 的目录加载任务。

    参数：
        task_dir: 任务目录的绝对路径。

    返回值：
        task.json 存在且有效时返回 TaskInfo，否则返回 None。

    没有 task.json 的目录不视为任务，直接跳过。
    task.json 存在但无法加载的情况不同：该任务会从 `task.py list`
    以及此迭代器提供的所有上下文中消失。调用者继续容错，
    但通过 stderr 报告跳过原因，避免任务从工作流中无声消失。
    """
    task_json = task_dir / FILE_TASK_JSON
    if not task_json.is_file():
        return None

    data, reason = read_json_checked(task_json)
    if data is None:
        problem, hint = describe_json_read_failure(task_json, reason)
        print(f"[WARN] 跳过任务 '{task_dir.name}': {problem}", file=sys.stderr)
        print(f"       {hint}", file=sys.stderr)
        return None

    return TaskInfo(
        dir_name=task_dir.name,
        directory=task_dir,
        title=data.get("title") or data.get("name") or "未知",
        status=data.get("status", "unknown"),
        assignee=data.get("assignee", ""),
        priority=data.get("priority", "P2"),
        children=tuple(data.get("children", [])),
        parent=data.get("parent"),
        package=data.get("package"),
        raw=data,
    )


def iter_active_tasks(tasks_dir: Path) -> Iterator[TaskInfo]:
    """按目录名顺序遍历所有活跃（未归档）任务。

    跳过 "archive" 目录和没有有效 task.json 的目录。

    参数：
        tasks_dir: 任务根目录路径。

    产出：
        每个有效任务的 TaskInfo。
    """
    if not tasks_dir.is_dir():
        return

    for d in sorted(tasks_dir.iterdir()):
        if not d.is_dir() or d.name == "archive":
            continue
        info = load_task(d)
        if info is not None:
            yield info


def get_all_statuses(tasks_dir: Path) -> dict[str, str]:
    """获取所有活跃任务的 {dir_name: status} 映射。

    用于计算子任务进度，无需加载完整 TaskInfo。

    参数：
        tasks_dir: 任务根目录路径。

    返回值：
        目录名到状态字符串的映射字典。
    """
    return {t.dir_name: t.status for t in iter_active_tasks(tasks_dir)}


def children_progress(
    children: tuple[str, ...] | list[str],
    all_statuses: dict[str, str],
) -> str:
    """格式化子任务进度字符串，例如 " [已完成 2/3]"。

    参数：
        children: 子任务目录名列表。
        all_statuses: get_all_statuses() 返回的状态映射。

    返回值：
        格式化字符串，没有子任务时返回 ""。
    """
    if not children:
        return ""
    # 活跃状态中缺失的子任务已归档（cmd_archive 会在移动目录之前
    # 设置 status=completed）。将其计为已完成，避免归档子任务后
    # 父任务进度倒退。
    done = sum(
        1 for c in children
        if c not in all_statuses or all_statuses.get(c) in ("completed", "done")
    )
    return f" [已完成 {done}/{len(children)}]"
