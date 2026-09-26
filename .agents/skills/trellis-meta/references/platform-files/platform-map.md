# 平台文件映射

本页按平台列出用户项目中常见的 Trellis 文件位置。实际项目是否存在某个平台目录，取决于用户执行过哪些 `trellis init --<platform>` 命令。

## 对照表

| 平台 | CLI 参数 | 主目录 | 技能目录 | 代理目录 | 钩子/扩展 |
| --- | --- | --- | --- | --- | --- |
| Claude Code | `--claude` | `.claude/` | `.claude/skills/` | `.claude/agents/` | `.claude/hooks/` + `.claude/settings.json` |
| Cursor | `--cursor` | `.cursor/` | `.cursor/skills/` | `.cursor/agents/` | `.cursor/hooks.json` + `.cursor/hooks/` |
| OpenCode | `--opencode` | `.opencode/` | `.opencode/skills/` | `.opencode/agents/` | `.opencode/plugins/` |
| Codex | `--codex` | `.codex/` | `.agents/skills/` | `.codex/agents/` | `.codex/hooks/` + `.codex/hooks.json` |
| Kilo | `--kilo` | `.kilocode/` | `.kilocode/skills/` | 通常没有 | `.kilocode/workflows/` |
| Kiro | `--kiro` | `.kiro/` | `.kiro/skills/` | `.kiro/agents/` | `.kiro/hooks/` |
| Gemini CLI | `--gemini` | `.gemini/` | `.agents/skills/` | `.gemini/agents/` | `.gemini/settings.json` + `.gemini/hooks/` |
| Antigravity | `--antigravity` | `.agent/` | `.agent/skills/` | 通常没有 | `.agent/workflows/` |
| Devin | `--devin` | `.devin/` | `.devin/skills/` | 通常没有 | `.devin/workflows/` |
| Qoder | `--qoder` | `.qoder/` | `.qoder/skills/` | `.qoder/agents/` | `.qoder/hooks/` + `.qoder/settings.json` |
| CodeBuddy | `--codebuddy` | `.codebuddy/` | `.codebuddy/skills/` | `.codebuddy/agents/` | `.codebuddy/hooks/` + `.codebuddy/settings.json` |
| GitHub Copilot | `--copilot` | `.github/` | `.github/skills/` | `.github/agents/` | `.github/copilot/hooks/` + 提示 |
| Factory Droid | `--droid` | `.factory/` | `.factory/skills/` | `.factory/droids/` | `.factory/hooks/` + 设置 |
| DeepSeek Harness (dsh) | `--dsh` | `.dsh/` | `.agents/skills/`（共享）+ `.dsh/skills/`（入口技能） | 无（工作流技能在当前会话内执行实施/检查） | 无（class-2 主动读取模式；无项目钩子/设置） |
| Pi Agent | `--pi` | `.pi/` | `.agents/skills/` | `.pi/agents/` | `.pi/extensions/trellis/`（原生 `trellis_subagent` 工具）+ `.pi/settings.json` |
| Trae IDE | `--trae` | `.trae/` | `.trae/skills/` | `.trae/agents/` | `.trae/hooks/` + `.trae/hooks.json` |
| Reasonix | `--reasonix` | `.reasonix/` | `.reasonix/skills/` | 无独立目录，子代理是使用 `runAs: subagent` frontmatter 的技能 | 无 |
| ZCode | `--zcode` | `.zcode/` | `.zcode/skills/` | `.zcode/agents/` | `.zcode/hooks/` + `.zcode/config.json`（SessionStart + UserPromptSubmit + PreToolUse Agent/Task）；子代理使用钩子注入上下文 |
| Grok Build | `--grok` | `.grok/` | `.grok/skills/` | `.grok/agents/` | 主动读取前置指令（无钩子；扁平 `.grok/commands/trellis-*.md`） |
| Kimi Code | `--kimi` | `.kimi-code/` | `.agents/skills/`（共享）+ `.kimi-code/skills/` | `.kimi-code/agents/`（自定义子代理；同一组提示也作为技能发布） | 无（主动读取前置指令；无项目钩子/设置） |
| Snow CLI | `--snow` | `.snow/` | `.snow/skills/` | `.snow/agents/`（自动发现；主要路径） | class-1：自动注入 + 项目代理 + `beforeSubAgentStart`（`.snow/hooks/` 的 `session`/`user`/`subagent` 模式 → `additionalContext` JSON）；无旧版子代理 JSON；命令为 `.snow/commands/trellis-*.json` |

