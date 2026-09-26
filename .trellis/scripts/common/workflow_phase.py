#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""工作流阶段提取。

从 .trellis/workflow.md 提取步骤级内容，可按平台过滤专属区块。

workflow.md 中的平台标记语法：

    [Claude Code, Cursor, ...]
    支持代理的平台专属内容
    [/Claude Code, Cursor, ...]

提供：
    get_phase_index  - 提取阶段索引（未传 --step 时）
    get_step         - 提取单个步骤（#### X.X）
    filter_platform  - 移除不包含指定平台的平台区块"""

from __future__ import annotations

import re

from .paths import DIR_WORKFLOW, get_repo_root


def _workflow_md_path():
    return get_repo_root() / DIR_WORKFLOW / "workflow.md"

# 匹配整行为平台标记的情况："[A, B, C]" 或 "[/A, B, C]"
_MARKER_RE = re.compile(r"^\[(/?)([A-Za-z][^\[\]]*)\]\s*$")

# 步骤标题："#### 1.0 标题" 或 "#### 1.0 ..."
_STEP_HEADING_RE = re.compile(r"^####\s+(\d+\.\d+)\b.*$")

# 阶段索引从此标题开始，到阶段 1 正文前结束；后续阶段正文按需提取。
_PHASE_INDEX_HEADINGS = {"## Phase Index", "## 阶段索引"}
_PHASE_ONE_HEADINGS = {"## Phase 1: Plan", "## 阶段 1：规划"}


def _read_workflow() -> str:
    path = _workflow_md_path()
    if not path.exists():
        raise FileNotFoundError(f"未找到 workflow.md： {path}")
    return path.read_text(encoding="utf-8")


def _parse_marker(line: str) -> tuple[bool, list[str]] | None:
    """解析平台标记行。

    返回：
        标记行返回 (is_closing, [platform_names])，否则返回 None。"""
    m = _MARKER_RE.match(line)
    if not m:
        return None
    is_closing = m.group(1) == "/"
    names = [p.strip() for p in m.group(2).split(",") if p.strip()]
    return is_closing, names


def get_phase_index() -> str:
    """返回 workflow.md 中的简要阶段索引。

    SessionStart 和未指定步骤的阶段上下文以此摘要说明流程。
    阶段 1/2/3 的详细指令通过 ``get_step`` 按需加载。
    ``[workflow-state:STATUS]`` 区块由逐轮 hook 消费，因此从此输出中移除。"""
    text = _read_workflow()
    lines = text.splitlines()

    start: int | None = None
    end: int | None = None
    for i, line in enumerate(lines):
        stripped = line.strip()
        if start is None and stripped in _PHASE_INDEX_HEADINGS:
            start = i
            continue
        if start is not None and stripped in _PHASE_ONE_HEADINGS:
            end = i
            break

    if start is None:
        return ""
    if end is None:
        end = len(lines)

    section = "\n".join(lines[start:end]).rstrip()
    # 移除 [workflow-state:STATUS]...[/workflow-state:STATUS] 区块，
    # 因为 inject-workflow-state.py 会逐轮单独注入它们。
    import re as _re
    tag_re = _re.compile(
        r"\[workflow-state:([A-Za-z0-9_-]+)\]\s*\n.*?\n\s*\[/workflow-state:\1\]\n?",
        _re.DOTALL,
    )
    return tag_re.sub("", section).rstrip() + "\n"


def get_step(step_id: str) -> str:
    """返回与 step_id 对应的 `#### X.X` 章节（标题和正文）。

    正文在下一个 `####`、`---` 或 `##` 标题处结束，以先出现者为准。"""
    text = _read_workflow()
    lines = text.splitlines()

    start: int | None = None
    for i, line in enumerate(lines):
        m = _STEP_HEADING_RE.match(line)
        if m and m.group(1) == step_id:
            start = i
            break
    if start is None:
        return ""

    end: int = len(lines)
    for j in range(start + 1, len(lines)):
        line = lines[j]
        if line.startswith("#### "):
            end = j
            break
        if line.startswith("## "):
            end = j
            break
        # 位于第 0 列的水平分隔线
        if line.strip() == "---":
            end = j
            break

    return "\n".join(lines[start:end]).rstrip() + "\n"


def _platform_matches(platform: str, block_names: list[str]) -> bool:
    """忽略大小写和分隔符匹配，接受 'cursor'、'Cursor'、'claude-code'、'Claude Code'。"""
    needle = platform.lower().replace("-", "").replace("_", "").replace(" ", "")
    for name in block_names:
        hay = name.lower().replace("-", "").replace("_", "").replace(" ", "")
        if needle == hay:
            return True
    return False


_PLATFORM_MARKER_LABELS: dict[str, str] = {
    # workflow.md 的平台标记使用产品名，而调用方传入稳定 ID
    # （start / continue 命令中的 `--platform {{CLI_FLAG}}`）。
    # `_platform_matches` 只去除分隔符，因此若 ID 与
    # 去掉空格后的标记名仍不同，将永远无法匹配，
    # `filter_platform` 会不报错地丢弃区块，只返回空章节。
    # 此表加入之前，曾有四个平台以这种错误状态发布。
    #
    # 平台 ID 与去除分隔符的标记名不同时，必须在此添加映射。
    # 上游 `test/registry-invariants.test.ts` 断言每个注册 ID
    # 均保留非空路由章节，因此缺失映射会在上游测试失败，
    # 而非静默清空该平台路由；本项目没有该上游测试文件。
    "claude": "Claude Code",
    "kimi": "Kimi Code",
    "omp": "Oh My Pi",
    "dsh": "DeepSeek Harness",
}


def resolve_effective_platform(platform: str, config: dict) -> str:
    """将 ``codex`` 映射为带派发模式的虚拟平台名。

    传入 ``--platform codex`` 时默认返回 ``codex-sub-agent``；
    在 ``.trellis/config.yaml`` 显式配置后返回 ``codex-inline``。
    ``sub-agent`` 仍是 ``auto`` 的别名。随后 ``filter_platform`` 保留标记中
    含该虚拟平台名的区块，例如 ``[codex-sub-agent, ...]`` 或
    ``[codex-inline, Kilo, Antigravity, Devin]``。

    Codex 原生上下文注入支持默认的 ``auto``。显式无效值安全回退为
    ``inline``；此渲染器也用于常规 CLI 输出，因此不会在这里输出警告。

    标记名称与平台 ID 不同的平台通过 ``_PLATFORM_MARKER_LABELS`` 解析；
    其余值原样返回。"""
    label = _PLATFORM_MARKER_LABELS.get(platform.strip().lower())
    if label:
        return label
    if platform == "codex":
        mode = "auto"
        codex_cfg = config.get("codex") if isinstance(config, dict) else None
        if codex_cfg is not None:
            if not isinstance(codex_cfg, dict):
                mode = "inline"
            else:
                cfg_mode = str(codex_cfg.get("dispatch_mode", mode)).strip().lower()
                if cfg_mode == "inline":
                    mode = "inline"
                elif cfg_mode in ("auto", "sub-agent"):
                    mode = "auto"
                else:
                    mode = "inline"
        return "codex-sub-agent" if mode == "auto" else "codex-inline"
    return platform


def filter_platform(content: str, platform: str) -> str:
    """保留 `[...]` 区块之外的行，以及包含指定平台的区块内的行。

    标记行本身不进入输出。"""
    lines = content.splitlines()
    out: list[str] = []

    in_block = False
    keep_block = False

    for line in lines:
        marker = _parse_marker(line)
        if marker is not None:
            is_closing, names = marker
            if not is_closing:
                in_block = True
                keep_block = _platform_matches(platform, names)
            else:
                in_block = False
                keep_block = False
            continue  # 移除标记行本身

        if in_block:
            if keep_block:
                out.append(line)
            continue
        out.append(line)

    # 合并移除标记后可能产生的三行及以上连续空行
    collapsed: list[str] = []
    blank_run = 0
    for line in out:
        if line.strip() == "":
            blank_run += 1
            if blank_run <= 2:
                collapsed.append(line)
        else:
            blank_run = 0
            collapsed.append(line)

    return "\n".join(collapsed).rstrip() + "\n"
