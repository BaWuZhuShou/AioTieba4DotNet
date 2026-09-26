#!/usr/bin/env python3
"""验证中文上下文与历史模板、平台协议、角色隔离及 UTF-8 字节边界。"""

from __future__ import annotations

import importlib.util
import io
import json
import os
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / ".trellis" / "scripts"))

from common import packages_context, session_context, workflow_phase


def load_hook(name: str):
    """直接加载仓库 hook，不执行入口或访问真实任务状态。"""
    spec = importlib.util.spec_from_file_location(
        name.replace("-", "_"), ROOT / ".codex" / "hooks" / f"{name}.py"
    )
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


SESSION_START = load_hook("session-start")
WORKFLOW_STATE = load_hook("inject-workflow-state")
SUBAGENT = load_hook("inject-subagent-context")


def workflow_fixture(index: str, phase: str) -> str:
    return f"""# 开发流程
## {index}
简要流程说明。
[workflow-state:in_progress]
正在实施，只由逐轮 hook 注入。
[/workflow-state:in_progress]
## {phase}
此处是阶段正文，不应进入索引。
#### 1.4 评审方案
确认已完成的规划。
#### 1.5 开始任务
独立步骤。
---
## 阶段 2：实施
#### 2.1 执行方式
公共说明。
[codex-sub-agent, Claude Code]
由子代理实施。
[/codex-sub-agent, Claude Code]
[codex-inline, Cursor]
由主会话实施。
[/codex-inline, Cursor]
#### 2.2 检查
检查正文。
"""


class PhaseCompatibilityTests(unittest.TestCase):
    def test_both_readers_accept_english_chinese_and_mixed_headings(self):
        for index in ("Phase Index", "阶段索引"):
            for phase in ("Phase 1: Plan", "阶段 1：规划"):
                with self.subTest(index=index, phase=phase):
                    content = workflow_fixture(index, phase)
                    with patch.object(workflow_phase, "_read_workflow", return_value=content):
                        summary = workflow_phase.get_phase_index()
                    self.assertIn(f"## {index}", summary)
                    self.assertIn("简要流程说明", summary)
                    self.assertNotIn("阶段正文", summary)
                    self.assertNotIn("workflow-state", summary)
                    with tempfile.TemporaryDirectory() as temporary:
                        workflow = Path(temporary) / "workflow.md"
                        workflow.write_text(content, encoding="utf-8")
                        native_summary = SESSION_START._build_workflow_toc(workflow)
                    self.assertIn(summary.strip(), native_summary)
                    self.assertNotIn("阶段正文", native_summary)
                    self.assertNotIn("workflow-state", native_summary)

    def test_step_boundaries_and_platform_filtering(self):
        for index, phase in (("Phase Index", "Phase 1: Plan"), ("阶段索引", "阶段 1：规划")):
            with patch.object(workflow_phase, "_read_workflow", return_value=workflow_fixture(index, phase)):
                review = workflow_phase.get_step("1.4")
                self.assertIn("评审方案", review)
                self.assertNotIn("1.5", review)
                self.assertEqual("", workflow_phase.get_step("9.9"))
                step = workflow_phase.get_step("2.1")
            for config, expected, excluded in (
                ({}, "由子代理实施", "由主会话实施"),
                ({"codex": {"dispatch_mode": "sub-agent"}}, "由子代理实施", "由主会话实施"),
                ({"codex": {"dispatch_mode": "inline"}}, "由主会话实施", "由子代理实施"),
                ({"codex": {"dispatch_mode": "invalid"}}, "由主会话实施", "由子代理实施"),
            ):
                effective = workflow_phase.resolve_effective_platform("codex", config)
                filtered = workflow_phase.filter_platform(step, effective)
                self.assertIn("公共说明", filtered)
                self.assertIn(expected, filtered)
                self.assertNotIn(excluded, filtered)
                self.assertNotIn("[/", filtered)
                self.assertNotIn("2.2", filtered)
            claude = workflow_phase.resolve_effective_platform("claude", {})
            self.assertIn("由子代理实施", workflow_phase.filter_platform(step, claude))

    def test_missing_index_and_generic_section_extraction(self):
        with patch.object(workflow_phase, "_read_workflow", return_value="# 无索引\n"):
            self.assertEqual("", workflow_phase.get_phase_index())
        self.assertEqual("", SESSION_START._extract_range("# 无索引", "Phase Index", "Phase 1: Plan"))
        self.assertEqual("## 自定义\n内容", SESSION_START._extract_range("## 自定义\n内容\n## 结束\n尾部", "自定义", "结束"))


