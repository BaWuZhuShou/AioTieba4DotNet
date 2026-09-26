#!/usr/bin/env python3
"""任务创建、读取、更新和删除操作。

提供功能：
    ensure_tasks_dir    - 确保任务根目录存在
    cmd_create          - 创建新任务
    cmd_rename          - 重命名任务及其全部引用
    cmd_archive         - 归档已完成任务
    cmd_set_branch      - 设置任务的 Git 分支
    cmd_set_base_branch - 设置 PR 目标分支
    cmd_set_scope       - 设置 PR 标题的范围
    cmd_set_meta        - 设置或覆盖任务元数据键
    cmd_add_subtask     - 将子任务关联到父任务
    cmd_remove_subtask  - 解除子任务与父任务的关联
"""

from __future__ import annotations

import argparse
import re
import shlex
import sys
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path

from .config import (
    get_codex_dispatch_mode,
    get_packages,
    get_session_auto_commit,
    is_monorepo,
    resolve_package,
    validate_package,
)
from .git import (
    INDEX_LOCK_RETRY_ATTEMPTS,
    branch_exists_locally,
    has_git_remote,
    index_lock_path,
    resolve_default_branch,
    run_git,
    run_git_retry_index_lock,
    stderr_indicates_index_lock,
)
from .io import describe_json_read_failure, read_json_checked, write_json
from .log import Colors, colored
from .paths import (
    DEVELOPER_HINT,
    DIR_ARCHIVE,
    DIR_TASKS,
    DIR_WORKFLOW,
    FILE_TASK_JSON,
    generate_task_date_prefix,
    get_developer,
    get_repo_root,
    get_tasks_dir,
)
from .safe_commit import (
    print_gitignore_warning,
    safe_archive_paths_to_add,
    safe_git_add,
)
from .task_utils import (
    archive_destination_for,
    archive_task_complete,
    find_task_by_name,
    is_within_tasks_dir,
    resolve_task_dir,
    run_task_hooks,
)


# =============================================================================
# 辅助函数
# =============================================================================

# 判断标题或描述是否为空前需要移除的字符。
# 这里有意使用 Python str.strip() 默认字符集与 ECMAScript String.trim()
# 字符集的并集，因为二者并不相同：Python 移除 U+0085（NEL）及
# U+001C-U+001F，JS 保留；JS 移除 U+FEFF（BOM），Python 保留。
# 移除并集可保证 create 接受的内容在任一端去空白后仍非空，
# 避免记录在这里通过，随后被 JS 归档前校验器判为空。
BLANK_CHARS = (
    "\t\n\v\f\r"                              # U+0009-U+000D
    "\x1c\x1d\x1e\x1f"                        # U+001C-U+001F 分隔符（仅 Python）
    " "                                       # U+0020 空格
    "\x85"                                    # U+0085 换行符（仅 Python）
    "\xa0"                                    # U+00A0 不换行空格
    "\u1680"                                  # 欧甘空格标记
    "\u2000\u2001\u2002\u2003\u2004\u2005"    # en/em 方空格，en/em/三分之一/四分之一 em 空格
    "\u2006\u2007\u2008\u2009\u200a"          # 六分之一 em 空格到极细空格
    "\u2028\u2029"                            # 行分隔符和段落分隔符
    "\u202f\u205f\u3000"                      # 窄不换行空格、数学空格、表意空格
    "\ufeff"                                  # 零宽不换行空格 / BOM（仅 JS）
)


def strip_blank(value: str | None) -> str:
    """移除 ``value`` 两端 :data:`BLANK_CHARS` 中的字符。"""
    return (value or "").strip(BLANK_CHARS)


def _slugify(title: str) -> str:
    """将标题转为 slug（仅适用于 ASCII）。"""
    result = title.lower()
    result = re.sub(r"[^a-z0-9]", "-", result)
    result = re.sub(r"-+", "-", result)
    result = result.strip("-")
    return result


def ensure_tasks_dir(repo_root: Path) -> Path:
    """确保任务根目录存在。"""
    tasks_dir = get_tasks_dir(repo_root)
    archive_dir = tasks_dir / "archive"

    if not tasks_dir.exists():
        tasks_dir.mkdir(parents=True)
        print(colored(f"已创建任务根目录： {tasks_dir}", Colors.GREEN), file=sys.stderr)

    if not archive_dir.exists():
        archive_dir.mkdir(parents=True)

    return tasks_dir


def _find_archived_task_by_dir_name(tasks_dir: Path, dir_name: str) -> Path | None:
    """按活跃任务的精确目录名查找归档目录。"""
    archive_dir = tasks_dir / DIR_ARCHIVE
    if not archive_dir.is_dir():
        return None

    for month_dir in sorted(archive_dir.iterdir()):
        if not month_dir.is_dir():
            continue
        candidate = month_dir / dir_name
        if candidate.is_dir():
            return candidate

    return None


def _repo_relative_path(path: Path, repo_root: Path) -> str:
    """尽可能将路径格式化为相对仓库根目录的路径。"""
    try:
        return path.relative_to(repo_root).as_posix()
    except ValueError:
        return str(path)


def _report_read_failure(path: Path, reason: str | None) -> None:
    """打印 JSON 文件无法加载的原因和处理方式。

    这些命令即将重写刚读取失败的文件，因此这是用户收到的最后一道告警。
    若仅以非零状态退出却没有输出，就无法区分解析错误、权限错误和空文件。
    """
    problem, hint = describe_json_read_failure(path, reason)
    print(colored(f"错误： {problem}", Colors.RED), file=sys.stderr)
    print(hint, file=sys.stderr)


def _ensure_children_list(data: dict) -> list:
    """以列表形式返回任务的 `children`，若不是列表则原地修复。

    `.get(key, default)` 仅在键不存在时返回默认值。旧格式或手工编辑的
    task.json 若包含 `"children": null`，会得到 None，而下方所有调用者
    都将结果作为列表使用。在 `cmd_create` 中，异常会发生在新 task.json
    写入之后，导致磁盘上存在一个未被父任务引用的任务。

    修复结果写入 `data`，而非仅作为返回值，因为解除关联时可能找不到
    要移除的名称，若不原地修复就会原样保存畸形值：本次虽不崩溃，
    下一个调用者仍会继承问题。

    所有非列表值都直接丢弃而不强制转换：字符串按字符迭代、字典按键迭代，
    两者都会形成看似合理却并非真实子任务集合的结果。
    """
    children = data.get("children")
    if not isinstance(children, list):
        children = []
        data["children"] = children
    return children


def _report_write_failure(path: Path) -> None:
    """打印 JSON 写入失败提示。写入是原子的，因此原内容不变。"""
    print(colored(f"错误：无法写入 {path}", Colors.RED), file=sys.stderr)
    print(
        "原文件未改变（写入是原子的）。"
        "请检查权限和磁盘剩余空间后重试。",
        file=sys.stderr,
    )


def _restore_child_links(unlinked: dict[Path, str | None]) -> None:
    """恢复本次归档尝试中已移除的父任务关联。

    下方解除关联的循环逐一遍历父任务的子任务。中途失败时，之前处理的
    子任务已变为 ``parent: null``，但父任务仍在活跃任务树中。
    这与该循环本就禁止产生的悬空引用属于对应的问题，后续也没有修复机制，
    因此必须在报告失败之前撤销已有操作。

    恢复尽力执行：若子任务无法写回，明确列出其名称，方便调用者手工
    重新关联，而不必猜测哪个任务出错。
    """
    broken: list[str] = []
    for child_json, original_parent in unlinked.items():
        child_data, _ = read_json_checked(child_json)
        if child_data is None:
            broken.append(child_json.parent.name)
            continue
        child_data["parent"] = original_parent
        if not write_json(child_json, child_data):
            broken.append(child_json.parent.name)
    if broken:
        print(
            colored(
                f"警告：无法恢复以下任务的父关联： {', '.join(broken)}. "
                "请逐一重新关联：`python3 .trellis/scripts/task.py "
                "add-subtask <parent> <child>`.",
                Colors.RED,
            ),
            file=sys.stderr,
        )


# =============================================================================
# 子代理平台检测与 JSONL 上下文文件
# =============================================================================

# 读取 implement.jsonl / check.jsonl 的平台配置目录。
# 应与 Trellis 上游 src/types/ai-tools.ts 的 AI_TOOLS 条目同步，
# 对应 workflow.md 中支持代理的技能路由平台。
# Codex 单独检查，因为显式 inline 模式不读取 JSONL。
# Kilo、Antigravity、Devin 也不在此列表：它们通过技能加载规范。
_SUBAGENT_CONFIG_DIRS: tuple[str, ...] = (
    ".claude",
    ".cursor",
    ".kiro",
    ".gemini",
    ".opencode",
    ".qoder",
    ".codebuddy",
    ".factory",   # Factory Droid
    ".github/copilot",
    ".pi",        # Pi Agent
    ".trae",      # Trae IDE
    ".omp",       # Oh My Pi
    ".zcode",     # ZCode
    ".grok",      # Grok Build
    ".kimi-code", # Kimi Code
)
_CODEX_CONFIG_DIR = ".codex"