## 能力分组

### 支持 Trellis 子代理

这些平台通常有 `trellis-research`、`trellis-implement` 和 `trellis-check` 文件：

- Claude Code
- Cursor
- OpenCode
- Codex
- Kiro
- Gemini CLI
- Qoder
- CodeBuddy
- GitHub Copilot
- Factory Droid
- Pi Agent
- Trae IDE
- Reasonix（以使用 `runAs: subagent` 的技能形式放在 `.reasonix/skills/` 下，没有独立 `agents/` 目录）
- ZCode
- Grok Build（`.grok/agents/`；通过带 `subagent_type` 的 `spawn_subagent` 派发）
- Kimi Code（`.kimi-code/agents/`；同一组提示也作为技能放在 `.kimi-code/skills/` 下）
- Snow CLI（`.snow/agents/`；自动发现的项目代理 + class-1 钩子）

修改实施/检查/研究行为时，应先查找对应平台的代理文件。

### 原生 Trellis 子代理工具

某些平台提供宿主运行时直接理解的原生工具。模型像调用其他工具一样调用它，宿主则渲染进度卡、对照 `.<platform>/agents/` 验证代理名称，并执行派发模式约束。

- Pi Agent——`trellis_subagent` 工具，定义于 `.pi/extensions/trellis/index.ts`。支持 `single` / `parallel` / `chain` 派发模式，并实时发出 `trellis-subagent-progress` 事件。

修改这些平台的子代理派发行为时，应编辑扩展文件，**而不是**代理 Markdown。代理 Markdown 定义职责，宿主扩展负责派发、验证与进度渲染。

### 主会话工作流平台

这些平台更多依赖工作流/技能来指导主会话：

- Kilo
- Antigravity
- Devin

修改行为时先检查工作流和技能，不要假设存在 Trellis 子代理。

### 共享 `.agents/skills/`

Codex、Gemini CLI、Pi Agent、Kimi Code 和 DeepSeek Harness (dsh) 写入共享 `.agents/skills/` 层。某些支持 agentskills.io 的工具也能读取此目录。用户希望多个兼容工具共享同一技能时，可优先考虑 `.agents/skills/`，但不要假设所有平台都会读取。ZCode 将 Trellis 托管技能保存在 `.zcode/skills/`。

## 修改平台文件时的判断规则

1. 用户指定平台：只修改该平台目录，除非共享工作流/规范文件也必须变化。
2. 用户说“所有平台都要这样”：逐平台同步等价入口，不要只改一个目录。
3. 用户只说“我的 AI”：检查项目实际存在的配置目录，推断当前 AI 平台。
4. 用户需要项目规则：优先使用 `.trellis/spec/` 或项目本地技能。
5. 用户需要改变 Trellis 行为：编辑 `.trellis/workflow.md` 及平台钩子/代理/技能/命令。

## 路径不一致时

平台生态会变化，用户项目也可能已有定制。若此表与本地文件不一致，以用户项目实际设置/配置为准：

- 检查设置注册的钩子。
- 检查命令/提示/工作流指向的脚本。
- 根据代理文件当前写明的读取规则判断行为。

不要仅因某个自定义文件未列在此路径表中就删除它。

### `.omp/`——Oh My Pi（OMP）

基于扩展的平台。OMP 原生提供方自动发现所有子目录。

```text
.omp/
├── commands/          # 斜杠命令（扁平 .md 文件）
├── skills/            # 自动触发技能（每个目录一个 SKILL.md）
├── agents/            # 代理定义（.md）
└── extensions/
    └── trellis/
        └── index.ts   # Trellis 扩展（上下文注入）
```

没有 `settings.json`：OMP 自动扫描 `.omp/` 子目录。
没有 Python 钩子：等价钩子行为位于 TypeScript 扩展中。
