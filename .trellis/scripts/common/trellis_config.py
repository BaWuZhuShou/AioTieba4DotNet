#!/usr/bin/env python3
"""独立的 .trellis/config.yaml 读取器。

提供 Trellis 共用的最小 YAML 解析器。``common.config`` 从这里导入
``parse_simple_yaml``，不另存副本。本模块不导入包内其他模块，钩子可单独加载，
也避免两个解析器发生偏差。文件缺失或格式错误时返回空字典，简化调用者逻辑。

支持的子集：``key: value`` 标量（所有值均为字符串）、按缩进嵌套的映射、
``- `` 标量列表、``#`` 注释（整行和引号外的行内注释），以及一层成对外引号。
不支持的结构（块标量、锚点、别名、合并键、流式集合、列表内嵌套映射）会向 stderr
报告并跳过，避免误解析为看似合理的错误值。
"""

from __future__ import annotations

import sys
from pathlib import Path
from typing import Optional


CONFIG_REL_PATH = ".trellis/config.yaml"


def _unquote(value: str) -> str:
    if len(value) >= 2 and value[0] == value[-1] and value[0] in ('"', "'"):
        return value[1:-1]
    return value


def _strip_inline_comment(value: str) -> str:
    """去除 ` # …` 行内注释，同时保留引号内的 `#`。

    YAML 将 ` #`（空格加井号）视为注释起点；标记内单独的 `#` 属于值的一部分。
    引号内的字符串不受影响。
    """
    in_quote: str | None = None
    for idx, ch in enumerate(value):
        if in_quote:
            if ch == in_quote:
                in_quote = None
            continue
        if ch in ('"', "'"):
            in_quote = ch
            continue
        if ch == "#" and (idx == 0 or value[idx - 1].isspace()):
            return value[:idx]
    return value


def _next_content_line(lines: list[str], start: int) -> tuple[int, str]:
    i = start
    while i < len(lines):
        stripped = lines[i].strip()
        if stripped and not stripped.startswith("#"):
            return i, lines[i]
        i += 1
    return i, ""


def _warn_unsupported(source: str, lineno: int, line: str, reason: str) -> None:
    """报告解析器无法表示的 YAML 结构，然后继续。"""
    print(
        f"[WARN] {source}:{lineno}: {reason}；已忽略：{line.strip()}",
        file=sys.stderr,
    )


def _is_block_scalar(value: str) -> bool:
    """对 ``|``、``>`` 及其换行截取/缩进指示符（``|-``、``>2``）返回 True。"""
    if not value or value[0] not in ("|", ">"):
        return False
    return all(ch in "+-0123456789" for ch in value[1:])


def _unsupported_value(key: str, value: str) -> str | None:
    """返回未加引号的标量值中不支持的结构名称，否则返回 None。

    只检查未加引号的值：``cmd: "[a] | b"`` 是用户明确写出的字符串，而裸写的
    ``notes: |`` 或 ``base: *anchor`` 若不处理，会把标记本身存入值并丢失真实内容。
    """
    if key == "<<":
        return "不支持 YAML 合并键"
    if _is_block_scalar(value):
        return "不支持块标量"
    if value.startswith("&"):
        return "不支持 YAML 锚点"
    if value.startswith("*"):
        return "不支持 YAML 别名"
    if value.startswith("["):
        return "不支持流式序列（请使用 `- ` 列表项）"
    if value.startswith("{"):
        return "不支持流式映射（请使用缩进映射）"
    return None


def _skip_indented_body(lines: list[str], start: int, indent: int) -> int:
    """跳过被拒绝的键所带的续行（如块标量正文）。"""
    i = start
    while i < len(lines):
        stripped = lines[i].strip()
        if stripped and len(lines[i]) - len(lines[i].lstrip()) <= indent:
            break
        i += 1
    return i


def _parse_yaml_block(
    lines: list[str], start: int, min_indent: int, target: dict, source: str
) -> int:
    i = start
    current_list: list | None = None
    # 保存开启 current_list 的键所在缩进，用于识别更深的 `key: value`
    # 是否属于列表内的映射。
    list_owner_indent = 0

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        if not stripped or stripped.startswith("#"):
            i += 1
            continue

        indent = len(line) - len(line.lstrip())
        if indent < min_indent:
            break

        if stripped.startswith("- "):
            if current_list is not None:
                current_list.append(_unquote(stripped[2:].strip()))
            i += 1
        elif ":" in stripped:
            if current_list is not None and indent > list_owner_indent:
                # `- name: cli` / `  path: x`：第二个键属于列表内的映射。
                # 若保存它，会将它提升到父字典中与列表同级，使嵌套键静默变为根键。
                _warn_unsupported(
                    source,
                    i + 1,
                    line,
                    "不支持列表内的映射",
                )
                i += 1
                continue

            key, _, value = stripped.partition(":")
            key = key.strip()
            value = _strip_inline_comment(value).strip()
            was_quoted = len(value) >= 2 and value[0] == value[-1] and value[0] in ('"', "'")

            if not was_quoted:
                reason = _unsupported_value(key, value)
                if reason is not None:
                    _warn_unsupported(source, i + 1, line, reason)
                    current_list = None
                    i = _skip_indented_body(lines, i + 1, indent)
                    continue

            value = _unquote(value)
            current_list = None

            if value or was_quoted:
                target[key] = value
                i += 1
            else:
                next_i, next_line = _next_content_line(lines, i + 1)
                if next_i >= len(lines):
                    target[key] = {}
                    i = next_i
                elif next_line.strip().startswith("- "):
                    current_list = []
                    list_owner_indent = indent
                    target[key] = current_list
                    i += 1
                else:
                    next_indent = len(next_line) - len(next_line.lstrip())
                    if next_indent > indent:
                        nested: dict = {}
                        target[key] = nested
                        i = _parse_yaml_block(lines, i + 1, next_indent, nested, source)
                    else:
                        target[key] = {}
                        i += 1
        else:
            i += 1

    return i


def parse_simple_yaml(content: str, source: str = "config.yaml") -> dict:
    """解析支持嵌套字典的简单 YAML（无依赖）。

    支持：
        - key: value（字符串）
        - key:（后接列表项）
            - item1
            - item2
        - key:（后接嵌套字典）
            nested_key: value
            nested_key2:
              - item

    根据缩进检测嵌套（多 2 个及以上空格为子级）。所有值都是字符串，由消费者转换。
    不支持的结构会向 stderr 报告 ``source`` 来源并跳过，详见模块文档字符串。

    参数：
        content: YAML 内容字符串。
        source: 警告中使用的来源标签，通常为配置文件路径。

    返回：
        解析后的字典（值可以是 str、list[str] 或 dict）。
    """
    lines = content.splitlines()
    result: dict = {}
    _parse_yaml_block(lines, 0, 0, result, source)
    return result


def read_trellis_config(repo_root: Optional[Path] = None) -> dict:
    """读取 .trellis/config.yaml；文件缺失或格式错误时返回 {}。"""
    root = repo_root or Path.cwd()
    config_file = root / CONFIG_REL_PATH
    try:
        content = config_file.read_text(encoding="utf-8")
    except (FileNotFoundError, OSError):
        return {}
    try:
        parsed = parse_simple_yaml(content, source=str(config_file))
    except Exception:
        return {}
    return parsed if isinstance(parsed, dict) else {}
