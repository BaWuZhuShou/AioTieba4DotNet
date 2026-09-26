---
name: trellis-channel
description: 使用 Trellis channel 进行实时多代理协作、启动 worker、交叉评审、进度检查、论坛频道操作和频道日志排查。
---

# trellis-channel

`trellis channel` 是本地多代理协作运行时。代理需要通过持久事件日志交流、将 worker 作为同级进程启动、对运行中的 worker 中断或排查，或在持久的 `--type forum` 频道记录反馈时使用。

典型用户表达：“和 codex/claude 讨论”“和另一个代理一起梳理需求”“启动 implement/check worker”“让代理评审”“开一个问题板 / 变更日志论坛”“看看这个主题”“频道卡住了 / 没输出”“进度被截断”“这个频道命令怎么写”。

本技能是索引。只加载当前工作需要的参考文件，不要预先加载全部文件。

## 首批命令

```bash
trellis --version
trellis channel --help
trellis channel list --all
trellis channel list --scope global --all
```

用户指定频道或主题时，先查看它，再询问背景：

```bash
trellis channel forum <board> --scope global
trellis channel thread <board> <thread> --scope global
trellis channel context list <board> --scope global --thread <thread>
```

## 按用户意图选择参考

| 用户意图 | 阅读 |
| --- | --- |
| “和 codex/claude 讨论一下”“和另一个代理一起梳理需求” | `references/workflows.md` |
| “派一个 implement/check agent”“让代理评审”“启动 worker” | 先读 `references/workflows.md`，再读 `references/workers.md` |
| “开问题区 / 主题群 / 变更日志 / 看板”“建论坛” | `references/forum.md` |
| “看看这个主题 / 关联上下文”“检查主题” | `references/forum.md` |
| “频道卡住了 / 没输出 / 进度被截断”“worker 停滞” | `references/progress-debugging.md` |
| “具体命令怎么写”“X 接受哪些参数” | `references/command-reference.md` |

## 核心规则

- 新论坛频道使用 `--type forum`。`thread` 是论坛频道内的一个主题。
- 使用 `--context-file` / `--context-raw` 和 `trellis channel context add/delete/list`。`--linked-context-*` 是已弃用术语。
- 长消息使用 `--stdin` 或 `--text-file`。不要把很长的中英混合文本放进 shell 位置参数。
- 格式化的 `messages` 输出是操作面板，可能截断进度。审计使用 `--raw`。
- `--as` 根据命令表示发言者或 worker 标识。涉及多个代理或会话时，使用明确且稳定的名称。
- `--scope project`（默认）操作当前 cwd 所属的项目存储桶；`--scope global` 操作共享的 `_global` 桶。应明确选择范围；不传 `--scope global` 时，项目列表看不到全局看板。
- 需求讨论应进行多轮质询。一轮回答加一次确认只能算评审，不能算充分讨论。
- **派发者等待模式**：以 `--kind done` / `--kind turn_finished`（由 Trellis 发出的系统事件）作为完成信号。本机 0.6.17 的帮助确认 `send` / `wait` / `messages` 不支持 `--tag`；旧资料中的 `phase_done`、`question`、`interrupt` 标签示例不能直接运行。中断使用专用 `channel interrupt` 命令。不要要求 worker 用 `send --tag <my_signal>` 发出完成信号；此外，LLM worker 也常只把信号写进正文而不执行命令。参见 `references/command-reference.md` 中的“tag 与 kind”。
- 论坛频道采用事件溯源。不要先解析 `events.jsonl`；使用 `forum`、`thread`、`messages --thread` 和 `context list`。
- `@mindfoldhq/trellis-core` 负责可复用的频道/主题状态、事件追加、seq 分配、上下文/标题投影、reducer 和任务辅助函数。CLI 负责参数、终端渲染、提示、worker 生命周期及进程退出。

## 参考文件

- `references/workflows.md` — 标准协作模式 A–F（同级代理需求讨论、启动评审、派发并等待、论坛问题记录、中断与重定向、单次运行）。
- `references/forum.md` — 论坛频道、上下文、标题、重命名、变更日志论坛、主题筛选。
- `references/workers.md` — spawn、代理卡片、上下文注入（`--file` / `--jsonl`）、中断和终止语义。
- `references/progress-debugging.md` — 进度/原始输出检查、停滞 worker 诊断、OOM 防护、退出码。
- `references/command-reference.md` — 当前 CLI 命令参考（全部子命令、参数、输出约定、范围/类型模型）。

## 不适用的情况

- 一份 Markdown 文件和提示就足够的单次静态评审。
- 用自我记录替代正常工具调用。
- 长期记忆检索。可执行的问题使用持久论坛频道；会话/历史搜索使用 `trellis mem`（`trellis-session-insight` 技能）。