def _has_subagent_platform(repo_root: Path) -> bool:
    """若配置了任一支持子代理的平台，则返回 True。

    通过仓库根目录下的已知配置目录检测。Codex 默认通过
    ``codex.dispatch_mode: auto`` 计入（包括旧别名 ``sub-agent``）；
    显式 inline 模式通过技能加载上下文，不读取 JSONL。
    """
    for config_dir in _SUBAGENT_CONFIG_DIRS:
        if (repo_root / config_dir).is_dir():
            return True
    if (repo_root / _CODEX_CONFIG_DIR).is_dir():
        return get_codex_dispatch_mode(repo_root) == "auto"
    return False


def _parse_meta_pairs(pairs: list[str] | None) -> dict[str, str] | None:
    """将可重复指定的 ``--meta key=value`` 参数解析为字典。

    遇到首个格式错误的参数（缺少 ``=`` 或键为空）时，打印包含错误值
    的提示并返回 ``None``。值按原字符串保存，不支持嵌套或类型转换。
    """
    meta: dict[str, str] = {}
    for pair in pairs or []:
        key, sep, value = pair.partition("=")
        if not sep or not key:
            print(
                colored(f"错误：--meta 值 '{pair}' 格式无效（应为 key=value）", Colors.RED),
                file=sys.stderr,
            )
            return None
        meta[key] = value
    return meta


def _default_prd_content(title: str, description: str | None = None) -> str:
    """返回创建每个任务时使用的默认 PRD 框架。"""
    goal = (description or "").strip() or "待补充。"
    heading = title.strip() or "未命名任务"
    return f"""# {heading}

## 目标

{goal}

## 需求

- 待补充

## 验收标准

- [ ] 待补充

## 说明

- `prd.md` 只描述需求、约束和验收标准。
- 轻量任务可以只保留 PRD。
- 复杂任务应在 `task.py start` 前补充技术设计 `design.md` 和执行计划 `implement.md`。
"""


# =============================================================================
# 命令： create
# =============================================================================

