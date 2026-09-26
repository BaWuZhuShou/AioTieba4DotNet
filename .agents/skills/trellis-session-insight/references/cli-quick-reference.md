# `trellis mem` CLI 参考

这里列出五个子命令的完整参数。将其作为权威参考；`trellis mem help` 在运行时打印相同内容，因此任何漂移都应视为缺陷。

## 子命令

| 命令 | 用途 |
| --- | --- |
| `list` | 列出会话；未指定子命令时默认使用它。 |
| `search <keyword>` | 查找内容匹配关键词的会话。 |
| `context <session-id>` | 深入一个会话：前 N 个命中轮次及前后上下文。配合 `--grep` 按关键词定位。 |
| `extract <session-id>` | 导出清理后的对话。组合 `--phase` / `--grep` 切片。 |
| `projects` | 列出活跃项目 `cwd` 及会话数，用于确定其他子命令应传入的 `--cwd`。 |

## 参数（在有意义的子命令上使用）

| 参数 | 子命令 | 含义 |
| --- | --- | --- |
| `--platform claude\|codex\|devin\|grok\|opencode\|pi\|zcode\|all` | 全部 | 默认 `all`。`devin` 指 Cognition Devin CLI（`sessions.db`），并非 `trellis init --devin` 对应的桌面版。 |
| `--since YYYY-MM-DD` | list / search | 日期下限，包含当天。 |
| `--until YYYY-MM-DD` | list / search | 日期上限，包含当天。 |
| `--global` | list / search | 包含本机全部项目的会话；默认当前项目 `cwd`。 |
| `--cwd <path>` | list / search | 显式指定项目 cwd，不从当前位置推断。 |
| `--limit N` | list / search | 输出行数上限；默认 `50`。 |
| `--grep KW` | extract / context | 按关键词筛选轮次；以空格分隔的多个词采用 AND 匹配。 |
| `--phase brainstorm\|implement\|all` | extract | 按 Trellis 任务边界切片。`brainstorm` = `[task.py create, task.py start)`；`implement` = 需求讨论窗口之外的轮次；默认 `all`。 |
| `--turns N` | context | 返回的命中轮次数；默认 `3`。 |
| `--around N` | context | 每个命中包含的前后轮次数；默认 `1`。 |
| `--max-chars N` | context | 总字符预算；默认 `6000`（约 1500 tokens）。 |
| `--include-children` | search / context | 将 OpenCode 子代理会话合并到父会话。 |
| `--json` | 全部 | 输出机器可解析的 JSON，而非人类可读文本。 |

## 常用单行命令

```bash
# 本机哪些历史会话讨论过“deadlock”？
trellis mem search "deadlock" --global --limit 20

# 在指定会话中找出前 5 个提及“lock contention”的轮次，
# 并包含前后各 2 轮上下文。
trellis mem context 5842592d --grep "lock contention" --turns 5 --around 2

# 恢复会话的需求讨论窗口，适合继续用户一周前开始的任务。
trellis mem extract 5842592d --phase brainstorm

# 列出本机有 Trellis 会话的全部项目及会话数量。
trellis mem projects
```

## 输出形式

- **默认人类可读输出**（无 `--json`）：按终端宽度换行，突出会话 ID，显示轮次标记。适合直接阅读，但粘贴到 Markdown 文件时较杂乱。
- **`--json`**：结构稳定，适合解析和处理。将 `mem` 输出传入后续步骤（例如汇总到经验教训章节）时，优先使用 `--json`。

## 注意事项

- **旧版本背景**：原参考将 `0.6.0-beta.*` 的 OpenCode 适配器描述为占位实现，并记录 `reader unavailable` 提示。2026-09-26 只读检查本机 0.6.17 帮助后，确认已列出 `opencode` 和 `--include-children`；帮助同时明确 OpenCode 的 `--phase` 会警告并返回全部内容。不要把旧占位说明当作当前支持结论，也不能把帮助列表当成具体历史会话已成功读取的证明。
- **本机 0.6.17 没有 `--task` 过滤。** 使用 `--cwd` 限定项目，再按任务标识 `search` 并核对对话。
- **`--platform devin` 指 Cognition Devin CLI**（`~/.local/share/devin/cli/sessions.db`），不是 `trellis init --devin`（Devin Desktop / Cascade），也不是 Factory Droid。
- **`--phase` 切片依赖会话记录的 bash 调用中出现 `task.py create` / `task.py start`。** 如果用户在另一个终端、记录的 AI 循环之外运行 `task.py`，会话就没有阶段边界；`--phase all` 是稳妥的回退选项。
- **`mem` 直接索引平台 JSONL 文件。** 用户清理 Claude / Codex / Pi 会话存储后，`mem` 无法恢复磁盘上已不存在的内容。
- **`mem` 只读。** 不远程同步，也不编辑平台 JSONL。根据 `mem` 发现写入内容时，需要另行调用你可用的编辑工具。

## 需要更多信息时

在用户的 shell 中运行 `trellis mem help`。运行时帮助是权威依据，快速迭代的 beta 版本中可能领先于本参考文档。
