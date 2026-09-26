#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""向日志文件新增会话记录并更新 index.md。

用法：
    python3 add_session.py --title "标题" --commit "hash" --summary "摘要" [--package cli]
    python3 add_session.py --title "标题" --branch "feat/my-branch"

    # 通过标准输入传入详细内容（使用 --stdin 显式启用）：
    cat << 'EOF' | python3 add_session.py --stdin --title "标题" --summary "摘要"
    <在此填写会话内容>
    EOF

    # 结构化内容（可以重复；没有条目的章节将省略）：
    python3 add_session.py --title "标题" --change "完成 X" --test "运行 Y" --next-step "处理 Z"

    # 本地对象数据库无法解析的提交（经过 amend 或尚未获取）
    # 必须显式提供标题，每个 OID 对应一个：
    python3 add_session.py --title "标题" --commit "abc1234" \
        --commit-subject "abc1234=fix(cli): 修复清单截断"

提交证据：
    每个 --commit 参数项必须是有长度限制的十六进制 OID（7–40 个字符）。
    在任何写入之前，从本地对象数据库解析每个提交的真实标题。
    OID 无法解析时，命令会在触碰日志或索引之前失败，绝不写入占位说明。
    没有提交的规划会话使用默认值 "-"。

重试收敛：
    日志追加、索引行和可选的自动提交共同构成一个可恢复操作。
    每条记录都携带由自身输入生成的指纹标记，因此中断后重新执行相同命令
    会修复待完成记录而不会重复追加；自动提交失败时，以非零状态退出并给出恢复检查点。
    已提交记录绝不会被复用：之后相同的请求会创建新会话，除非显式使用
    --idempotency-key 指定幂等键。

分支解析顺序：
    1. --branch 命令行参数（显式指定）
    2. 当前任务 task.json 的 branch 字段（对应分支仍存在时）
    3. git branch --show-current（自动检测）
    4. None（省略分支）"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from datetime import datetime
from pathlib import Path

from common.paths import (
    DIR_TASKS,
    DIR_WORKFLOW,
    FILE_JOURNAL_PREFIX,
    get_repo_root,
    get_current_task,
    get_developer,
    get_workspace_dir,
)
from common.developer import ensure_developer
from common.git import branch_exists_locally, run_git
from common.io import write_text_atomic
from common.log import Colors, colored
from common.safe_commit import (
    print_gitignore_warning,
    safe_git_add,
    safe_trellis_paths_to_add,
)
from common.tasks import load_task
from common.types import TaskInfo
from common.config import (
    get_packages,
    get_session_auto_commit,
    get_session_commit_message,
    get_max_journal_lines,
    is_monorepo,
    resolve_package,
    validate_package,
)


# 限定长度且可安全用于 argv 的输入格式。预检时拒绝格式之外的内容，
# 此时尚未写入任何字节。
COMMIT_TOKEN_RE = re.compile(r"^[0-9a-fA-F]{7,40}$")
IDEMPOTENCY_KEY_RE = re.compile(r"^[A-Za-z0-9._-]{1,64}$")
MAX_SUBJECT_LEN = 500
DEFAULT_SUMMARY = "未提供会话摘要。"
# 原英文默认摘要也是旧记录指纹的输入；仅作为机器身份保留，界面使用中文。
LEGACY_DEFAULT_SUMMARY = "Session summary was not supplied."

# 机器可读的记录身份。HTML 注释不会显示，
# 因此它的存在不改变条目中的可读证据。
MARKER_PREFIX = "<!-- trellis-session:"
# 指纹输入变化时递增版本。无版本标记采用 v1，
# 指纹包含日历日期；见 compute_record_fingerprint。
MARKER_VERSION = 2
LEGACY_MARKER_RE = re.compile(r"^<!-- trellis-session: fp=([0-9a-f]{16}) -->$")
ENTRY_DATE_RE = re.compile(r"^\*\*(?:Date|日期)\*\*[:：]\s*(\d{4}-\d{2}-\d{2})\s*$")
SESSION_HEADING_RE = re.compile(r"^## (?:Session|会话) (\d+)[:：]", re.MULTILINE)
SESSION_COUNT_RE = re.compile(r"(?:Total Sessions|会话总数)[^\d\r\n]{0,8}(\d+)")

# 按操作推进顺序排列的记录状态。
STATE_ABSENT = "absent"
STATE_JOURNAL_RECORDED = "journal-recorded"
STATE_INDEX_RECORDED = "index-recorded"
STATE_COMMITTED = "committed"

# 自动提交结果。
COMMIT_DONE = "committed"
COMMIT_SKIPPED = "skipped"
COMMIT_BLOCKED = "blocked"
COMMIT_FAILED = "failed"

# 对其他引用的 Git 探测尽力执行，绝不让单次探测阻塞会话。
GIT_PROBE_TIMEOUT = 15.0
# 会话编号计算所纳入的引用数量上限。即使仓库有数百个
# 过期分支，也不能让记录会话退化为遍历整棵树。
MAX_CONVERGENCE_REFS = 100


# =============================================================================
# 辅助函数
# =============================================================================

def get_latest_journal_info(dev_dir: Path) -> tuple[Path | None, int, int]:
    """获取最新日志文件的信息。

    返回：
        (文件路径、文件编号、行数) 元组。"""
    latest_file: Path | None = None
    latest_num = -1

    for f in dev_dir.glob(f"{FILE_JOURNAL_PREFIX}*.md"):
        if not f.is_file():
            continue

        match = re.search(r"(\d+)$", f.stem)
        if match:
            num = int(match.group(1))
            if num > latest_num:
                latest_num = num
                latest_file = f

    if latest_file:
        lines = len(latest_file.read_text(encoding="utf-8").splitlines())
        return latest_file, latest_num, lines

    return None, 0, 0


def get_current_session(index_file: Path) -> int:
    """从 index.md 获取当前会话编号。"""
    if not index_file.is_file():
        return 0

    content = index_file.read_text(encoding="utf-8")
    return _max_session_in_index(content)


def _max_session_in_index(content: str) -> int:
    """读取 index.md 中英文或中文的会话总数，混合内容取最大值。"""
    numbers = [int(match.group(1)) for match in SESSION_COUNT_RE.finditer(content)]
    return max(numbers, default=0)


def _max_session_in_journal(content: str) -> int:
    """取日志正文中英文或中文会话标题的最大编号；无记录时返回 0。"""
    numbers = [int(m.group(1)) for m in SESSION_HEADING_RE.finditer(content)]
    return max(numbers) if numbers else 0


def _extract_journal_num(filename: str) -> int:
    """从文件名提取日志编号，用于排序。"""
    match = re.search(r"(\d+)", filename)
    return int(match.group(1)) if match else 0


