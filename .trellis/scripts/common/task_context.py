#!/usr/bin/env python3
"""任务 JSONL 上下文管理。

提供功能：
    cmd_add_context   - 向 JSONL 上下文文件添加条目
    cmd_validate      - 校验 JSONL 上下文文件
    cmd_list_context  - 列出 JSONL 上下文条目

说明：
    ``cmd_init_context`` 已在 v0.5.0-beta.12 移除。``task.py create``
    会创建空 JSONL 上下文文件；任务需要子代理或规范上下文时，
    由 AI 代理在规划期间整理真实条目。当前规划产物契约见
    ``.trellis/workflow.md``。

    较旧 Trellis 版本会预填 ``{"_example": ...}`` 占位行。
    ``cmd_validate`` 现在拒绝这种行，避免任务本地校验通过后，
    却被 PR 预检当作未完成的脚手架而拒绝。
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from .config import get_context_injection_limits
from .git import branch_exists_locally
from .io import read_json
from .log import Colors, colored
from .paths import DIR_ARCHIVE, DIR_TASKS, DIR_WORKFLOW, FILE_TASK_JSON, get_repo_root
from .task_utils import resolve_task_dir

# 看起来属于代码而非规范或研究文档的扩展名。具有这些扩展名的条目若不在
# .trellis/spec/、docs/docs-site 或任务自身目录下，`task.py validate` 会告警。
# 读取者是子代理，因此代码路径应由代理自行从差异读取，
# 不应放入 implement.jsonl / check.jsonl。
_CODE_FILE_EXTENSIONS = {
    ".ts",
    ".tsx",
    ".js",
    ".jsx",
    ".mjs",
    ".cjs",
    ".py",
    ".go",
    ".rs",
    ".java",
    ".rb",
    ".c",
    ".cc",
    ".cpp",
    ".h",
}


# =============================================================================
# 命令： add-context
# =============================================================================

def cmd_add_context(args: argparse.Namespace) -> int:
    """向 JSONL 上下文文件添加条目。"""
    repo_root = get_repo_root()
    target_dir = resolve_task_dir(args.dir, repo_root)
    if target_dir is None:
        return 1

    jsonl_name = args.file
    path = args.path
    reason = args.reason or "手动添加"

    if not target_dir or not target_dir.is_dir():
        print(colored(f"错误：找不到目录： {target_dir}", Colors.RED))
        return 1

    # JSONL 名称来自用户输入并拼接到任务目录，因此必须是普通文件名，
    # 避免在其他位置创建文件。
    if "/" in jsonl_name or "\\" in jsonl_name or jsonl_name in (".", ".."):
        print(colored(
            f"错误：上下文文件必须使用普通名称（例如 implement、check）： {jsonl_name}",
            Colors.RED,
        ))
        return 1

    # 支持简写
    if not jsonl_name.endswith(".jsonl"):
        jsonl_name = f"{jsonl_name}.jsonl"

    jsonl_file = target_dir / jsonl_name
    full_path = repo_root / path

    entry_type = "file"
    if full_path.is_dir():
        entry_type = "directory"
        if not path.endswith("/"):
            path = f"{path}/"
    elif not full_path.is_file():
        print(colored(f"错误：找不到路径： {path}", Colors.RED))
        return 1

    # 检查条目是否已存在
    if jsonl_file.is_file():
        content = jsonl_file.read_text(encoding="utf-8")
        if f'"{path}"' in content:
            print(colored(f"警告：条目已存在： {path}", Colors.YELLOW))
            return 0

    # 添加条目
    entry: dict
    if entry_type == "directory":
        entry = {"file": path, "type": "directory", "reason": reason}
    else:
        entry = {"file": path, "reason": reason}

    with jsonl_file.open("a", encoding="utf-8") as f:
        f.write(json.dumps(entry, ensure_ascii=False) + "\n")

    print(colored(f"已添加 {entry_type}： {path}", Colors.GREEN))
    return 0


# =============================================================================
# 命令： validate
# =============================================================================

def curated_entry_count(jsonl_file: Path) -> int | None:
    """统计 JSONL 上下文清单中已整理的条目数。

    文件不存在时返回 None：``task.py create`` 仅在支持子代理的平台
    生成清单，文件缺失意味着没有子代理读取，调用者无需据此设门禁。
    已整理条目是含真值 ``file``（或旧版 ``path``）字段的 JSON 对象行，
    与子代理注入钩子实际加载的行一致。
    """
    if not jsonl_file.is_file():
        return None
    try:
        lines = jsonl_file.read_text(encoding="utf-8").splitlines()
    except OSError:
        return 0
    count = 0
    for line in lines:
        line = line.strip()
        if not line:
            continue
        try:
            data = json.loads(line)
        except json.JSONDecodeError:
            continue
        if isinstance(data, dict) and (data.get("file") or data.get("path")):
            count += 1
    return count


def cmd_validate(args: argparse.Namespace) -> int:
    """校验 JSONL 上下文文件。"""
    repo_root = get_repo_root()
    target_dir = resolve_task_dir(args.dir, repo_root)

    if target_dir is None or not target_dir.is_dir():
        print(colored("错误：必须提供任务目录", Colors.RED))
        return 1

    print(colored("=== 校验上下文文件 ===", Colors.BLUE))
    print(f"目标目录： {target_dir}")
    print()

    # 已记录分支失效时只告警，不让校验失败；
    # 分支很可能已经合并并删除（#399 第 2 项）。
    task_json_path = target_dir / FILE_TASK_JSON
    if task_json_path.is_file():
        task_data = read_json(task_json_path)
        stored_branch = task_data.get("branch") if task_data else None
        if stored_branch and not branch_exists_locally(stored_branch, repo_root):
            print(
                colored(
                    f"警告：已记录分支 '{stored_branch}' 在本地已不存在"
                    "（可能已合并并删除）。",
                    Colors.YELLOW,
                )
            )
            print()

    total_errors = 0
    for jsonl_name in ["implement.jsonl", "check.jsonl"]:
        jsonl_file = target_dir / jsonl_name
        errors = _validate_jsonl(jsonl_file, repo_root, target_dir)
        total_errors += errors

    print()
    if total_errors == 0:
        print(colored("✓ 所有校验通过", Colors.GREEN))
        return 0
    else:
        print(colored(f"✗ 校验失败（{total_errors} 个错误）", Colors.RED))
        return 1


def _is_exempt_from_code_file_warning(file_path: str, task_rel: str) -> bool:
    """判断 JSONL 条目路径是否豁免代码文件规范告警。

    豁免范围包括规范文档（``.trellis/spec/``）、文档（``docs``、
    ``docs-site``）及任务自身目录（执行计划、生成产物等合理放在此处）。
    """
    posix_path = file_path.replace("\\", "/").lstrip("/")
    exempt_prefixes = (".trellis/spec/", "docs/", "docs-site/")
    if posix_path.startswith(exempt_prefixes):
        return True
    if task_rel and (posix_path == task_rel or posix_path.startswith(f"{task_rel}/")):
        return True
    return False


def _resolve_context_entry_path(
    file_path: str, repo_root: Path, task_dir: Path | None
) -> Path | None:
    """解析 JSONL 条目，将归档任务的自引用绑定到归档副本。

    仅为归档任务重映射精确的历史自引用。
    ``None`` 表示重映射路径穿越或解析到了该归档之外。
    """
    repo_path = repo_root / file_path
    if task_dir is None:
        return repo_path

    try:
        task_parts = task_dir.resolve().relative_to(repo_root.resolve()).parts
    except ValueError:
        return repo_path

    archive_prefix = (DIR_WORKFLOW, DIR_TASKS, DIR_ARCHIVE)
    if len(task_parts) != 5 or task_parts[:3] != archive_prefix:
        return repo_path

    year_month = task_parts[3]
    if (
        len(year_month) != 7
        or year_month[4] != "-"
        or not year_month[:4].isdigit()
        or not year_month[5:].isdigit()
    ):
        return repo_path

    historical_root = f"{DIR_WORKFLOW}/{DIR_TASKS}/{task_dir.name}"
    posix_path = file_path.replace("\\", "/")
    if posix_path == historical_root:
        relative_parts: tuple[str, ...] = ()
    elif posix_path.startswith(f"{historical_root}/"):
        relative_path = posix_path[len(historical_root) + 1 :]
        if relative_path.endswith("/"):
            relative_path = relative_path[:-1]
        relative_parts = tuple(relative_path.split("/")) if relative_path else ()
        if any(part in ("", ".", "..") for part in relative_parts):
            return None
    else:
        return repo_path

    try:
        archive_root = task_dir.resolve()
        resolved_path = task_dir.joinpath(*relative_parts).resolve()
        resolved_path.relative_to(archive_root)
    except (OSError, RuntimeError, ValueError):
        return None
    return resolved_path


def _validate_jsonl(jsonl_file: Path, repo_root: Path, task_dir: Path | None = None) -> int:
    """校验单个 JSONL 文件。

    旧版 Trellis 写入的 ``{"_example": ...}`` 占位行属于硬错误：
    PR 预检将其视为未完成的脚手架，若在此接受就会本地通过、后续失败。
    其他缺少 ``file`` 字段的行静默跳过，与读取端保持一致。

    除硬错误（文件或目录缺失、JSON 无效）外，还打印不阻断的规范告警
    （不计入 ``errors``，不改变退出码）：条目看起来是代码文件而非
    规范或研究文档，或文件大小超过配置的子代理上下文注入上限
    （``context_injection.max_file_bytes``）。
    """
    file_name = jsonl_file.name
    errors = 0

    if not jsonl_file.is_file():
        print(f"  {colored(f'{file_name}: 未找到（已跳过）', Colors.YELLOW)}")
        return 0

    task_rel = ""
    if task_dir is not None:
        try:
            task_rel = task_dir.resolve().relative_to(repo_root.resolve()).as_posix()
        except ValueError:
            task_rel = ""

    max_file_bytes = get_context_injection_limits(repo_root).get("max_file_bytes", 0)

    line_num = 0
    real_entries = 0
    for line in jsonl_file.read_text(encoding="utf-8").splitlines():
        line_num += 1
        if not line.strip():
            continue

        try:
            data = json.loads(line)
        except json.JSONDecodeError:
            print(f"  {colored(f'{file_name}:{line_num}: JSON 无效', Colors.RED)}")
            errors += 1
            continue

        if not isinstance(data, dict):
            print(
                f"  {colored(f'{file_name}:{line_num}: 应为 JSON 对象', Colors.RED)}"
            )
            errors += 1
            continue

        if "_example" in data:
            error_message = (
                f"{file_name}:{line_num}: 旧版本留下了 `_example` 占位行："
                "task.py create；请删除此行，或替换为"
                '{"file": "<path>", "reason": "<why>"}'
            )
            print(f"  {colored(error_message, Colors.RED)}")
            errors += 1
            continue

        file_path = data.get("file")
        entry_type = data.get("type", "file")

        if not file_path:
            # 注释行或没有路径的未知行，静默跳过
            continue

        if not isinstance(file_path, str):
            # 真值非字符串（例如 {"file": 1}）若进入路径拼接会触发 TypeError，
            # 导致校验在本应报告问题的行上崩溃。
            print(
                f"  {colored(f'{file_name}:{line_num}: `file` 必须是字符串路径', Colors.RED)}"
            )
            errors += 1
            continue

        real_entries += 1
        full_path = _resolve_context_entry_path(file_path, repo_root, task_dir)
        if entry_type == "directory":
            if full_path is None or not full_path.is_dir():
                print(f"  {colored(f'{file_name}:{line_num}: 找不到目录： {file_path}', Colors.RED)}")
                errors += 1
            continue

        if full_path is None or not full_path.is_file():
            print(f"  {colored(f'{file_name}:{line_num}: 找不到文件： {file_path}', Colors.RED)}")
            errors += 1
            continue

        extension = Path(file_path).suffix.lower()
        if extension in _CODE_FILE_EXTENSIONS and not _is_exempt_from_code_file_warning(
            file_path, task_rel
        ):
            warning_message = (
                f"{file_name}:{line_num}: 警告：{file_path} 看起来是代码文件；"
                "implement/check.jsonl 应引用规范或研究文档；"
                "代理自行读取代码"
            )
            print(f"  {colored(warning_message, Colors.YELLOW)}")

        if max_file_bytes:
            # 这只是建议性规范告警，不能导致 `validate` 失败。
            # 上方 `is_file()` 检查通过后，若权限改变或文件被删除，
            # `stat()` 仍可能抛出异常。
            try:
                size: int | None = full_path.stat().st_size
            except OSError:
                size = None
            if size is not None and size > max_file_bytes:
                warning_message = (
                    f"{file_name}:{line_num}: 警告：{file_path} 大小为 {size} 字节，"
                    f"超过 context_injection.max_file_bytes（{max_file_bytes}）；"
                    "注入时将截断内容"
                )
                print(f"  {colored(warning_message, Colors.YELLOW)}")

    if errors == 0 and real_entries == 0:
        # 仅有初始内容或空清单，会让该任务的子代理没有任何规范上下文（#573）。
        # 过去这里静默通过，导致报告中超过一半任务未整理上下文。
        action = file_name.split(".", 1)[0]
        print(
            f"  {colored(f'{file_name}: ✗ (已整理条目为 0，子代理将没有规范上下文)', Colors.RED)}"
        )
        print(
            f"    整理清单：  python3 .trellis/scripts/task.py add-context <task> {action} <path> \"<why>\""
        )
        print(
            "    若有意留空，可在启动时跳过检查： task.py start <task> --allow-empty-context"
        )
        return 1

    if errors == 0:
        print(f"  {colored(f'{file_name}: ✓ （{real_entries} 个条目）', Colors.GREEN)}")
    else:
        print(f"  {colored(f'{file_name}: ✗ （{errors} 个错误）', Colors.RED)}")

    return errors


# =============================================================================
# 命令： list-context
# =============================================================================

def cmd_list_context(args: argparse.Namespace) -> int:
    """列出 JSONL 上下文条目。"""
    repo_root = get_repo_root()
    target_dir = resolve_task_dir(args.dir, repo_root)

    if target_dir is None or not target_dir.is_dir():
        print(colored("错误：必须提供任务目录", Colors.RED))
        return 1

    print(colored("=== 上下文文件 ===", Colors.BLUE))
    print()

    for jsonl_name in ["implement.jsonl", "check.jsonl"]:
        jsonl_file = target_dir / jsonl_name
        if not jsonl_file.is_file():
            continue

        print(colored(f"[{jsonl_name}]", Colors.CYAN))

        count = 0
        curated = False
        for line in jsonl_file.read_text(encoding="utf-8").splitlines():
            if not line.strip():
                continue

            try:
                data = json.loads(line)
            except json.JSONDecodeError:
                continue

            if not isinstance(data, dict):
                continue

            file_path = data.get("file")
            if not file_path:
                # 占位或注释行，不计入真实条目
                continue
            curated = True

            count += 1
            entry_type = data.get("type", "file")
            reason = data.get("reason", "-")

            if entry_type == "directory":
                print(f"  {colored(f'{count}.', Colors.GREEN)} [DIR] {file_path}")
            else:
                print(f"  {colored(f'{count}.', Colors.GREEN)} {file_path}")
            print(f"     {colored('→', Colors.YELLOW)} {reason}")

        if not curated:
            print(f"  {colored('（尚未整理任何条目）', Colors.YELLOW)}")

        print()

    return 0
