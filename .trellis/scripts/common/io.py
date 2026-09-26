"""文件 I/O 工具。

read_json / write_json 是 JSON 文件操作的统一入口；write_text_atomic 用于保存
持久会话状态的 Markdown 文件（journal、index.md）。
"""

from __future__ import annotations

import json
import os
import tempfile
from pathlib import Path


JSON_READ_MISSING = "missing"
JSON_READ_INVALID = "invalid"
JSON_READ_UNREADABLE = "unreadable"
JSON_READ_NOT_OBJECT = "not-object"
JSON_READ_EMPTY = "empty"
JSON_READ_UNDECODABLE = "undecodable"


def read_json(path: Path) -> dict | None:
    """读取并解析 JSON 文件。

    文件不存在、JSON 无效或无法读取时返回 None。
    仅用于可选读取；准备覆盖文件，或需要区分解析错误与权限错误时，应使用 read_json_checked。
    """
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (FileNotFoundError, json.JSONDecodeError, OSError, UnicodeDecodeError):
        # UnicodeDecodeError 不是 OSError。若不捕获，非 UTF-8 会话文件会突破宽容读取，
        # 使钩子路径失败，而不能降级为“无活动任务”。
        return None


def read_json_checked(path: Path) -> tuple[dict | None, str | None]:
    """读取 JSON 对象，并区分各种失败原因。

    成功返回 ``(data, None)``，失败返回 ``(None, reason)``，其中 reason 是
    ``JSON_READ_*`` 常量之一。空对象也视为失败：解析为 ``{}`` 的状态文件不含调用者
    要读取的字段，若视为成功，就会静默地用默认值重建状态。
    """
    try:
        text = path.read_text(encoding="utf-8")
    except FileNotFoundError:
        return None, JSON_READ_MISSING
    except UnicodeDecodeError:
        # 它不是 OSError，过去会越过两个处理器并显示回溯。此读取器要求每种失败
        # 都有可识别的原因；“无效 UTF-8”与“无效 JSON”的修复方式不同。
        return None, JSON_READ_UNDECODABLE
    except OSError:
        return None, JSON_READ_UNREADABLE

    try:
        data = json.loads(text)
    except json.JSONDecodeError:
        return None, JSON_READ_INVALID

    if not isinstance(data, dict):
        return None, JSON_READ_NOT_OBJECT
    if not data:
        return None, JSON_READ_EMPTY
    return data, None


def describe_json_read_failure(path: Path, reason: str | None) -> tuple[str, str]:
    """根据 read_json_checked 的原因返回 ``(发生了什么, 如何处理)``。"""
    if reason == JSON_READ_MISSING:
        return (f"{path}：文件不存在", "请传入现有任务目录，或先创建任务。")
    if reason == JSON_READ_UNREADABLE:
        return (
            f"{path}：无法读取（权限不足或 I/O 错误）",
            "请检查文件与目录权限后重试。",
        )
    if reason == JSON_READ_INVALID:
        return (
            f"{path}：不是有效的 JSON",
            f"请修复语法后重试（可用 `python3 -m json.tool {path}` 检查）。",
        )
    if reason == JSON_READ_NOT_OBJECT:
        return (
            f"{path}：顶层不是 JSON 对象",
            "请将文件恢复为 JSON 对象（{ ... }）后重试。",
        )
    if reason == JSON_READ_EMPTY:
        return (
            f"{path}：包含空 JSON 对象",
            "请恢复任务字段（或重新创建任务）后重试。",
        )
    if reason == JSON_READ_UNDECODABLE:
        return (
            f"{path}：不是有效的 UTF-8 文本",
            "请以 UTF-8 重新保存文件（或从 Git 恢复）后重试。",
        )
    return (f"{path}：无法加载", "请检查文件后重试。")


def write_json(path: Path, data: dict) -> bool:
    """将字典写入格式化的 JSON 文件。

    写入为原子操作：先写入同目录的临时文件，再重命名以替换目标。写入中途崩溃或
    按 Ctrl-C 会保留原文件，而不会截断它，避免损坏的 task.json 使任务从
    `task.py list` 中静默消失。

    成功返回 True，出错返回 False。
    """
    return write_text_atomic(path, json.dumps(data, indent=2, ensure_ascii=False))


def write_text_atomic(path: Path, text: str) -> bool:
    """原子写入文本文件（同目录临时文件，然后替换）。

    对保存持久会话状态的 Markdown 文件（journal、index.md）提供与 :func:`write_json`
    相同的不原地截断保证。写入中途崩溃或按 Ctrl-C 会保留原内容，避免产生无法在重试时
    判定状态的半条记录。

    成功返回 True，出错返回 False。
    """
    try:
        fd, tmp = tempfile.mkstemp(
            dir=str(path.parent), prefix=f".{path.name}.", suffix=".tmp"
        )
    except OSError:
        return False

    try:
        try:
            f = os.fdopen(fd, "w", encoding="utf-8")
        except OSError:
            # fdopen 未接管 fd，需要自行关闭。
            os.close(fd)
            raise
        with f:
            f.write(text)
        os.replace(tmp, path)
        return True
    except OSError:
        try:
            os.unlink(tmp)
        except OSError:
            pass
        return False
    except BaseException:
        # 写入时按 Ctrl-C：删除临时文件，再传播中断。
        try:
            os.unlink(tmp)
        except OSError:
            pass
        raise