def cmd_create(args: argparse.Namespace) -> int:
    """创建新任务。"""
    repo_root = get_repo_root()

    # 在 ensure_tasks_dir 和其他任何写入之前先检查标题与描述，
    # 使被拒绝的创建请求不会修改目录树。归档前校验要求二者均非空；
    # 在此拦截只需重输一条命令，避免几小时后才阻塞 PR。
    if not strip_blank(args.title):
        print(colored("错误：必须提供标题", Colors.RED), file=sys.stderr)
        print(
            '请提供非空 <title>（仅空白不算）：'
            "标题为空的任务无法归档。",
            file=sys.stderr,
        )
        return 1

    description = strip_blank(getattr(args, "description", None))
    if not description:
        print(colored("错误：必须提供 --description", Colors.RED), file=sys.stderr)
        print(
            '请提供 --description "<此任务的交付内容>"（仅空白不'
            "算）：描述为空的任务无法归档。",
            file=sys.stderr,
        )
        return 1

    # 校验 --meta（来自 CLI；在创建目录前快速失败）
    meta = _parse_meta_pairs(getattr(args, "meta", None))
    if meta is None:
        return 1

    # 校验 --package（来自 CLI；快速失败）
    package: str | None = getattr(args, "package", None)
    if not is_monorepo(repo_root):
        # 单仓库：忽略 --package，不使用包前缀
        if package:
            print(colored(f"警告：单仓库项目会忽略 --package", Colors.YELLOW), file=sys.stderr)
        package = None
    elif package:
        if not validate_package(package, repo_root):
            packages = get_packages(repo_root)
            available = ", ".join(sorted(packages.keys())) if packages else "(none)"
            print(colored(f"错误：未知包 '{package}'。可用包： {available}", Colors.RED), file=sys.stderr)
            return 1
    else:
        # 推断：default_package → None（创建时还没有 task.json）
        package = resolve_package(repo_root=repo_root)

    # 默认由当前开发者负责
    assignee = args.assignee
    if not assignee:
        assignee = get_developer(repo_root)
        if not assignee:
            print(colored("错误：尚未设置开发者。请先运行 init_developer.py 或使用 --assignee", Colors.RED), file=sys.stderr)
            print(DEVELOPER_HINT, file=sys.stderr)
            return 1

    ensure_tasks_dir(repo_root)

    # 以当前开发者作为创建者
    creator = get_developer(repo_root) or assignee

    # 未提供 slug 时自动生成。由标题生成的 slug 已经 _slugify 处理，
    # 显式 --slug 则没有，因此拒绝拼入目录名后可能逃出任务根目录的字符。
    slug = args.slug or _slugify(args.title)
    if not slug:
        print(colored("错误：无法从标题生成 slug；中文标题请显式提供英文 --slug", Colors.RED), file=sys.stderr)
        return 1

    if args.slug and ("/" in slug or "\\" in slug or ".." in slug):
        print(
            colored(
                f"错误：--slug 必须是普通名称，不能包含路径分隔符或 '..'： {slug}",
                Colors.RED,
            ),
            file=sys.stderr,
        )
        return 1

    # 以 MM-DD-slug 格式创建任务目录
    tasks_dir = get_tasks_dir(repo_root)
    date_prefix = generate_task_date_prefix()

    # 防止 --slug 自带日期前缀（例如粘贴了完整任务目录名），
    # 否则会生成 MM-DD-MM-DD-slug（问题 #377）。
    # 仅检查显式 --slug，保留由标题生成的 slug。
    if args.slug:
        m = re.match(r"^(\d{2})-(\d{2})-(.+)$", slug)
        if m and 1 <= int(m.group(1)) <= 12 and 1 <= int(m.group(2)) <= 31:
            slug_prefix = f"{m.group(1)}-{m.group(2)}"
            if slug_prefix == date_prefix:
                slug = m.group(3)
                print(
                    colored(
                        f'警告：--slug 不应包含 MM-DD 前缀；已规范化为 "{slug}"',
                        Colors.YELLOW,
                    ),
                    file=sys.stderr,
                )
            else:
                print(
                    colored(
                        f"错误：--slug 以日期前缀开头（{slug_prefix}-），但 task.py create 始终使用当天日期（{date_prefix}）。",
                        Colors.RED,
                    ),
                    file=sys.stderr,
                )
                print(f"请只传 slug 主体，例如 --slug {m.group(3)}", file=sys.stderr)
                return 1

    # 在任何创建操作之前解析 --parent。显式 --parent 无法关联时
    # 应判定请求失败，而非仅告警；继续创建会留下不完整的关系，
    # 却让调用者误以为成功。
    parent_dir: Path | None = None
    parent_data: dict | None = None
    if args.parent:
        parent_dir = resolve_task_dir(args.parent, repo_root)
        if parent_dir is None:
            print(colored(f"错误：无法解析父任务： {args.parent}", Colors.RED), file=sys.stderr)
            print("未创建任务。请向 --parent 传入已存在的任务目录。", file=sys.stderr)
            return 1
        parent_json_path = parent_dir / FILE_TASK_JSON
        if not parent_json_path.is_file():
            print(colored(f"错误：找不到父任务的 task.json： {args.parent}", Colors.RED), file=sys.stderr)
            print("未创建任务。请向 --parent 传入已存在的任务目录。", file=sys.stderr)
            return 1
        parent_data, parent_reason = read_json_checked(parent_json_path)
        if parent_data is None:
            _report_read_failure(parent_json_path, parent_reason)
            print("未创建任务。请修复父任务 task.json 后重试。", file=sys.stderr)
            return 1

    dir_name = f"{date_prefix}-{slug}"
    task_dir = tasks_dir / dir_name
    task_json_path = task_dir / FILE_TASK_JSON

    archived_task_dir = _find_archived_task_by_dir_name(tasks_dir, dir_name)
    if archived_task_dir:
        print(colored(f"错误：任务已经归档： {dir_name}", Colors.RED), file=sys.stderr)
        print(f"归档位置： {_repo_relative_path(archived_task_dir, repo_root)}", file=sys.stderr)
        print("如需创建新任务，请使用新的 slug。", file=sys.stderr)
        return 1

    # 同一天复用 slug 是常见失误；继续执行会重写已有 task.json，
    # 重置状态、子任务、父任务、分支及元数据，而这些信息无法从别处重建。
    # 除非调用者明确要求覆盖，否则像上方归档名称冲突一样拒绝。
    if task_dir.exists():
        if not getattr(args, "force", False):
            print(colored(f"错误：任务已存在： {dir_name}", Colors.RED), file=sys.stderr)
            print(f"现有任务位置： {_repo_relative_path(task_dir, repo_root)}", file=sys.stderr)
            print(
                "请换用其他 --slug，或传入 --force 覆盖其 task.json。",
                file=sys.stderr,
            )
            return 1
        print(
            colored(
                f"警告：--force 将覆盖已有任务的 task.json： {dir_name}",
                Colors.YELLOW,
            ),
            file=sys.stderr,
        )
        created_dir = False
    else:
        task_dir.mkdir(parents=True)
        created_dir = True

    today = datetime.now().strftime("%Y-%m-%d")

    # 记录 PR 目标分支。优先使用仓库真实默认分支（origin/HEAD），
    # 避免在功能分支上创建任务时误将功能分支记为 PR 目标（#399 第 1 项）。
    # 无法解析默认分支时（未配置远程、离线等），回退到当前检出的分支，
    # 保持既有行为。若两者均不正确，可用 --base-branch 覆盖。
    _, branch_out, _ = run_git(["branch", "--show-current"], cwd=repo_root)
    current_branch = branch_out.strip() or "main"
    explicit_base_branch: str | None = getattr(args, "base_branch", None)
    if explicit_base_branch:
        base_branch = explicit_base_branch
    else:
        resolved_base_branch = resolve_default_branch(repo_root)
        if resolved_base_branch:
            base_branch = resolved_base_branch
        else:
            base_branch = current_branch
            print(
                colored(
                    f"警告：无法解析仓库默认分支"
                    f"（未配置远程、离线等）；将 base_branch 记录为"
                    f"当前检出分支 '{base_branch}'。可用 --base-branch 覆盖。",
                    Colors.YELLOW,
                ),
                file=sys.stderr,
            )

    task_data = {
        "id": slug,
        "name": slug,
        "title": args.title,
        "description": description,
        "status": "planning",
        "dev_type": None,
        "scope": None,
        "package": package,
        "priority": args.priority,
        "creator": creator,
        "assignee": assignee,
        "createdAt": today,
        "completedAt": None,
        "branch": None,
        "base_branch": base_branch,
        "worktree_path": None,
        "commit": None,
        "pr_url": None,
        "subtasks": [],
        "children": [],
        "parent": None,
        "relatedFiles": [],
        "notes": "",
        "meta": meta,
    }

    # 没有 task.json 的目录不算任务：`list` 会隐藏它，生命周期命令也会拒绝。
    # 此处应失败，不能打印“已创建任务”并输出路径供脚本串联使用。
    if not write_json(task_json_path, task_data):
        _report_write_failure(task_json_path)
        print(colored(f"未创建任务： {dir_name}", Colors.RED), file=sys.stderr)
        if created_dir:
            try:
                task_dir.rmdir()
            except OSError:
                print(
                    f"遗留的空目录： {_repo_relative_path(task_dir, repo_root)}",
                    file=sys.stderr,
                )
        return 1

    prd_path = task_dir / "prd.md"
    if not prd_path.exists():
        prd_path.write_text(
            _default_prd_content(args.title, description),
            encoding="utf-8",
        )

    # 为支持子代理的平台创建空 implement.jsonl / check.jsonl。
    # 文件保留为空，等待代理在规划阶段整理真实条目。
    # 占位行会被 `task.py validate` 和 PR 预检视为未完成的脚手架，
    # 因此整理说明打印到下方控制台，而不写入文件。
    # 不支持代理的平台（Kilo、Antigravity、Devin）跳过此步；
    # 它们通过 trellis-before-dev 技能加载规范，不使用 JSONL。
    created_jsonl = False
    if _has_subagent_platform(repo_root):
        for jsonl_name in ("implement.jsonl", "check.jsonl"):
            jsonl_path = task_dir / jsonl_name
            if not jsonl_path.exists():
                jsonl_path.write_text("", encoding="utf-8")
        created_jsonl = True

    # 建立双向关联。双方都已在上方校验，
    # 因此这里的失败是写入失败，不是参数错误。
    if parent_dir is not None and parent_data is not None:
        parent_json_path = parent_dir / FILE_TASK_JSON

        # 将子任务加入父任务的 children 列表
        parent_children = _ensure_children_list(parent_data)
        if dir_name not in parent_children:
            parent_children.append(dir_name)
            parent_data["children"] = parent_children
            if not write_json(parent_json_path, parent_data):
                _report_write_failure(parent_json_path)
                print(
                    f"任务已存在于 {_repo_relative_path(task_dir, repo_root)} 但尚未 "
                    f"关联到 {parent_dir.name}。重新关联：task.py add-subtask "
                    f"{parent_dir.name} {dir_name}",
                    file=sys.stderr,
                )
                return 1

        # 在子任务的 task.json 中设置 parent
        task_data["parent"] = parent_dir.name
        if not write_json(task_json_path, task_data):
            _report_write_failure(task_json_path)
            print(
                f"关联仅写入一半： {parent_dir.name} 现已列出 '{dir_name}' as a child, "
                f"但新任务尚未记录父任务。",
                file=sys.stderr,
            )
            return 1

        print(colored(f"已关联为以下任务的子任务： {parent_dir.name}", Colors.GREEN), file=sys.stderr)

    # 自动激活新任务，使每轮状态提示进入 planning。
    # 尽力执行：缺少会话身份时（在 AI 会话外运行 CLI）降级处理，
    # 任务仍会创建，用户可稍后执行 task.py start。
    # 指针按会话隔离，不影响其他 AI 会话。
    if getattr(args, "no_start", False):
        print(
            colored(
                "已跳过会话激活（--no-start）；准备好后运行 task.py start。",
                Colors.YELLOW,
            ),
            file=sys.stderr,
        )
    else:
        try:
            from .active_task import resolve_context_key, set_active_task
        except Exception as exc:
            print(
                colored(f"警告：无法激活会话（导入失败： {exc})", Colors.YELLOW),
                file=sys.stderr,
            )
        else:
            try:
                context_key = resolve_context_key()
            except Exception as exc:
                print(
                    colored(f"警告：激活会话失败（上下文解析： {exc})", Colors.YELLOW),
                    file=sys.stderr,
                )
            else:
                # 缺少会话身份是 AI 会话外运行 CLI 的正常情况，
                # 见上方说明；静默处理，不视为失败。
                if context_key:
                    try:
                        rel_dir = task_dir.relative_to(repo_root).as_posix()
                    except ValueError:
                        rel_dir = str(task_dir)
                    try:
                        active = set_active_task(rel_dir, repo_root)
                    except Exception as exc:
                        print(
                            colored(f"警告：激活会话失败（指针持久化： {exc})", Colors.YELLOW),
                            file=sys.stderr,
                        )
                    else:
                        if active:
                            print(
                                colored(f"已为本会话激活任务： {active.task_path}", Colors.GREEN),
                                file=sys.stderr,
                            )
                            print(f"来源： {active.source}", file=sys.stderr)
                        else:
                            print(
                                colored("警告：激活会话失败（未返回指针）", Colors.YELLOW),
                                file=sys.stderr,
                            )

    print(colored(f"已创建任务： {dir_name}", Colors.GREEN), file=sys.stderr)
    print("", file=sys.stderr)
    print(colored("下一步：", Colors.BLUE), file=sys.stderr)
    print("  - 在 prd.md 中填写需求和验收标准", file=sys.stderr)
    print("  - 轻量任务：可只保留 PRD", file=sys.stderr)
    print("  - 复杂任务：在 task.py start 前补充 design.md 和 implement.md", file=sys.stderr)
    if created_jsonl:
        print(
            "  - 将 implement.jsonl / check.jsonl（创建时为空）整理为规范或研究"
            "清单；子代理需要上下文时，须在 task.py start 前完成：",
            file=sys.stderr,
        )
        print(
            '      每行一个 JSON 对象： {"file": "<path>", "reason": "<why>"}; '
            "只引用规范或研究文档，不添加代码路径",
            file=sys.stderr,
        )
        print(
            "      列出可用规范： python3 .trellis/scripts/get_context.py --mode packages",
            file=sys.stderr,
        )
    print("  - 通过 /trellis:continue 或阶段上下文决定下一步", file=sys.stderr)
    print("", file=sys.stderr)

    # 输出相对路径，供脚本串联使用
    print(f"{DIR_WORKFLOW}/{DIR_TASKS}/{dir_name}")

    run_task_hooks("after_create", task_json_path, repo_root)
    return 0


# =============================================================================
# 命令： rename
# =============================================================================

