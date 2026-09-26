"""Trellis 任务数据的核心类型定义。

提供：
    TaskData     — task.json 结构的 TypedDict（仅用于读取路径的类型提示）
    TaskInfo     — 已加载任务的不可变数据类（公共 API 类型）
    AgentRecord  — registry.json 中代理条目的 TypedDict
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import TypedDict


# =============================================================================
# task.json 结构（TypedDict，仅用于读取路径的类型提示）
# =============================================================================

class TaskData(TypedDict, total=False):
    """task.json 在磁盘上的结构。

    仅用于读取 task.json 时的类型注解。
    写入必须使用原始字典，避免丢失未知字段。
    """

    id: str
    name: str
    title: str
    description: str
    status: str
    dev_type: str
    scope: str | None
    package: str | None
    priority: str
    creator: str
    assignee: str
    createdAt: str
    completedAt: str | None
    branch: str | None
    base_branch: str | None
    worktree_path: str | None
    commit: str | None
    pr_url: str | None
    subtasks: list[str]
    children: list[str]
    parent: str | None
    relatedFiles: list[str]
    notes: str
    meta: dict


# =============================================================================
# 已加载的任务对象（不可变数据类，公共 API 类型）
# =============================================================================

@dataclass(frozen=True)
class TaskInfo:
    """已加载任务的不可变视图。

    由 load_task() / iter_active_tasks() 创建。
    包含常用字段；原始字典保存在 `raw` 中，用于回写和访问不常用字段。
    """

    dir_name: str
    directory: Path
    title: str
    status: str
    assignee: str
    priority: str
    children: tuple[str, ...]
    parent: str | None
    package: str | None
    raw: dict  # 原始字典，用于写入及访问不常用字段

    @property
    def name(self) -> str:
        """任务名称（id 或 name 字段）。"""
        return self.raw.get("name") or self.raw.get("id") or self.dir_name

    @property
    def description(self) -> str:
        return self.raw.get("description", "")

    @property
    def branch(self) -> str | None:
        return self.raw.get("branch")

    @property
    def meta(self) -> dict:
        return self.raw.get("meta", {})


# =============================================================================
# registry.json 代理条目
# =============================================================================

class AgentRecord(TypedDict, total=False):
    """registry.json 中代理条目的结构。"""

    id: str
    pid: int
    task_dir: str
    worktree_path: str
    branch: str
    platform: str
    started_at: str
    status: str