class HookProtocolTests(unittest.TestCase):
    def test_chinese_state_templates_and_inline_routing(self):
        states = ("no_task", "task_error", "planning", "planning-inline", "in_progress", "in_progress-inline", "completed")
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / ".trellis").mkdir()
            (root / ".trellis" / "workflow.md").write_text(
                "\n".join(f"[workflow-state:{status}]\n中文提示 {status}\n[/workflow-state:{status}]" for status in states),
                encoding="utf-8",
            )
            templates = WORKFLOW_STATE.load_breadcrumbs(root)
        self.assertEqual(set(states), set(templates))
        for state in states:
            block = WORKFLOW_STATE.build_breadcrumb(None if state == "no_task" else "中文任务", state, templates)
            self.assertTrue(block.startswith("<workflow-state>\n"))
            self.assertTrue(block.endswith("\n</workflow-state>"))
            self.assertIn(f"中文提示 {state}", block)
            self.assertIn("状态：" if state == "no_task" else "任务：", block)
        key = WORKFLOW_STATE.resolve_breadcrumb_key("planning", "codex", {"codex": {"dispatch_mode": "inline"}})
        self.assertEqual("planning-inline", key)
        fallback = WORKFLOW_STATE.build_breadcrumb("任务", "unknown", {})
        self.assertIn("请查阅 workflow.md", fallback)

    def test_per_turn_machine_envelope_and_kiro_plain_text(self):
        for platform, event in (("codex", "UserPromptSubmit"), ("gemini", "BeforeAgent"), ("kiro", None)):
            with self.subTest(platform=platform), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                (root / ".trellis").mkdir()
                (root / ".trellis" / "workflow.md").write_text("[workflow-state:no_task]\n请确认任务范围。\n[/workflow-state:no_task]", encoding="utf-8")
                output = io.StringIO()
                with patch.dict(os.environ, {}, clear=True), \
                     patch.object(WORKFLOW_STATE, "_load_hook_input", return_value={"cwd": str(root), "prompt": "继续"}), \
                     patch.object(WORKFLOW_STATE, "_detect_platform", return_value=platform), \
                     patch.object(WORKFLOW_STATE, "get_active_task", return_value=None), \
                     patch.object(WORKFLOW_STATE, "_read_trellis_config", return_value={}), \
                     patch("sys.stdout", output):
                    self.assertEqual(0, WORKFLOW_STATE.main())
                if event is None:
                    self.assertTrue(output.getvalue().startswith("<workflow-state>"))
                    continue
                payload = json.loads(output.getvalue())
                self.assertEqual({"hookSpecificOutput"}, set(payload))
                body = payload["hookSpecificOutput"]
                self.assertEqual({"hookEventName", "additionalContext"}, set(body))
                self.assertEqual(event, body["hookEventName"])
                self.assertIn("请确认任务范围", body["additionalContext"])
                if platform == "codex":
                    self.assertIn("<trellis-bootstrap>", body["additionalContext"])
                    self.assertIn("<codex-mode>auto：", body["additionalContext"])

    def test_session_start_envelope_retains_protocol_tags(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / ".trellis").mkdir()
            (root / ".trellis" / "workflow.md").write_text(workflow_fixture("阶段索引", "阶段 1：规划"), encoding="utf-8")
            output = io.StringIO()
            with patch.dict(os.environ, {}, clear=True), \
                 patch("sys.stdin", io.StringIO(json.dumps({"cwd": str(root)}))), \
                 patch("sys.stdout", output), \
                 patch.object(SESSION_START, "configure_project_encoding"), \
                 patch.object(SESSION_START, "_build_compact_current_state", return_value="开发者：测试"), \
                 patch.object(SESSION_START, "_get_task_status", return_value="状态：规划中"):
                SESSION_START.main()
            payload = json.loads(output.getvalue())
        self.assertEqual({"suppressOutput", "systemMessage", "hookSpecificOutput"}, set(payload))
        self.assertTrue(payload["suppressOutput"])
        self.assertIn("上下文已注入", payload["systemMessage"])
        self.assertEqual("SessionStart", payload["hookSpecificOutput"]["hookEventName"])
        context = payload["hookSpecificOutput"]["additionalContext"]
        for tag in ("session-context", "first-reply-notice", "current-state", "trellis-workflow", "guidelines", "task-status", "ready"):
            self.assertIn(f"<{tag}>", context)
            self.assertIn(f"</{tag}>", context)
        self.assertIn("## 阶段索引", context)
        self.assertNotIn("阶段正文", context)

    def test_missing_or_malformed_task_stays_task_error(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            task = root / ".trellis" / "tasks" / "fixture"
            task.mkdir(parents=True)
            active = SimpleNamespace(task_path=str(task), stale=False, source="fixture")
            with patch.object(WORKFLOW_STATE, "_resolve_active_task", return_value=active):
                for content in (None, "{", "[]", '{"status": ""}', '{"status": 1}'):
                    if content is not None:
                        (task / "task.json").write_text(content, encoding="utf-8")
                    self.assertEqual(("fixture", "task_error", "fixture"), WORKFLOW_STATE.get_active_task(root, {}))


class SubagentContextTests(unittest.TestCase):
    def make_task(self, root: Path) -> str:
        """只在临时目录生成用于验证角色隔离的任务。"""
        task = root / ".trellis" / "tasks" / "fixture"
        task.mkdir(parents=True)
        relative = task.relative_to(root).as_posix()
        for role in ("implement", "check"):
            (task / f"{role}-spec.md").write_text(f"仅供 {role} 的规范。", encoding="utf-8")
            (task / f"{role}.jsonl").write_text(json.dumps({"file": f"{relative}/{role}-spec.md", "reason": "角色隔离"}) + "\n", encoding="utf-8")
        for name, content in (("prd.md", "测试需求"), ("design.md", "测试设计"), ("implement.md", "测试执行计划")):
            (task / name).write_text(content, encoding="utf-8")
        (root / ".trellis" / "spec" / "backend").mkdir(parents=True)
        return relative

    def test_implement_check_order_and_research_isolation(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            task = self.make_task(root)
            with patch.object(SUBAGENT, "_get_limits", return_value=SUBAGENT.DEFAULT_LIMITS):
                for role, loader in (("implement", SUBAGENT.get_implement_context), ("check", SUBAGENT.get_check_context)):
                    output = loader(str(root), task)
                    self.assertIn(f"仅供 {role} 的规范", output)
                    other = "check" if role == "implement" else "implement"
                    self.assertNotIn(f"仅供 {other} 的规范", output)
                    positions = [output.index(text) for text in (f"仅供 {role} 的规范", "测试需求", "测试设计", "测试执行计划")]
                    self.assertEqual(sorted(positions), positions)
            with patch.object(SUBAGENT, "read_jsonl_entries", side_effect=AssertionError("研究不得读取角色清单")), \
                 patch.object(SUBAGENT, "_materialize_artifact", side_effect=AssertionError("研究不得读取任务文件")):
                research = SUBAGENT.get_research_context(str(root), task)
            self.assertIn("项目规范目录结构", research)
            self.assertIn("backend/", research)
            self.assertNotIn("测试需求", research)

    def test_native_event_roles_keep_parent_session_isolation_and_envelope(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / ".git").mkdir()
            task = self.make_task(root)
            for role in SUBAGENT.AGENTS_ALL:
                with self.subTest(role=role):
                    output = io.StringIO()
                    with patch.object(SUBAGENT, "get_current_task", return_value=task) as resolver, \
                         patch.object(SUBAGENT, "_get_limits", return_value=SUBAGENT.DEFAULT_LIMITS), \
                         patch("sys.stdout", output):
                        SUBAGENT._handle_codex_subagent_start({"hook_event_name": "SubagentStart", "agent_type": role, "session_id": "parent-fixture", "cwd": str(root)})
                    resolver.assert_called_once_with(str(root), {"session_id": "parent-fixture"}, platform="codex", allow_single_session_fallback=False, allow_environment_context=False, require_existing=True)
                    payload = json.loads(output.getvalue())["hookSpecificOutput"]
                    self.assertEqual({"hookEventName", "additionalContext"}, set(payload))
                    self.assertEqual("SubagentStart", payload["hookEventName"])
                    context = payload["additionalContext"]
                    self.assertIn("<!-- trellis-hook-injected -->", context)
                    self.assertIn(f"Active task: {task}", context)
                    self.assertIn(f"`{role}`", context)
                    self.assertIn("不要再创建 Trellis 子代理", context)
                    if role == SUBAGENT.AGENT_RESEARCH:
                        self.assertNotIn("测试需求", context)
                        self.assertNotIn("仅供 implement 的规范", context)
                        self.assertNotIn("仅供 check 的规范", context)

    def test_native_event_without_parent_or_task_does_not_borrow_context(self):
        for payload in (
            {"hook_event_name": "SubagentStart", "agent_type": SUBAGENT.AGENT_IMPLEMENT},
            {"hook_event_name": "SubagentStart", "agent_type": "unrelated", "session_id": "fixture"},
        ):
            output = io.StringIO()
            with patch.object(SUBAGENT, "get_current_task") as resolver, patch("sys.stdout", output):
                SUBAGENT._handle_codex_subagent_start(payload)
            resolver.assert_not_called()
            self.assertEqual("", output.getvalue())

    def test_utf8_truncation_at_every_byte_boundary(self):
        for text in ("ascii", "中文", "A中文🙂éZ", "éé", "🙂🙂"):
            encoded = text.encode("utf-8")
            for cap in range(1, len(encoded) + 2):
                with self.subTest(text=text, cap=cap):
                    truncated = SUBAGENT.truncate_utf8(encoded, cap)
                    self.assertLessEqual(len(truncated), cap)
                    decoded = truncated.decode("utf-8", errors="strict")
                    self.assertTrue(text.startswith(decoded))
                    self.assertEqual(encoded[:cap].decode("utf-8", errors="ignore"), decoded)
            self.assertEqual(encoded, SUBAGENT.truncate_utf8(encoded, 0))
            self.assertEqual(encoded, SUBAGENT.truncate_utf8(encoded, -1))

    def test_chinese_file_cap_total_budget_and_binary_notice(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "规范.md").write_text("中文内容", encoding="utf-8")
            limits = {"max_file_bytes": 6, "max_artifact_bytes": 6, "max_total_bytes": 1024}
            budget = SUBAGENT._Budget(1024)
            block = SUBAGENT._materialize_file(str(root), "规范.md", "测试", limits, budget)
            self.assertIn("中文\n[Trellis：已按 6 字节上限截断", block)
            self.assertNotIn("\ufffd", block)
            self.assertEqual(len(block.encode("utf-8")), budget.used)

            # 预算按编码后字节计数；不足时不内联，只保留可追溯索引提示。
            budget = SUBAGENT._Budget(1)
            notice = SUBAGENT._materialize_file(str(root), "规范.md", "测试原因", limits, budget)
            self.assertIn("已达上下文总量上限", notice)
            self.assertNotIn("===", notice)
            self.assertIn("规范.md", notice)
            self.assertEqual(len(notice.encode("utf-8")), budget.used)

            (root / "binary.md").write_bytes(b"\x00\xff")
            budget = SUBAGENT._Budget(1024)
            binary = SUBAGENT._materialize_file(str(root), "binary.md", "二进制夹具", limits, budget)
            self.assertIn("二进制文件", binary)
            self.assertNotIn("\ufffd", binary)
            self.assertEqual(len(binary.encode("utf-8")), budget.used)

            artifact = SUBAGENT._materialize_artifact(str(root), "规范.md", "需求", "测试", limits, SUBAGENT._Budget(1024))
            self.assertIn("中文\n[Trellis：已按 6 字节上限截断", artifact)
            self.assertNotIn("\ufffd", artifact)

    def test_unavailable_curated_context_has_chinese_fallback(self):
        with tempfile.TemporaryDirectory() as temporary, patch("sys.stderr", io.StringIO()) as errors:
            fallback = SUBAGENT.get_agent_context(temporary, "task", "implement", SUBAGENT.DEFAULT_LIMITS, SUBAGENT._Budget(1024))
        self.assertIn("未注入规范或研究上下文", fallback)
        self.assertIn(".trellis/spec/", fallback)
        self.assertIn("task/implement.jsonl", fallback)
        self.assertIn("警告", errors.getvalue())


class SessionContextTests(unittest.TestCase):
    def test_external_english_update_input_local_chinese_output_once(self):
        for separator in ("->", "→"):
            self.assertEqual("0.7.0", session_context._extract_available_update_version(f"Trellis update available: 0.6.17 {separator} 0.7.0"))
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / ".trellis").mkdir()
            (root / ".trellis" / ".version").write_text("0.6.17\n", encoding="utf-8")
            with patch.object(session_context, "_fetch_trellis_version_output", return_value="Trellis update available: 0.6.17 -> 0.7.0") as fetch:
                hint = session_context.get_update_hint(root, "context-fixture")
                self.assertEqual("发现 Trellis 更新：0.6.17 -> 0.7.0，运行 trellis update", hint)
                self.assertIsNone(session_context.get_update_hint(root, "context-fixture"))
                fetch.assert_called_once_with()

    def test_chinese_package_text_preserves_machine_json(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / ".trellis" / "spec" / "backend").mkdir(parents=True)
            with patch.object(packages_context, "get_packages", return_value={}):
                text = packages_context.get_context_packages_text(root)
                payload = packages_context.get_context_packages_json(root)
        self.assertIn("单仓库项目", text)
        self.assertIn("规范层： backend", text)
        self.assertEqual({"mode": "single-repo", "specLayers": ["backend"]}, payload)


if __name__ == "__main__":
    unittest.main()