# task.json 中保存任务自身 slug 的字段。目录名也包含 slug，前面加创建日期；
# 其他引用任务的位置（其他任务的 parent / children / subtasks，JSONL 路径）
# 保存完整目录名。
RENAME_IDENTITY_FIELDS: tuple[str, ...] = ("id", "name")

_JSONL_NAMES: tuple[str, ...] = ("implement.jsonl", "check.jsonl")


@dataclass
class _RenamePlan:
    """在任何写入之前计算重命名所需的全部变更。

    ``--dry-run`` 和实际运行都从这个结构生成输出，
    保证显示的变更清单与实际应用的变更一致。
    """

    task_dir: Path
    new_dir: Path
    old_name: str
    new_name: str
    old_rel: str
    new_rel: str
    task_json_path: Path
    task_data: dict
    # 每个待变更身份字段的（字段、当前值、新值）。
    identity: list[tuple[str, object, str]] = field(default_factory=list)
    # （JSONL 路径、变更行号、完整新文件文本）。
    jsonl: list[tuple[Path, list[int], str]] = field(default_factory=list)
    # （其他任务的 task.json、重写后的数据、变更引用的标签）。
    backrefs: list[tuple[Path, dict, list[str]]] = field(default_factory=list)
    # .trellis/ 下其他位置旧名称引用的（文件、行号）。
    reported: list[tuple[Path, int]] = field(default_factory=list)
    # 无法读取 task.json 的任务目录；其中的反向引用
    # 既未重写，也无法确认不存在。
    unreadable: list[Path] = field(default_factory=list)


def _split_date_prefix(dir_name: str) -> tuple[str, str]:
    """将 ``MM-DD-slug`` 拆分成 ``("MM-DD", "slug")``。

    没有合理的日期前缀时返回 ``("", dir_name)``，因此手工创建且无日期
    前缀的目录会重命名为不带前缀的 slug。
    """
    m = re.match(r"^(\d{2})-(\d{2})-(.+)$", dir_name)
    if m and 1 <= int(m.group(1)) <= 12 and 1 <= int(m.group(2)) <= 31:
        return f"{m.group(1)}-{m.group(2)}", m.group(3)
    return "", dir_name


def _validate_rename_slug(slug: str, date_prefix: str) -> str | None:
    """返回目标 slug 主体；拒绝时先报告原因，再返回 None。

    与 ``create`` 的 ``--slug`` 处理一致：禁止路径分隔符和 ``..``
    （slug 会拼入目录名）；若粘贴了日期前缀，则移除前缀以免重复。
    但重命名保留任务原始创建日期，不使用当天日期。
    """
    if not slug:
        print(colored("错误：必须提供新 slug", Colors.RED), file=sys.stderr)
        return None

    if "/" in slug or "\\" in slug or ".." in slug:
        print(
            colored(
                f"错误：<new-slug> 必须是普通名称，不能包含路径分隔符或 '..'： {slug}",
                Colors.RED,
            ),
            file=sys.stderr,
        )
        return None

    slug_prefix, body = _split_date_prefix(slug)
    if not slug_prefix:
        return slug
    if slug_prefix == date_prefix:
        print(
            colored(
                f'警告：<new-slug> 不应包含 MM-DD 前缀；已规范化为 "{body}"',
                Colors.YELLOW,
            ),
            file=sys.stderr,
        )
        return body
    print(
        colored(
            f"错误：<new-slug> 以日期前缀开头（{slug_prefix}-），但重命名保留"
            f"任务原始创建日期（{date_prefix}）。",
            Colors.RED,
        ),
        file=sys.stderr,
    )
    print(f"请只传 slug 主体，例如 {body}", file=sys.stderr)
    return None


def _plan_jsonl_rewrites(
    task_dir: Path, old_rel: str, new_rel: str
) -> list[tuple[Path, list[int], str]]:
    """规划对指向旧任务目录的上下文条目的重写。

    匹配 ``<old path>/`` 而非裸路径，避免误改仅以本任务名开头的兄弟任务。
    """
    rewrites: list[tuple[Path, list[int], str]] = []
    needle = f"{old_rel}/"
    replacement = f"{new_rel}/"

    for jsonl_name in _JSONL_NAMES:
        jsonl_path = task_dir / jsonl_name
        if not jsonl_path.is_file():
            continue
        try:
            lines = jsonl_path.read_text(encoding="utf-8").splitlines(keepends=True)
        except (OSError, UnicodeDecodeError):
            continue

        changed: list[int] = []
        for index, line in enumerate(lines):
            if needle in line:
                lines[index] = line.replace(needle, replacement)
                changed.append(index + 1)
        if changed:
            rewrites.append((jsonl_path, changed, "".join(lines)))

    return rewrites


def _plan_backrefs(
    tasks_dir: Path, task_dir: Path, old_name: str, new_name: str
) -> tuple[list[tuple[Path, dict, list[str]]], list[Path]]:
    """规划其他活跃任务中的反向引用重写。

    返回 ``(changes, unreadable)``。``subtasks`` 是 ``children`` 的旧名称，
    旧 task.json 仍可能使用，因此两个列表都要重写。
    """
    changes: list[tuple[Path, dict, list[str]]] = []
    unreadable: list[Path] = []

    for candidate in sorted(tasks_dir.iterdir()):
        if not candidate.is_dir() or candidate.name == DIR_ARCHIVE:
            continue
        if candidate == task_dir:
            continue
        json_path = candidate / FILE_TASK_JSON
        if not json_path.is_file():
            continue

        data, _reason = read_json_checked(json_path)
        if data is None:
            unreadable.append(json_path)
            continue

        labels: list[str] = []
        if data.get("parent") == old_name:
            data["parent"] = new_name
            labels.append("parent")
        for list_field in ("children", "subtasks"):
            values = data.get(list_field)
            if not isinstance(values, list):
                continue
            for index, value in enumerate(values):
                if value == old_name:
                    values[index] = new_name
                    labels.append(f"{list_field}[{index}]")

        if labels:
            changes.append((json_path, data, labels))

    return changes, unreadable


def _plan_reported_refs(
    repo_root: Path, task_dir: Path, rewritten: set[Path], old_name: str
) -> list[tuple[Path, int]]:
    """查找 .trellis/ 下其他位置残留的旧任务名引用。

    这些引用只报告，不重写：日志条目和工作流正文以自由文本引用任务，
    盲目替换会让重命名产生未经请求的差异。边界锚定的模式排除仅包含
    当前名称的更长任务名。

    运行时会话指针不在其中，因为它们不是正文，且确实会被重写，
    见 ``repoint_task_in_sessions``。若将其列为“未重写”，就会错误描述
    这个一旦过期就会影响下一条命令的文件。
    """
    from .active_task import _runtime_sessions_dir

    pattern = re.compile(
        r"(?<![0-9A-Za-z_-])" + re.escape(old_name) + r"(?![0-9A-Za-z_-])"
    )
    hits: list[tuple[Path, int]] = []
    trellis_dir = repo_root / DIR_WORKFLOW
    sessions_dir = _runtime_sessions_dir(repo_root)
    if not trellis_dir.is_dir():
        return hits

    for path in sorted(trellis_dir.rglob("*")):
        if not path.is_file() or path in rewritten:
            continue
        if path == task_dir or task_dir in path.parents:
            continue
        if path.parent == sessions_dir:
            continue
        if any(
            part == "__pycache__" or part.startswith(".backup-")
            for part in path.relative_to(trellis_dir).parts[:-1]
        ):
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        for lineno, line in enumerate(text.splitlines(), 1):
            if pattern.search(line):
                hits.append((path, lineno))

    return hits


def _render_rename_plan(plan: _RenamePlan, repo_root: Path) -> list[str]:
    """显示变更清单；--dry-run 和实际运行的输出一致。"""
    lines = [
        f"重命名： {plan.old_name} -> {plan.new_name}",
        f"  目录： {plan.old_rel} -> {plan.new_rel}",
    ]
    for field_name, old_value, new_value in plan.identity:
        lines.append(f"  task.json: {field_name}: {old_value} -> {new_value}")
    for json_path, _data, labels in plan.backrefs:
        rel = _repo_relative_path(json_path, repo_root)
        for label in labels:
            lines.append(
                f"  反向引用： {rel}: {label}: {plan.old_name} -> {plan.new_name}"
            )
    for jsonl_path, linenos, _text in plan.jsonl:
        rel = _repo_relative_path(jsonl_path, repo_root)
        for lineno in linenos:
            lines.append(
                f"  jsonl: {rel}:{lineno}: {plan.old_rel}/ -> {plan.new_rel}/"
            )
    for path, lineno in plan.reported:
        lines.append(
            f"  已报告（未重写）： {_repo_relative_path(path, repo_root)}:{lineno}"
        )
    lines.append(
        f"  会话：位于以下位置的所有活跃任务指针 {plan.old_rel} 将改指向 {plan.new_rel}"
    )
    return lines