def count_journal_files(dev_dir: Path, active_num: int) -> str:
    """统计日志文件并返回表格行。"""
    active_file = f"{FILE_JOURNAL_PREFIX}{active_num}.md"
    result_lines = []

    files = sorted(
        [f for f in dev_dir.glob(f"{FILE_JOURNAL_PREFIX}*.md") if f.is_file()],
        key=lambda f: _extract_journal_num(f.stem),
        reverse=True
    )

    for f in files:
        filename = f.name
        lines = len(f.read_text(encoding="utf-8").splitlines())
        status = "当前" if filename == active_file else "已归档"
        result_lines.append(f"| `{filename}` | ~{lines} | {status} |")

    return "\n".join(result_lines)


def get_current_git_branch(repo_root: Path) -> str | None:
    """返回当前检出分支；分离 HEAD 或非 Git 环境返回 None。"""
    rc, branch_out, _ = run_git(["branch", "--show-current"], cwd=repo_root)
    if rc != 0:
        return None
    detected = branch_out.strip()
    return detected or None


def branch_ref_exists(repo_root: Path, branch: str) -> bool:
    """分支存在于本地或本地 origin 跟踪引用时返回 True。"""
    for ref in (f"refs/heads/{branch}", f"refs/remotes/origin/{branch}"):
        rc, _, _ = run_git(["show-ref", "--verify", "--quiet", ref], cwd=repo_root)
        if rc == 0:
            return True
    return False


def resolve_session_branch(
    repo_root: Path,
    cli_branch: str | None,
    task_data: TaskInfo | None,
) -> str | None:
    """解析日志分支，不信任 task.json 中已过期的分支字段。"""
    if cli_branch:
        return cli_branch

    current_branch = get_current_git_branch(repo_root)
    raw_task_branch = task_data.raw.get("branch") if task_data else None
    task_branch = raw_task_branch.strip() if isinstance(raw_task_branch, str) else ""
    if not task_branch:
        return current_branch

    if branch_ref_exists(repo_root, task_branch):
        return task_branch

    if current_branch:
        print(
            f"警告：task.json 中的分支 '{task_branch}' 在本地及 origin/{task_branch} 均已不存在；使用当前分支 '{current_branch}'。",
            file=sys.stderr,
        )
        return current_branch

    print(
        f"警告：task.json 中的分支 '{task_branch}' 在本地及 origin/{task_branch} 均已不存在；省略分支。",
        file=sys.stderr,
    )
    return None


def is_git_worktree(repo_root: Path) -> bool:
    """判断 repo_root 是否为关联工作树，而非主工作树。

    标准检测方法：将每个工作树的 `git rev-parse --git-dir` 和所有工作树
    共用的 `git rev-parse --git-common-dir` 解析为绝对路径并比较。
    在主工作树中，这两个路径指向同一目录。"""
    rc_dir, git_dir, _ = run_git(["rev-parse", "--git-dir"], cwd=repo_root)
    rc_common, git_common_dir, _ = run_git(
        ["rev-parse", "--git-common-dir"], cwd=repo_root
    )
    if rc_dir != 0 or rc_common != 0:
        return False

    git_dir_path = (repo_root / git_dir.strip()).resolve()
    git_common_dir_path = (repo_root / git_common_dir.strip()).resolve()
    return git_dir_path != git_common_dir_path


def warn_if_parallel_worktree(repo_root: Path) -> None:
    """提示并行工作树或分支的 index.md 冲突属于预期情况，可安全处理。

    仅在关联 Git 工作树（非主工作树）且启用 session_auto_commit 时提示，
    不阻断操作（#415 的快速修复层级）。"""
    if not get_session_auto_commit(repo_root):
        return
    if not is_git_worktree(repo_root):
        return
    print(
        colored(
            "[提示] 当前处于 Git 工作树且启用了 session_auto_commit："
            "journal-*.md 通过 .gitattributes 自动合并；并行工作树或分支中的 "
            "index.md 冲突属于预期情况，选择任一侧即可安全解决"
            "（任务状态存于 task.json，不在 index.md）。"
            "上游 Trellis 的 .trellis/spec/cli/backend/directory-structure.md "
            '中有工作区日志合并行为说明（本项目不包含该上游规格）。',
            Colors.YELLOW,
        ),
        file=sys.stderr,
    )


def create_new_journal_file(
    dev_dir: Path, num: int, developer: str, today: str, max_lines: int = 2000,
) -> Path | None:
    """创建新日志文件；写入失败返回 None。"""
    prev_num = num - 1
    new_file = dev_dir / f"{FILE_JOURNAL_PREFIX}{num}.md"

    content = f"""# 开发日志 - {developer}（第 {num} 部分）

> 续自 `{FILE_JOURNAL_PREFIX}{prev_num}.md`（约 {max_lines} 行时归档）
> 开始日期：{today}

---

"""
    if not write_text_atomic(new_file, content):
        return None
    return new_file


# =============================================================================
# 提交证据（R1）：只接受准确标题，否则不写入
# =============================================================================

def parse_commit_tokens(commit: str) -> tuple[list[str], str | None]:
    """将 --commit 拆分为有长度限制的十六进制 OID，返回 (参数项列表, 错误)。"""
    raw = (commit or "").strip()
    if not raw or raw == "-":
        return [], None

    tokens: list[str] = []
    for part in raw.split(","):
        token = part.strip()
        if not token:
            continue
        if not COMMIT_TOKEN_RE.match(token):
            return [], (
                f"--commit 参数项 '{token}' 无效：应为 7–40 个字符的十六进制提交 OID，"
                "没有提交的规划会话使用 '-'"
            )
        token = token.lower()
        if token not in tokens:
            tokens.append(token)
    return tokens, None


def parse_subject_overrides(values: list[str] | None) -> tuple[dict[str, str], str | None]:
    """将 `--commit-subject <oid>=<subject>` 解析为一一对应的映射。"""
    overrides: dict[str, str] = {}
    for raw in values or []:
        if "=" not in raw:
            return {}, (
                f"--commit-subject '{raw}' 无效：应为 <oid>=<subject>"
            )
        oid_part, subject = raw.split("=", 1)
        oid = oid_part.strip().lower()
        subject = subject.strip()
        if not COMMIT_TOKEN_RE.match(oid):
            return {}, (
                f"--commit-subject 键 '{oid_part.strip()}' 无效："
                "应为 7–40 个字符的十六进制提交 OID"
            )
        if not subject:
            return {}, f"'{oid}' 的 --commit-subject 无效：标题为空"
        if len(subject) > MAX_SUBJECT_LEN:
            return {}, (
                f"'{oid}' 的 --commit-subject 无效：标题超过 "
                f"{MAX_SUBJECT_LEN} 个字符"
            )
        if oid in overrides:
            return {}, f"'{oid}' 的 --commit-subject 映射重复"
        overrides[oid] = subject
    return overrides, None


