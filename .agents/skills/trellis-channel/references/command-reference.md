# 命令参考

`trellis channel` 子命令的当前权威参考，依据 Trellis 上游 `packages/cli/src/commands/channel/` 源码（`index.ts` 的 Commander 注册和各子命令处理器）校验。

版本核对：2026-09-26 本机 `trellis --version` 为 `0.6.17`，已只读检查 `send`、`wait`、`interrupt`、`messages`、`forum`、`thread`、`context` 的帮助。确认这些入口没有 `--tag`，且论坛 / 主题 / 上下文语法如下。读取输出的筛选和格式还对照了本机 `dist/commands/channel/messages.js` 与 `wait.js`；其余行为描述来自捆绑参考。本次未运行频道或 worker，不能将帮助或源码检查当作事件与生命周期的实测。

除非另有说明，所有子命令均接受 `--scope <project|global>`；默认 `project`，解析到当前 cwd 所属的项目存储桶。

## 顶层

```
trellis channel <subcommand>
```

> 多代理协作运行时：通过共享事件日志启动、协调和中断 worker 代理。

---

## 创建 / 列表

### `create <name>`

```bash
trellis channel create <name>
  [--scope project|global]                # 默认 project
  [--type chat|forum]                     # 默认 chat
  [--task <path>]                         # 关联的 Trellis 任务目录
  [--project <slug>]
  [--labels a,b,c]
  [--description <text>]                  # 稳定的频道说明
  [--context-file <abs-path>] ...         # 可重复
  [--context-raw  <text>]      ...        # 可重复
  [--linked-context-file <abs-path>]      # 已弃用别名
  [--linked-context-raw  <text>]          # 已弃用别名
  [--cwd <path>]                          # 记录到 create 事件
  [--by <agent>]                          # 默认 main
  [--force]                               # 覆盖已有频道
  [--ephemeral]                           # 默认列表隐藏，可清理
```

行为：

- 追加 `create` 事件；`type` 不可变，之后不能在 forum 与 chat 之间切换。
- `--ephemeral` 频道默认在 `channel list` 中隐藏，并由 `channel prune --ephemeral` 清理。
- `--linked-context-*` 归并到 `--context-*`；使用时发出弃用提示。

### `list`

```bash
trellis channel list
  [--scope project|global]
  [--json]
  [--project <slug>]                      # 对 task 字段进行子串匹配
  [--all]                                 # 包含临时频道（后缀 '*'）
  [--all-projects]                        # 扫描所有项目存储桶
```

行为：

- 默认范围是当前 cwd 所属项目。`--all-projects` 扫描全部存储桶。
- 格式化模式按最近活动排序，打印 `NAME WORKERS EVENTS LAST KIND TYPE TASK`，底部说明隐藏的临时频道数量。
- `--json` 切换为 JSON 数组。

---

## 聊天消息

### `send <name> [text]`

```bash
trellis channel send <name> [text]
  --as <agent>                            # 必填：作者
  [--scope project|global]
  [--to <agents,csv>]                     # 默认广播
  [--stdin | --text-file <path>]          # 从 stdin 或文件读取正文
  [--delivery-mode appendOnly|requireKnownWorker|requireRunningWorker]
```

行为：

- 正文优先级：位置参数 `[text]` → `--stdin` → `--text-file`。
- `--to` 只有一个条目时保存为字符串，多个时保存为数组，省略则广播。
- `--delivery-mode` 选择定向投递校验方式：
  - `appendOnly`：相当于默认行为，只记录。
  - `requireKnownWorker`：目标必须有 `spawned` 事件。
  - `requireRunningWorker`：worker 当前必须存活。
- 将追加的事件作为单行 JSON 打印到 stdout。

