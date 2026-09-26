# 技能、命令、提示与工作流

技能和命令是用户与 Trellis 交互的文本入口。不同平台使用不同名称，但核心目的相同：用户表达某种意图时，告诉 AI 如何进入 Trellis 流程。

## 概念差异

| 类型 | 触发方式 | 适用内容 |
| --- | --- | --- |
| 技能 | AI 自动匹配或用户明确提及 | 长期能力、工作流规则、修改指南。 |
| 命令 | 用户显式调用 | continue、finish-work 等明确操作入口。 |
| 提示 | 用户显式调用或平台选择 | 类似命令，但使用平台提示格式。 |
| 工作流 | 用户显式选择或平台自动匹配 | 没有子代理/钩子时指导主会话。 |

Trellis 工作流技能通常共享一组语义：brainstorm（需求探索）、before-dev（开发前准备）、check（检查）、update-spec（更新规范）、break-loop（深度复盘）。`trellis-meta` 等多文件内置技能使用分层参考资料。

## 常见路径

| 平台 | 常见入口 |
| --- | --- |
| Claude Code | `.claude/skills/`、`.claude/commands/` |
| Cursor | `.cursor/skills/`、`.cursor/commands/` |
| OpenCode | `.opencode/skills/`、`.opencode/commands/` |
| Codex | `.agents/skills/`、`.codex/skills/` |
| Kilo | `.kilocode/skills/`、`.kilocode/workflows/` |
| Kiro | `.kiro/skills/` |
| Gemini CLI | `.agents/skills/`、`.gemini/commands/` |
| Antigravity | `.agent/skills/`、`.agent/workflows/` |
| Devin | `.devin/skills/`、`.devin/workflows/` |
| Qoder | `.qoder/skills/`、`.qoder/commands/` |
| CodeBuddy | `.codebuddy/skills/`、`.codebuddy/commands/` |
| GitHub Copilot | `.github/skills/`、`.github/prompts/` |
| Factory Droid | `.factory/skills/`、`.factory/commands/` |
| Pi Agent | `.agents/skills/` |
| Reasonix | `.reasonix/skills/` |
| ZCode | `.zcode/skills/`、`.zcode/commands/` |
| Kimi Code | `.agents/skills/`、`.kimi-code/skills/`（命令以 `/skill:trellis-*` 技能发布） |

在用户项目中，以 init 实际生成的文件为准。

## 技能结构

常见技能是一个目录：

```text
trellis-meta/
├── SKILL.md
└── references/
```

`SKILL.md` 应告诉 AI：

- 何时使用此技能。
- 当前任务应先阅读哪个参考文件。
- 哪些事不应做。

参考文件承载较长说明，避免入口文件包含所有内容。

## 命令/提示/工作流结构

命令、提示和工作流通常是单文件，内容应包括：

- 何时使用。
- 读取哪些 `.trellis/` 文件。
- 运行哪些脚本。
- 完成后如何报告。

它们不应保存任务状态；任务状态属于 `.trellis/tasks/` 和 `.trellis/.runtime/`。

## 本地修改场景

| 用户需求 | 修改位置 |
| --- | --- |
| 修改 AI 自动触发规则 | 对应技能 frontmatter 中的 description。 |
| 修改用户命令行为 | 对应命令/提示/工作流文件。 |
| 添加项目本地技能 | 平台技能目录或共享 `.agents/skills/`。 |
| 让多个平台共享一种能力 | 在各平台技能目录编写等价技能，或在支持的平台上使用共享 `.agents/skills/` 层。 |
| 修改 finish/continue 入口 | 平台命令/提示/工作流。 |

## 修改原则

1. **入口文件保持简短，参考文件承载长内容**。对 `trellis-meta` 等多文件技能尤其重要。
2. **触发描述要具体**。描述过宽会误触发，过窄则可能无法触发。
3. **同一语义跨平台保持一致**。文件格式可以不同，但行为说明应一致。
4. **项目专用能力放在本地技能中**。不要把团队私有流程放进公共 `trellis-meta`。

如果用户只是希望本地 AI 多了解一条项目规则，通常应创建项目本地技能或更新 `.trellis/spec/`，不必修改 Trellis 内置工作流技能。
