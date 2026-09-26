# 本地工作区记忆系统

`.trellis/workspace/` 保存跨会话记忆，让 AI 与人类能够理解不同窗口、不同日期的历史工作。

## 目录结构

```text
.trellis/workspace/
├── index.md
└── <developer>/
    ├── index.md
    ├── journal-1.md
    └── journal-2.md
```

| 文件 | 用途 |
| --- | --- |
| `.trellis/.developer` | 当前开发者身份。 |
| `.trellis/workspace/index.md` | 全局工作区概览。 |
| `.trellis/workspace/<developer>/index.md` | 某位开发者的会话索引。 |
| `.trellis/workspace/<developer>/journal-N.md` | 会话日志。 |

## 开发者身份

首次使用时执行：

```bash
python3 ./.trellis/scripts/init_developer.py <name>
```

这会创建 `.trellis/.developer` 和对应工作区目录。AI 不应随意修改开发者身份；如果身份错误，先确认当前项目使用者是谁。

## 日志

`journal-N.md` 记录每次会话已完成或部分完成的工作。默认每份日志约容纳 2000 行，超过后轮换到下一份文件。

记录会话的常用命令：

```bash
python3 ./.trellis/scripts/add_session.py \
  --title "会话标题" \
  --summary "变更内容" \
  --commit "abc1234"
```

无提交的规划或审查工作，也可使用 `--no-commit` 或空提交值记录。

## 工作区记忆与任务的关系

| 系统 | 保存内容 |
| --- | --- |
| `.trellis/tasks/` | 某个任务的需求、设计、研究与状态。 |
| `.trellis/workspace/` | 跨任务、跨会话的工作记录。 |
| `.trellis/spec/` | 作为长期约定保存的工程知识。 |

仅对当前任务有用的信息，放入任务目录。

描述当前会话发生事项的信息，放入工作区日志。

以后每次编写代码都应遵循的信息，放入规范。

## 本地定制位置

| 需求 | 修改位置 |
| --- | --- |
| 修改日志最大行数 | `.trellis/config.yaml` 中的 `max_journal_lines`。 |
| 修改会话自动提交说明 | `.trellis/config.yaml` 中的 `session_commit_message`。 |
| 修改会话内容格式 | `.trellis/scripts/add_session.py`。 |
| 修改上下文中的工作区显示 | `.trellis/scripts/common/session_context.py`。 |

## AI 使用规则

AI 不应将工作区视为唯一权威源。恢复任务时先读取当前任务，再使用工作区了解背景。任务完成后，将重要过程说明记录到工作区；如产生长期规则，则更新规范。
