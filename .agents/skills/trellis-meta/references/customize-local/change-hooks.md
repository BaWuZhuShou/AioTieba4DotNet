# 修改本地钩子

钩子是连接平台与 Trellis 的自动化层。用户希望改变“何时注入上下文”“Shell 命令如何继承会话”或“代理启动前读取哪些文件”时，通常应修改钩子。

## 先读取这些文件

1. 目标平台的设置/配置，例如 `.claude/settings.json`、`.codex/hooks.json`、`.cursor/hooks.json`、`.trae/hooks.json`
2. 目标平台的钩子目录
3. `.trellis/scripts/common/active_task.py`
4. `.trellis/scripts/common/session_context.py`
5. `.trellis/workflow.md`

## 常见钩子类型

| 钩子 | 用途 |
| --- | --- |
| session-start | 会话启动、清空或压缩时注入 Trellis 概览。 |
| workflow-state | 每次用户输入时注入状态提示。 |
| sub-agent context | 代理启动前注入 PRD/规范/研究。 |
| shell session bridge | 让 Shell 中的 `task.py` 命令能看到同一会话身份。 |

## 修改步骤

1. 在设置/配置中找到钩子注册项。
2. 确认注册的脚本路径存在。
3. 阅读钩子脚本，识别输入、输出及调用的 `.trellis/scripts/` 文件。
4. 修改钩子行为。
5. 如果钩子依赖工作流内容，同步 `.trellis/workflow.md`。

## 示例：修改新会话的注入内容

先找到 session-start 钩子：

```text
.claude/settings.json
.claude/hooks/session-start.py
```

如果钩子最终调用 `.trellis/scripts/get_context.py` 或 `session_context.py`，修改本地脚本通常比在钩子中硬编码内容更稳健。

## 示例：代理未读取 JSONL

先确认：

```bash
python3 ./.trellis/scripts/task.py current --source
python3 ./.trellis/scripts/task.py validate <task>
```

如果任务和 JSONL 正确，再判断平台使用钩子推送还是代理主动读取。钩子推送模式下编辑 `inject-subagent-context`；代理主动读取模式下编辑代理文件。

## 注意事项

- 设置负责注册，钩子脚本负责行为；两者应一起检查。
- 不同平台支持不同的钩子事件。不要直接复制其他平台的设置。
- 钩子应读取项目本地 `.trellis/`，不应依赖 Trellis 上游源码路径。
- 钩子失败应输出可见错误，避免 AI 静默丢失上下文。
