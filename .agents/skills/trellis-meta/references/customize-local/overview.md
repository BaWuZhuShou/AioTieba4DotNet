# 本地定制概览

本目录面向在用户项目中工作的本地 AI；该项目已通过 npm 安装 Trellis 并运行过 `trellis init`。AI 应修改项目内生成的 `.trellis/` 和平台目录，而不是 Trellis CLI 上游源码。

## 先判断用户实际希望修改什么

| 用户表述 | 先阅读 |
| --- | --- |
| “修改 Trellis 流程 / 阶段 / 下一步提示” | `change-workflow.md` |
| “修改任务创建、状态、归档或钩子” | `change-task-lifecycle.md` |
| “AI 没读上下文 / 修改注入内容” | `change-context-loading.md` |
| “某个平台的钩子行为不符合预期” | `change-hooks.md` |
| “修改实施/检查/研究代理的行为” | `change-agents.md` |
| “添加技能/命令/工作流/提示” | `change-skills-or-commands.md` |
| “调整项目规范结构” | `change-spec-structure.md` |
| “添加团队约定和本地说明” | `add-project-local-conventions.md` |

## 通用操作顺序

1. **确认平台和目录**：检查实际存在的目录，如 `.claude/`、`.codex/`、`.cursor/`、`.zcode/`。
2. **确认当前活动任务**：运行 `python3 ./.trellis/scripts/task.py current --source`。
3. **读取本地权威源**：优先读取 `.trellis/workflow.md`、`.trellis/config.yaml` 及相关平台文件。
4. **限定修改范围**：只编辑与用户需求有关的文件。
5. **同步语义**：共享流程变化时，检查平台入口是否也需修改；平台入口变化时，检查 `.trellis/workflow.md` 是否仍然一致。

## 本地文件优先级

| 层次 | 文件 |
| --- | --- |
| 工作流 | `.trellis/workflow.md` |
| 项目配置 | `.trellis/config.yaml` |
| 任务资料 | `.trellis/tasks/<task>/` |
| 项目规范 | `.trellis/spec/` |
| 运行脚本 | `.trellis/scripts/` |
| 平台集成 | `.claude/`、`.codex/`、`.cursor/`、`.opencode/`、`.zcode/` 等目录 |
| 共享技能 | `.agents/skills/` |

## 默认不要做的事

- 不要编辑全局 npm 安装目录。
- 不要编辑 `node_modules/@mindfoldhq/trellis`。
- 不要假设用户拥有 Trellis GitHub 仓库。
- 不要用默认模板覆盖用户已经修改的本地文件。
- 不要把团队项目规则放进公共 `trellis-meta`；项目规则属于 `.trellis/spec/` 或本地技能。

## 何时检查上游源码

只有用户明确表达以下目标之一时，才切换到上游源码视角：

- “我要给 Trellis 提 PR”
- “我要修改 npm 包的发布内容”
- “我要 fork Trellis”
- “我要修改 `trellis init/update` 的生成逻辑”

其他情况默认修改用户项目中的本地 Trellis 文件。
