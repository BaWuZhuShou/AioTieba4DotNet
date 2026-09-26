"""任务中文模板、机器字段与归档提交的隔离回归检查。"""

from __future__ import annotations

import contextlib
import io
import json
import os
from pathlib import Path
import shlex
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch


SCRIPTS_DIR = Path(__file__).resolve().parents[1]
if str(SCRIPTS_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_DIR))

from common.task_store import _print_index_lock_warning


class TaskLocalizationTests(unittest.TestCase):
    """通过真实 CLI 操作临时仓库，不创建或归档当前项目的任务。"""

    def setUp(self) -> None:
        temporary = tempfile.TemporaryDirectory(prefix="trellis-task-zh-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name) / "中文仓库 with spaces"
        self.root.mkdir()
        self.env = os.environ.copy()
        for key in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE"):
            self.env.pop(key, None)
        self.env.update({
            "TRELLIS_CONTEXT_ID": "task-localization-fixture",
            "TRELLIS_DEVELOPER": "任务回归",
            "PYTHONDONTWRITEBYTECODE": "1",
        })
        self.git("init", "-b", "main")
        self.git("config", "user.name", "任务回归")
        self.git("config", "user.email", "task-test@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.hooksPath", str(self.root / "empty-hooks"))
        (self.root / ".trellis").mkdir()
        (self.root / ".codex").mkdir()
        (self.root / ".trellis/config.yaml").write_text(
            "session_auto_commit: true\n", encoding="utf-8"
        )
        (self.root / ".gitignore").write_text(
            ".trellis/.runtime/\n", encoding="utf-8"
        )
        (self.root / "unrelated.txt").write_text("原始内容\n", encoding="utf-8")
        self.git("add", ".")
        self.git("commit", "-m", "test: 初始化临时仓库")
        self.git("checkout", "-b", "task/localization")

    def git(self, *args: str) -> str:
        result = subprocess.run(
            ["git", *args], cwd=self.root, env=self.env, text=True,
            encoding="utf-8", capture_output=True, check=True,
        )
        return result.stdout

    def task(self, *args: str, expected: int = 0) -> subprocess.CompletedProcess[str]:
        result = subprocess.run(
            [sys.executable, "-B", str(SCRIPTS_DIR / "task.py"), *args],
            cwd=self.root, env=self.env, text=True, encoding="utf-8",
            capture_output=True,
        )
        self.assertEqual(result.returncode, expected, result.stdout + result.stderr)
        return result

    def create_task(self, slug: str = "chinese-task") -> Path:
        result = self.task(
            "create", "中文任务", "--description", "校验中文模板和机器契约",
            "--slug", slug, "--base-branch", "main", "--no-start",
            "--meta", "tracking=中文值",
        )
        relative = result.stdout.strip().splitlines()[-1]
        return self.root / relative

    def test_chinese_task_and_context_round_trip_keep_machine_schema(self) -> None:
        task_dir = self.create_task()
        data = json.loads((task_dir / "task.json").read_text(encoding="utf-8"))
        self.assertEqual(set(data), {
            "id", "name", "title", "description", "status", "dev_type", "scope",
            "package", "priority", "creator", "assignee", "createdAt", "completedAt",
            "branch", "base_branch", "worktree_path", "commit", "pr_url", "subtasks",
            "children", "parent", "relatedFiles", "notes", "meta",
        })
        self.assertEqual(data["status"], "planning")
        self.assertEqual(data["id"], "chinese-task")
        self.assertEqual(data["meta"], {"tracking": "中文值"})
        prd = (task_dir / "prd.md").read_text(encoding="utf-8")
        for heading in ("# 中文任务", "## 目标", "## 需求", "## 验收标准", "## 说明"):
            self.assertIn(heading, prd)
        self.assertIn("校验中文模板和机器契约", prd)
        spec_dir = self.root / ".trellis/spec"
        spec_dir.mkdir()
        (spec_dir / "testing.md").write_text("# 中文规范\n", encoding="utf-8")
        relative = task_dir.relative_to(self.root).as_posix()
        for role in ("implement", "check"):
            manifest = task_dir / f"{role}.jsonl"
            self.assertEqual(manifest.read_text(encoding="utf-8"), "")
            self.task("add-context", relative, role, ".trellis/spec/testing.md", "验证规范")
            self.assertEqual(json.loads(manifest.read_text(encoding="utf-8")), {
                "file": ".trellis/spec/testing.md", "reason": "验证规范",
            })
        self.task("add-context", relative, "implement", ".trellis/spec")
        directory_entry = json.loads(
            (task_dir / "implement.jsonl").read_text(encoding="utf-8").splitlines()[-1]
        )
        self.assertEqual(directory_entry, {
            "file": ".trellis/spec/", "type": "directory", "reason": "手动添加",
        })
        self.assertIn("所有校验通过", self.task("validate", relative).stdout)
        self.task("start", relative)
        current = json.loads(self.task("current", "--json").stdout)
        self.assertEqual(set(current), {"current_task", "source", "stale"})
        self.assertEqual(current["current_task"]["status"], "in_progress")
        self.assertEqual(current["current_task"]["branch"], "task/localization")
        self.assertEqual(current["current_task"]["dir"], relative)
        self.assertFalse(current["stale"])
        listed = json.loads(self.task("list", "--json").stdout)["tasks"][0]
        self.assertEqual(set(listed), {
            "dir", "id", "title", "status", "display_status", "priority", "assignee",
            "parent", "children", "package",
        })
        self.assertEqual(listed["status"], "in_progress")
        self.assertEqual(listed["title"], "中文任务")

    def test_chinese_title_requires_explicit_slug_without_changing_rules(self) -> None:
        result = self.task(
            "create", "中文任务", "--description", "测试目录规则", expected=1,
        )
        self.assertIn("--slug", result.stderr)
        self.assertIn("中文标题", result.stderr)
        self.assertEqual(list((self.root / ".trellis/tasks").rglob("task.json")), [])

    def test_archive_subject_is_chinese_and_unrelated_staged_files_stay_out(self) -> None:
        task_dir = self.create_task()
        self.task("set-branch", str(task_dir), "task/localization")
        other = self.root / ".trellis/tasks/01-01-other"
        other.mkdir()
        (other / "task.json").write_text(
            '{"title":"其他任务","status":"planning"}\n', encoding="utf-8"
        )
        self.git("add", ".trellis/tasks")
        self.git("commit", "-m", "test: 记录待归档任务")
        before = self.git("rev-parse", "HEAD").strip()
        (self.root / "unrelated.txt").write_text("其他工作\n", encoding="utf-8")
        (other / "task.json").write_text(
            '{"title":"其他任务的新改动","status":"planning"}\n', encoding="utf-8"
        )
        self.git("add", "unrelated.txt", str(other / "task.json"))
        staged_before = set(self.git("diff", "--cached", "--name-only").splitlines())
        result = self.task("archive", str(task_dir))
        archived = self.root / result.stdout.strip().splitlines()[-1]
        self.assertFalse(task_dir.exists())
        self.assertTrue((archived / "task.json").is_file())
        self.assertEqual(
            json.loads((archived / "task.json").read_text(encoding="utf-8"))["status"],
            "completed",
        )
        self.assertEqual(
            self.git("log", "-1", "--format=%s").strip(), f"chore(task): 归档 {task_dir.name}"
        )
        self.assertEqual(self.git("rev-list", "--count", f"{before}..HEAD").strip(), "1")
        changed = set(self.git(
            "-c", "core.quotepath=false", "diff-tree", "--no-commit-id", "--name-only",
            "--no-renames", "-r", "HEAD",
        ).splitlines())
        source = task_dir.relative_to(self.root).as_posix() + "/"
        destination = archived.relative_to(self.root).as_posix() + "/"
        self.assertTrue(changed)
        self.assertTrue(all(path.startswith((source, destination)) for path in changed))
        self.assertEqual(
            set(self.git("diff", "--cached", "--name-only").splitlines()), staged_before
        )

    @unittest.skipUnless(os.name == "posix", "恢复提示使用 POSIX shell")
    def test_archive_recovery_command_quotes_paths_and_limits_the_commit(self) -> None:
        paths = [
            ".trellis/tasks/archive/2026-09/09-26-path with spaces",
            ".trellis/tasks/09-26-special'$(literal)",
        ]
        task_name = "09-26-special'$(literal)"
        output = io.StringIO()
        with contextlib.redirect_stderr(output), patch.dict(os.environ, self.env, clear=True):
            _print_index_lock_warning("git add", task_name, self.root, paths)
        command = next(
            line.split("：", 1)[1]
            for line in output.getvalue().splitlines()
            if "手工提交（POSIX shell）" in line
        )
        add, commit = command.split(" && ", 1)
        self.assertEqual(shlex.split(add), ["git", "add", "-A", "--", *paths])
        self.assertEqual(shlex.split(commit), [
            "git", "commit", "-m", f"chore(task): 归档 {task_name}", "--", *paths,
        ])
        # 实际运行显示给用户的命令，证明 shell 引用和提交范围共同有效。
        for path in paths:
            directory = self.root / path
            directory.mkdir(parents=True)
            (directory / "task.json").write_text('{"title":"恢复验证"}\n', encoding="utf-8")
        (self.root / "unrelated.txt").write_text("应继续暂存\n", encoding="utf-8")
        self.git("add", "unrelated.txt")
        subprocess.run(
            ["sh", "-c", command], cwd=self.root, env=self.env, text=True,
            encoding="utf-8", capture_output=True, check=True,
        )
        self.assertEqual(
            self.git("log", "-1", "--format=%s").strip(), f"chore(task): 归档 {task_name}"
        )
        self.assertEqual(self.git("diff", "--cached", "--name-only").splitlines(), ["unrelated.txt"])
        for path in paths:
            self.assertEqual(
                json.loads(self.git("show", f"HEAD:{path}/task.json")), {"title": "恢复验证"}
            )


if __name__ == "__main__":
    unittest.main()