def resolve_commit_subject(repo_root: Path, oid: str) -> str | None:
    """从本地对象数据库解析单个 OID 的真实提交标题。

    使用 argv 传参，绝不拼入 shell；通过 ``^{commit}`` 解引用，
    防止十六进制外观的引用名解析为树或标签。
    对象缺失、有歧义或没有标题时返回 None。"""
    rc, out, _ = run_git(
        ["show", "-s", "--format=%s", f"{oid}^{{commit}}", "--"], cwd=repo_root
    )
    if rc != 0:
        return None
    for line in out.splitlines():
        subject = line.strip()
        if subject:
            return subject[:MAX_SUBJECT_LEN]
    return None


def build_commit_evidence(
    repo_root: Path,
    tokens: list[str],
    overrides: dict[str, str],
) -> tuple[list[tuple[str, str]], str | None]:
    """为每个提交 OID 配对准确标题；无法配对则失败。

    不存在占位分支：OID 既无法在本地解析，也没有显式映射时，
    在任何修改发生之前停止命令。"""
    evidence: list[tuple[str, str]] = []
    unresolved: list[str] = []

    for oid in tokens:
        if oid in overrides:
            evidence.append((oid, overrides[oid]))
            continue
        subject = resolve_commit_subject(repo_root, oid)
        if subject is None:
            unresolved.append(oid)
            continue
        evidence.append((oid, subject))

    if unresolved:
        return [], (
            "无法解析以下提交的证据："
            + ", ".join(unresolved)
            + "。请获取对象、修正 OID，或使用 --commit-subject <oid>=<subject> "
            "传入准确标题。未写入任何内容。"
        )

    unused = sorted(oid for oid in overrides if oid not in tokens)
    if unused:
        return [], (
            "以下 OID 不在 --commit 中，却提供了 --commit-subject："
            + ", ".join(unused)
            + "。映射必须一一对应。未写入任何内容。"
        )

    return evidence, None


def escape_markdown_cell(text: str) -> str:
    """转义提交标题，使其可安全写入 Markdown 表格单元格。"""
    collapsed = " ".join(text.split())
    return collapsed.replace("\\", "\\\\").replace("|", "\\|")


# =============================================================================
# 记录身份（R2）
# =============================================================================

def _normalize_text(value: str | None) -> str:
    return " ".join((value or "").split())


def _fingerprint_payload(
    developer: str,
    title: str,
    summary: str,
    package: str | None,
    branch: str | None,
    evidence: list[tuple[str, str]],
    changes: list[str] | None,
    extra_content: str | None,
    tests: list[str] | None,
    next_steps: list[str] | None,
    idempotency_key: str | None,
) -> dict:
    """规范化单条记录的语义输入，供两个版本的指纹方案共用。"""
    normalized_summary = _normalize_text(summary)
    if normalized_summary == DEFAULT_SUMMARY:
        normalized_summary = LEGACY_DEFAULT_SUMMARY
    return {
        "developer": developer,
        "title": _normalize_text(title),
        "summary": normalized_summary,
        "package": package or "",
        "branch": branch or "",
        "commits": [[oid, _normalize_text(subject)] for oid, subject in evidence],
        "changes": [_normalize_text(c) for c in changes or []],
        "extra": _normalize_text(extra_content),
        "tests": [_normalize_text(t) for t in tests or []],
        "next_steps": [_normalize_text(n) for n in next_steps or []],
        "idempotency_key": idempotency_key or "",
    }


def _hash_payload(payload: dict) -> str:
    encoded = json.dumps(payload, sort_keys=True, ensure_ascii=False).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()[:16]


def compute_record_fingerprint(payload: dict) -> str:
    """根据单条记录规范化后的语义输入计算定长指纹。

    它是记录仍在工作树中待完成时的重试键，不是全局去重键。
    两次独立会话若文字相同，只有调用者显式说明才能区分，因此绝不接续
    已提交的匹配记录（见 classify_record）。

    v2 刻意移除了日期。v1 包含日历日期，跨午夜重试时会找不到自己的记录：
    日志和索引已写入，但提交失败；重算的指纹不再匹配日志中的标记，
    导致重试为同一会话追加第二条记录。

    移除日期不会错误合并同一天的两次会话，因为它们的日期本来就相同。
    跨日时，日期仅能区分文字、提交、分支和包均逐字节相同的记录。
    cmd_add_session 已拒绝复用这种已提交记录，而是进入载荷的下一代
    `generation`，不复用已提交标记。因此这个键恰好回答所需的问题：
    当前工作树中是否存在输入与此相符的未提交记录？"""
    return _hash_payload(payload)


def compute_legacy_fingerprint(payload: dict, date: str) -> str:
    """计算 v1 指纹，用于识别变更前写入的标记。

    仅用于查找；不再使用此方案写入新记录。"""
    return _hash_payload({**payload, "date": date})


def render_marker(fingerprint: str) -> str:
    """生成机器可读的记录标记；在 Markdown 中不显示。"""
    return f"{MARKER_PREFIX} v={MARKER_VERSION} fp={fingerprint} -->"


def render_legacy_marker(fingerprint: str) -> str:
    """生成无版本标签的 v1 标记形式；仅用于读取，绝不写入。"""
    return f"{MARKER_PREFIX} fp={fingerprint} -->"


# =============================================================================
# 会话编号（避免并行分支间冲突）
# =============================================================================

def _repo_relative(repo_root: Path, path: Path) -> str | None:
    try:
        return path.resolve().relative_to(repo_root.resolve()).as_posix()
    except (OSError, ValueError):
        return None


def _default_branch_local(repo_root: Path) -> str | None:
    """只通过本地信息解析默认分支，绝不访问网络。

    `resolve_default_branch()` 会回退到 `git remote show origin`，可能阻塞等待获取。
    记录会话属于频繁操作，因此只检查本地符号引用和两个常规分支名。"""
    rc, out, _ = run_git(
        ["symbolic-ref", "--quiet", "refs/remotes/origin/HEAD"],
        cwd=repo_root,
        timeout=GIT_PROBE_TIMEOUT,
    )
    if rc == 0 and out.strip():
        return out.strip().rsplit("/", 1)[-1]
    for candidate in ("main", "master"):
        if branch_exists_locally(candidate, repo_root):
            return candidate
    return None


