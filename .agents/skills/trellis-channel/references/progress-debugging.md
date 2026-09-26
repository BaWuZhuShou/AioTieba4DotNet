# 进度与排查

格式化输出面向操作人员，原始输出用于审计。子命令（`forum`、`thread`、`messages`、`context`）是审计*接口*；手动搜索 `events.jsonl` 前，先用这些接口。

版本核对：2026-09-26 本机 Trellis 为 0.6.17。`wait` / `messages` 没有 `--tag`；`forum <name>` 和 `thread <name> <thread>` 无需旧示例的 `list` / `show`。`context` 只管理持久背景，没有 `--as` 收件箱查询接口。本文已按只读帮助修正这些入口；未运行 worker 或验证运行时事件。

## 格式化输出与 `--raw`

`trellis channel messages <channel>` 展示紧凑的人类可读视图：时间戳、身份、kind 和简短正文。它便于操作人员浏览频道，不适合直接用于诊断。

格式化输出可能且确实会截断：

- 很长的进度增量（`text_delta`、部分工具参数）。
- 工具名和命令行。
- 多行状态字段与结构化 `detail` 数据块。
- 超过列宽预算的论坛主题标题。

发现异常现象时，例如 worker 看似卡住、进度行在词语中间结束、action 字段显示 `...`，切换到 `--raw`。原始模式按 `events.jsonl` 中的实际内容逐行输出 JSON 事件，不丢弃内容。

```bash
# 格式化输出（操作视图）
trellis channel messages <channel> --kind done --last 10
trellis channel messages <channel> --kind error --last 10

# 原始输出（诊断视图）——每行一条 JSON
trellis channel messages <channel> --raw --kind progress --last 20
trellis channel messages <channel> --raw --last 50
```

经验规则：不要依据被截断的进度行诊断 worker。

### 重建流式文本

要重建模型某一轮实际流式输出的内容，按顺序拼接 progress 事件的 `detail.text_delta`：

```bash
trellis channel messages <channel> --raw --kind progress --last 80 \
  | python3 -c 'import json,sys; [print((json.loads(l).get("detail") or {}).get("text_delta",""), end="") for l in sys.stdin if l.strip()]'
```

## worker 停滞诊断

现象：`trellis channel list` 显示 worker 正在运行，但 `messages` 没有新事件，`wait` 一直超时。

排查顺序：

1. **定位频道文件。** 不确定频道在哪个存储桶时，使用 `list --all --all-projects`。

   ```bash
   trellis channel list --all --all-projects
   CHAN=~/.trellis/channels/<bucket>/<channel>
   ```

2. **确认 supervisor 和 worker 的 PID 仍存活。**

   ```bash
   cat "$CHAN/<worker>.pid"            # supervisor PID
   cat "$CHAN/<worker>.worker-pid"     # 实际 CLI 子进程 PID
   ps -p "$(cat "$CHAN/<worker>.pid")"
   ps -p "$(cat "$CHAN/<worker>.worker-pid")"
   ```

   如果 supervisor PID 已不存在，频道仍列出该 worker，就是残留条目。用 `trellis channel kill <name> --as <worker> --force` 清理。

3. **持续查看 worker 日志。** 不会进入频道的 provider / MCP / 工具启动输出，以此处为准。

   ```bash
   tail -f "$CHAN/<worker>.log"
   ```

4. **查看最后的原始事件。** 发出了 `progress` 却没有 `message` / `done` 的 worker，通常仍在流式输出或阻塞于工具调用：

   ```bash
   trellis channel messages <channel> --raw --last 50
   ```

常见“进程存活但无输出”的原因：

- provider 在首个 token 前冷启动（时间较长，但最终会继续）。
- MCP 服务启动时阻塞，可在 worker 日志中看到。
- worker 等待工具结果，但工具子进程已挂起。
- 提示过大 / 模型被限流；检查 worker 日志中的 provider 侧错误。

## 解读 progress 事件

`progress` 事件表示一段进行中的工作。结构随 `action` 字段变化，但关键字段始终位于 `detail` 下：

- `detail.text_delta`：模型增量输出；跨事件拼接可重建流式回答。
- `detail.tool_name`、`detail.tool_input`：即将执行或正在执行的工具调用。
- `detail.status`：长时间操作使用的简短状态字符串（`starting`、`running`、`flushing`、`done`）。
- `detail.action`：语义标签，例如主题心跳使用 `status`。

progress 事件按设计会产生**大量噪声**。除非传 `--include-progress`，否则 `wait` 会忽略它们。确实需要查看时，优先使用：

```bash
trellis channel messages <channel> --raw --kind progress --last 80
```