def _rename_interrupted(plan: _RenamePlan) -> None:
    """说明如何完成中途停止的重命名。"""
    print(
        f"任务仍位于 {plan.old_rel}，尚未移动。到当前步骤为止的操作"
        f"均具备幂等性：修复写入失败后，重新运行同一重命名命令"
        f"即可完成。",
        file=sys.stderr,
    )


def _apply_rename(plan: _RenamePlan, repo_root: Path) -> int:
    """写入计划中的变更。

    有意最后才移动目录。之前的所有操作都是原地编辑，重复运行同一命令
    会将这些编辑判为已完成，因此中断后可用同一命令完成余下操作。
    若先移动目录，旧名称将无法解析，余下编辑只能手工完成。
    """
    if plan.identity:
        data = dict(plan.task_data)
        for field_name, _old_value, new_value in plan.identity:
            data[field_name] = new_value
        if not write_json(plan.task_json_path, data):
            _report_write_failure(plan.task_json_path)
            print("没有重命名任何内容。", file=sys.stderr)
            return 1

    for jsonl_path, _linenos, text in plan.jsonl:
        try:
            jsonl_path.write_text(text, encoding="utf-8")
        except OSError as exc:
            print(
                colored(f"错误：无法写入 {jsonl_path}: {exc}", Colors.RED),
                file=sys.stderr,
            )
            _rename_interrupted(plan)
            return 1

    for json_path, data, _labels in plan.backrefs:
        if not write_json(json_path, data):
            _report_write_failure(json_path)
            _rename_interrupted(plan)
            return 1

    try:
        plan.task_dir.rename(plan.new_dir)
    except OSError as exc:
        print(
            colored(f"错误：无法移动 {plan.old_rel} -> {plan.new_rel}: {exc}", Colors.RED),
            file=sys.stderr,
        )
        _rename_interrupted(plan)
        return 1

    # 最后在移动后执行：指向旧路径的会话此时会解析到不存在的目录，
    # `current` 会将任务报告为过期，上下文钩子也不会注入任何内容，
    # 除非用户再次执行 `start`。归档会清除指针，因为任务已离开活跃集合；
    # 重命名则迁移指针，因为正在处理的仍是同一个任务。
    from .active_task import repoint_task_in_sessions

    repoint_task_in_sessions(str(plan.task_dir), str(plan.new_dir), repo_root)

    return 0


def cmd_rename(args: argparse.Namespace) -> int:
    """重命名任务及其全部引用。"""
    repo_root = get_repo_root()
    tasks_dir = get_tasks_dir(repo_root)

    task_dir = resolve_task_dir(args.name, repo_root)
    if task_dir is None or not task_dir.is_dir():
        if task_dir is not None:
            print(colored(f"错误：找不到任务： {args.name}", Colors.RED), file=sys.stderr)
        return 1

    # 比 resolve_task_dir 的范围限制更严格：归档任务已离开活跃集合，
    # 此处不再维护它的反向引用。
    if not is_within_tasks_dir(task_dir, repo_root):
        print(
            colored(
                f"错误：拒绝重命名 '{args.name}': "
                f"{task_dir} 不是以下目录下的活跃任务： {tasks_dir}",
                Colors.RED,
            ),
            file=sys.stderr,
        )
        return 1

    old_name = task_dir.name
    date_prefix, _old_slug = _split_date_prefix(old_name)

    slug = _validate_rename_slug(args.new_slug, date_prefix)
    if slug is None:
        return 1

    new_name = f"{date_prefix}-{slug}" if date_prefix else slug
    if new_name == old_name:
        print(
            colored(f"错误： '{new_name}' 已是任务当前名称", Colors.RED),
            file=sys.stderr,
        )
        return 1

    new_dir = task_dir.parent / new_name
    if new_dir.exists():
        print(
            colored(f"错误：任务已存在： {new_name}", Colors.RED),
            file=sys.stderr,
        )
        print(f"现有任务位置： {_repo_relative_path(new_dir, repo_root)}", file=sys.stderr)
        print("请选用其他 slug。", file=sys.stderr)
        return 1

    archived_task_dir = _find_archived_task_by_dir_name(tasks_dir, new_name)
    if archived_task_dir:
        print(
            colored(f"错误：任务已经归档： {new_name}", Colors.RED),
            file=sys.stderr,
        )
        print(f"归档位置： {_repo_relative_path(archived_task_dir, repo_root)}", file=sys.stderr)
        print("请选用其他 slug。", file=sys.stderr)
        return 1

    task_json_path = task_dir / FILE_TASK_JSON
    if not task_json_path.is_file():
        print(
            colored(f"错误：以下位置找不到 task.json： {task_dir}", Colors.RED),
            file=sys.stderr,
        )
        return 1

    task_data, reason = read_json_checked(task_json_path)
    if task_data is None:
        _report_read_failure(task_json_path, reason)
        print("没有重命名任何内容。", file=sys.stderr)
        return 1

    plan = _RenamePlan(
        task_dir=task_dir,
        new_dir=new_dir,
        old_name=old_name,
        new_name=new_name,
        old_rel=_repo_relative_path(task_dir, repo_root),
        new_rel=_repo_relative_path(new_dir, repo_root),
        task_json_path=task_json_path,
        task_data=task_data,
    )
    plan.identity = [
        (name, task_data.get(name), slug)
        for name in RENAME_IDENTITY_FIELDS
        if task_data.get(name) != slug
    ]
    plan.jsonl = _plan_jsonl_rewrites(task_dir, plan.old_rel, plan.new_rel)
    plan.backrefs, plan.unreadable = _plan_backrefs(
        tasks_dir, task_dir, old_name, new_name
    )
    rewritten = {path for path, _linenos, _text in plan.jsonl}
    rewritten.update(path for path, _data, _labels in plan.backrefs)
    plan.reported = _plan_reported_refs(repo_root, task_dir, rewritten, old_name)

    for line in _render_rename_plan(plan, repo_root):
        print(line)

    for json_path in plan.unreadable:
        print(
            colored(
                f"警告： {_repo_relative_path(json_path, repo_root)} 无法读取；"
                "其中所有反向引用保持原样。",
                Colors.YELLOW,
            ),
            file=sys.stderr,
        )

    if getattr(args, "dry_run", False):
        print(colored("预演完成：未写入任何内容。", Colors.YELLOW), file=sys.stderr)
        return 0

    rc = _apply_rename(plan, repo_root)
    if rc != 0:
        return rc

    print(colored(f"已重命名： {old_name} -> {new_name}", Colors.GREEN), file=sys.stderr)
    if plan.reported:
        print(
            colored(
                f"{len(plan.reported)} 个其他位置的引用位于 {DIR_WORKFLOW}/ "
                "仍指向旧任务名；已列在上方，未作重写。",
                Colors.YELLOW,
            ),
            file=sys.stderr,
        )
    return 0


# =============================================================================
# 命令： archive
# =============================================================================

def _task_branch_field(data: dict, key: str) -> str:
    """将分支字段读为去除首尾空白的字符串（未设置或非字符串时为 ""）。"""
    value = data.get(key)
    return strip_blank(value) if isinstance(value, str) else ""


def _validate_branch_metadata(
    data: dict,
    task_name: str,
    repo_root: Path,
    skip: bool,
) -> bool:
    """在任务离开活跃任务树之前检查分支元数据。

    必须停止归档时返回 False。归档是最后一个能看到任务的关口，因此在此
    拒绝事后无人能重建的元数据，避免以后手工修复（#399 后续处理）。

    “有 PR 的任务”采用务实判断：有远程的仓库中，携带 base_branch 的
    任务创建时预期会有 PR，因此缺少 `branch` 意味着元数据未记录，
    不能认定工作没有分支。纯本地仓库和无 base_branch 的任务不受此限制。

    已记录的分支在本地不存在时仍只告警：合并后删除功能分支很常见，
    此时拒绝归档并不合理。
    """
    branch = _task_branch_field(data, "branch")
    base_branch = _task_branch_field(data, "base_branch")
    task_py = f"python3 {DIR_WORKFLOW}/scripts/task.py"

    if branch and not branch_exists_locally(branch, repo_root):
        print(
            colored(
                f"警告：已记录分支 '{branch}' 在本地已不存在"
                "（可能已合并并删除）。",
                Colors.YELLOW,
            ),
            file=sys.stderr,
        )

    if skip:
        return True

    if branch and base_branch and branch == base_branch:
        print(
            colored(
                f"错误：拒绝归档 '{task_name}'：branch 和 base_branch"
                f"均为 '{branch}'。PR 不能指向自身分支，因此该"
                "元数据无法描述已合并的工作。",
                Colors.RED,
            ),
            file=sys.stderr,
        )
        print("请修复有误的字段：", file=sys.stderr)
        print(f"  {task_py} set-branch {task_name} <feature-branch>", file=sys.stderr)
        print(f"  {task_py} set-base-branch {task_name} <target-branch>", file=sys.stderr)
        print(
            f"  {task_py} archive {task_name} --skip-branch-validation"
            "   # 仅适用于从未对应 PR 的任务",
            file=sys.stderr,
        )
        return False

    if not branch and base_branch and has_git_remote(repo_root):
        print(
            colored(
                f"错误：拒绝归档 '{task_name}'：未记录分支，"
                f"但任务指向 base_branch'{base_branch}'，且仓库存在"
                "远程：实际开发所用分支从未记录。",
                Colors.RED,
            ),
            file=sys.stderr,
        )
        print("修复方式：", file=sys.stderr)
        print(f"  {task_py} set-branch {task_name} <branch>", file=sys.stderr)
        print(
            f"  {task_py} archive {task_name} --skip-branch-validation"
            "   # 仅适用于工作没有独立分支的情况",
            file=sys.stderr,
        )
        return False

    return True