def _convergence_refs(repo_root: Path) -> list[str]:
    """返回需要将其工作区记录状态计入编号的引用。

    按重要性排序并限制数量：先检查 HEAD 和默认分支（各分支最终合并的目标），
    再检查其他本地分支（并行工作树的分支在此，实际出现过编号冲突），
    最后检查远程跟踪引用。"""
    refs: list[str] = []

    def add(ref: str) -> None:
        if ref not in refs:
            refs.append(ref)

    rc, _, _ = run_git(["rev-parse", "--verify", "--quiet", "HEAD"], cwd=repo_root)
    if rc == 0:
        add("HEAD")

    default = _default_branch_local(repo_root)
    if default:
        for ref in (f"refs/heads/{default}", f"refs/remotes/origin/{default}"):
            rc, _, _ = run_git(
                ["show-ref", "--verify", "--quiet", ref], cwd=repo_root
            )
            if rc == 0:
                add(ref)

    for pattern in ("refs/heads/", "refs/remotes/"):
        rc, out, _ = run_git(
            ["for-each-ref", "--format=%(refname)", pattern],
            cwd=repo_root,
            timeout=GIT_PROBE_TIMEOUT,
        )
        if rc != 0:
            continue
        for line in out.splitlines():
            ref = line.strip()
            # refs/remotes/origin/HEAD 等符号引用与其指向的分支重复，
            # 且 git grep 会拒绝其中部分引用。
            if ref and not ref.endswith("/HEAD"):
                add(ref)

    return refs[:MAX_CONVERGENCE_REFS]


def _max_session_across_refs(repo_root: Path, refs: list[str], dev_rel: str) -> int:
    """取任意 refs 中 dev_rel 路径下已记录的最大会话编号。

    使用一次 `git grep` 查询全部引用，避免对每个引用分别执行 ls-tree 和 show。
    实际仓库的分支数量没有上限，逐引用操作的成本会随之增长。
    退出状态 1 表示没有匹配项，是正常结果。"""
    if not refs:
        return 0

    rc, out, _ = run_git(
        [
            "grep",
            "--no-color",
            "-h",
            "-E",
            "-e",
            r"^## (Session|会话) [0-9]+[:：]",
            "-e",
            r"Total Sessions|会话总数",
        ]
        + refs
        + ["--", dev_rel],
        cwd=repo_root,
        timeout=GIT_PROBE_TIMEOUT,
    )
    if rc > 1 or not out:
        return 0

    # `git grep` 可能为命中行加上 `<rev>:<path>:` 前缀，
    # 因此在整行内匹配编号，不限定行首。
    numbers = [int(m.group(1)) for m in re.finditer(r"## (?:Session|会话) (\d+)[:：]", out)]
    numbers += [
        int(m.group(1)) for m in SESSION_COUNT_RE.finditer(out)
    ]
    return max(numbers) if numbers else 0


def max_local_session(dev_dir: Path, index_file: Path) -> int:
    """取工作树中可见的最大会话编号。"""
    highest = get_current_session(index_file)
    for f in dev_dir.glob(f"{FILE_JOURNAL_PREFIX}*.md"):
        if not f.is_file():
            continue
        try:
            content = f.read_text(encoding="utf-8")
        except OSError:
            continue
        highest = max(highest, _max_session_in_journal(content))
    return highest


def resolve_next_session(repo_root: Path, dev_dir: Path, index_file: Path) -> int:
    """综合本地工作树及其他分支，计算下一个会话编号。

    仅根据工作树推导编号时，两个分支在合并前记录会话会使用相同编号
    （2026-08-06 曾观察到两次）。结合所有已记录引用的编号，包括默认分支
    和并行工作树记录的其他本地分支，使编号在并行分支间保持单调。
    分支合并时，`journal-*.md` 的 merge=union 会保留双方条目。"""
    highest = max_local_session(dev_dir, index_file)

    dev_rel = _repo_relative(repo_root, dev_dir)
    if dev_rel:
        highest = max(
            highest,
            _max_session_across_refs(
                repo_root, _convergence_refs(repo_root), dev_rel
            ),
        )

    return highest + 1


# =============================================================================
# 待完成记录分类（R3/R4）
# =============================================================================

def find_marker_entries(dev_dir: Path, marker: str) -> list[tuple[Path, int | None]]:
    """定位携带 marker 的每条日志记录。

    返回 (日志文件, 会话编号)。标记不在英文或中文会话标题正下方时，
    编号为 None；此类待完成证据格式无效，绝不能猜测后将其标为完成。"""
    hits: list[tuple[Path, int | None]] = []
    for f in sorted(dev_dir.glob(f"{FILE_JOURNAL_PREFIX}*.md")):
        if not f.is_file():
            continue
        try:
            lines = f.read_text(encoding="utf-8").splitlines()
        except OSError:
            continue
        for i, line in enumerate(lines):
            if line.strip() != marker:
                continue
            session_num: int | None = None
            for j in range(i - 1, max(-1, i - 4), -1):
                match = SESSION_HEADING_RE.match(lines[j])
                if match:
                    session_num = int(match.group(1))
                    break
            hits.append((f, session_num))
    return hits


def content_at_head(repo_root: Path, path: Path) -> str | None:
    """获取 HEAD 中已提交的文件内容；不存在时返回 None。

    `./` 前缀使路径相对当前工作目录解析。单独使用 `HEAD:<path>` 时，
    路径相对 Git 顶层，而 Git 顶层并不总是 repo_root：Trellis 根目录是
    最近的含 `.trellis/` 的目录，可能位于 Git 根目录之下。没有该前缀时，
    此布局下的每次查找都会失败，已提交记录会被误判为待完成。"""
    rel = _repo_relative(repo_root, path)
    if not rel:
        return None
    rc, out, _ = run_git(["show", f"HEAD:./{rel}"], cwd=repo_root)
    return out if rc == 0 else None


def index_has_session_row(index_file: Path, session_num: int) -> bool:
    """检查会话历史管理区是否已有该会话的索引行。"""
    if not index_file.is_file():
        return False
    try:
        lines = index_file.read_text(encoding="utf-8").splitlines()
    except OSError:
        return False

    row_re = re.compile(r"^\|\s*%d\s*\|" % session_num)
    in_history = False
    for line in lines:
        if "@@@auto:session-history" in line:
            in_history = True
            continue
        if "@@@/auto:session-history" in line:
            in_history = False
            continue
        if in_history and row_re.match(line):
            return True
    return False


def _entry_date_at(lines: list[str], marker_index: int) -> str | None:
    """读取 marker_index 处标记所属条目的英文或中文日期字段。

    generate_session_content 在标记下方两行输出日期。扫描一个短窗口，
    而非依赖固定偏移，可兼容标题附近空行变化，并在到达下一条记录前停止。"""
    for line in lines[marker_index + 1:marker_index + 6]:
        match = ENTRY_DATE_RE.match(line.strip())
        if match:
            return match.group(1)
    return None


