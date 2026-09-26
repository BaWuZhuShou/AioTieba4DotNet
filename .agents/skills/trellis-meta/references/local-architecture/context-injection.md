# 本地上下文注入系统

Trellis 上下文注入旨在让 AI 在合适的时机读取正确文件，减少对模型记忆的依赖。在用户项目中，注入由 `.trellis/` 脚本与平台钩子、代理和技能共同实现。

## 注入的上下文类型

| 类型 | 来源 | 用途 |
| --- | --- | --- |
| 会话上下文 | `.trellis/scripts/get_context.py` | 当前开发者、Git 状态、活动任务、活动任务列表、日志、包。 |
| 工作流上下文 | `.trellis/workflow.md` | 当前 Trellis 流程与下一步行动。 |
| 规范上下文 | `.trellis/spec/` + 任务 JSONL | 实施/检查中必须遵循的规范。 |
| 任务上下文 | `.trellis/tasks/<task>/prd.md`、`design.md`、`implement.md`、`research/` | 当前任务的需求、设计、执行计划与研究。 |
| 平台上下文 | 平台钩子/设置/代理 | 让不同 AI 工具通过各自机制读取上述文件。 |

## session-start（会话启动）

支持 session-start 的平台会在会话启动、清空、压缩或收到类似事件时注入 Trellis 概览。注入内容通常包括：

- 工作流摘要。
- 当前任务状态。
- 活动任务列表。
- 规范索引路径。
- 开发者身份与 Git 状态。

如果用户认为 AI 在新会话中不知道当前任务，应先检查平台的 session-start 钩子或等价机制是否已安装并运行。

## workflow-state（工作流状态）

workflow-state 是在每轮用户交互附近注入的轻量提示。它根据当前任务状态，从 `.trellis/workflow.md` 选择一个块，例如 `no_task`、`planning`、`in_progress` 或 `completed`。

用户希望改变“某个状态下 AI 下一步该做什么”时，应先编辑 `.trellis/workflow.md` 中对应的状态块。

## 子代理上下文

实施和检查代理需要任务上下文。Trellis 有两种加载模式：

1. **钩子推送**：平台代理启动前，钩子注入 JSONL 引用文件，以及 `prd.md`、`design.md`（如存在）和 `implement.md`（如存在）。
2. **代理主动读取**：代理定义指示代理在启动后读取活动任务、JSONL 上下文和任务产物。

两种模式都将任务目录中的 JSONL 文件作为规范/研究上下文清单。任务产物独立读取，顺序为：`prd.md` → `design.md`（如存在）→ `implement.md`（如存在）。

## JSONL 读取规则

`implement.jsonl` 和 `check.jsonl` 每行包含一个 JSON 对象：

```jsonl
{"file": ".trellis/spec/backend/index.md", "reason": "后端规则"}
```

读取器应跳过没有 `file` 字段的行（例如旧版 `_example` 占位条目）。配置 JSONL 时，AI 只应包含规范/研究文件，不预先登记即将修改的代码文件。

## 活动任务与上下文键

活动任务状态位于 `.trellis/.runtime/sessions/`，按会话隔离。钩子尝试从平台事件、环境变量、对话记录路径或 `TRELLIS_CONTEXT_ID` 解析上下文键。

如果 Shell 命令看不到同一上下文键，`task.py current --source` 可能报告无活动任务。此时应检查平台是否将会话身份传入 Shell，不要手写全局当前任务文件。

## 本地定制位置

| 需求 | 修改位置 |
| --- | --- |
| 修改 session-start 注入内容 | 平台的 `session-start` 钩子或插件文件。 |
| 修改逐轮 workflow-state 规则 | `.trellis/workflow.md` 中的 `[workflow-state:STATUS]` 块。平台工作流状态钩子原样解析这些块，不内置备用文本。 |
| 修改子代理读取上下文的方式 | 平台代理定义、`inject-subagent-context` 钩子或代理前置指令。 |
| 修改 JSONL 验证/显示 | `.trellis/scripts/common/task_context.py`。 |
| 修改活动任务解析 | `.trellis/scripts/common/active_task.py`。 |

修改上下文注入时应验证两点：新会话能看到正确任务，子代理能看到正确任务产物/规范/研究。
