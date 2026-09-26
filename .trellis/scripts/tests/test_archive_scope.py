"""归档提交只纳入本次移动和关联元数据，保留其他工作的 Git 状态。"""

from __future__ import annotations

import argparse
import contextlib
from datetime import datetime
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPTS_DIR = Path(__file__).resolve().parents[1]
if str(SCRIPTS_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_DIR))

from common import task_store


class ArchiveScopeTests(unittest.TestCase):
    def setUp(self) -> None:
        temporary = tempfile.TemporaryDirectory(prefix="trellis-archive-scope-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.git("init", "-b", "main")
        self.git("config", "user.name", "归档回归")
        self.git("config", "user.email", "archive-test@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.hooksPath", str(self.root / "empty-hooks"))
        self.tasks = self.root / ".trellis/tasks"
        self.parent = self.tasks / "09-26-selected"
        self.child = self.tasks / "09-26-child"
        self.write(self.parent / "task.json", json.dumps({
            "name": self.parent.name, "title": "归档范围验证", "status": "in_progress",
            "children": [self.child.name],
        }))
        self.write(self.child / "task.json", json.dumps({
            "name": self.child.name, "title": "关联子任务", "status": "in_progress",
            "parent": self.parent.name,
        }))
        month = datetime.now().strftime("%Y-%m")
        self.unrelated = [
            self.tasks / "archive/2000-01" / self.parent.name / "notes.md",
            self.tasks / "archive" / month / "other" / "notes.md",
            self.child / "notes.md",
        ]
        for path in self.unrelated:
            self.write(path, "原始内容\n")
        self.write(self.root / ".trellis/config.yaml", "session_auto_commit: true\n")
        self.git("add", ".")
        self.git("commit", "-m", "fixture: 初始任务证据")
        for path in self.unrelated:
            path.write_text("其他工作已暂存\n", encoding="utf-8")
            self.git("add", str(path.relative_to(self.root)))
            path.write_text("其他工作尚未暂存\n", encoding="utf-8")

    def write(self, path: Path, content: str) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")

    def git(self, *args: str) -> str:
        env = os.environ.copy()
        for key in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE"):
            env.pop(key, None)
        return subprocess.run(
            ["git", *args], cwd=self.root, env=env, text=True, encoding="utf-8",
            capture_output=True, check=True,
        ).stdout

    def unrelated_state(self) -> tuple[str, str, list[bytes]]:
        paths = [str(path.relative_to(self.root)) for path in self.unrelated]
        return (
            self.git("diff", "--cached", "--binary", "--", *paths),
            self.git("diff", "--binary", "--", *paths),
            [path.read_bytes() for path in self.unrelated],
        )

    def test_archive_preserves_other_archives_and_child_work(self) -> None:
        before = self.unrelated_state()
        output = io.StringIO()
        with (
            patch.object(task_store, "get_repo_root", return_value=self.root),
            contextlib.redirect_stdout(output),
            contextlib.redirect_stderr(io.StringIO()),
        ):
            result = task_store.cmd_archive(argparse.Namespace(
                name=self.parent.name, no_commit=False, skip_branch_validation=False,
            ))
        self.assertEqual(result, 0)
        destination = self.root / output.getvalue().strip()
        self.assertTrue((destination / "task.json").is_file())
        self.assertFalse(self.parent.exists())
        self.assertEqual(self.unrelated_state(), before)
        expected = {
            str((self.parent / "task.json").relative_to(self.root)),
            str((destination / "task.json").relative_to(self.root)),
            str((self.child / "task.json").relative_to(self.root)),
        }
        committed = set(self.git("show", "--pretty=format:", "--name-only", "--no-renames", "HEAD").splitlines())
        self.assertEqual(committed, expected)
        self.assertEqual(json.loads((destination / "task.json").read_text())["status"], "completed")
        self.assertIsNone(json.loads((self.child / "task.json").read_text())["parent"])

    def test_commit_uses_returned_destination_instead_of_guessing_month(self) -> None:
        before = self.unrelated_state()
        destination = self.tasks / "archive/2001-02" / self.parent.name
        destination.parent.mkdir(parents=True)
        shutil.move(str(self.parent), str(destination))
        with contextlib.redirect_stderr(io.StringIO()):
            self.assertTrue(task_store._auto_commit_archive(
                self.parent.name, self.root, archive_dest=destination,
            ))
        self.assertEqual(self.unrelated_state(), before)
        committed = set(self.git("show", "--pretty=format:", "--name-only", "--no-renames", "HEAD").splitlines())
        self.assertEqual(committed, {
            str((self.parent / "task.json").relative_to(self.root)),
            str((destination / "task.json").relative_to(self.root)),
        })


if __name__ == "__main__":
    unittest.main()
