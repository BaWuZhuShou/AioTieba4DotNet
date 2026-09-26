---
name: trellis-session-insight
description: "通过 `trellis mem` CLI 检索过去的 AI 对话。用户询问“上次 X 怎么解的”“之前讨论过吗”“关于 X 当时怎么决定的”“提醒我这个任务做了什么”“想起一段对话”，或当前需求讨论与旧工作重叠、排查熟悉的错误、跨会话继续任务、收尾复盘时使用。返回原始历史对话；根据当下情况决定更新规范、追加任务笔记、在回答中引用，或仅用于理解。"
---

# Trellis 会话洞察

本技能指导 AI **如何调用 `trellis mem`**——项目的跨会话记忆素材来源——以及**何时适合使用它**。

本技能有意定位为**能力说明，而非工作流**。没有固定输出文件、强制回写步骤，也没有“每次 finish-work 后都运行”的规则。如何使用 `mem` 返回的内容，应根据当前对话判断。本技能让 AI 知道这项能力存在，并能自行选择。

## 什么是 `trellis mem`

这是一个本地 CLI，用于索引用户过去的 Claude Code、Codex、Devin CLI、Grok、OpenCode、Pi Agent 和 ZCode 对话日志，支持列出、搜索、按 Trellis 任务边界切片，以及导出清理后的对话。Claude 和 Codex 使用 `~/.claude/projects/` 与 `~/.codex/sessions/`。Devin CLI（Cognition 终端代理，并非 `trellis init --devin` 对应的桌面版）使用 `~/.local/share/devin/cli/sessions.db`。Grok 使用 `~/.grok/sessions/`。OpenCode 使用 `~/.local/share/opencode/opencode.db`（零依赖 SQLite 读取器）。Pi 使用默认或由环境配置的会话根目录、全局 `~/.pi/agent/settings.json` 和当前项目的 `.pi/settings.json`；相对 `sessionDir` 从设置文件所在目录解析。读取项目本地 Pi 设置时，必须通过当前 cwd 或 `--cwd` 按项目范围查询。ZCode 使用 `~/.zcode/cli/db/db.sqlite`。

`mem` 不上传任何内容。所有读取都在本地完成。

## 何时使用

判断标准是：“有经验的同事会不会先问，我们是不是已经讨论过？”以下是具体情形：

- **避免重复需求讨论。** 新任务涉及用户以前处理过的领域，希望在再次提问前确认是否已有决定。
- **排查熟悉的错误。** 当前错误模式像用户以前报告或修复过的问题；找出相关旧会话可能省去一整轮排查。
- **跨会话继续。** 用户隔一段时间继续工作，只说“上次做到哪了”或“继续上次的”，没有给出细节。
- **检索决策。** 用户提到“我们关于 X 的决定”，但决定只在旧讨论里，没有写入 `prd.md` / `spec/`。
- **收尾复盘。** 用户明确要求回顾本任务的决策、困难和意外；不要把它设为每次 finish-work 的强制步骤。
- **发现历史工作模式。** 用户问“我是不是一直在 X 上犯同样的错误”或“我每次都踩这个坑吗”，跨会话检索可以回答。

如果均不适用，就不要调用 `mem`。它是工具，不是仪式。

## 何时不使用

- 相关上下文已在当前轮次、`prd.md`、`design.md`、近期 `git log` 或打开的文件中。`mem` 用于找回已不在手边的信息。
- 用户问的是代码事实，而非历史对话事实。`git log -p` / `grep` / 直接读文件更快，也更权威。
- 当前是 `trellis-implement` / `trellis-check` 子代理，派发提示已包含精选的 `implement.jsonl` / `check.jsonl` 上下文。再加 `mem` 通常只会增加干扰。
- 用户明确说“不要翻历史，只回答我问的”。

## 如何处理 `mem` 返回的内容

将输出视为**原始素材**，而非交付物。取得内容后，根据当前对话选择：

- **在回答中直接引用**：某段历史交流能回答当前问题时，引用它并注明 session-id / 阶段，方便用户核验。
- **更新 `<task>/prd.md` 或 `<task>/design.md`**：`mem` 找到本应记录但遗漏的关键决策时，先向用户展示拟议修改。
- **追加任务本地笔记**：发现属于当前任务记录却不适合 PRD 的信息时，写入 `<task>/notes.md` 等笔记，或扩展已有笔记。
- **更新 `.trellis/spec/`**：发现能帮助后续任务的项目级约定或陷阱时，运行 `trellis-update-spec`；`session-insight` 的职责止于发现。
- **仅用于理解**：在后续几轮中据此更好地回答，不写入文件。一次性回忆通常适合这种方式。

Trellis 不规定唯一存放位置。把每次回忆都塞进固定文件会累积噪声，应按具体情况决定。

## 如何调用

完整 CLI 说明位于 `references/cli-quick-reference.md`。多数情况使用以下命令之一：

```bash
# 查找内容提及关键词的会话（默认当前项目；
# 加 --global 可搜索本机全部项目）。
trellis mem search "<keyword>"

# 导出一个会话的对话，可按阶段或关键词筛选。
trellis mem extract <session-id> --phase brainstorm
trellis mem extract <session-id> --grep "<keyword>"

# 深入一个会话：前 N 个命中轮次及其前后上下文。
trellis mem context <session-id> --turns 3 --around 2

# 尚不知道会话 ID 时，先列出再筛选。
trellis mem list --cwd <project-path>
trellis mem projects   # → 列出活跃项目 cwd，再缩小范围
```

阶段切片（`--phase brainstorm|implement|all`）在 `task.py create` 和 `task.py start` 边界切分会话。复盘当前任务时，`--phase brainstorm` 恢复规划讨论，`--phase implement` 恢复执行过程。默认为 `all`。

## 触发模式

`references/triggering-patterns.md` 列出更多应触发“使用 `mem`”判断的用户表达，供校准使用时机。

## 范围外

- `mem` 不修改代码或更新文件。任何回写由你根据当下情况决定。
- `mem` 对平台 JSONL 存储只读，不向远端推送或同步。
- 本技能不替代 `trellis-update-spec`（将发现提升为项目级规范时应使用它），也不替代平台原生任务 / 规范工作流。