def cmd_archive(args: argparse.Namespace) -> int:
    """归档已完成任务。"""
    repo_root = get_repo_root()
    task_name = args.name

    if not task_name:
        print(colored("错误：必须提供任务名", Colors.RED), file=sys.stderr)
        return 1

    tasks_dir = get_tasks_dir(repo_root)

    # 解析任务目录（支持任务名、相对路径或绝对路径）
    task_dir = resolve_task_dir(task_name, repo_root)

    if task_dir is None or not task_dir.is_dir():
        if task_dir is None:
            # resolve_task_dir 已报告原因；保留归档专用拒绝提示，
            # 与 is_within_tasks_dir 检查的表述保持一致。
            print(colored(
                f"错误：拒绝归档 '{task_name}': "
                f"无法将其解析为以下目录下的任务： {tasks_dir}",
                Colors.RED), file=sys.stderr)
        else:
            print(colored(f"错误：找不到任务： {task_name}", Colors.RED), file=sys.stderr)
        print("活跃任务：", file=sys.stderr)
        # 延迟导入，避免循环依赖
        from .tasks import iter_active_tasks
        for t in iter_active_tasks(tasks_dir):
            print(f"  - {t.dir_name}/", file=sys.stderr)
        return 1

    # 仅允许归档直接位于 .trellis/tasks/ 下的真实任务。
    # resolve_task_dir 保证目标在任务根目录内；此处进一步限定为直接子目录，
    # 避免已归档任务（.trellis/tasks/archive/<month>/<task>）再次归档。
    if not is_within_tasks_dir(task_dir, repo_root):
        print(colored(
            f"错误：拒绝归档 '{task_name}': "
            f"{task_dir} 不是以下目录下的任务： {tasks_dir}",
            Colors.RED), file=sys.stderr)
        return 1

    # 在下方任何任务状态变更之前检查目标。移动函数也会拒绝冲突，
    # 但到那时本命令已将任务标为完成、更新子任务父关联，并清除会话指针，
    # 这些操作都需要手工撤销，因此提前检查。
    archive_dest_check = archive_destination_for(task_dir)
    if archive_dest_check.exists():
        print(colored(
            f"错误：拒绝归档 '{task_name}': "
            f"归档目标已存在： "
            f"{_repo_relative_path(archive_dest_check, repo_root)}",
            Colors.RED), file=sys.stderr)
        print(f"任务保留在： {_repo_relative_path(task_dir, repo_root)}", file=sys.stderr)
        print("请先移动或重命名已有归档任务，再重试。", file=sys.stderr)
        return 1

    dir_name = task_dir.name
    task_json_path = task_dir / FILE_TASK_JSON

    # 归档前更新状态
    today = datetime.now().strftime("%Y-%m-%d")
    # 下方修改了 task.json 的子任务目录名；
    # 传入 safe_archive_paths_to_add，将它们暂存到本次提交。
    modified_children: list[str] = []
    if task_json_path.is_file():
        data, read_reason = read_json_checked(task_json_path)
        if data is None:
            # task.json 损坏时仍应归档，但必须告知用户，
            # 否则缺少 completed 状态会让归档看起来无声地只做了一半。
            problem, _ = describe_json_read_failure(task_json_path, read_reason)
            print(
                colored(
                    f"警告： {problem}；归档时不更新 status/children。",
                    Colors.YELLOW,
                ),
                file=sys.stderr,
            )
        else:
            # 在任何变更前检查：任务离开活跃树后分支元数据无法恢复。
            # 失效分支只告警。
            if not _validate_branch_metadata(
                data,
                task_name,
                repo_root,
                getattr(args, "skip_branch_validation", False),
            ):
                print(
                    f"未归档： {_repo_relative_path(task_dir, repo_root)} 保持不变。",
                    file=sys.stderr,
                )
                return 1

            data["status"] = "completed"
            data["completedAt"] = today
            if not write_json(task_json_path, data):
                _report_write_failure(task_json_path)
                print(
                    f"未归档： {_repo_relative_path(task_dir, repo_root)} 保持不变。 "
                    "归档仍标记为进行中的任务会将其从 `list` 中隐藏，"
                    "却保留错误状态。",
                    file=sys.stderr,
                )
                return 1

            # 归档时处理子任务关系。
            # 保留父任务 children 列表中的当前任务，确保进度统计
            # （children_progress）一致；不在活跃集合中的子任务视为已完成。
            task_children = data.get("children", [])

            # 若当前任务是父任务，清除所有子任务的 parent 字段。
            # 记录每个移除的关联，便于循环中后续失败时恢复，见 _restore_child_links。
            # 以子任务的 task.json 为键：children 若重复列出同一子任务，
            # 否则会第二次记录已清空的快照，并在恢复时用 null 覆盖真实父关联。
            unlinked_children: dict[Path, str | None] = {}
            if task_children:
                for child_name in task_children:
                    child_dir_path = find_task_by_name(child_name, tasks_dir)
                    if child_dir_path:
                        child_json = child_dir_path / FILE_TASK_JSON
                        if child_json.is_file():
                            child_data, child_reason = read_json_checked(child_json)
                            if child_data is None:
                                problem, _ = describe_json_read_failure(child_json, child_reason)
                                print(
                                    colored(
                                        f"警告： {problem}；子任务 '{child_dir_path.name}' "
                                        "保留其父任务引用。",
                                        Colors.YELLOW,
                                    ),
                                    file=sys.stderr,
                                )
                                continue
                            # 仅在首次处理子任务时记录其原始父任务：children 若重复列出
                            # 同一子任务，否则会对已清空的值建立快照，恢复时用 null 覆盖关联。
                            first_visit = child_json not in unlinked_children
                            original_parent = child_data.get("parent")
                            if first_visit:
                                unlinked_children[child_json] = original_parent
                            child_data["parent"] = None
                            if not write_json(child_json, child_data):
                                # 移动前停止：子任务若指向已离开 .trellis/tasks/ 的父任务，
                                # 就会留下后续无人修复的悬空引用。
                                # 恢复上方已解除关联的子任务，因为父任务会留在原处，
                                # 反向丢失关联同样无法恢复。
                                # 目前各步骤都具备幂等性，可以安全重试。
                                if first_visit:
                                    # 此子任务的关联并未移除，因此无需恢复。
                                    del unlinked_children[child_json]
                                _restore_child_links(unlinked_children)
                                _report_write_failure(child_json)
                                print(
                                    f"未归档： {_repo_relative_path(task_dir, repo_root)} "
                                    f"已标为完成但仍留在原处，因为子任务 "
                                    f"'{child_dir_path.name}' 无法解除关联。"
                                    "请修复子任务后重新归档。",
                                    file=sys.stderr,
                                )
                                return 1
                            modified_children.append(child_dir_path.name)

    # 路径移动之前，清除所有仍指向此任务的会话。
    from .active_task import clear_task_from_sessions
    clear_task_from_sessions(str(task_dir), repo_root)

    # 归档
    result = archive_task_complete(task_dir, repo_root)
    if "archived_to" in result:
        archive_dest = Path(result["archived_to"])
        year_month = archive_dest.parent.name
        print(colored(f"已归档： {dir_name} -> archive/{year_month}/", Colors.GREEN), file=sys.stderr)

        # 未指定 --no-commit 时自动提交
        if not getattr(args, "no_commit", False):
            if not _auto_commit_archive(
                dir_name, repo_root, modified_children, archive_dest=archive_dest
            ):
                print(
                    colored(
                        "归档已在磁盘上移动，但 Git 自动提交未完成。"
                        "继续之前请处理 `git status` 显示的问题。",
                        Colors.RED,
                    ),
                    file=sys.stderr,
                )
                return 1

        # 返回归档路径
        print(f"{DIR_WORKFLOW}/{DIR_TASKS}/{DIR_ARCHIVE}/{year_month}/{dir_name}")

        # 使用归档后的路径运行钩子
        archived_json = archive_dest / FILE_TASK_JSON
        run_task_hooks("after_archive", archived_json, repo_root)
        return 0

    return 1