> **注意：** `send` **没有** `--tag` 或 `--kind` 参数。参见下文[“tag 与 kind”](#tag-与-kind事件结构如何控制)。

### `messages <name>`

```bash
trellis channel messages <name>
  [--scope project|global]
  [--raw]                                 # 每行一个 JSON 事件
  [--follow]                              # 流式输出新事件
  [--last <N>]                            # 最后 N 个匹配事件
  [--since <seq>]                         # seq > N
  [--kind <kind>]                         # CHANNEL_EVENT_KINDS 中的一个值
  [--from <csv>]                          # 作者筛选
  [--to <target>]                         # 路由目标筛选
  [--thread <key>]                        # 仅论坛可用
  [--action <thread-action>]              # 仅论坛可用
  [--no-progress]                         # 隐藏 progress 事件
```

行为：

- 自动识别论坛频道：没有筛选条件时渲染主题看板，而非事件流。`--thread` / `--action` 仅适用于论坛，对聊天频道使用会报错。
- `--kind` 按 `CHANNEL_EVENT_KINDS` 校验，只接受单值，不接受 CSV；CSV 是 `wait` 的能力。

### `wait <name>`

```bash
trellis channel wait <name>
  --as <agent>                            # 必填：筛选上下文中的 self
  [--scope project|global]
  [--timeout <Ns|Nm|Nh|Nms>]              # 由 parseDuration 解析
  [--from <a,b>]                          # 作者 CSV
  [--kind <k1,k2>]                        # CSV，OR 语义
  [--thread <key>]                        # 论坛筛选
  [--action <thread-action>]              # 论坛筛选
  [--to <target>]                         # 默认自身代理（广播 + 自身）
  [--include-progress]                    # progress 事件也唤醒
  [--all]                                 # 要求每个 --from 都匹配
```

行为：

- 将匹配事件以 JSON 流式输出，每行一条。
- 默认 `--to` 筛选调用者自身代理；广播仍会匹配，即广播 + 显式发给自身。
- `--all` 要求提供 `--from`，并阻塞直到列出的每个代理都产生匹配事件。
- **超时以 124 退出**；使用 `--all` 时，向 stderr 打印 `timeout: still waiting on ...`。

---

## tag 与 kind：事件结构如何控制

原参考描述 v0.6.0 的 channel CLI **没有 `--tag` 参数**；本次 0.6.17 的相关帮助同样未提供它。`--kind` 也不是某个旧 `--tag` 参数的别名。

当前源码的具体模型：

- `--kind` 是唯一的事件类型筛选条件，限定为 Trellis 发出的白名单值（Trellis 上游 `packages/core/src/channel/internal/store/events.ts` 中的 `CHANNEL_EVENT_KINDS`）：
  - `create`、`join`、`leave`、`message`、`thread`、`context`、`channel`、`spawned`、`killed`、`respawned`、`progress`、`done`、`error`、`waiting`、`awake`、`undeliverable`、`interrupt_requested`、`turn_started`、`turn_finished`、`interrupted`、`supervisor_warning`。
  - 传入其他值会抛出 `Invalid --kind '<x>'. Must be one of: …`。
- `--kind` 用于 `wait`（CSV，OR 语义）和 `messages`（单值）。`send`、`run` 不能发出自定义 kind；每次 `send` 都写入 `message` 事件。
- 中止 worker 当前轮次**不是**标签，而是专用 `channel interrupt` 命令：追加 `interrupt_requested` / `interrupted` 事件对，并在 provider 层中断 worker。

派发者等待 worker 的实用规则：

- 用 `--kind done,turn_finished` 表示“worker 完成一轮”；它们是 supervisor 自动发出的系统事件，不依赖 worker LLM 记住并发出自定义信号。
- 只有确实需要中止当前轮次时，才使用 `trellis channel interrupt` 命令。
- **不要**自行创造用户侧标签作为完成信号。不存在 `--tag` 筛选；worker 在最终消息里写的自定义字符串，只是 `message` 事件中的文本，不能被 `wait` 按标签匹配。

长正文始终通过 stdin 或文件传入：

```bash
trellis channel send T --as A --stdin < /tmp/message.md
trellis channel send T --as A --text-file /tmp/message.md
```

---

## 中断

### `interrupt <name> [text]`

```bash
trellis channel interrupt <name> [text]
  --as <agent>                            # 必填：调用者
  --to <agent>                            # 必填：目标 worker
  [--scope project|global]
  [--stdin | --text-file <path>]
```

行为：

- 发送替代指令，支持时由 supervisor 执行 provider 级中断（Claude `/interrupt`、Codex 取消轮次）。旧段落将事件记为带 `reason: "user"` 的 `interrupt`，与本参考的 `interrupt_requested` / `interrupted` 模型存在版本差异；帮助不展示实际事件，具体以安装版本的原始事件为准。
- 将追加的事件 JSON 打印到 stdout。

---

## worker

### `spawn <name>`

```bash
trellis channel spawn <name>
  [--scope project|global]
  [--agent <agent-name>]                  # 加载 .trellis/agents/<name>.md
  [--provider claude|codex]               # 覆盖代理文件配置
  [--as <worker-name>]                    # 默认代理名称
  [--cwd <path>]
  [--model <id>]
  [--resume <id>]                         # 恢复 session/thread ID
  [--timeout <Ns|Nm|Nh>]                  # 指定时长后自动终止
  [--warn-before <Ns|Nm|Nh>]              # supervisor_warning 提前量
                                          # 默认 5m，0ms 禁用
  [--file <path>] ...                     # 支持 glob，可重复；注入内容
  [--jsonl <path>] ...                    # Trellis 清单，可重复
  [--by <agent>]                          # spawn 事件作者
                                          # 默认 TRELLIS_CHANNEL_AS 环境变量或 'main'
  [--inbox-policy explicitOnly|broadcastAndExplicit]
                                          # 默认 explicitOnly
  [--idle-timeout <Ns|Nm|Nh>]             # OOM 防护空闲 TTL
                                          # 默认 5m，0 禁用
  [--max-live-workers <n>]                # 启动时存活 worker 预算
                                          # 默认 6，0 禁用
```

行为：

- 按适配器注册表校验 provider（Trellis 上游 `packages/cli/src/commands/channel/adapters/`）；当前支持 `claude`、`codex`。
- worker 在首次收到 `send --to <worker>` 前保持收件箱空闲状态。
- 记录带 `pid`、`provider`、`agent`、`files`、`manifests` 的 `spawned` 事件。
- OOM 防护优先级：CLI 参数 → 环境变量（`TRELLIS_CHANNEL_WORKER_IDLE_TIMEOUT`、`TRELLIS_CHANNEL_MAX_LIVE_WORKERS`）→ `.trellis/config.yaml#channel.worker_guard` → 内置默认值。

### `run [name]`

```bash
trellis channel run [name?]
  [--agent <name>]
  [--provider claude|codex]
  [--as <worker-name>]
  [--cwd <path>]
  [--model <id>]
  [--file <path>] ...                     # 可重复，支持 glob
  [--jsonl <path>] ...                    # 可重复
  [--message <text> | --message-file <path> | --stdin]
  [--timeout <Ns|Nm|Nh>]                  # 默认 5m
```

行为：

- 单次运行。省略 `name` 时自动生成 `run-<hex>`。
- 创建临时频道（`createMode=run`），启动一个 worker，发送提示，等待 `done`，将助手最终文本打印到 stdout，成功后删除频道。失败时保留频道供检查，退出码为 1。

> `run` **没有** `--tag` 参数。通过 supervisor 发出的 `done` 事件检测完成。

### `kill <name>`

```bash
trellis channel kill <name>
  --as <agent>                            # 必填：worker 代理名
  [--scope project|global]
  [--force]                               # 立即 SIGKILL
```

行为：

- 默认流程：SIGTERM → 宽限 8 秒 → 升级到 SIGKILL；需要 SIGKILL 时 CLI 写入 `killed` 事件，确保日志如实记录。
- 清理 `pid`、`worker-pid`、`config`、`spawnlock` 辅助文件；保留 `log`、`session-id`、`thread-id` 供取证 / 恢复。

### `rm <name>`

```bash
trellis channel rm <name>
  [--scope project|global]
```

行为：

- 终止所有存活 worker，然后删除整个频道目录。
- 打印 `Removed channel '<name>'`。

### `prune`

```bash
trellis channel prune
  [--scope project|global]                # 省略则扫描全部项目
  [--all | --empty | --idle <Ns|Nm|Nh|Nd> | --ephemeral]   # 互斥
  [--yes]                                 # 实际删除（默认预演）
  [--dry-run]                             # 默认 true；与默认行为相同
  [--keep <names,csv>]                    # 排除列表
```

行为：

- 筛选参数互斥，否则报错。
- 默认预演；`--yes` 切换为实际删除。
- 不带 `--scope` 时扫描**全部**项目存储桶（有意用于跨仓库清理）；带 `--scope project|global` 时仅限对应桶。
- 无论使用何种筛选，始终跳过有存活 worker 的频道。
- 输出：每个候选一行 `name  last-ts  (reason)`，最后打印摘要。

---

## 论坛频道

### `post <name> <action>`

```bash
trellis channel post <name> <action>
  --as <agent>                            # 必填
  [--scope project|global]
  [--thread <key>]                        # action=opened 之外均必填
  [--title <text>]
  [--text <text> | --stdin | --text-file <path>]
  [--description <text>]                  # 稳定的主题说明
  [--status <status>]
  [--labels a,b]                          # 替换主题标签
  [--assignees a,b]                       # 替换负责人
  [--summary <text>]
  [--context-file <abs-path>] ...
  [--context-raw  <text>]      ...
  [--linked-context-file <abs-path>]      # 已弃用别名
  [--linked-context-raw  <text>]          # 已弃用别名
```

行为：

- CLI 入口允许自由填写 `<action>`；惯用值包括 `opened`、`comment`、`status`、`labels`、`assignees`、`summary`、`processed`。
- 拒绝 `action=rename`，应使用 `thread rename`。
- `--labels` / `--assignees` 采用替换语义，不是追加。
- 输出：向 stdout 打印追加的事件 JSON。

### `forum <name>`

```bash
trellis channel forum <name>
  [--scope project|global]
  [--status <status>]
  [--raw]
```

行为：

- 列出归并后的主题状态。`--status` 按主题当前状态筛选；`--raw` 为每个主题打印一条 JSON。

### `thread <name> <thread>` / `thread rename`

```bash
trellis channel thread <name> <thread-key>
  [--scope project|global]
  [--raw]

trellis channel thread rename <name> <old-thread> <new-thread>
  --as <agent>                            # 必填
  [--scope project|global]
```

行为：

- `thread <name> <key>` 展示单个主题时间线：先打印标题行 `<thread> [<status>] <title>`，再打印 description / labels / assignees / summary / timeline 各行。`--raw` 切换到原始事件。
- `thread rename` 是唯一的修改操作；`post --action rename` 会被拒绝。

---

## 上下文 / 标题

### `context add` / `context delete` / `context list`

```bash
trellis channel context add <name>
  [--as <agent>]                          # 默认 main
  [--scope project|global]
  [--thread <key>]                        # 主题级而非频道级
  [--file <abs-path>] ...                 # 可重复
  [--raw <text>]      ...                 # 可重复
                                          # --file 或 --raw 至少提供一个

trellis channel context delete <name>
  [--as <agent>]                          # 默认 main
  [--scope project|global]
  [--thread <key>]
  [--file <abs-path>] ...
  [--raw <text>]      ...

trellis channel context list <name>
  [--scope project|global]
  [--thread <key>]
  [--raw]                                 # 每行一个 JSON 条目
```

行为：

- `add` / `delete` 追加 `context` 事件并打印事件 JSON。
- `list` 投影当前上下文条目；格式化输出为 `file <path>` / `raw <truncated text>` 各行，空时显示 `(no context)`。

### `title set <name>` / `title clear <name>`

```bash
trellis channel title set <name>
  --title <text>                          # 必填
  [--as <agent>]                          # 默认 main
  [--scope project|global]

trellis channel title clear <name>
  [--as <agent>]                          # 默认 main
  [--scope project|global]
```

行为：

- 追加 `title` 事件，为频道投影稳定显示标题。输出事件 JSON。

---

## 隐藏 / 内部命令

| 命令 | 用途 |
| --- | --- |
| `channel __supervisor <channel> <worker> <config>` | `spawn` 派生进程时调用的入口，不要直接调用。 |
| `channel __parse-trace <adapter> <file>` | 开发辅助工具：通过对应适配器重放已记录的 stream-json / 线协议跟踪，并打印产生的频道事件。适配器按 provider 注册表校验。 |

---

## 事件模型

`CHANNEL_EVENT_KINDS`（由 `parseChannelKind` 强制执行的白名单）：

`create`、`join`、`leave`、`message`、`thread`、`context`、`channel`、`spawned`、`killed`、`respawned`、`progress`、`done`、`error`、`waiting`、`awake`、`undeliverable`、`interrupt_requested`、`turn_started`、`turn_finished`、`interrupted`、`supervisor_warning`。

`MEANINGFUL_EVENT_KINDS`（未显式指定 `--kind` 时，`wait` 默认匹配的子集）：

`create`、`join`、`leave`、`message`、`thread`、`context`、`channel`、`spawned`、`killed`、`respawned`、`done`、`error`。

其他类型仍会进入存储；`wait` 可用 `--kind` 显式选择类型，或用 `--include-progress` 纳入进度。本机 0.6.17 的 `messages` 设置 `includeNonMeaningful: true`，默认读取范围不受上述子集限制；按需使用 `--kind` 或 `--no-progress` 收窄。

论坛频道采用事件溯源；使用 CLI reducer（`forum`、`thread`、`context list`）投影状态。

---

## 输出约定

- **修改命令**（`send`、`interrupt`、`post`、`context add/delete`、`title set/clear`、`thread rename`）：将追加事件以单行 JSON 打印到 **stdout**。
- **流式读取**：`wait` 向 stdout 逐行打印 JSON 事件；`messages --follow` 沿用展示格式，需同时传 `--raw` 才逐行打印 JSON。
- **格式化读取**（`list`、`messages`、`forum`、`thread`、`context list`）：打印带颜色、对齐的表格 / 时间线。
- **`run`**：stdout 只打印助手最终文本，方便调用者管道处理；诊断说明进入 stderr。
- **错误**：通过 `chalk.red("Error:")` 写入 stderr，并 `exit 1`。
- **`wait` 超时**：专门以 **124** 退出。
