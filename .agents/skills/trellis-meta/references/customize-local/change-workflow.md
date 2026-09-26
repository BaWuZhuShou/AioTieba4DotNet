# 修改本地工作流

用户希望改变 Trellis 阶段、下一步提示、是否创建任务、是否使用子代理或何时检查/收尾时，应先编辑 `.trellis/workflow.md`。

## 先读取这些文件

1. `.trellis/workflow.md`
2. 当前平台的入口文件，例如技能/命令/提示/工作流
3. 当前任务的 `task.json` 和 `prd.md`

## 常见需求与修改位置

| 需求 | 修改位置 |
| --- | --- |
| 修改阶段名称或顺序 | 阶段索引（旧英文标题 `Phase Index`）及对应阶段章节；同步检查标题解析器。 |
| 修改无活动任务时是否创建任务 | `[workflow-state:no_task]` 状态块。 |
| 修改规划时的下一步 | 阶段 1 和 `[workflow-state:planning]`。 |
| 修改 in_progress 时是否必须使用代理 | 阶段 2 和 `[workflow-state:in_progress]`。 |
| 修改完成后的收尾 | 阶段 3 及仍生效的 `[workflow-state:in_progress]` / `[workflow-state:in_progress-inline]`；`completed` 块当前仅为保留提示，不能只修改它。 |
| 修改用户意图触发的技能 | 技能路由表。 |

## 修改步骤

1. 在 `.trellis/workflow.md` 中找到相关章节。
2. 修改规则时保留明确的触发条件和下一步行动。
3. 添加或重命名技能/代理时，同步平台目录中的对应文件。
4. 修改工作流状态只需编辑 `.trellis/workflow.md` 中的 `[workflow-state:STATUS]` 块。钩子只负责解析，会读取块中的全部内容。起止标签的 STATUS 字符串必须一致（`[workflow-state:foo]…[/workflow-state:foo]`）；不匹配的 STATUS 标签对会被静默丢弃。
5. 让 AI 重新读取 `.trellis/workflow.md`；不要继续沿用旧对话中的规则。

## 示例：放宽任务创建要求

如需改变可跳过任务创建的条件，通常编辑 `[workflow-state:no_task]`：

```md
[workflow-state:no_task]
如果一次回复即可解释清楚、不修改文件且无需研究，则不必创建任务。
[/workflow-state:no_task]
```

如果正式的阶段 1 流程也需要变化，同步阶段 1 章节。

## 示例：某个平台不使用子代理

用户只希望一个平台不使用子代理时，先确认该平台在工作流中是否有独立分组。随后修改该平台组的阶段 2 路由，不要删除所有平台的 `trellis-implement` / `trellis-check` 指令。

## `/trellis:continue` 路由表

`/trellis:continue` 通过判断下一步需加载的阶段步骤来恢复任务。判断依据是 `task.json.status` 与任务目录中的产物是否存在。映射固定在命令本身；添加自定义状态的定制版本必须同时扩展 workflow.md 标签块和下表。

| `status` | 产物状态 | 恢复位置 |
| --- | --- | --- |
| `planning` | 缺少 `prd.md` | 阶段 1.1（加载 `trellis-brainstorm`） |
| `planning` | 轻量任务的 `prd.md` 已完成 | 请求启动评审，通过后执行 `task.py start` |
| `planning` | 复杂任务缺少 `design.md` 或 `implement.md` | 补全缺少的规划产物 |
| `planning` | 复杂任务已有 `prd.md`、`design.md` 和 `implement.md` | 请求启动评审，通过后执行 `task.py start` |
| `in_progress` | 对话历史中尚无实施 | 阶段 2.1（`trellis-implement`） |
| `in_progress` | 实施完成，未运行 `trellis-check` | 阶段 2.2（`trellis-check`） |
| `in_progress` | 检查通过 | 阶段 3.3（更新规范）→ 3.4（提交） |
| `completed` | 任务仍在活动目录树中 | 阶段 3.5（运行 `/trellis:finish-work` 归档） |

添加自定义状态（如 `in-review`）时，既要在 `.trellis/workflow.md` 中添加 `[workflow-state:in-review]` 块作为逐轮状态提示，也要扩展此路由表。通常编辑 `/trellis:continue` 命令文件（`.{platform}/commands/trellis/continue.md` 或等价文件），增加决定恢复位置的一行。没有路由项，`/trellis:continue` 会落入默认分支，用户无法进入预期步骤。

## 注意事项

`.trellis/workflow.md` 是本地项目工作流，并非不可变的模板。用户可以按团队习惯调整。编辑后，平台入口文件可能仍保留旧说明，也应一并检查。