def _auto_commit_archive(
    task_name: str,
    repo_root: Path,
    modified_children: list[str] | None = None,
    *,
    archive_dest: Path,
) -> bool:
    """暂存 Trellis 所属任务路径，并在归档后提交。

    严格限定为已归档任务的源路径、目标路径，以及本次修改了
    ``task.json`` 的子任务文件（更新父任务到子任务的关系）。
    其他活跃任务目录中的未提交变更不会混入归档提交。

    若路径被 ``.gitignore`` 忽略，则告警并跳过，不使用 ``git add -f`` 重试。
    告警明确禁止 ``git add -f .trellis/``（这会纳入缓存和备份），
    并指明可设置 ``session_auto_commit: false``。

    遵循 ``.trellis/config.yaml`` 的 ``session_auto_commit``：
    设为 ``false`` 时立即返回，不操作 Git；磁盘上的归档移动不受影响。
    """
    if not get_session_auto_commit(repo_root):
        print(
            "[OK] session_auto_commit: false；跳过 Git 暂存和提交。",
            file=sys.stderr,
        )
        return True

    source_rel = f"{DIR_WORKFLOW}/{DIR_TASKS}/{task_name}"
    rc, tracked_out, _ = run_git(
        ["ls-files", "--", source_rel],
        cwd=repo_root,
    )
    source_was_tracked = rc == 0 and bool(tracked_out.strip())

    paths = safe_archive_paths_to_add(
        repo_root, modified_children=modified_children,
        archive_dest=archive_dest,
    )
    if not paths:
        print("[OK] 没有需要提交的任务变更。", file=sys.stderr)
        return True

    success, _, err = safe_git_add(paths, repo_root, retry_on_index_lock=True)
    if not success:
        if err and "ignored by" in err.lower():
            print_gitignore_warning(paths)
        elif stderr_indicates_index_lock(err):
            _print_index_lock_warning(
                "git add", task_name, repo_root, [*paths, source_rel]
            )
        else:
            print(
                f"[WARN] git add 失败： {err.strip() if err else '未知错误'}",
                file=sys.stderr,
            )
        return not source_was_tracked

    # 额外防范“幽灵删除”：safe_git_add 使用 `git add`（无 -A），
    # 仅暂存新增和修改。源任务目录已由 shutil.move 移走，
    # 因此需显式 `git rm --cached` 将源文件删除纳入同一提交；
    # 否则它们会相对 HEAD 保留为未提交的“幽灵删除”，直到后续操作纳入。
    #
    # `--ignore-unmatch` 在任务从未被跟踪时不执行任何操作
    # （例如归档仅存在于工作区的任务）。
    rc, _, err = run_git_retry_index_lock(
        ["rm", "-r", "--cached", "--ignore-unmatch", "--", source_rel],
        cwd=repo_root,
    )
    if rc != 0 and stderr_indicates_index_lock(err):
        # 此时提交会记录归档副本，却没有删除源路径，
        # 导致历史中留下只归档了一半的目录树。
        _print_index_lock_warning(
            "git rm --cached", task_name, repo_root, [*paths, source_rel]
        )
        return not source_was_tracked

    rc, _, _ = run_git(
        ["diff", "--cached", "--quiet", "--", *paths, source_rel],
        cwd=repo_root,
    )
    if rc == 0:
        print("[OK] 没有需要提交的任务变更。", file=sys.stderr)
        return True

    commit_msg = f"chore(task): 归档 {task_name}"
    # 提交时使用显式 pathspec：裸 `git commit` 会将开发者在归档前
    # 已暂存的无关条目混入维护提交（#579）。包含 source_rel，
    # 使上方暂存的源路径删除也进入同一提交。
    rc, _, err = run_git_retry_index_lock(
        ["commit", "-m", commit_msg, "--", *paths, source_rel], cwd=repo_root
    )
    if rc == 0:
        print(f"[OK] 已自动提交： {commit_msg}", file=sys.stderr)
        return True
    elif stderr_indicates_index_lock(err):
        _print_index_lock_warning(
            "git commit", task_name, repo_root, [*paths, source_rel]
        )
        return not source_was_tracked
    else:
        print(f"[WARN] 自动提交失败： {err.strip()}", file=sys.stderr)
        return not source_was_tracked


def _print_index_lock_warning(
    action: str, task_name: str, repo_root: Path, paths: list[str]
) -> None:
    """报告归档自动提交因持续存在的 index.lock 而放弃。

    移动操作已经成功，因此状态一致：任务位于 archive/，变更可能已暂存，
    也可能未暂存，但不会只提交一半。仅提交尚未完成，需要用户或读取
    日志的代理手工补做。

    ``paths`` 是自动提交原本会暂存的路径，恢复命令保持同样的窄范围。
    若笼统执行 ``git add -A -- .trellis/``，会将其他活跃任务的未提交
    变更混入归档提交。
    """
    lock = index_lock_path(repo_root)
    print(
        f"[WARN] {action} 重试 {INDEX_LOCK_RETRY_ATTEMPTS} 次后已放弃："
        f"另一个进程正在占用 {lock}",
        file=sys.stderr,
    )
    print(
        "[WARN] 任务已在磁盘上移入 archive/，仅提交尚未完成。",
        file=sys.stderr,
    )
    print(
        "[WARN] 请关闭占用锁的程序（IDE Git 集成、状态",
        file=sys.stderr,
    )
    print(
        f"[WARN] 守护进程或其他会话），若锁已失效则删除 {lock}，然后",
        file=sys.stderr,
    )
    # 提示沿用 POSIX shell 语法；对参数逐项引用，保留空格和特殊字符。
    # 提交也带同一组 pathspec，避免恢复时纳入其他已暂存文件。
    add_command = shlex.join(["git", "add", "-A", "--", *paths])
    commit_command = shlex.join(
        ["git", "commit", "-m", f"chore(task): 归档 {task_name}", "--", *paths]
    )
    print(f"[WARN] 手工提交（POSIX shell）：{add_command} && {commit_command}", file=sys.stderr)


# =============================================================================
# 命令： add-subtask
# =============================================================================

def cmd_add_subtask(args: argparse.Namespace) -> int:
    """将子任务关联到父任务。"""
    repo_root = get_repo_root()

    parent_dir = resolve_task_dir(args.parent_dir, repo_root)
    child_dir = resolve_task_dir(args.child_dir, repo_root)
    if parent_dir is None or child_dir is None:
        return 1

    if not parent_dir:
        print(colored(f"错误：找不到父任务的 task.json： {args.parent_dir}", Colors.RED), file=sys.stderr)
        return 1

    if not child_dir:
        print(colored(f"错误：找不到子任务的 task.json： {args.child_dir}", Colors.RED), file=sys.stderr)
        return 1

    parent_json_path = parent_dir / FILE_TASK_JSON
    child_json_path = child_dir / FILE_TASK_JSON

    if not parent_json_path.is_file():
        print(colored(f"错误：找不到父任务的 task.json： {args.parent_dir}", Colors.RED), file=sys.stderr)
        return 1

    if not child_json_path.is_file():
        print(colored(f"错误：找不到子任务的 task.json： {args.child_dir}", Colors.RED), file=sys.stderr)
        return 1

    parent_data, parent_reason = read_json_checked(parent_json_path)
    if parent_data is None:
        _report_read_failure(parent_json_path, parent_reason)
        return 1
    child_data, child_reason = read_json_checked(child_json_path)
    if child_data is None:
        _report_read_failure(child_json_path, child_reason)
        return 1

    # 检查子任务是否已有父任务
    existing_parent = child_data.get("parent")
    if existing_parent:
        print(colored(f"错误：子任务已有父任务： {existing_parent}", Colors.RED), file=sys.stderr)
        return 1

    # 将子任务加入父任务的 children 列表
    parent_children = _ensure_children_list(parent_data)
    child_dir_name = child_dir.name
    if child_dir_name not in parent_children:
        parent_children.append(child_dir_name)
        parent_data["children"] = parent_children

    # 在子任务的 task.json 中设置 parent
    child_data["parent"] = parent_dir.name

    # 写入双方文件。关联涉及两个文件；第二次写入若静默失败，
    # 双方就会无声地不一致。因此分别检查，并明确哪一侧已写入，
    # 方便用户知道需要撤销的操作。
    if not write_json(parent_json_path, parent_data):
        print(colored(f"错误：无法写入父任务 task.json： {parent_json_path}", Colors.RED), file=sys.stderr)
        print("未写入任何关联。", file=sys.stderr)
        return 1
    if not write_json(child_json_path, child_data):
        print(colored(f"错误：无法写入子任务 task.json： {child_json_path}", Colors.RED), file=sys.stderr)
        print(
            f"关联仅写入一半： {parent_json_path} 现已列出 "
            f"'{child_dir.name}' 为子任务，但子任务尚未记录父任务。",
            file=sys.stderr,
        )
        return 1

    print(colored(f"已关联： {child_dir.name} -> {parent_dir.name}", Colors.GREEN), file=sys.stderr)
    return 0


