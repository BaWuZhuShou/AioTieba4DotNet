"""支持多平台的 CLI 适配器。

封装 Claude Code、OpenCode、Cursor、iFlow、Codex、Kilo、Kiro Code、Gemini CLI、
Antigravity、Devin、Qoder、CodeBuddy、GitHub Copilot、Factory Droid 和 Pi Agent 的接口差异。

支持的平台：
- claude: Claude Code（默认）
- opencode: OpenCode
- cursor: Cursor IDE
- iflow: iFlow CLI
- codex: Codex CLI（基于技能）
- kilo: Kilo CLI
- kiro: Kiro Code（基于技能）
- gemini: Gemini CLI
- antigravity: Antigravity（基于工作流）
- devin: Devin（原 Windsurf，基于工作流）
- qoder: Qoder
- codebuddy: CodeBuddy
- copilot: GitHub Copilot（VS Code）
- droid: Factory Droid（基于命令）
- pi: Pi Agent（由扩展支持）
- trae: Trae IDE（仅 IDE，基于钩子）
- omp: Oh My Pi
- grok: Grok Build（主动拉取技能/代理，无钩子上下文注入）
- kimi: Kimi Code（主动拉取技能，命令以技能交付，无钩子上下文注入）

用法：
    from common.cli_adapter import CLIAdapter

    adapter = CLIAdapter("opencode")
    cmd = adapter.build_run_command(
        agent="dispatch",
        session_id="abc123",
        prompt="启动流水线"
    )
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import ClassVar, Literal

Platform = Literal[
    "claude",
    "opencode",
    "cursor",
    "iflow",
    "codex",
    "kilo",
    "kiro",
    "gemini",
    "antigravity",
    "devin",
    "qoder",
    "codebuddy",
    "copilot",
    "droid",
    "pi",
    "trae",
    "omp",
    "grok",
    "kimi",
]


@dataclass
class CLIAdapter:
    """适配不同的 AI 编程 CLI 工具。"""

    platform: Platform

    # =========================================================================
    # 代理名称映射
    # =========================================================================

    # OpenCode 包含不可覆盖的内置代理。
    # 参见 https://github.com/sst/opencode/issues/4271
    # 说明：这是类级常量，不是数据类字段。
    _AGENT_NAME_MAP: ClassVar[dict[Platform, dict[str, str]]] = {
        "claude": {},  # 无需映射
        "opencode": {
            "plan": "trellis-plan",  # 'plan' 是 OpenCode 的内置名称
        },
    }

    def get_agent_name(self, agent: str) -> str:
        """获取平台对应的代理名称。

        参数：
            agent: 原始代理名称，如 'plan'、'dispatch'。

        返回：
            平台对应的代理名称，如 OpenCode 的 'trellis-plan'。
        """
        mapping = self._AGENT_NAME_MAP.get(self.platform, {})
        return mapping.get(agent, agent)

    # =========================================================================
    # 代理路径
    # =========================================================================

    @property
    def config_dir_name(self) -> str:
        """获取平台对应的配置目录名。

        返回：
            目录名（'.claude'、'.opencode'、'.cursor'、'.iflow'、'.codex'、'.kilocode'、
            '.kiro'、'.gemini'、'.agent'、'.devin'、'.qoder'、'.codebuddy'、'.github/copilot'、
            '.factory'、'.pi' 或 '.trae'）。
        """
        if self.platform == "opencode":
            return ".opencode"
        elif self.platform == "cursor":
            return ".cursor"
        elif self.platform == "iflow":
            return ".iflow"
        elif self.platform == "codex":
            return ".codex"
        elif self.platform == "kilo":
            return ".kilocode"
        elif self.platform == "kiro":
            return ".kiro"
        elif self.platform == "gemini":
            return ".gemini"
        elif self.platform == "antigravity":
            return ".agent"
        elif self.platform == "devin":
            return ".devin"
        elif self.platform == "qoder":
            return ".qoder"
        elif self.platform == "codebuddy":
            return ".codebuddy"
        elif self.platform == "copilot":
            return ".github/copilot"
        elif self.platform == "droid":
            return ".factory"
        elif self.platform == "pi":
            return ".pi"
        elif self.platform == "trae":
            return ".trae"
        elif self.platform == "omp":
            return ".omp"
        elif self.platform == "grok":
            return ".grok"
        elif self.platform == "kimi":
            return ".kimi-code"
        else:
            return ".claude"

    def get_config_dir(self, project_root: Path) -> Path:
        """获取平台对应的配置目录。

        参数：
            project_root: 项目根目录。

        返回：
            配置目录路径（.claude、.opencode、.cursor、.iflow、.codex、.kilocode、.kiro、
            .gemini、.agent、.devin、.qoder、.codebuddy、.github/copilot、.factory、.pi 或 .trae）。
        """
        return project_root / self.config_dir_name

    def get_agent_path(self, agent: str, project_root: Path) -> Path:
        """获取代理定义文件路径。

        参数：
            agent: 原始代理名称（映射前）。
            project_root: 项目根目录。

        返回：
            代理定义文件路径（多数平台为 .md，Codex 为 .toml）。
        """
        mapped_name = self.get_agent_name(agent)
        if self.platform == "codex":
            return self.get_config_dir(project_root) / "agents" / f"{mapped_name}.toml"
        return self.get_config_dir(project_root) / "agents" / f"{mapped_name}.md"

    def get_commands_path(self, project_root: Path, *parts: str) -> Path:
        """获取命令目录或指定命令文件的路径。

        参数：
            project_root: 项目根目录。
            *parts: 附加路径片段，如 'trellis'、'finish-work.md'。

        返回：
            命令目录或文件路径。

        说明：
            Cursor 使用前缀命名：.cursor/commands/trellis-<name>.md
            Antigravity 使用工作流目录：.agent/workflows/<name>.md
            Devin 使用工作流目录：.devin/workflows/trellis-<name>.md
            Copilot 使用提示文件：.github/prompts/<name>.prompt.md
            Pi 使用提示模板：.pi/prompts/trellis-<name>.md
            Claude/OpenCode 使用子目录：.claude/commands/trellis/<name>.md
        """
        if self.platform == "pi":
            prompts_dir = self.get_config_dir(project_root) / "prompts"
            if not parts:
                return prompts_dir
            if len(parts) >= 2 and parts[0] == "trellis":
                filename = parts[-1]
                if filename.endswith(".md"):
                    filename = filename[:-3]
                return prompts_dir / f"trellis-{filename}.md"
            return prompts_dir / Path(*parts)
        # OMP 与 Grok：斜杠命令平铺于 .{platform}/commands/trellis-<name>.md
        if self.platform in ("omp", "grok"):
            commands_dir = self.get_config_dir(project_root) / "commands"
            if not parts:
                return commands_dir
            if len(parts) >= 2 and parts[0] == "trellis":
                filename = parts[-1]
                if filename.endswith(".md"):
                    filename = filename[:-3]
                return commands_dir / f"trellis-{filename}.md"
            return commands_dir / Path(*parts)

        # Kimi：命令以技能形式位于 .kimi-code/skills/trellis-<name>/SKILL.md
        if self.platform == "kimi":
            skills_dir = self.get_config_dir(project_root) / "skills"
            if not parts:
                return skills_dir
            if len(parts) >= 2 and parts[0] == "trellis":
                filename = parts[-1]
                if filename.endswith(".md"):
                    filename = filename[:-3]
                return skills_dir / f"trellis-{filename}" / "SKILL.md"
            return skills_dir / Path(*parts)

        if self.platform == "devin":
            workflow_dir = self.get_config_dir(project_root) / "workflows"
            if not parts:
                return workflow_dir
            if len(parts) >= 2 and parts[0] == "trellis":
                filename = parts[-1]
                return workflow_dir / f"trellis-{filename}"
            return workflow_dir / Path(*parts)

        if self.platform in ("antigravity", "kilo"):
            workflow_dir = self.get_config_dir(project_root) / "workflows"
            if not parts:
                return workflow_dir
            if len(parts) >= 2 and parts[0] == "trellis":
                filename = parts[-1]
                return workflow_dir / filename
            return workflow_dir / Path(*parts)

        if self.platform == "copilot":
            prompts_dir = project_root / ".github" / "prompts"
            if not parts:
                return prompts_dir
            if len(parts) >= 2 and parts[0] == "trellis":
                filename = parts[-1]
                if filename.endswith(".md"):
                    filename = filename[:-3]
                return prompts_dir / f"{filename}.prompt.md"
            return prompts_dir / Path(*parts)

        if not parts:
            return self.get_config_dir(project_root) / "commands"

        # Cursor 使用前缀命名，而非子目录
        if self.platform == "cursor" and len(parts) >= 2 and parts[0] == "trellis":
            # 将 trellis/<name>.md 转换为 trellis-<name>.md
            filename = parts[-1]
            return (
                self.get_config_dir(project_root) / "commands" / f"trellis-{filename}"
            )

        return self.get_config_dir(project_root) / "commands" / Path(*parts)

    def get_trellis_command_path(self, name: str) -> str:
        """获取 Trellis 命令文件的相对路径。

        参数：
            name: 不含扩展名的命令名，如 'finish-work'、'check'。

        返回：
            供 JSONL 条目使用的相对路径字符串。

        说明：
            Cursor: .cursor/commands/trellis-<name>.md
            Codex: .agents/skills/trellis-<name>/SKILL.md
            Kiro: .kiro/skills/trellis-<name>/SKILL.md
            Gemini: .gemini/commands/trellis/<name>.toml
            Antigravity: .agent/workflows/<name>.md
            Devin: .devin/workflows/trellis-<name>.md
            Pi: .pi/prompts/trellis-<name>.md
            其他平台: .{platform}/commands/trellis/<name>.md
        """
        if self.platform == "cursor":
            return f".cursor/commands/trellis-{name}.md"
        elif self.platform == "codex":
            # 0.5.0-beta.0 重命名了全部技能目录，添加 `trellis-` 前缀。
            # 该版本清单包含 60 多项重命名记录。
            return f".agents/skills/trellis-{name}/SKILL.md"
        elif self.platform == "kiro":
            return f".kiro/skills/trellis-{name}/SKILL.md"
        elif self.platform == "gemini":
            return f".gemini/commands/trellis/{name}.toml"
        elif self.platform == "antigravity":
            return f".agent/workflows/{name}.md"
        elif self.platform == "devin":
            return f".devin/workflows/trellis-{name}.md"
        elif self.platform == "kilo":
            return f".kilocode/workflows/{name}.md"
        elif self.platform == "copilot":
            return f".github/prompts/{name}.prompt.md"
        elif self.platform == "droid":
            return f".factory/commands/trellis/{name}.md"
        elif self.platform == "pi":
            return f".pi/prompts/trellis-{name}.md"
        elif self.platform in ("omp", "grok"):
            return f"{self.config_dir_name}/commands/trellis-{name}.md"
        elif self.platform == "kimi":
            return f".kimi-code/skills/trellis-{name}/SKILL.md"
        else:
            return f"{self.config_dir_name}/commands/trellis/{name}.md"

    # =========================================================================
    # 环境变量
    # =========================================================================

    def get_non_interactive_env(self) -> dict[str, str]:
        """获取非交互模式的环境变量。

        返回：
            需要设置的环境变量字典。
        """
        if self.platform == "opencode":
            return {"OPENCODE_NON_INTERACTIVE": "1"}
        elif self.platform == "iflow":
            return {"IFLOW_NON_INTERACTIVE": "1"}
        elif self.platform == "codex":
            return {"CODEX_NON_INTERACTIVE": "1"}
        elif self.platform == "kiro":
            return {"KIRO_NON_INTERACTIVE": "1"}
        elif self.platform == "gemini":
            return {}  # Gemini CLI 没有非交互模式环境变量
        elif self.platform == "antigravity":
            return {}
        elif self.platform == "devin":
            return {}
        elif self.platform == "qoder":
            return {}
        elif self.platform == "codebuddy":
            return {}
        elif self.platform == "copilot":
            return {}
        elif self.platform == "droid":
            return {}
        elif self.platform == "pi":
            return {}
        elif self.platform == "trae":
            return {}
        elif self.platform == "omp":
            return {}
        elif self.platform == "grok":
            return {}
        elif self.platform == "kimi":
            return {}
        else:
            return {"CLAUDE_NON_INTERACTIVE": "1"}

    # =========================================================================
    # CLI 命令构建
    # =========================================================================

    def build_run_command(
        self,
        agent: str,
        prompt: str,
        session_id: str | None = None,
        skip_permissions: bool = True,
        verbose: bool = True,
        json_output: bool = True,
    ) -> list[str]:
        """构建运行代理的 CLI 命令。

        参数：
            agent: 代理名称，必要时会映射。
            prompt: 发给代理的提示。
            session_id: 可选会话 ID（创建时仅 Claude Code 支持）。
            skip_permissions: 是否跳过权限提示。
            verbose: 是否启用详细输出。
            json_output: 是否使用 JSON 输出格式。

        返回：
            命令参数列表。
        """
        mapped_agent = self.get_agent_name(agent)

        if self.platform == "opencode":
            cmd = ["opencode", "run"]
            cmd.extend(["--agent", mapped_agent])

            # 说明：OpenCode 的 'run' 模式默认非交互。
            # 没有与 Claude Code 的 --dangerously-skip-permissions 等价的选项。
            # 参见 https://github.com/anomalyco/opencode/issues/9070

            if json_output:
                cmd.extend(["--format", "json"])

            if verbose:
                cmd.extend(["--log-level", "DEBUG", "--print-logs"])

            # 说明：OpenCode 不支持在创建时指定 --session-id，
            # 必须在启动后从日志提取会话 ID。

            cmd.append(prompt)

        elif self.platform == "iflow":
            cmd = ["iflow", "-y", "-p"]
            cmd.append(f"${mapped_agent} {prompt}")
        elif self.platform == "codex":
            cmd = ["codex", "exec"]
            cmd.append(prompt)
        elif self.platform == "kiro":
            cmd = ["kiro", "run", prompt]
        elif self.platform == "gemini":
            cmd = ["gemini"]
            cmd.append(prompt)
        elif self.platform == "antigravity":
            raise ValueError(
                "Antigravity 工作流是界面斜杠命令，不支持通过 CLI 运行代理。"
            )
        elif self.platform == "devin":
            raise ValueError(
                "Devin 工作流是界面斜杠命令，不支持通过 CLI 运行代理。"
            )
        elif self.platform == "qoder":
            cmd = ["qodercli", "-p", prompt]
        elif self.platform == "codebuddy":
            raise ValueError(
                "CodeBuddy 不支持非交互模式（没有 CLI 代理）"
            )
        elif self.platform == "copilot":
            raise ValueError(
                "GitHub Copilot 仅支持 IDE，不支持通过 CLI 运行代理。"
            )
        elif self.platform == "droid":
            raise ValueError(
                "暂不支持通过 Factory Droid CLI 运行代理。"
            )
        elif self.platform == "pi":
            cmd = ["pi", "-p", prompt]
        elif self.platform == "trae":
            raise ValueError(
                "Trae 仅支持 IDE，不支持通过 CLI 运行代理。"
            )
        elif self.platform == "omp":
            raise ValueError(
                "OMP 使用原生 task 工具运行代理，不支持通过 CLI 运行代理。"
            )
        elif self.platform == "grok":
            # 无界面单提示运行；子代理使用进程内的 spawn_subagent。
            cmd = ["grok", "-p", prompt, "--yolo"]
        elif self.platform == "kimi":
            # 无界面单提示运行，自动批准；子代理是会话内派发的内置 coder/explore/plan 代理。
            cmd = ["kimi", "-p", prompt, "--yolo"]

        else:  # claude
            cmd = ["claude", "-p"]
            cmd.extend(["--agent", mapped_agent])

            if session_id:
                cmd.extend(["--session-id", session_id])

            if skip_permissions:
                cmd.append("--dangerously-skip-permissions")

            if json_output:
                cmd.extend(["--output-format", "stream-json"])

            if verbose:
                cmd.append("--verbose")

            cmd.append(prompt)

        return cmd

    def build_resume_command(self, session_id: str) -> list[str]:
        """构建恢复会话的 CLI 命令。

        参数：
            session_id: 要恢复的会话 ID（iFlow 忽略此参数）。

        返回：
            命令参数列表。
        """
        if self.platform == "opencode":
            return ["opencode", "run", "--session", session_id]
        elif self.platform == "iflow":
            # iFlow 使用 -c 继续最近的对话。
            # iFlow 不支持会话 ID，因此忽略 session_id。
            return ["iflow", "-c"]
        elif self.platform == "codex":
            return ["codex", "resume", session_id]
        elif self.platform == "kiro":
            return ["kiro", "resume", session_id]
        elif self.platform == "gemini":
            return ["gemini", "--resume", session_id]
        elif self.platform == "antigravity":
            raise ValueError(
                "Antigravity 工作流是界面斜杠命令，不支持通过 CLI 恢复会话。"
            )
        elif self.platform == "devin":
            raise ValueError(
                "Devin 工作流是界面斜杠命令，不支持通过 CLI 恢复会话。"
            )
        elif self.platform == "qoder":
            return ["qodercli", "--resume", session_id]
        elif self.platform == "codebuddy":
            raise ValueError(
                "CodeBuddy 不支持非交互模式（没有 CLI 代理）"
            )
        elif self.platform == "copilot":
            raise ValueError(
                "GitHub Copilot 仅支持 IDE，不支持通过 CLI 恢复会话。"
            )
        elif self.platform == "droid":
            raise ValueError(
                "暂不支持通过 Factory Droid CLI 恢复会话。"
            )
        elif self.platform == "pi":
            return ["pi", "-c", session_id]
        elif self.platform == "trae":
            raise ValueError(
                "Trae 仅支持 IDE，不支持通过 CLI 恢复会话。"
            )
        elif self.platform == "omp":
            raise ValueError(
                "OMP 使用原生 task 工具运行代理，不支持通过 CLI 恢复会话。"
            )
        elif self.platform == "grok":
            return ["grok", "-c"]
        elif self.platform == "kimi":
            return ["kimi", "--session", session_id]
        else:
            return ["claude", "--resume", session_id]

    def get_resume_command_str(self, session_id: str, cwd: str | None = None) -> str:
        """获取人类可读的恢复命令字符串。

        参数：
            session_id: 要恢复的会话 ID。
            cwd: 可选的工作目录，将先 cd 到此处。

        返回：
            用于展示的命令字符串。
        """
        cmd = self.build_resume_command(session_id)
        cmd_str = " ".join(cmd)

        if cwd:
            return f"cd {cwd} && {cmd_str}"
        return cmd_str

    # =========================================================================
    # 平台检测辅助函数
    # =========================================================================

    @property
    def is_opencode(self) -> bool:
        """检查平台是否为 OpenCode。"""
        return self.platform == "opencode"

    @property
    def is_claude(self) -> bool:
        """检查平台是否为 Claude Code。"""
        return self.platform == "claude"

    @property
    def is_cursor(self) -> bool:
        """检查平台是否为 Cursor。"""
        return self.platform == "cursor"

    @property
    def is_iflow(self) -> bool:
        """检查平台是否为 iFlow CLI。"""
        return self.platform == "iflow"

    @property
    def cli_name(self) -> str:
        """获取 CLI 可执行文件名称。

        说明：Cursor 没有 CLI 工具，返回类似 None 的值。
        """
        if self.is_opencode:
            return "opencode"
        elif self.is_cursor:
            return "cursor"  # 说明：Cursor 仅支持 IDE，没有 CLI
        elif self.platform == "iflow":
            return "iflow"
        elif self.platform == "kiro":
            return "kiro"
        elif self.platform == "gemini":
            return "gemini"
        elif self.platform == "antigravity":
            return "agy"
        elif self.platform == "devin":
            return "devin"
        elif self.platform == "qoder":
            return "qodercli"
        elif self.platform == "codebuddy":
            return "codebuddy"
        elif self.platform == "copilot":
            return "copilot"
        elif self.platform == "droid":
            return "droid"
        elif self.platform == "pi":
            return "pi"
        elif self.platform == "trae":
            return "trae"
        elif self.platform == "omp":
            return "omp"
        elif self.platform == "grok":
            return "grok"
        elif self.platform == "kimi":
            return "kimi"
        else:
            return "claude"

    @property
    def supports_cli_agents(self) -> bool:
        """检查平台是否支持通过 CLI 运行代理。

        Claude Code、OpenCode、iFlow 和 Codex 支持 CLI 代理执行。
        Cursor 仅支持 IDE，不支持 CLI 代理。
        """
        return self.platform in (
            "claude",
            "opencode",
            "iflow",
            "codex",
            "pi",
            "grok",
            "kimi",
        )

    @property
    def requires_agent_definition_file(self) -> bool:
        """检查平台运行时是否需要代理定义文件（.md/.toml）。

        Claude Code、OpenCode、iFlow：需要代理 .md 文件（--agent 参数）。
        Codex：自动发现 .codex/agents/*.toml 中的代理，不使用 --agent 参数。
        """
        return self.platform in ("claude", "opencode", "iflow")

    # =========================================================================
    # 会话 ID 处理
    # =========================================================================

    @property
    def supports_session_id_on_create(self) -> bool:
        """检查平台是否支持在创建时指定会话 ID。

        Claude Code：支持（--session-id）。
        OpenCode：不支持（自动生成，需从日志提取）。
        iFlow：不支持会话 ID。
        """
        return self.platform == "claude"

    def extract_session_id_from_log(self, log_content: str) -> str | None:
        """从日志输出提取会话 ID（仅 OpenCode）。

        OpenCode 生成的会话 ID 格式为 ses_xxx。

        参数：
            log_content: 日志文件内容。

        返回：
            找到时返回会话 ID，否则返回 None。
        """
        import re

        # OpenCode 会话 ID 模式
        match = re.search(r"ses_[a-zA-Z0-9]+", log_content)
        if match:
            return match.group(0)
        return None


# =============================================================================
# 工厂函数
# =============================================================================


def get_cli_adapter(platform: str = "claude") -> CLIAdapter:
    """获取指定平台的 CLI 适配器。

    参数：
        platform: 平台名称（'claude'、'opencode'、'cursor'、'iflow'、'codex'、'kilo'、
        'kiro'、'gemini'、'antigravity'、'devin'、'qoder'、'codebuddy'、'copilot'、'droid'、
        'pi' 或 'trae'）。

    返回：
        CLIAdapter 实例。

    异常：
        ValueError: 平台不受支持。

    说明：
        接受 'windsurf' 作为 'devin' 的弃用别名（Windsurf 已更名为 Devin），
        并在校验前规范化。
    """
    # 弃用别名：Windsurf 已更名为 Devin。
    if platform == "windsurf":
        platform = "devin"
    if platform not in (
        "claude",
        "opencode",
        "cursor",
        "iflow",
        "codex",
        "kilo",
        "kiro",
        "gemini",
        "antigravity",
        "devin",
        "qoder",
        "codebuddy",
        "copilot",
        "droid",
        "pi",
        "trae",
        "omp",
        "grok",
        "kimi",
    ):
        raise ValueError(
            f"不支持的平台：{platform}（必须是 'claude', 'opencode', 'cursor', 'iflow', 'codex', 'kilo', 'kiro', 'gemini', 'antigravity', 'devin', 'qoder', 'codebuddy', 'copilot', 'droid', 'pi', 'trae', 'omp', 'grok', 或 'kimi'）"
        )

    return CLIAdapter(platform=platform)  # type: ignore


_ALL_PLATFORM_CONFIG_DIRS = (
    ".claude",
    ".cursor",
    ".iflow",
    ".opencode",
    ".codex",
    ".kilocode",
    ".kiro",
    ".gemini",
    ".agent",
    ".devin",
    ".windsurf",  # 已弃用：Devin 更名前的配置目录，仍可作为平台标志
    ".qoder",
    ".codebuddy",
    ".github/copilot",
    ".factory",
    ".pi",
    ".trae",
    ".omp",
    ".grok",
    ".kimi-code",
)
"""detect_platform 排除检查使用的平台专属配置目录名。
`.agents/skills/` 不在其中：它是跨平台共享层，由 Codex 写入，也由 Amp/Cline/Warp
等通过 agentskills.io 标准读取，不代表单一平台。它的存在不得阻止检测 Kiro、
Antigravity、Devin 或其他平台。
"""


def _has_other_platform_dir(project_root: Path, exclude: set[str]) -> bool:
    """检查是否存在 *exclude* 之外的平台配置目录。"""
    return any(
        (project_root / d).is_dir()
        for d in _ALL_PLATFORM_CONFIG_DIRS
        if d not in exclude
    )


def detect_platform(project_root: Path) -> Platform:
    """根据已有配置目录自动检测平台。

    检测顺序：
    1. TRELLIS_PLATFORM 环境变量（若已设置）。
    2. 存在 .opencode 目录 → opencode
    3. 存在 .iflow 目录 → iflow
    4. 存在 .cursor 但不存在 .claude → cursor
    5. 存在 .gemini 目录 → gemini
    6. 存在 .codex 且无其他平台目录 → codex
    7. 存在 .kilocode 目录 → kilo
    8. 存在 .kiro/skills 且无其他平台目录 → kiro
    9. 存在 .agent/workflows 且无其他平台目录 → antigravity
    10. 存在 .devin/workflows（或旧 .windsurf/workflows）且无其他平台目录 → devin
    11. 存在 .codebuddy 目录 → codebuddy
    12. 存在 .qoder 目录 → qoder
    13. 存在 .github/copilot 目录 → copilot
    14. 存在 .factory 目录 → droid
    15. 存在 .pi 目录 → pi
    16. 存在 .trae 目录 → trae
    17. 默认 → claude

    参数：
        project_root: 项目根目录。

    返回：
        检测到的平台（'claude'、'opencode'、'cursor'、'iflow'、'codex'、'kilo'、'kiro'、
        'gemini'、'antigravity'、'devin'、'qoder'、'codebuddy'、'copilot'、'droid'、'pi'、
        'trae'，默认为 'claude'）。
    """
    import os

    # 优先检查环境变量
    env_platform = os.environ.get("TRELLIS_PLATFORM", "").lower()
    # 弃用别名：Windsurf 已更名为 Devin。
    if env_platform == "windsurf":
        env_platform = "devin"
    if env_platform in (
        "claude",
        "opencode",
        "cursor",
        "iflow",
        "codex",
        "kilo",
        "kiro",
        "gemini",
        "antigravity",
        "devin",
        "qoder",
        "codebuddy",
        "copilot",
        "droid",
        "pi",
        "trae",
        "omp",
        "grok",
        "kimi",
    ):
        return env_platform  # type: ignore

    # 检查 .opencode 目录（OpenCode 专属）
    if (project_root / ".opencode").is_dir():
        return "opencode"

    # 检查 .iflow 目录（iFlow 专属）
    if (project_root / ".iflow").is_dir():
        return "iflow"

    # 检查 .cursor 目录（Cursor 专属）。
    # 仅在 .claude 不存在时识别为 cursor，避免混淆。
    if (project_root / ".cursor").is_dir() and not (project_root / ".claude").is_dir():
        return "cursor"

    # 检查 .gemini 目录（Gemini CLI 专属）
    if (project_root / ".gemini").is_dir():
        return "gemini"

    # 检查 .codex 目录（Codex 专属）。
    # 仅有 .agents/skills/ 不会触发 codex 检测，因为它是共享标准。
    if (project_root / ".codex").is_dir() and not _has_other_platform_dir(
        project_root, {".codex", ".agents"}
    ):
        return "codex"

    # 检查 .kilocode 目录（Kilo 专属）
    if (project_root / ".kilocode").is_dir():
        return "kilo"

    # 仅在无其他平台配置时检查 Kiro 技能目录
    if (project_root / ".kiro" / "skills").is_dir() and not _has_other_platform_dir(
        project_root, {".kiro"}
    ):
        return "kiro"

    # 仅在无其他平台配置时检查 Antigravity 工作流目录
    if (
        project_root / ".agent" / "workflows"
    ).is_dir() and not _has_other_platform_dir(
        project_root, {".agent", ".gemini"}
    ):
        return "antigravity"

    # 仅在无其他平台配置时检查 Devin 工作流目录。`.windsurf/workflows` 是更名前
    # 的旧路径，仍识别为 devin 以保持兼容，直到用户通过 `trellis update --migrate` 迁移。
    if (
        (project_root / ".devin" / "workflows").is_dir()
        or (project_root / ".windsurf" / "workflows").is_dir()
    ) and not _has_other_platform_dir(
        project_root, {".devin", ".windsurf"}
    ):
        return "devin"

    # 检查 .codebuddy 目录（CodeBuddy 专属）
    if (project_root / ".codebuddy").is_dir():
        return "codebuddy"

    # 检查 .qoder 目录（Qoder 专属）
    if (project_root / ".qoder").is_dir():
        return "qoder"

    # 检查 .github/copilot 目录（GitHub Copilot 专属）
    if (project_root / ".github" / "copilot").is_dir():
        return "copilot"

    # 检查 .factory 目录（Factory Droid 专属）
    if (project_root / ".factory").is_dir():
        return "droid"

    # 检查 .pi 目录（Pi Agent 专属）
    if (project_root / ".pi").is_dir():
        return "pi"

    # 检查 .trae 目录（Trae IDE 专属）
    if (project_root / ".trae").is_dir():
        return "trae"

    # 检查 .omp 目录（OMP 专属）
    if (project_root / ".omp").is_dir():
        return "omp"

    # 检查 .grok 目录（Grok Build 专属）
    if (project_root / ".grok").is_dir():
        return "grok"

    # 检查 .kimi-code 目录（Kimi Code 专属）
    if (project_root / ".kimi-code").is_dir():
        return "kimi"

    # 回退：检出目录仅有 Codex 共享技能层（.agents/skills/trellis-* 目录），
    # 没有明确的平台配置目录。新克隆可能出现这种情况：.codex/ 被忽略或缺失，
    # 而共享技能已提交。必须排除同时存在 .claude/ 或其他平台目录的情况，
    # 因为 .agents/skills/ 可作为 Amp/Cline/Warp 等的共享读取层，与任意平台共存。
    agents_skills = project_root / ".agents" / "skills"
    if agents_skills.is_dir() and not _has_other_platform_dir(
        project_root, set()
    ):
        try:
            for entry in agents_skills.iterdir():
                if entry.is_dir() and entry.name.startswith("trellis-"):
                    return "codex"
        except OSError:
            pass

    return "claude"


def get_cli_adapter_auto(project_root: Path) -> CLIAdapter:
    """自动检测平台并获取 CLI 适配器。

    参数：
        project_root: 项目根目录。

    返回：
        检测到的平台对应的 CLIAdapter 实例。
    """
    platform = detect_platform(project_root)
    return CLIAdapter(platform=platform)
