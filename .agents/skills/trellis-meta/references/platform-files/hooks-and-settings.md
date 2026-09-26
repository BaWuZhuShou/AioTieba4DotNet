# 钩子与设置

钩子/设置是连接平台与 Trellis 的入口层，决定平台在什么事件下执行哪些脚本、插件或扩展。

## 设置的职责

设置/配置文件通常注册：

- session-start 钩子：新会话开始或上下文重置时注入 Trellis 概览。
- workflow-state 钩子：解析 `.trellis/workflow.md` 中的 `[workflow-state:STATUS]` 块，每次用户输入时输出与当前任务 `status` 匹配的正文。只负责解析，脚本不内置备用工作流内容。
- 子代理上下文钩子：实施/检查/研究代理启动时注入任务上下文。
- Shell/会话桥接：让 Shell 命令看到相同的 Trellis 会话身份。
- 平台插件或扩展入口。

常见文件：

| 平台 | 设置/配置 |
| --- | --- |
| Claude Code | `.claude/settings.json` |
| Cursor | `.cursor/hooks.json` |
| Codex | `.codex/hooks.json`、`.codex/config.toml` |
| OpenCode | `.opencode/package.json`、`.opencode/plugins/*` |
| Kiro | `.kiro/hooks/` + 平台配置 |
| Gemini CLI | `.gemini/settings.json` |
| Qoder | `.qoder/settings.json` |
| CodeBuddy | `.codebuddy/settings.json` |
| GitHub Copilot | `.github/copilot/hooks.json` |
| Factory Droid | `.factory/settings.json` |
| Pi Agent | `.pi/settings.json`、`.pi/extensions/trellis/` |
| Trae IDE | `.trae/hooks.json` |

Reasonix 采用主动读取模式，代理文件包含启动后读取上下文的前置指令。ZCode 使用 `.zcode/config.json` 与共享钩子，包括用于子代理提示注入的 PreToolUse。Kimi Code 同样采用主动读取模式，Trellis 不会为它写入项目级设置/钩子文件（钩子只存在于用户级 `~/.kimi-code/config.toml`）；因此代理提示以技能及 `.kimi-code/agents/` 子代理定义发布，使用相同前置指令。

项目是否存在这些文件，取决于用户运行过哪些 `trellis init --<platform>` 参数。

## 钩子脚本类型

| 脚本 | 用途 |
| --- | --- |
| `session-start.py` | 生成会话启动上下文。 |
| `inject-workflow-state.py` | 解析 `.trellis/workflow.md` 中的 `[workflow-state:STATUS]` 块，输出与当前任务状态匹配的正文。无匹配块时回退为“请参阅 workflow.md 中的当前步骤”提示（旧英文形式为 `Refer to workflow.md for current step.`）。 |
| `inject-subagent-context.py` | 向子代理注入 PRD、JSONL 上下文及相关规范/研究。 |
| `inject-shell-session-context.py` | 让 Shell 命令继承 Trellis 会话身份。 |

并非每个平台都有每种钩子。不要只因平台缺少某个钩子就复制其他平台的文件；应先确认该平台是否支持对应事件。

## 本地修改场景

| 用户需求 | 修改位置 |
| --- | --- |
| AI 在新会话中应看到更多/更少上下文 | 平台的 `session-start` 钩子。 |
| 改变每轮提示策略 | `.trellis/workflow.md` 中的 `[workflow-state:STATUS]` 块。钩子原样解析 workflow.md，无需编辑脚本。 |
| 子代理无法读取 PRD/规范 | `inject-subagent-context` 钩子或代理前置指令。 |
| Shell 中的 `task.py current` 没有活动任务 | Shell/会话桥接钩子或平台环境变量配置。 |
| 禁用某项自动注入 | 设置/配置中对应的钩子注册项。 |

## 修改原则

1. **设置负责接线，钩子定义行为**。只改钩子，平台可能从不调用它；只改设置，行为可能仍未改变。
2. **先确认平台事件名称**。不同平台对 SessionStart、UserPromptSubmit、AgentSpawn、Shell 执行等事件使用不同名称。
3. **钩子读取本地 `.trellis/`，不读上游源码**。默认目标是用户项目中的 `.trellis/scripts/` 和 `.trellis/workflow.md`。
4. **错误必须可见**。钩子失败应说明哪些内容未被注入，不应静默让 AI 缺少上下文。

## 排查路径

用户反馈“AI 没读 Trellis 状态”时：

1. 检查平台设置是否注册了钩子。
2. 检查钩子文件是否存在。
3. 手动运行钩子依赖的 `.trellis/scripts/get_context.py` 或 `task.py current --source` 命令。
4. 检查 `.trellis/.runtime/sessions/` 中是否存在活动任务状态。
5. 检查平台 Shell 是否传递会话身份。