# =============================================================================
# 命令： remove-subtask
# =============================================================================

def cmd_remove_subtask(args: argparse.Namespace) -> int:
    """解除子任务与父任务的关联。"""
    repo_root = get_repo_root()

    parent_dir = resolve_task_dir(args.parent_dir, repo_root)
    child_dir = resolve_task_dir(args.child_dir, repo_root)
    if parent_dir is None or child_dir is None:
        return 1

    if not parent_dir:
        print(colored(f"错误：找不到父任务的 task.json： {args.parent_dir}", Colors.RED), file=sys.stderr)
        return 1

    if not child_dir:
        print(colored(f"错误：找不到子任务的 task.json： {args.child_dir}", Colors.RED), file=sys.stderr)
        return 1

    parent_json_path = parent_dir / FILE_TASK_JSON
    child_json_path = child_dir / FILE_TASK_JSON

    if not parent_json_path.is_file():
        print(colored(f"错误：找不到父任务的 task.json： {args.parent_dir}", Colors.RED), file=sys.stderr)
        return 1

    if not child_json_path.is_file():
        print(colored(f"错误：找不到子任务的 task.json： {args.child_dir}", Colors.RED), file=sys.stderr)
        return 1

    parent_data, parent_reason = read_json_checked(parent_json_path)
    if parent_data is None:
        _report_read_failure(parent_json_path, parent_reason)
        return 1
    child_data, child_reason = read_json_checked(child_json_path)
    if child_data is None:
        _report_read_failure(child_json_path, child_reason)
        return 1

    # 从父任务的 children 列表移除子任务
    parent_children = _ensure_children_list(parent_data)
    child_dir_name = child_dir.name
    if child_dir_name in parent_children:
        parent_children.remove(child_dir_name)
        parent_data["children"] = parent_children

    # 清空子任务 task.json 中的 parent
    child_data["parent"] = None

    # 写入双方，见 cmd_add_subtask：不检查第二次写入
    # 会让双方在没有报错的情况下不一致。
    if not write_json(parent_json_path, parent_data):
        print(colored(f"错误：无法写入父任务 task.json： {parent_json_path}", Colors.RED), file=sys.stderr)
        print("未解除任何关联。", file=sys.stderr)
        return 1
    if not write_json(child_json_path, child_data):
        print(colored(f"错误：无法写入子任务 task.json： {child_json_path}", Colors.RED), file=sys.stderr)
        print(
            f"解除关联仅写入一半： {parent_json_path} 已不再列出 "
            f"'{child_dir.name}'，但子任务仍记录父任务。",
            file=sys.stderr,
        )
        return 1

    print(colored(f"已解除关联： {child_dir.name} 与 {parent_dir.name}", Colors.GREEN), file=sys.stderr)
    return 0


# =============================================================================
# 命令： set-branch
# =============================================================================

def cmd_set_branch(args: argparse.Namespace) -> int:
    """设置任务的 Git 分支。"""
    repo_root = get_repo_root()
    target_dir = resolve_task_dir(args.dir, repo_root)
    if target_dir is None:
        return 1
    branch = args.branch

    if not branch:
        print(colored("错误：缺少参数", Colors.RED))
        print("用法： python3 task.py set-branch <task-dir> <branch-name>")
        return 1

    if not target_dir:
        # 此处 target_dir 为 None，不能放入提示。
        # 指向仓库外的引用也会进入此分支。
        print(colored(f"错误：找不到任务： {args.dir}", Colors.RED))
        return 1

    task_json = target_dir / FILE_TASK_JSON
    if not task_json.is_file():
        print(colored(f"错误：以下位置找不到 task.json： {target_dir}", Colors.RED))
        return 1

    data, reason = read_json_checked(task_json)
    if data is None:
        _report_read_failure(task_json, reason)
        return 1

    data["branch"] = branch
    if not write_json(task_json, data):
        _report_write_failure(task_json)
        return 1

    print(colored(f"✓ 分支已设为： {branch}", Colors.GREEN))
    return 0


# =============================================================================
# 命令： set-base-branch
# =============================================================================

def cmd_set_base_branch(args: argparse.Namespace) -> int:
    """设置任务的基准分支（PR 目标）。"""
    repo_root = get_repo_root()
    target_dir = resolve_task_dir(args.dir, repo_root)
    if target_dir is None:
        return 1
    base_branch = args.base_branch

    if not base_branch:
        print(colored("错误：缺少参数", Colors.RED))
        print("用法： python3 task.py set-base-branch <task-dir> <base-branch>")
        print("示例： python3 task.py set-base-branch <dir> develop")
        print()
        print("这会设置 PR 目标分支，即功能最终合入的分支。")
        return 1

    if not target_dir:
        # 此处 target_dir 为 None，不能放入提示。
        # 指向仓库外的引用也会进入此分支。
        print(colored(f"错误：找不到任务： {args.dir}", Colors.RED))
        return 1

    task_json = target_dir / FILE_TASK_JSON
    if not task_json.is_file():
        print(colored(f"错误：以下位置找不到 task.json： {target_dir}", Colors.RED))
        return 1

    data, reason = read_json_checked(task_json)
    if data is None:
        _report_read_failure(task_json, reason)
        return 1

    data["base_branch"] = base_branch
    if not write_json(task_json, data):
        _report_write_failure(task_json)
        return 1

    print(colored(f"✓ 基准分支已设为： {base_branch}", Colors.GREEN))
    print(f"  PR 将指向： {base_branch}")
    return 0


# =============================================================================
# 命令： set-scope
# =============================================================================

def cmd_set_scope(args: argparse.Namespace) -> int:
    """设置 PR 标题的范围。"""
    repo_root = get_repo_root()
    target_dir = resolve_task_dir(args.dir, repo_root)
    if target_dir is None:
        return 1
    scope = args.scope

    if not scope:
        print(colored("错误：缺少参数", Colors.RED))
        print("用法： python3 task.py set-scope <task-dir> <scope>")
        return 1

    if not target_dir:
        # 此处 target_dir 为 None，不能放入提示。
        # 指向仓库外的引用也会进入此分支。
        print(colored(f"错误：找不到任务： {args.dir}", Colors.RED))
        return 1

    task_json = target_dir / FILE_TASK_JSON
    if not task_json.is_file():
        print(colored(f"错误：以下位置找不到 task.json： {target_dir}", Colors.RED))
        return 1

    data, reason = read_json_checked(task_json)
    if data is None:
        _report_read_failure(task_json, reason)
        return 1

    data["scope"] = scope
    if not write_json(task_json, data):
        _report_write_failure(task_json)
        return 1

    print(colored(f"✓ 范围已设为： {scope}", Colors.GREEN))
    return 0


# =============================================================================
# 命令： set-meta
# =============================================================================

def cmd_set_meta(args: argparse.Namespace) -> int:
    """设置或覆盖现有任务的一个元数据键。"""
    repo_root = get_repo_root()
    target_dir = resolve_task_dir(args.dir, repo_root)
    if target_dir is None:
        return 1
    key = args.key
    value = args.value

    if not key:
        print(colored("错误：缺少参数", Colors.RED))
        print("用法： python3 task.py set-meta <task-dir> <key> <value>")
        return 1

    if not target_dir:
        # 此处 target_dir 为 None，不能放入提示。
        # 指向仓库外的引用也会进入此分支。
        print(colored(f"错误：找不到任务： {args.dir}", Colors.RED))
        return 1

    task_json = target_dir / FILE_TASK_JSON
    if not task_json.is_file():
        print(colored(f"错误：以下位置找不到 task.json： {target_dir}", Colors.RED))
        return 1

    data, reason = read_json_checked(task_json)
    if data is None:
        _report_read_failure(task_json, reason)
        return 1

    meta = data.get("meta")
    if not isinstance(meta, dict):
        meta = {}
    meta[key] = value
    data["meta"] = meta
    if not write_json(task_json, data):
        _report_write_failure(task_json)
        return 1

    print(colored(f"✓ 已设置元数据： {key} = {value}", Colors.GREEN))
    return 0
