---
name: trellis-meta
description: "理解并定制用户项目中的本地 Trellis 架构。修改 .trellis 及平台钩子、设置、代理、技能、命令、提示、工作流、通道运行时（trellis channel）、.trellis/agents/ 下的捆绑运行代理、可选工作流模板、基于注册表的规范刷新、trellis init 生成的跨会话记忆（trellis mem），或面向 AI 的捆绑技能（trellis-channel、trellis-session-insight、trellis-spec-bootstrap）及其自动分发流程时使用。"
---

# Trellis 架构与定制

本技能面向已经在项目中运行过 `trellis init` 的本地 Trellis 用户。AI 阅读后应理解用户项目中的 Trellis 架构、运行模型和定制入口，再根据用户需求修改生成的 `.trellis/` 与平台目录文件。

Trellis v0.6 在早期工作流 / 持久化 / 平台模型之上增加了三个架构部分。第一，多代理协作运行时：`trellis channel` 通过 `~/.trellis/channels/<project>/<channel>/events.jsonl` 中按项目划分的 JSONL 事件日志协调多个 AI 工作进程，提供工作进程 OOM 防护、论坛/主题通道、持久幂等键，以及捆绑的 `.trellis/agents/{check,implement}.md` 运行定义。第二，跨会话记忆：`trellis mem list | search | context | extract | projects` 读取磁盘上已有的 Claude Code、Codex 和 Pi Agent 原始 JSONL，按 `--phase brainstorm|implement|all` 切分，不上传任何内容。第三，双 npm 包发布：`@mindfoldhq/trellis`（CLI）与 `@mindfoldhq/trellis-core`（带 `/channel`、`/task`、`/mem`、`/testing` 子路径的 SDK）使用同一版本同步发布。这些与各平台集成文件一样，都是主要定制入口。

默认操作范围是用户项目中的本地文件：

- `.trellis/`：工作流、配置、任务、规范、工作区、脚本、捆绑运行代理和运行状态。
- 平台目录：`.claude/`、`.codex/`、`.cursor/`、`.opencode/`、`.kiro/`、`.gemini/`、`.qoder/`、`.codebuddy/`、`.github/`、`.factory/`、`.pi/`、`.reasonix/`、`.kilocode/`、`.agent/`、`.devin/`、`.kimi-code/` 等。Pi 还在文件结构之上提供原生 `trellis_subagent` 工具，支持 `single` / `parallel` / `chain` 派发模式、节流进度卡和 `isTrellisAgent()` 校验。Reasonix 将工作流技能和子代理技能都存为 `.reasonix/skills/<name>/SKILL.md`；子代理技能使用 `runAs: subagent` frontmatter。Kimi Code 将工作流技能放在共享 `.agents/skills/` 层，以 `.kimi-code/skills/<name>/SKILL.md` 提供命令与代理提示，并将同一组代理提示作为自定义子代理安装到 `.kimi-code/agents/<name>.md`。
- 共享技能层：`.agents/skills/`。
- 项目树之外由用户拥有的通道存储：`~/.trellis/channels/<project>/<channel>/events.jsonl`。
- 可通过 `trellis mem` 查询的原始平台对话日志：`~/.claude/projects/`、`~/.codex/sessions/` 和 `~/.pi/agent/sessions/`（OpenCode 的能力按已安装版本核对；本机 0.6.17 的帮助说明其 `--phase` 会警告并返回该会话的全部内容，旧 beta 的占位限制不能当作当前结论）。

不要假设用户拥有 Trellis 源码仓库。不要默认修改全局 npm 安装目录或 `node_modules`；`@mindfoldhq/trellis` 和 `@mindfoldhq/trellis-core` 都是已发布包，每次发布共享一个版本号和一个 Git 标签。

## 使用方式

1. 先阅读 `references/local-architecture/overview.md`，建立本地 Trellis 系统模型。
2. 请求涉及特定 AI 工具时，阅读 `references/platform-files/platform-map.md` 和对应平台文件说明。
3. 请求涉及多代理派发或通道工作进程时，阅读 `references/local-architecture/multi-agent-channel.md` 和捆绑的 `.trellis/agents/` 文件。
4. 用户希望改变行为时，阅读 `references/customize-local/overview.md`，再打开具体定制主题。
5. 编辑前读取用户项目中的实际文件，并以本地内容为准。