def resolve_effective_marker(
    dev_dir: Path,
    payload: dict,
    marker: str,
) -> tuple[str, str | None]:
    """跨两个指纹方案查找该记录实际携带的标记。

    返回 (标记, 错误)。若条目携带 v2 标记，优先使用它；变更后写入的记录
    都采用该方案，只需扫描一次。

    否则查找使用 v1 写入的待完成记录。v1 标记只有哈希，但条目在标记下方
    输出自身日期，因此可以恢复生成 v1 指纹时使用的日期：用该日期重算，
    再与实际标记比较。这是精确匹配；与前后一天的窗口不同，周末后恢复重试
    也不会悄悄失效。

    返回 v1 标记时不修改条目，因为它是一条即将提交的进行中记录。
    在修复途中改写标记会将其推进到状态机没有建模的状态。"""
    if find_marker_entries(dev_dir, marker):
        return marker, None

    matches: set[str] = set()
    for journal in sorted(dev_dir.glob(f"{FILE_JOURNAL_PREFIX}*.md")):
        if not journal.is_file():
            continue
        try:
            lines = journal.read_text(encoding="utf-8").splitlines()
        except OSError:
            continue
        for i, line in enumerate(lines):
            legacy = LEGACY_MARKER_RE.match(line.strip())
            if not legacy:
                continue
            date = _entry_date_at(lines, i)
            if date is None:
                continue
            if compute_legacy_fingerprint(payload, date) == legacy.group(1):
                matches.add(line.strip())

    if len(matches) > 1:
        return "", (
            f"发现 {len(matches)} 条采用旧版无版本标记且匹配此记录的待完成日志。"
            "无法猜测应恢复哪一条；请删除重复条目，或传入 --idempotency-key "
            "记录新会话。"
        )
    if matches:
        return matches.pop(), None
    return marker, None


def classify_record(
    repo_root: Path,
    dev_dir: Path,
    index_file: Path,
    marker: str,
) -> tuple[str, Path | None, int | None, str | None]:
    """判断这条确切记录的当前状态。

    返回 (状态, 日志文件, 会话编号, 错误)。仅接续唯一、精确且尚未提交的匹配项。
    有歧义、格式错误或部分写入的记录均返回错误，让调用方安全失败。"""
    hits = find_marker_entries(dev_dir, marker)

    if not hits:
        return STATE_ABSENT, None, None, None

    if len(hits) > 1:
        files = ", ".join(sorted({p.name for p, _ in hits}))
        return "", None, None, (
            f"发现 {len(hits)} 条日志携带此记录的标记（{files}）。"
            "无法猜测应恢复哪一条；请删除重复条目，或传入 --idempotency-key "
            "记录新会话。"
        )

    journal_file, session_num = hits[0]
    if session_num is None:
        return "", None, None, (
            f"{journal_file.name} 携带此记录的标记，但上方缺少 "
            "'## 会话 N:' 或旧版 '## Session N:' 标题。待完成记录格式错误；"
            "请手工修复后重试。"
        )

    head_content = content_at_head(repo_root, journal_file)
    if head_content is not None and marker in head_content:
        return STATE_COMMITTED, journal_file, session_num, None

    if index_has_session_row(index_file, session_num):
        return STATE_INDEX_RECORDED, journal_file, session_num, None

    recorded_total = get_current_session(index_file)
    if recorded_total > session_num:
        return "", None, None, (
            f"会话 {session_num} 的日志条目没有索引行，但 index.md 已记录 "
            f"{recorded_total} 次会话。修复该行会改写较晚的记录；请手工解决 index.md。"
        )

    return STATE_JOURNAL_RECORDED, journal_file, session_num, None


# =============================================================================
# 渲染
# =============================================================================

def _render_bullet_section(header: str, items: list[str], bullet_prefix: str = "- ") -> str:
    """将 Markdown 章节渲染为项目列表；没有内容时返回空字符串。

    没有提供条目的章节会完全省略，不回退为占位文本。"""
    if not items:
        return ""
    bullets = "\n".join(f"{bullet_prefix}{item}" for item in items)
    return f"\n\n### {header}\n\n{bullets}"


def _render_main_changes(changes: list[str], extra_content: str | None) -> str:
    """通过 --change 条目或自由文本生成主要变更章节。"""
    if changes:
        return _render_bullet_section("主要变更", changes)
    if extra_content:
        return f"\n\n### 主要变更\n\n{extra_content}"
    return ""


def generate_session_content(
    session_num: int,
    title: str,
    evidence: list[tuple[str, str]],
    summary: str,
    today: str,
    marker: str,
    package: str | None = None,
    branch: str | None = None,
    changes: list[str] | None = None,
    extra_content: str | None = None,
    tests: list[str] | None = None,
    next_steps: list[str] | None = None,
) -> str:
    """生成中文会话内容；提交证据保留真实标题。"""
    if evidence:
        commit_table = """| 哈希 | 提交说明 |
|------|---------|"""
        for oid, subject in evidence:
            commit_table += f"\n| `{oid}` | {escape_markdown_cell(subject)} |"
    else:
        commit_table = "（无提交，规划会话）"

    package_line = f"\n**包**: {package}" if package else ""
    branch_line = f"\n**分支**: `{branch}`" if branch else ""

    main_changes_section = _render_main_changes(changes or [], extra_content)
    testing_section = _render_bullet_section("测试", tests or [], bullet_prefix="- [OK] ")
    next_steps_section = _render_bullet_section("后续步骤", next_steps or [])

    return f"""

## 会话 {session_num}: {title}
{marker}

**日期**: {today}
**任务**: {title}{package_line}{branch_line}

### 摘要

{summary}{main_changes_section}

### Git 提交

{commit_table}{testing_section}

### 状态

[OK] **已完成**{next_steps_section}
"""


def format_commit_display(evidence: list[tuple[str, str]]) -> str:
    """将提交 OID 渲染为索引表格中的显示内容。"""
    if not evidence:
        return "-"
    return ", ".join(f"`{oid}`" for oid, _ in evidence)


