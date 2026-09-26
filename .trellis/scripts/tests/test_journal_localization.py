"""日志中文生成、旧格式兼容与提交范围的隔离回归检查。"""

from __future__ import annotations

import contextlib
import importlib.util
import io
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPTS_DIR = Path(__file__).resolve().parents[1]
if str(SCRIPTS_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_DIR))

_spec = importlib.util.spec_from_file_location("journal_localization_target", SCRIPTS_DIR / "add_session.py")
assert _spec is not None and _spec.loader is not None
journal = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(journal)
from common.developer import init_developer


class JournalLocalizationTests(unittest.TestCase):
    """所有 Git 操作均在临时仓库中执行，不写当前项目的任务或日志。"""

    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory(prefix="trellis-journal-zh-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.git("init", "-b", "main")
        self.git("config", "user.name", "日志回归")
        self.git("config", "user.email", "journal-test@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.hooksPath", str(self.root / "empty-hooks"))
        (self.root / ".trellis").mkdir()
        (self.root / ".trellis/config.yaml").write_text(
            'session_auto_commit: true\nsession_commit_message: "chore: 记录开发日志"\n',
            encoding="utf-8",
        )
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertTrue(init_developer("tester", self.root))
        self.dev_dir = self.root / ".trellis/workspace/tester"
        self.index = self.dev_dir / "index.md"
        self.log = self.dev_dir / "journal-1.md"
        self.git("add", ".")
        self.git("commit", "-m", "fixture: initial evidence")
        self.base = self.git("rev-parse", "HEAD").strip()

    def git(self, *args: str) -> str:
        env = os.environ.copy()
        for key in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE"):
            env.pop(key, None)
        result = subprocess.run(
            ["git", *args], cwd=self.root, env=env, text=True,
            encoding="utf-8", capture_output=True, check=True,
        )
        return result.stdout

    def record(self, title: str = "中文会话", **kwargs) -> int:
        with (
            patch.object(journal, "get_repo_root", return_value=self.root),
            contextlib.redirect_stdout(io.StringIO()),
            contextlib.redirect_stderr(io.StringIO()),
        ):
            return journal.add_session(title, summary="验证可观察结果", **kwargs)

    def test_new_developer_templates_are_readable_and_parseable(self) -> None:
        self.assertIn("# 开发日志 - tester", self.log.read_text())
        self.assertIn("| # | 日期 | 标题 | 提交 | 分支 |", self.index.read_text())
        self.assertIn("**会话总数**: 0", self.index.read_text())
        self.assertEqual(journal.get_current_session(self.index), 0)
        self.assertEqual(journal.resolve_next_session(self.root, self.dev_dir, self.index), 1)

    def test_mixed_counts_and_headings_use_the_highest_number(self) -> None:
        self.index.write_text("- **Total Sessions**: 4\n- **会话总数**: 9\n", encoding="utf-8")
        self.log.write_text("## Session 7: old\n## 会话 12: 新\n## 会话 11：全角\n", encoding="utf-8")
        self.assertEqual(journal.get_current_session(self.index), 9)
        self.assertEqual(journal.max_local_session(self.dev_dir, self.index), 12)
        self.assertEqual(journal.resolve_next_session(self.root, self.dev_dir, self.index), 13)

    def test_old_and_chinese_dates_recover_legacy_fingerprints(self) -> None:
        payload = journal._fingerprint_payload(
            "tester", "same input", "summary", None, None, [], [], None, [], [], None,
        )
        legacy = journal.render_legacy_marker(journal.compute_legacy_fingerprint(payload, "2026-08-01"))
        modern = journal.render_marker(journal.compute_record_fingerprint(payload))
        for heading in ("Session", "会话"):
            for date_label in ("Date", "日期"):
                with self.subTest(heading=heading, date_label=date_label):
                    self.log.write_text(
                        f"## {heading} 3: 保留历史输入\n{legacy}\n\n**{date_label}**: 2026-08-01\n",
                        encoding="utf-8",
                    )
                    before = self.log.read_bytes()
                    self.assertEqual(journal.resolve_effective_marker(self.dev_dir, payload, modern), (legacy, None))
                    self.assertEqual(journal.find_marker_entries(self.dev_dir, legacy), [(self.log, 3)])
                    state, path, number, error = journal.classify_record(self.root, self.dev_dir, self.index, legacy)
                    self.assertEqual((state, path, number, error), (journal.STATE_JOURNAL_RECORDED, self.log, 3, None))
                    self.assertEqual(self.log.read_bytes(), before)

    def test_english_chinese_and_mixed_history_headers_migrate(self) -> None:
        initial = self.index.read_text()
        labels = (
            ["Date", "Title", "Commits", "Branch", "Base Branch"],
            ["日期", "标题", "提交", "分支", "基准分支"],
            ["Date", "标题", "Commits", "分支", "Base Branch"],
        )
        old_row = "| 1 | 2026-01-02 | original evidence | `123abcd` |"
        for columns in (4, 5, 6):
            for names in labels:
                with self.subTest(columns=columns, names=names):
                    header = "| # | " + " | ".join(names[:columns - 1]) + " |"
                    body = initial.replace("| # | 日期 | 标题 | 提交 | 分支 |", header)
                    body = body.replace("<!-- @@@/auto:session-history -->", old_row + "\n<!-- @@@/auto:session-history -->")
                    self.index.write_text(body, encoding="utf-8")
                    with contextlib.redirect_stdout(io.StringIO()):
                        self.assertTrue(journal.update_index(self.index, self.dev_dir, "迁移", [], 2, self.log.name, "2026-09-26"))
                    result = self.index.read_text()
                    self.assertEqual(result.count("| # | 日期 | 标题 | 提交 | 分支 |"), 1)
                    self.assertEqual(result.count("| 2 | 2026-09-26 | 迁移 | - | `-` |"), 1)
                    self.assertIn(old_row, result)
                    self.assertEqual(journal.get_current_session(self.index), 2)

    def test_cross_ref_git_grep_counts_both_languages(self) -> None:
        self.git("checkout", "-b", "legacy")
        self.index.write_text("- **Total Sessions**: 19\n", encoding="utf-8")
        self.log.write_text("## Session 21: old evidence\n", encoding="utf-8")
        self.git("add", ".trellis/workspace")
        self.git("commit", "-m", "fixture: legacy record")
        self.git("checkout", "main")
        self.git("checkout", "-b", "chinese")
        self.index.write_text("- **会话总数**: 34\n", encoding="utf-8")
        self.log.write_text("## 会话 31: 历史记录\n", encoding="utf-8")
        self.git("add", ".trellis/workspace")
        self.git("commit", "-m", "fixture: 中文记录")
        self.git("checkout", "main")
        rel = self.dev_dir.relative_to(self.root).as_posix()
        self.assertEqual(journal._max_session_across_refs(self.root, ["legacy"], rel), 21)
        self.assertEqual(journal._max_session_across_refs(self.root, ["chinese"], rel), 34)
        self.assertEqual(journal.resolve_next_session(self.root, self.dev_dir, self.index), 35)

    def test_auto_commit_preserves_other_staged_work_and_is_idempotent(self) -> None:
        selected = self.root / ".trellis/tasks/selected"
        selected.mkdir(parents=True)
        (selected / "task.json").write_text('{"title":"本任务"}\n', encoding="utf-8")
        other = self.root / ".trellis/tasks/other"
        other.mkdir()
        (other / "notes.md").write_text("其他任务\n", encoding="utf-8")
        (self.root / "unrelated.txt").write_text("其他暂存内容\n", encoding="utf-8")
        self.git("add", "unrelated.txt", ".trellis/tasks/other")
        expected_staged = {"unrelated.txt", ".trellis/tasks/other/notes.md"}
        with (
            patch.object(journal, "get_current_task", return_value=".trellis/tasks/selected"),
            patch.object(journal, "run_git", wraps=journal.run_git) as captured,
        ):
            self.assertEqual(self.record(commit=self.base, idempotency_key="stable-key"), 0)
        committed_paths = set(self.git("show", "--pretty=format:", "--name-only", "HEAD").splitlines())
        expected_committed = {
            ".trellis/workspace/tester/index.md", ".trellis/workspace/tester/journal-1.md",
            ".trellis/tasks/selected/task.json",
        }
        self.assertEqual(committed_paths, expected_committed)
        self.assertEqual(set(self.git("diff", "--cached", "--name-only").splitlines()), expected_staged)
        commits = [call.args[0] for call in captured.call_args_list if call.args[0][0] == "commit"]
        self.assertEqual(len(commits), 1)
        self.assertEqual(commits[0][:4], ["commit", "-m", "chore: 记录开发日志", "--"])
        self.assertEqual(set(commits[0][4:]), {
            ".trellis/workspace/tester/index.md", ".trellis/workspace/tester/journal-1.md", ".trellis/tasks/selected",
        })
        self.assertIn(f"`{self.base}` | fixture: initial evidence", self.log.read_text())
        before = (self.git("rev-parse", "HEAD"), self.log.read_bytes(), self.index.read_bytes())
        self.assertEqual(self.record(commit=self.base, idempotency_key="stable-key"), 0)
        self.assertEqual((self.git("rev-parse", "HEAD"), self.log.read_bytes(), self.index.read_bytes()), before)
        self.assertEqual(set(self.git("diff", "--cached", "--name-only").splitlines()), expected_staged)

    def test_failed_commit_resumes_without_adding_a_second_record(self) -> None:
        with patch.object(journal, "_auto_commit_workspace", return_value=journal.COMMIT_FAILED):
            self.assertEqual(self.record(), 1)
        content = self.log.read_bytes()
        self.assertEqual(journal.get_current_session(self.index), 1)
        self.assertEqual(self.record(), 0)
        self.assertEqual(self.log.read_bytes(), content)
        self.assertEqual(self.log.read_text().count("## 会话 1:"), 1)
        self.assertEqual(journal.get_current_session(self.index), 1)
        self.assertEqual(self.git("status", "--porcelain"), "")

    def test_translated_default_summary_preserves_old_retry_identity(self) -> None:
        with (
            patch.object(journal, "get_repo_root", return_value=self.root),
            contextlib.redirect_stdout(io.StringIO()),
            contextlib.redirect_stderr(io.StringIO()),
        ):
            # 旧版省略摘要时写入的默认值；升级不能因此改变重试身份。
            self.assertEqual(journal.add_session(
                "升级重试", summary="Session summary was not supplied.",
                auto_commit=False, idempotency_key="upgrade",
            ), 0)
            before = (self.log.read_bytes(), self.index.read_bytes())
            self.assertEqual(journal.add_session("升级重试", idempotency_key="upgrade"), 0)
            committed = self.git("rev-parse", "HEAD")
            self.assertEqual(journal.add_session("升级重试", idempotency_key="upgrade"), 0)
        self.assertEqual((self.log.read_bytes(), self.index.read_bytes()), before)
        self.assertEqual(journal.get_current_session(self.index), 1)
        self.assertEqual(self.git("rev-parse", "HEAD"), committed)

    def test_new_default_summary_renders_in_chinese(self) -> None:
        with (
            patch.object(journal, "get_repo_root", return_value=self.root),
            contextlib.redirect_stdout(io.StringIO()),
            contextlib.redirect_stderr(io.StringIO()),
        ):
            self.assertEqual(journal.add_session("新中文会话", auto_commit=False), 0)
        text = self.log.read_text()
        self.assertIn("未提供会话摘要。", text)
        self.assertNotIn("Session summary was not supplied.", text)

    def test_committed_identical_request_without_key_is_a_new_session(self) -> None:
        self.assertEqual(self.record(), 0)
        self.assertEqual(self.record(), 0)
        self.assertEqual(journal.get_current_session(self.index), 2)
        self.assertIn("## 会话 1:", self.log.read_text())
        self.assertIn("## 会话 2:", self.log.read_text())
        markers = [line for line in self.log.read_text().splitlines() if line.startswith(journal.MARKER_PREFIX)]
        self.assertEqual(len(set(markers)), 2)

    def test_rollover_continues_legacy_number_and_writes_chinese(self) -> None:
        original = "# Legacy journal\n## Session 7: old\n" + "historical evidence\n" * 45
        self.log.write_text(original, encoding="utf-8")
        with patch.object(journal, "get_max_journal_lines", return_value=50):
            self.assertEqual(self.record(auto_commit=False), 0)
        following = self.dev_dir / "journal-2.md"
        self.assertEqual(self.log.read_text(), original)
        self.assertIn("# 开发日志 - tester（第 2 部分）", following.read_text())
        self.assertIn("## 会话 8:", following.read_text())
        self.assertIn("**日期**:", following.read_text())
        self.assertIn("**当前文件**: `journal-2.md`", self.index.read_text())
        self.assertEqual(journal.get_current_session(self.index), 8)

    def test_unresolved_commit_evidence_writes_nothing(self) -> None:
        before = (self.log.read_bytes(), self.index.read_bytes())
        self.assertEqual(self.record(commit="abcdef1234567890"), 1)
        self.assertEqual((self.log.read_bytes(), self.index.read_bytes()), before)


if __name__ == "__main__":
    unittest.main()