## 参考资料

### 本地架构

- `references/local-architecture/overview.md`：本地 Trellis 分层架构（工作流 / 持久化 / 平台 / 通道运行时）与定制原则。
- `references/local-architecture/generated-files.md`：`trellis init` 生成的文件及其定制边界，包括 `.trellis/agents/`。
- `references/local-architecture/workflow.md`：`.trellis/workflow.md` 中的阶段、路由、工作流状态块及可选工作流模板（`native`、`tdd`、`channel-driven-subagent-dispatch`、市场模板）。
- `references/local-architecture/task-system.md`：任务目录、活动任务、JSONL 上下文、父子任务树和任务运行状态。
- `references/local-architecture/spec-system.md`：`.trellis/spec/` 的组织、注入及基于 `registry.spec` 来源的刷新。
- `references/local-architecture/workspace-memory.md`：`.trellis/workspace/` 日志、`trellis mem` 跨会话回顾，以及 `@mindfoldhq/trellis-core/mem` SDK。
- `references/local-architecture/context-injection.md`：钩子、子代理前置指令和通道运行时工作进程收件箱路由。
- `references/local-architecture/multi-agent-channel.md`：`trellis channel` 子命令、项目级事件存储、论坛/主题通道、工作进程 OOM 防护、持久幂等与捆绑的 `.trellis/agents/` 运行代理。
- `references/local-architecture/bundled-skills.md`：自动分发的捆绑技能（`trellis-meta`、`trellis-spec-bootstrap`、`trellis-session-insight`），以及 `getBundledSkillTemplates()` 如何将其发往各平台技能根目录。

### 平台文件

- `references/platform-files/overview.md`：共享 `.trellis/` 文件与平台目录的关系，以及四种平台集成模式（钩子驱动、代理前置指令、主会话工作流、通道运行时）。
- `references/platform-files/platform-map.md`：所有支持平台的技能、代理、钩子和扩展路径，包括 Reasonix 和 Pi 的原生 `trellis_subagent` 扩展。
- `references/platform-files/hooks-and-settings.md`：设置/配置、钩子、插件和扩展如何连接 Trellis；涵盖 `channel.worker_guard.*` 与 `codex.dispatch_mode`。
- `references/platform-files/agents.md`：各平台 `trellis-research` / `trellis-implement` / `trellis-check` 子代理文件，以及通道运行时捆绑的 `.trellis/agents/{check,implement}.md`。
- `references/platform-files/skills-and-commands.md`：技能、命令、提示和工作流的差异及修改方式。

### 本地定制

- `references/customize-local/overview.md`：按用户需求选择正确的本地定制入口。
- `references/customize-local/change-workflow.md`：修改阶段、路由、下一步行动、工作流状态及所选工作流模板。
- `references/customize-local/change-task-lifecycle.md`：修改任务创建、状态、归档行为、父子关联、归档 slug 冲突处理和生命周期钩子。
- `references/customize-local/change-context-loading.md`：修改任务、规范、日志、钩子上下文、通道收件箱消息和 `trellis mem` 回顾的加载方式。
- `references/customize-local/change-hooks.md`：修改平台钩子、设置、任务生命周期钩子（`hooks.after_*`）和 Shell 会话桥接。
- `references/customize-local/change-agents.md`：修改平台代理、捆绑通道运行代理中的研究、实施、检查行为，以及 Codex 的 `dispatch_mode` 开关。
- `references/customize-local/change-skills-or-commands.md`：添加或修改本地技能、命令、提示和工作流；涵盖上游捆绑技能自动分发。
- `references/customize-local/change-spec-structure.md`：调整 `.trellis/spec/` 下的项目规范结构，包括基于注册表的来源。
- `references/customize-local/add-project-local-conventions.md`：将团队规则放入项目本地规范或本地技能。

## 当前规则

