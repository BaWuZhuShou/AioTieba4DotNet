# 平台文件概览

Trellis 将同一套本地架构连接到不同 AI 工具。`.trellis/` 保存共享运行脚本与状态；平台目录保存适配文件，定义各 AI 工具如何进入 Trellis。

本地 AI 修改 Trellis 时，应先区分两类文件：

- **共享文件**：`.trellis/workflow.md`、`.trellis/tasks/`、`.trellis/spec/`、`.trellis/scripts/`。
- **平台文件**：`.claude/`、`.snow/`、`.codex/`、`.cursor/`、`.opencode/`、`.kiro/`、`.gemini/`、`.qoder/`、`.codebuddy/`、`.github/`、`.factory/`、`.pi/`、`.trae/`、`.kilocode/`、`.agent/`、`.devin/`、`.reasonix/`、`.zcode/`、`.kimi-code/` 等目录。

平台文件不保存业务状态，而是让对应 AI 工具读取 Trellis 状态、调用 Trellis 脚本、加载 Trellis 技能/代理/钩子。

## 平台文件分类

| 类别 | 常见路径 | 用途 |
| --- | --- | --- |
| 设置/配置 | `.claude/settings.json`、`.codex/hooks.json`、`.qoder/settings.json`、`.trae/hooks.json` | 注册钩子、插件、扩展或平台行为。 |
| 钩子/插件/扩展 | `.claude/hooks/`、`.opencode/plugins/`、`.pi/extensions/` | 在会话启动、用户输入、代理启动、Shell 执行等事件时注入上下文。 |
| 代理 | `.claude/agents/`、`.codex/agents/`、`.kiro/agents/`、`.zcode/agents/` | 定义 `trellis-research`、`trellis-implement` 和 `trellis-check`。 |
| 技能 | `.claude/skills/`、`.agents/skills/`、`.qoder/skills/`、`.zcode/skills/` | 自动触发或可按需读取的能力说明。 |
| 命令/提示/工作流 | `.cursor/commands/`、`.github/prompts/`、`.devin/workflows/`、`.zcode/commands/` | 用户显式调用的入口。 |

## 三种平台集成模式

### 1. 钩子 / 扩展驱动

这些平台可在特定事件触发脚本或插件，主动向 AI 注入 Trellis 上下文。

常见能力：

- session-start 注入 `.trellis/` 概览。
- 每轮用户交互的工作流状态提示。
- 子代理启动时注入 PRD/规范/研究。
- Shell 命令继承会话身份。

如需改变“AI 在什么时候知道什么”，先检查钩子/插件/扩展与设置。

### 2. 代理前置指令 / 主动读取

有些平台无法可靠地让钩子改写子代理提示，因此代理文件本身会指示代理在启动后读取活动任务、PRD 和 JSONL 上下文。

如需改变子代理加载上下文的方式，应检查代理文件本身。

### 3. 主会话工作流

有些平台没有 Trellis 子代理或钩子能力，依赖工作流/技能/命令指导主会话 AI 读取文件、运行脚本并推进任务。

如需改变行为，应检查平台工作流/技能/命令及 `.trellis/workflow.md`。

## 本地修改顺序

用户要求定制某个平台的行为时，AI 应按以下顺序检查：

1. 阅读 `.trellis/workflow.md`，确认共享流程。
2. 阅读目标平台的设置/配置，查看注册了哪些钩子/代理/技能/命令。
3. 阅读目标平台的代理/技能/命令/钩子。
4. 修改最接近用户需求的本地文件。
5. 如果变更影响共享流程，同步 `.trellis/workflow.md` 或 `.trellis/spec/`。

不要只改平台文件而忘记共享工作流；也不要只改 `.trellis/workflow.md`，却忽略平台入口可能仍保留旧说明。