def update_index(
    index_file: Path,
    dev_dir: Path,
    title: str,
    evidence: list[tuple[str, str]],
    new_session: int,
    active_file: str,
    today: str,
    branch: str | None = None,
) -> bool:
    """使用新会话信息更新 index.md。"""
    commit_display = format_commit_display(evidence)

    # 从 active_file 文件名获取编号
    match = re.search(r"(\d+)", active_file)
    active_num = int(match.group(1)) if match else 0
    files_table = count_journal_files(dev_dir, active_num)

    print(f"正在为会话 {new_session} 更新 index.md……")
    print(f"  标题：{title}")
    print(f"  提交：{commit_display}")
    print(f"  当前文件：{active_file}")
    print()

    content = index_file.read_text(encoding="utf-8")

    if "@@@auto:current-status" not in content:
        print("错误：index.md 中未找到管理标记，请确保标记存在。", file=sys.stderr)
        return False

    # 处理各管理区
    lines = content.splitlines()
    new_lines = []

    in_current_status = False
    in_active_documents = False
    in_session_history = False
    header_written = False

    for line in lines:
        if "@@@auto:current-status" in line:
            new_lines.append(line)
            in_current_status = True
            new_lines.append(f"- **当前文件**: `{active_file}`")
            new_lines.append(f"- **会话总数**: {new_session}")
            new_lines.append(f"- **最后活动日期**: {today}")
            continue

        if "@@@/auto:current-status" in line:
            in_current_status = False
            new_lines.append(line)
            continue

        if "@@@auto:active-documents" in line:
            new_lines.append(line)
            in_active_documents = True
            new_lines.append("| 文件 | 行数 | 状态 |")
            new_lines.append("|------|-------|--------|")
            new_lines.append(files_table)
            continue

        if "@@@/auto:active-documents" in line:
            in_active_documents = False
            new_lines.append(line)
            continue

        if "@@@auto:session-history" in line:
            new_lines.append(line)
            in_session_history = True
            header_written = False
            continue

        if "@@@/auto:session-history" in line:
            in_session_history = False
            new_lines.append(line)
            continue

        if in_current_status:
            continue

        if in_active_documents:
            continue

        if in_session_history:
            # 将旧的 4/6 列和现有 5 列表头统一迁移为中文、仅含分支的 5 列历史表。
            if re.match(
                r"^\|\s*#\s*\|\s*(?:Date|日期)\s*\|\s*(?:Title|标题)\s*\|\s*(?:Commits|提交)\s*\|\s*(?:Branch|分支)\s*\|\s*(?:Base Branch|基准分支)\s*\|\s*$",
                line,
            ):
                new_lines.append("| # | 日期 | 标题 | 提交 | 分支 |")
                continue
            if re.match(r"^\|\s*#\s*\|\s*(?:Date|日期)\s*\|\s*(?:Title|标题)\s*\|\s*(?:Commits|提交)\s*\|\s*(?:Branch|分支)\s*\|\s*$", line):
                new_lines.append("| # | 日期 | 标题 | 提交 | 分支 |")
                continue
            if re.match(r"^\|\s*#\s*\|\s*(?:Date|日期)\s*\|\s*(?:Title|标题)\s*\|\s*(?:Commits|提交)\s*\|\s*$", line):
                new_lines.append("| # | 日期 | 标题 | 提交 | 分支 |")
                continue
            if re.match(r"^\|[-| ]+\|\s*$", line) and not header_written:
                new_lines.append("|---|------|-------|---------|--------|")
                new_lines.append(f"| {new_session} | {today} | {title} | {commit_display} | `{branch or '-'}` |")
                header_written = True
                continue
            new_lines.append(line)
            continue

        new_lines.append(line)

    if not write_text_atomic(index_file, "\n".join(new_lines)):
        print(f"错误：无法写入 {index_file}", file=sys.stderr)
        return False
    print("[OK] 已更新 index.md。")
    return True


# =============================================================================
# 主函数
# =============================================================================

def _auto_commit_workspace(repo_root: Path) -> str:
    """暂存并提交属于 Trellis 的工作区和当前任务路径。

    范围仅限具体产物：当前开发者的日志和 index.md，以及通过
    get_current_task 解析出的当前任务目录。绝不暂存整个 `.trellis/`，
    也不遍历所有活动任务目录（#303：不能把并行窗口中其他任务的未提交
    目录带入会话自动提交）。若 `.gitignore` 阻止这些路径，提示并跳过，
    绝不通过 `-f` 重试。

    遵守 `.trellis/config.yaml` 的 session_auto_commit 设置：为 false 时
    立即返回，不触碰 Git；调用方仍会将日志和索引写入磁盘。

    返回 COMMIT_DONE、COMMIT_SKIPPED、COMMIT_BLOCKED 或 COMMIT_FAILED。
    COMMIT_BLOCKED 表示 `.trellis/` 被 Git 忽略：用户已配置 Git 不管理此树，
    因而像 session_auto_commit: false 一样属于配置性跳过，不是需要重试的失败。"""
    if not get_session_auto_commit(repo_root):
        print(
            "[OK] session_auto_commit: false，跳过 Git 暂存和提交。",
            file=sys.stderr,
        )
        return COMMIT_SKIPPED

    # 当前并非 Git 仓库：与被 Git 忽略的情况一样，这是用户配置的
    # 环境，而非临时 Git 错误。重试不可能成功，
    # 因此不能进入 COMMIT_FAILED 对应的退出码 1 重试循环。
    rc, _, _ = run_git(["rev-parse", "--is-inside-work-tree"], cwd=repo_root)
    if rc != 0:
        print(
            "[WARN] 当前不是 Git 仓库；日志和索引已写入，跳过自动提交。",
            file=sys.stderr,
        )
        return COMMIT_BLOCKED

    commit_msg = get_session_commit_message(repo_root)
    # 解析当前任务，将暂存范围限制在该目录。引用形式为
    # ``.trellis/tasks/<name>``（或位于 archive/ 下），只传入名称。
    current = get_current_task(repo_root)
    if current:
        task_name = Path(current).name
        paths = safe_trellis_paths_to_add(repo_root, task_name=task_name)
    else:
        # 当前任务不明（0 个或至少 2 个并行会话，正是 #303 中的
        # 并行窗口场景）。不能回退到宽泛的
        # `tasks_dir.iterdir()` 扫描，否则会再次把其他任务的未提交
        # 目录带入会话提交。只暂存开发者日志和
        # 索引，跳过所有任务目录。
        paths = [
            p
            for p in safe_trellis_paths_to_add(repo_root, task_name=None)
            if not p.startswith(f"{DIR_WORKFLOW}/{DIR_TASKS}/")
        ]
    if not paths:
        print("[OK] 没有需要提交的工作区变更。", file=sys.stderr)
        return COMMIT_SKIPPED

    success, _, err = safe_git_add(paths, repo_root)
    if not success:
        if err and "ignored by" in err.lower():
            print_gitignore_warning(paths)
            return COMMIT_BLOCKED
        print(
            f"[WARN] git add 失败：{err.strip() if err else '未知错误'}",
            file=sys.stderr,
        )
        return COMMIT_FAILED

    # 检查刚才暂存的路径是否存在已暂存变更。
    rc, _, _ = run_git(
        ["diff", "--cached", "--quiet", "--", *paths], cwd=repo_root
    )
    if rc == 0:
        print("[OK] 没有需要提交的工作区变更。", file=sys.stderr)
        return COMMIT_SKIPPED

    # 提交时显式指定 pathspec。直接执行 `git commit` 会将开发者在
    # 脚本运行前暂存的无关条目一并带入
    # chore 提交（#579）。pathspec 可保留这些暂存工作而不将其提交。
    rc, _, commit_err = run_git(
        ["commit", "-m", commit_msg, "--", *paths], cwd=repo_root
    )
    if rc == 0:
        print(f"[OK] 已自动提交：{commit_msg}", file=sys.stderr)
        return COMMIT_DONE

    print(
        f"[WARN] 自动提交失败：{commit_err.strip()}",
        file=sys.stderr,
    )
    return COMMIT_FAILED