- `.trellis/workflow.md` 是本地工作流的权威源；初始内容在 `trellis init` 时从工作流模板中选择（内置 `native`、`tdd`、`channel-driven-subagent-dispatch` 或市场模板），可通过 `trellis workflow --template <id>` 重新选择。活动模板引用的 `.trellis/agents/<name>.md` 缺失时，会在 stderr 输出指向 `trellis update` 的非阻断警告。
- `.trellis/config.yaml` 是项目级 Trellis 配置入口。它包含任务生命周期钩子（`hooks.after_create` / `after_start` / `after_finish` / `after_archive`）、日志设置（`session_commit_message` / `max_journal_lines` / `session_auto_commit`）、通道工作进程防护（`channel.worker_guard.idle_timeout` / `max_live_workers`）、Codex 派发模式（`codex.dispatch_mode: auto | inline`，旧值 `sub-agent` 是 `auto` 的兼容别名），以及规范注册表块（`registry.spec.source` + `registry.spec.template`）。
- `.trellis/spec/` 保存用户项目专用的编码约定和设计约束。设置 `registry.spec` 后，`trellis update` 会刷新文件；本地编辑会通过 `.trellis/.template-hashes.json` 显示为“用户已修改”（`modified by user`）冲突。
- `.trellis/tasks/` 保存任务 PRD、设计说明、实施计划、研究文件和 JSONL 上下文。任务构成父子树：`task.py create --parent <slug>`、`task.py add-subtask <parent> <child>`、`task.py remove-subtask <parent> <child>` 和 `task.py list-context <task>`。`task.py create` 会拒绝 `.trellis/tasks/archive/**` 中已存在的 slug。
- `.trellis/workspace/` 保存**专门撰写**的开发者日志。原始跨会话对话**不在此处**，而是在磁盘上的 `~/.claude/projects/`、`~/.codex/sessions/` 和 `~/.pi/agent/sessions/`，通过 `trellis mem search|extract|context` 找回。捆绑的 `trellis-session-insight` 技能说明何时使用 `mem`。
- `.trellis/agents/{check,implement}.md` 是捆绑的平台无关通道运行代理定义，由 `trellis channel spawn --agent <name>` 加载。文件可编辑；`trellis update` 会补齐缺失文件。编辑各平台的 `trellis-implement.md` / `trellis-check.md` **不会**改变通道运行时工作进程的行为。
- `~/.trellis/channels/<project>/<channel>/events.jsonl` 是每个项目、每个通道的运行事件日志。由用户拥有，序号通过文件锁分配，支持持久 `idempotencyKey`；不会存放在 `.trellis/` 下。
- 多文件捆绑技能（`trellis-meta`、`trellis-spec-bootstrap`、`trellis-session-insight`、`trellis-channel`）由 **Trellis 上游源码** `packages/cli/src/templates/common/index.ts` 中的 `getBundledSkillTemplates()` 自动分发到各平台技能根目录。向上游 `packages/cli/src/templates/common/bundled-skills/` 添加新目录后，下次 `trellis update` 会将其发往所有平台。
- 平台设置/配置文件决定实际运行哪些钩子、代理、技能、命令、提示和工作流。Reasonix 没有设置文件；行为编码在技能 frontmatter 中。
- `.trellis/.template-hashes.json` 和 `.trellis/.runtime/` 是管理/运行状态文件。编辑前先确认必要性。

## 禁止事项

- 不要把 Trellis 上游源码当作本地定制的默认目标。
- 不要为实现项目需求而修改全局 npm 安装目录、`node_modules/@mindfoldhq/trellis` 或 `node_modules/@mindfoldhq/trellis-core`；这两个包同步发布。
- 不要用默认模板覆盖用户修改的本地文件；先检查 `.trellis/.template-hashes.json`，优先使用 `.new` 旁文件，避免破坏性覆盖。
- 不要把团队私有项目规则放进任何公共捆绑技能（`trellis-meta`、`trellis-spec-bootstrap`、`trellis-session-insight`、`trellis-channel`）；应放入 `.trellis/spec/`、项目本地技能、当前任务或工作区日志。`trellis update` 会覆盖捆绑技能目录中的内容。
- 不要手动编辑 `~/.trellis/channels/<project>/<channel>/events.jsonl`；序号在文件锁下分配，安全重放的写入应通过 `trellis channel` CLI 或 `@mindfoldhq/trellis-core/channel` SDK。
- 目标是改变通道运行时工作进程行为时，不要编辑 `.claude/agents/trellis-implement.md`（或其他平台子代理文件）；应编辑 `.trellis/agents/<name>.md`。
- 不要把已删除或从未发布的机制描述为当前 Trellis 行为；声称某个配置开关存在前，先对照本地 `.trellis/config.yaml` 和已安装 CLI 的 `trellis --help`。