事件流持续稳定地产生 progress，却始终没有 `done` / `error` / `message` 收尾，是工具调用挂起的典型表现；检查 worker 日志中的子进程情况。

## 等待语义（速查）

`channel wait` 从 EOF 开始监听 `events.jsonl`，以下事件可唤醒它：

- `message`
- `done`
- `error`
- `killed`
- 仅在带 `--include-progress` 时监听 `progress`

常用筛选：

```bash
trellis channel wait T --as main --from check --kind done --timeout 15m
trellis channel wait T --as main --from check,check-cx --kind done --all --timeout 15m
# 等待重定向时，先按安装版本确认 --kind 的中断事件名；旧 --tag interrupt 不可用。
trellis channel wait T --as main --thread release-note --action status --timeout 10m
```

退出码：`0` 匹配成功，`124` 超时，`1` / `2` 错误。`wait --all` 超时时，stderr 列出尚未匹配的 worker。

## 审计 `events.jsonl`：使用子命令，不要先用 `grep`

每个频道都将完整历史存入 `$CHAN/events.jsonl`。排查时很容易想直接 `tail` / `grep` / `jq` 该文件。不要养成这种习惯；论坛频道尤其**不得**这样处理。

优先使用子命令的原因：

- `messages` 已支持带筛选条件重放文件（`--kind`、`--from`、`--last`、`--thread`、`--action`），并能用 `--raw` 得到精确 JSON。你想写单行脚本完成的操作，`messages` 已经具备。
- `wait` 使用 EOF 语义消费同一文件；用 `tail -f | jq` 重新实现会在高负载下丢事件，并在轮转时打乱顺序。
- 旧资料把 `context` 描述为包含游标状态的 worker 收件箱视图，但本机帮助只支持持久上下文的 `add/delete/list`。不能据此宣称已查询待处理收件箱；手写筛选同样不遵守 `<worker>.inbox-cursor`。

### 论坛频道：不要直接解析 `events.jsonl`

论坛频道将多个逻辑主题复用到同一个 `events.jsonl`。各事件携带 `thread`、`action` 等字段，论坛子命令知道如何归并；旧资料提及的 tag 不对应本机可用的筛选参数。手工解析会：

- 混合多个主题，使主题看起来前后不连贯。
- 遗漏主题生命周期事件（打开 / 状态 / 关闭），这些事件会影响后续事件的解释。
- 忽略 worker 收件箱游标，导致看到 worker 已消费的事件，却误以为它们仍待处理。

使用能识别论坛语义的视图：

```bash
# 列出论坛频道内的逻辑主题
trellis channel forum <channel>

# 完整检查一个主题
trellis channel thread <channel> <thread>

# 重放主题消息（支持 --raw、--kind、--last）
trellis channel messages <channel> --thread <thread> --raw --last 100

# 查看频道的持久背景；这不是指定 worker 的待处理收件箱。
trellis channel context list <channel>
```

仅在怀疑 CLI 本身有问题时直接读取 `events.jsonl`，例如确认事件是否真正持久化，或在排查 supervisor 时与 `<worker>.inbox-cursor` 比较。

## 常见故障

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| `trellis: command not found` | 未全局安装 CLI | `npm install -g @mindfoldhq/trellis` |
| `wait` 立即退出 | 筛选条件错误或身份冲突 | 使用不同的 `--as`，检查原始消息 |
| zsh 对消息文本报错 | shell 解释了标点 | 使用 `--stdin` 或 `--text-file` |
| 进度行被截断 | 格式化输出截断 | 使用 `messages --raw --kind progress` |
| worker 始终不发言 | provider 启动 / 提示 / MCP 延迟 | 检查 `<worker>.log`、`ps` 和原始事件 |
| 在另一个 cwd 找不到频道 | 项目存储桶不匹配 | `cd` 到项目，使用 `--scope global`，或 `list --all-projects` |
| 列表存在残留 worker | supervisor 退出但未清理 | `trellis channel kill <name> --as <worker> --force` |
| 论坛主题看起来混乱 | 直接解析了 `events.jsonl` | 使用 `forum`、`thread`、`messages --thread` |

## 存储布局

```text
~/.trellis/channels/
└── <bucket>/
    └── <channel-name>/
        ├── events.jsonl
        ├── <channel>.lock
        ├── <worker>.log
        ├── <worker>.pid
        ├── <worker>.worker-pid
        ├── <worker>.config
        ├── <worker>.session-id
        ├── <worker>.thread-id
        ├── <worker>.inbox-cursor
        └── <worker>.spawnlock
```

代理通常应使用 CLI，而非直接读文件。仅在 CLI 视图不足以排查时直接读文件；即便如此，也不要直接读取论坛频道的 `events.jsonl`。