def _print_commit_checkpoint(session_num: int, journal_name: str) -> None:
    """为自动提交失败输出可操作的恢复检查点（R5）。"""
    print("", file=sys.stderr)
    print(
        f"[BLOCKED] 恢复检查点：会话 {session_num} 已记录于 "
        f"{journal_name} 和 index.md，但自动提交尚未完成。",
        file=sys.stderr,
    )
    print(
        "[BLOCKED] 待完成记录按写入后的原样保留，没有回滚任何内容。",
        file=sys.stderr,
    )
    print(
        "[BLOCKED] 修复上面的 Git 错误，再重新执行完全相同的 add_session.py 命令："
        "它会从提交步骤恢复，不会新增第二次会话。",
        file=sys.stderr,
    )


def add_session(
    title: str,
    commit: str = "-",
    summary: str = DEFAULT_SUMMARY,
    changes: list[str] | None = None,
    extra_content: str | None = None,
    tests: list[str] | None = None,
    next_steps: list[str] | None = None,
    auto_commit: bool = True,
    package: str | None = None,
    branch: str | None = None,
    commit_subjects: list[str] | None = None,
    idempotency_key: str | None = None,
) -> int:
    """新增会话；遇到中断记录时恢复执行，避免重复写入。"""
    repo_root = get_repo_root()
    warn_if_parallel_worktree(repo_root)
    ensure_developer(repo_root)

    developer = get_developer(repo_root)
    if not developer:
        print("错误：开发者尚未初始化", file=sys.stderr)
        return 1

    dev_dir = get_workspace_dir(repo_root)
    if not dev_dir:
        print("错误：未找到工作区目录", file=sys.stderr)
        return 1

    # -------------------------------------------------------------------
    # 预检：全部成功之前不执行任何写入。
    # -------------------------------------------------------------------
    if idempotency_key is not None and not IDEMPOTENCY_KEY_RE.match(idempotency_key):
        print(
            f"错误：--idempotency-key '{idempotency_key}' 无效："
            "应为 1–64 个 [A-Za-z0-9._-] 范围内的字符",
            file=sys.stderr,
        )
        return 1

    tokens, token_error = parse_commit_tokens(commit)
    if token_error:
        print(f"错误：{token_error}", file=sys.stderr)
        return 1

    overrides, override_error = parse_subject_overrides(commit_subjects)
    if override_error:
        print(f"错误：{override_error}", file=sys.stderr)
        return 1

    evidence, evidence_error = build_commit_evidence(repo_root, tokens, overrides)
    if evidence_error:
        print(f"错误：{evidence_error}", file=sys.stderr)
        return 1

    max_lines = get_max_journal_lines(repo_root)
    index_file = dev_dir / "index.md"
    today = datetime.now().strftime("%Y-%m-%d")

    payload = _fingerprint_payload(
        developer, title, summary, package, branch, evidence,
        changes, extra_content, tests, next_steps, idempotency_key,
    )
    marker = render_marker(compute_record_fingerprint(payload))

    # `today` 不再参与指纹计算，使记录在跨日期后重试仍可被找到。
    # 它仍用于为输出条目标注日期。
    marker, resolve_error = resolve_effective_marker(dev_dir, payload, marker)
    if resolve_error:
        print(f"错误：{resolve_error}", file=sys.stderr)
        return 1

    state, matched_file, matched_num, classify_error = classify_record(
        repo_root, dev_dir, index_file, marker
    )
    if classify_error:
        print(f"错误：{classify_error}", file=sys.stderr)
        return 1

    if state == STATE_COMMITTED and idempotency_key:
        print(
            f"[OK] 幂等键 '{idempotency_key}' 对应的会话 {matched_num} "
            f"已在 {matched_file.name if matched_file else '日志'} 中记录并提交；"
            "无需操作。",
            file=sys.stderr,
        )
        return 0

    # 已提交记录已经完成，所以后续相同请求属于
    # 合法的新会话。不能复用已提交标记：两条记录携带
    # 同一标记，会让之后每次执行产生歧义，而
    # classify_record 不会猜测该选哪条。因此进入该记录的
    # 下一代。标记仍是由
    # (payload, generation) 决定的纯函数，因此新条目重试时会得到同一
    # 标记并继续恢复。第 0 代完全省略 generation 字段，
    # 使首次生成的标记与此方案之前写入的标记逐字节一致。
    generation = 0
    while state == STATE_COMMITTED:
        generation += 1
        marker = render_marker(
            compute_record_fingerprint({**payload, "generation": generation})
        )
        state, matched_file, matched_num, classify_error = classify_record(
            repo_root, dev_dir, index_file, marker
        )
        if classify_error:
            print(f"错误：{classify_error}", file=sys.stderr)
            return 1

    print("========================================", file=sys.stderr)
    print("新增会话", file=sys.stderr)
    print("========================================", file=sys.stderr)
    print("", file=sys.stderr)

    # -------------------------------------------------------------------
    # 缺失记录 → journal-recorded
    # -------------------------------------------------------------------
    target_file: Path | None
    target_num: int
    new_session: int

    if state == STATE_ABSENT:
        journal_file, current_num, current_lines = get_latest_journal_info(dev_dir)
        new_session = resolve_next_session(repo_root, dev_dir, index_file)

        session_content = generate_session_content(
            new_session, title, evidence, summary, today, marker, package, branch,
            changes=changes, extra_content=extra_content, tests=tests,
            next_steps=next_steps,
        )
        content_lines = len(session_content.splitlines())

        print(f"会话：{new_session}", file=sys.stderr)
        print(f"标题：{title}", file=sys.stderr)
        print(f"提交：{format_commit_display(evidence)}", file=sys.stderr)
        print("", file=sys.stderr)
        print(f"当前日志文件：{FILE_JOURNAL_PREFIX}{current_num}.md", file=sys.stderr)
        print(f"当前行数：{current_lines}", file=sys.stderr)
        print(f"新增内容行数：{content_lines}", file=sys.stderr)
        print(f"追加后的总行数：{current_lines + content_lines}", file=sys.stderr)
        print("", file=sys.stderr)

        target_file = journal_file
        target_num = current_num

        if current_lines + content_lines > max_lines:
            target_num = current_num + 1
            print(f"[!] 超过 {max_lines} 行，正在创建 {FILE_JOURNAL_PREFIX}{target_num}.md", file=sys.stderr)
            target_file = create_new_journal_file(dev_dir, target_num, developer, today, max_lines)
            if target_file is None:
                print(
                    f"错误：无法创建 {FILE_JOURNAL_PREFIX}{target_num}.md；未记录任何内容。",
                    file=sys.stderr,
                )
                return 1
            print(f"已创建：{target_file}", file=sys.stderr)

        if target_file is None:
            print(
                "错误：没有可追加的日志文件，请先运行 init_developer.py。",
                file=sys.stderr,
            )
            return 1

        try:
            existing = target_file.read_text(encoding="utf-8") if target_file.is_file() else ""
        except OSError as exc:
            print(f"错误：无法读取 {target_file}：{exc}", file=sys.stderr)
            return 1

        if not write_text_atomic(target_file, existing + session_content):
            print(
                f"错误：无法向 {target_file.name} 追加会话；原日志内容完整，未记录任何新内容。",
                file=sys.stderr,
            )
            return 1
        print(f"[OK] 已向 {target_file.name} 追加会话。", file=sys.stderr)
        state = STATE_JOURNAL_RECORDED
    else:
        if matched_file is None or matched_num is None:
            print(
                f"错误：恢复状态 '{state}' 没有匹配的日志条目。",
                file=sys.stderr,
            )
            return 1
        target_file = matched_file
        new_session = matched_num
        target_num = _extract_journal_num(target_file.stem)
        print(
            f"[RESUME] 会话 {new_session} 已存在于 {target_file.name} 且尚未提交；"
            f"从 '{state}' 状态继续，不重复追加。",
            file=sys.stderr,
        )
        print("", file=sys.stderr)

    print("", file=sys.stderr)

    # -------------------------------------------------------------------
    # 日志已记录 → index-recorded
    # -------------------------------------------------------------------
    active_file = f"{FILE_JOURNAL_PREFIX}{target_num}.md"
    if state == STATE_JOURNAL_RECORDED:
        if not update_index(
            index_file,
            dev_dir,
            title,
            evidence,
            new_session,
            active_file,
            today,
            branch,
        ):
            print(
                f"[BLOCKED] 恢复检查点：会话 {new_session} 已在 {active_file} 中，"
                "但 index.md 未更新。请修复 index.md，再执行完全相同的命令，"
                "以修复索引行且不新增第二次会话。",
                file=sys.stderr,
            )
            return 1
        state = STATE_INDEX_RECORDED
    else:
        print(f"[OK] index.md 已记录会话 {new_session}。", file=sys.stderr)

    print("", file=sys.stderr)
    print("========================================", file=sys.stderr)
    print(f"[OK] 会话 {new_session} 已成功添加。", file=sys.stderr)
    print("========================================", file=sys.stderr)
    print("", file=sys.stderr)
    print("已更新文件：", file=sys.stderr)
    print(f"  - {target_file.name if target_file else '日志'}", file=sys.stderr)
    print("  - index.md", file=sys.stderr)

    # -------------------------------------------------------------------
    # 索引已记录 → committed
    # -------------------------------------------------------------------
    if not auto_commit:
        return 0

    print("", file=sys.stderr)
    outcome = _auto_commit_workspace(repo_root)

    if outcome == COMMIT_FAILED:
        _print_commit_checkpoint(
            new_session, target_file.name if target_file else "日志"
        )
        return 1

    if outcome == COMMIT_DONE and target_file is not None:
        committed = content_at_head(repo_root, target_file)
        if committed is None or marker not in committed:
            print(
                f"[WARN] 自动提交报告成功，但 HEAD 中的 {target_file.name} "
                "没有本次会话记录。",
                file=sys.stderr,
            )
            _print_commit_checkpoint(new_session, target_file.name)
            return 1

    return 0


