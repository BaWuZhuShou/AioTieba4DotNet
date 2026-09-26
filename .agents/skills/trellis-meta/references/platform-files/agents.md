# 代理

Trellis 代理文件定义专门角色。用户项目中常见的 Trellis 代理有：

- `trellis-research`
- `trellis-implement`
- `trellis-check`

各平台的文件位置和格式不同，但职责边界应保持一致。

## 代理职责

| 代理 | 职责 |
| --- | --- |
| `trellis-research` | 调查问题，将发现写入当前任务的 `research/`。 |
| `trellis-implement` | 根据 `prd.md`、可选的 `design.md` / `implement.md`、`implement.jsonl` 及相关规范/研究进行实施。 |
| `trellis-check` | 审查变更、修复发现的问题，并运行必要检查。 |

代理文件不应退化为通用聊天提示。它应定义输入来源、写入边界、是否可修改代码，以及结果报告方式。

## 常见路径

| 平台 | 代理路径 |
| --- | --- |
| Claude Code | `.claude/agents/trellis-*.md` |
| Cursor | `.cursor/agents/trellis-*.md` |
| OpenCode | `.opencode/agents/trellis-*.md` |
| Codex | `.codex/agents/trellis-*.toml` |
| Kiro | `.kiro/agents/trellis-*.json` |
| Gemini CLI | `.gemini/agents/trellis-*.md` |
| Qoder | `.qoder/agents/trellis-*.md` |
| CodeBuddy | `.codebuddy/agents/trellis-*.md` |
| Factory Droid | `.factory/droids/trellis-*.md` |
| Pi Agent | `.pi/agents/trellis-*.md` |
| Reasonix | `.reasonix/skills/trellis-*/SKILL.md`（子代理 frontmatter） |
| ZCode | `.zcode/agents/trellis-*.md` |
| Kimi Code | `.kimi-code/agents/trellis-*.md`（自定义子代理；同一组提示也以 `.kimi-code/skills/trellis-*/SKILL.md` 发布） |

GitHub Copilot 的代理/提示支持由 `.github/agents/`、`.github/prompts/`、`.github/skills/` 等目录共同提供；应检查用户项目中实际生成的文件。

Kilo、Antigravity、Devin 等主会话工作流平台可能没有 Trellis 子代理文件，通常依赖工作流/技能指导主会话。

## 两种上下文加载模式

### 钩子推送

平台钩子在代理启动前注入任务上下文。代理文件自身可以更侧重职责与边界。

常见于支持代理钩子的平台。

### 代理主动读取

代理文件指示代理启动后读取：

- `python3 ./.trellis/scripts/task.py current --source`
- `implement.jsonl` 或 `check.jsonl`
- JSONL 引用的规范/研究文件
- 当前任务的 `prd.md`
- `design.md`（如存在）
- `implement.md`（如存在）

此模式适用于钩子无法可靠改写子代理提示的平台。

## 本地修改场景

| 用户需求 | 修改位置 |
| --- | --- |
| 实施代理必须遵循额外限制 | 平台的 `trellis-implement` 代理文件。 |
| 检查代理必须运行项目专用命令 | `trellis-check` 代理文件，必要时更新 `.trellis/spec/`。 |
| 研究代理必须输出固定格式 | `trellis-research` 代理文件。 |
| 代理无法读取任务上下文 | 代理前置指令或 `inject-subagent-context` 钩子。 |
| 添加项目专用代理 | 平台代理目录 + 相关工作流/命令/技能入口。 |

## 修改原则

1. **保持职责单一**。不要将研究、实施和检查职责混入同一个代理。
2. **明确读取顺序**。代理必须知道先从活动任务开始，读取 JSONL/规范上下文，再读取 `prd.md`、`design.md`（如存在）和 `implement.md`（如存在）。
3. **明确写入边界**。研究通常只写入 `research/`；实施可以写代码；检查可以修复问题。
4. **多平台项目保持语义同步**。用户同时配置 Claude、Codex 和 Cursor 时，应判断对某个平台代理的修改是否也需应用到其他平台。

## 不要默认编辑上游模板

本地 AI 默认应修改用户项目内部的平台代理文件。只有用户明确希望向 Trellis 贡献变更时，才讨论上游模板源码。
