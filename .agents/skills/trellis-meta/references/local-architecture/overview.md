# 本地 Trellis 架构概览

`trellis-meta` 面向已经运行 `trellis init` 的用户项目。用户机器上通常只有通过 npm 安装的 `trellis` 命令，以及项目内生成的 Trellis 文件；未必有 Trellis CLI 源码。

因此，AI 使用本技能时，默认定制目标是用户项目中的本地文件：

- `.trellis/`：工作流、任务、规范、记忆、脚本和运行状态。
- 平台目录：`.claude/`、`.codex/`、`.cursor/`、`.opencode/`、`.kiro/`、`.gemini/`、`.qoder/`、`.codebuddy/`、`.github/`、`.factory/`、`.pi/`、`.kilocode/`、`.agent/`、`.devin/`、`.reasonix/`、`.zcode/` 等目录。
- 共享技能层：`.agents/skills/`。

不要默认引导用户 fork Trellis CLI 仓库。只有用户明确要求修改 Trellis 上游源码、发布 npm 包或贡献 PR 时，才以上游源码为操作目标。

## 本地系统模型

Trellis 在用户项目中提供三层结构：

1. **工作流层**：`.trellis/workflow.md` 定义阶段、路由、下一步行动和提示块。
2. **持久化层**：`.trellis/tasks/`、`.trellis/spec/` 和 `.trellis/workspace/` 存储任务、规范和会话记忆。
3. **平台集成层**：平台目录中的钩子、设置、代理、技能、命令、提示和工作流，将 Trellis 工作流连接到不同 AI 工具。

三层都位于用户项目内，因此 AI 可以直接读取和修改。

## 核心路径

| 路径 | 用途 |
| --- | --- |
| `.trellis/workflow.md` | 工作流阶段、技能路由和工作流状态提示块。 |
| `.trellis/config.yaml` | 项目配置、任务生命周期钩子、monorepo 包配置和日志配置。 |
| `.trellis/spec/` | 用户项目专用的编码约定和思考指南。 |
| `.trellis/tasks/` | 各任务的 PRD、技术说明、研究文件和 JSONL 上下文。 |
| `.trellis/workspace/` | 按开发者保存的日志与跨会话记忆。 |
| `.trellis/scripts/` | 命令、钩子和上下文注入使用的本地 Python 运行脚本。 |
| `.trellis/.runtime/` | 会话级运行状态，例如当前任务指针。 |
| `.trellis/.template-hashes.json` | Trellis 托管文件的模板哈希，供更新时判断本地文件是否被用户修改。 |

## AI 定制原则

1. **先找本地权威源**：不要凭记忆编辑。先读取 `.trellis/workflow.md`、`.trellis/config.yaml`、相关平台目录和任务文件。
2. **编辑用户项目，不改 npm 包缓存**：修改项目内生成文件，不改 `node_modules` 或全局 npm 安装目录。
3. **保持平台文件与 `.trellis/` 一致**：工作流路由改变时，也检查平台技能或命令是否仍描述相同流程。
4. **项目专用规则放入 `.trellis/spec/` 或本地技能**：不要把团队约定放进 `trellis-meta`。
5. **保留用户修改**：文件已有本地修改时，从当前内容继续工作，不要用默认模板覆盖。

## 本目录的使用方式

- 了解初始化后有哪些文件，阅读 `generated-files.md`。
- 修改阶段、路由或下一步行动，阅读 `workflow.md`。
- 修改任务模型、JSONL 上下文或活动任务行为，阅读 `task-system.md`。
- 修改编码约定的注入，阅读 `spec-system.md`。
- 了解日志和跨会话记忆，阅读 `workspace-memory.md`。
- 修改钩子或子代理上下文加载，阅读 `context-injection.md`。