# =============================================================================
# 主入口
# =============================================================================

def main() -> int:
    """命令行入口。"""
    parser = argparse.ArgumentParser(
        description="向日志文件新增会话并更新 index.md"
    )
    parser.add_argument("--title", required=True, help="会话标题")
    parser.add_argument(
        "--commit",
        default="-",
        help=(
            "逗号分隔的提交 OID（每项 7–40 个十六进制字符）。在写入前解析每个提交的"
            "真实标题；无法解析的 OID 会使命令失败。规划会话使用 '-'。"
        ),
    )
    parser.add_argument(
        "--commit-subject",
        action="append",
        metavar="OID=SUBJECT",
        help=(
            "为本地无法解析的提交显式提供标题（可重复）。必须与 --commit 一一对应。"
        ),
    )
    parser.add_argument("--summary", default=DEFAULT_SUMMARY, help="简要摘要")
    parser.add_argument("--content-file", help="详细内容文件的路径")
    parser.add_argument("--package", help="包名称标签（如 cli、docs-site）")
    parser.add_argument("--branch", help="分支名称（省略时自动检测）")
    parser.add_argument("--change", action="append", help="主要变更条目（可重复）")
    parser.add_argument("--test", action="append", help="测试条目（可重复）")
    parser.add_argument("--next-step", action="append", help="后续步骤条目（可重复）")
    parser.add_argument(
        "--idempotency-key",
        help=(
            "调用者提供的重试键（[A-Za-z0-9._-]，1–64 个字符）。"
            "相同记录已提交时不执行操作，也不创建新会话。"
        ),
    )
    parser.add_argument("--no-commit", action="store_true",
                        help="跳过工作区变更的自动提交")
    parser.add_argument("--stdin", action="store_true",
                        help="从标准输入读取额外内容（显式启用）")

    args = parser.parse_args()

    extra_content: str | None = None
    if args.content_file:
        content_path = Path(args.content_file)
        if content_path.is_file():
            extra_content = content_path.read_text(encoding="utf-8")
    elif args.stdin:
        extra_content = sys.stdin.read()

    # 只加载一次当前任务，供包和分支解析共用
    repo_root = get_repo_root()
    current = get_current_task(repo_root)
    task_data = load_task(repo_root / current) if current else None

    package = args.package
    if package:
        # 来自命令行：monorepo 中快速报错，单包仓库中忽略
        if not is_monorepo(repo_root):
            print("警告：单包仓库忽略 --package", file=sys.stderr)
            package = None
        elif not validate_package(package, repo_root):
            packages = get_packages(repo_root)
            available = ", ".join(sorted(packages.keys())) if packages else "（无）"
            print(f"错误：未知包 '{package}'。可用包：{available}", file=sys.stderr)
            return 1
    else:
        # 推导顺序：当前任务的 task.json.package → default_package → None
        task_package = task_data.package if task_data else None
        package = resolve_package(task_package, repo_root)

    branch = resolve_session_branch(repo_root, args.branch, task_data)

    return add_session(
        args.title, args.commit, args.summary,
        changes=args.change, extra_content=extra_content, tests=args.test,
        next_steps=args.next_step,
        auto_commit=not args.no_commit,
        package=package,
        branch=branch,
        commit_subjects=args.commit_subject,
        idempotency_key=args.idempotency_key,
    )


if __name__ == "__main__":
    sys.exit(main())
