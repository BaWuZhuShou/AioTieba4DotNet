# worker 与代理卡片

需要同级代理独立执行并通过频道事件日志回报时使用 worker。worker 是注册到频道的子进程（claude 或 codex）；supervisor 向它转发收件箱消息，并将输出转换为频道事件。

版本核对：2026-09-26 本机 Trellis 为 0.6.17。`send` / `wait` / `interrupt` 帮助没有 `--tag`；普通提示用 `send`，立即重定向用 `interrupt`。帮助不列出实际发出的事件类型，下面涉及旧 `interrupt` 事件名的说明只作兼容背景，不作为本次已验证事实。

## 启动

```bash
trellis channel create impl-task --by dispatcher --cwd /path/to/repo
trellis channel spawn impl-task --provider codex --as codex-impl --timeout 30m

echo "按 .trellis/.../prd.md 实现表 X 的 schema" \
  | trellis channel send impl-task --as dispatcher --to codex-impl --stdin

trellis channel wait impl-task --as dispatcher --from codex-impl --kind done --timeout 30m
```

`spawn` 派生 `channel __supervisor` worker，发出 `spawned`、流式输出 `progress`，并应以 `done`、`error` 或 `killed` 结束。在收到 `send --to <worker>`（或设置 `--inbox-policy broadcastAndExplicit` 时收到广播）前，worker 保持收件箱空闲状态。

关键 `spawn` 参数：

- `--agent <name>`：加载 `.trellis/agents/<name>.md`（provider/model/as/系统提示默认值）。
- `--provider <claude|codex>`：覆盖代理卡片配置；按适配器注册表校验。
- `--as <name>`：频道内 worker 标识；默认代理名称。
- `--cwd <path>`：worker 工作目录，也是 `--file` / `--jsonl` 的路径限制根目录。
- `--model <id>`：覆盖模型。
- `--resume <id>`：恢复已有 claude 会话 / codex thread。
- `--timeout <duration>`：在 `30s` / `2m` / `1h` 后自动终止。
- `--warn-before <duration>`：提前多久发出 supervisor_warning（默认 `5m`；`0ms` 禁用）。
- `--file <path>`（可重复，支持 glob）：向系统提示注入文件内容。
- `--jsonl <path>`（可重复）：Trellis jsonl 清单（每行 `{file, reason}`）。
- `--by <agent>`：`spawned` 事件作者，默认 `$TRELLIS_CHANNEL_AS` 或 `main`。
- `--inbox-policy <explicitOnly|broadcastAndExplicit>`：默认 `explicitOnly`。
- `--idle-timeout <duration>`：OOM 防护的空闲 TTL（默认 `5m`；`0` 禁用）。
- `--max-live-workers <n>`：启动时的存活 worker 预算（默认 `6`；`0` 禁用）。

成功事件 `spawned` 记录 `pid`、`provider`、`agent`、注入的 `files` 及解析后的 `manifests`，方便后续观察者审计上下文。

## 代理卡片

`--agent <name>` 解析到 `.trellis/agents/<name>.md`。卡片名称必须匹配 `[A-Za-z0-9._-]+`。默认 Trellis 安装提供两张卡片：

- `.trellis/agents/check.md`：代码质量评审者。
- `.trellis/agents/implement.md`：负责实施的编码 worker。

```yaml
---
name: check
description: 代码质量检查专家。
provider: claude
---
```

frontmatter 字段提供 `spawn` 默认值（provider、model、`as`）；Markdown 正文成为 worker 的系统提示角色。卡片**不会**自动附加任务文件；每次 spawn 必须显式注入上下文，见下文。

启动具名代理前，始终先检查项目卡片：

```bash
ls .trellis/agents
sed -n '1,100p' .trellis/agents/check.md
```

## 上下文注入

两个参数将内容注入 worker 系统提示的 `# CONTEXT FILES` 区块，由 `context-loader` 组装：

- `--file <path>`：可重复，支持 glob（`*`、`**`）；读取并拼接每个匹配文件。
- `--jsonl <path>`：可重复的 Trellis 清单，每行格式为 `{"file":"<path>","reason":"<why>"}`。reason 保留为各文件内容上方的标题注释。

加载器执行的限制：

- 单文件硬上限 1 MB（超出即报错）。
- 单文件达到 200 KB 时向 stderr 警告。
- 组装后的总上下文达到 500 KB 时向 stderr 警告。
- 防路径越界：所有解析后的路径必须位于 `--cwd` 下。

针对任务目录启动检查代理的示例：

```bash
TASK=.trellis/tasks/05-13-example
trellis channel spawn cr-example --agent check --provider codex --as check-cx \
  --file "$TASK/prd.md" \
  --file "$TASK/design.md" \
  --file "$TASK/implement.md" \
  --jsonl "$TASK/check.jsonl" \
  --cwd "$PWD" --timeout 30m
```

`spawned` 事件既记录字面的 `files` 数组，也记录从 `--jsonl` 展开的 `manifests`，因此审计记录能还原 worker 实际看到的内容。

## 名称与路由

`--as` 有两种含义：

- `send` / `wait` / `interrupt`：发言者身份，即所产生事件的作者。
- `spawn`：worker 标识，其他代理通过 `--to` 寻址。

多个 worker 或 provider 参与同一频道时，使用明确名称：

```bash
trellis channel spawn cr-feature --agent check --as check-claude
trellis channel spawn cr-feature --agent check --provider codex --as check-cx

trellis channel wait cr-feature --as main \
  --from check-claude,check-cx --kind done --all --timeout 15m
```

`--all` 要求提供 `--from`，并阻塞到列出的每个 worker 都产生匹配事件；超时以 **124** 退出，并向 stderr 打印 `timeout: still waiting on ...`。

## 软中断：`interrupt`

`channel interrupt` 进行协作式重定向，并在适配器支持时，发出 provider 级轮次中断及替代指令。旧说明将追加事件记为 `interrupt`（reason 为 `"user"`）；命令参考使用 `interrupt_requested` / `interrupted`，实际事件应按安装版本的原始输出核对。worker 应放弃当前轮次、立即响应新输入，同时保留会话时使用。

```bash
echo "停止重构解析器，改为修复 src/foo.ts 中失败的测试" \
  | trellis channel interrupt impl-task --as dispatcher --to codex-impl --stdin
```

参数：

- `--as <agent>` **（必填）**：调用者身份。
- `--to <agent>` **（必填）**：目标 worker。
- `--scope <project|global>`：频道范围。
- `--stdin` / `--text-file <path>` / `[text]`：替代指令正文。

下游 `wait` / `messages` 可按实际事件类型响应重定向，例如记录路由变化，或让其他 worker 等待协调者的修正。旧资料的 `kind: "interrupt"` 与 `--kind interrupt` 写法存在版本差异；不要未经核对直接套用，参见 `command-reference.md` 的事件模型。

可以等到 worker 下一轮再处理的低优先级提示，发送普通消息即可。旧示例的 `--tag question` 已按本机帮助移除：

```bash
echo "到下一轮时检查这个。" \
  | trellis channel send impl-task --as dispatcher --to codex-impl \
      --stdin
```

## 硬中断：`kill` + `--resume`

worker 必须**立即**停止时使用 `kill`，例如失控循环、错误指令已开始执行，或适配器未响应 `interrupt`。supervisor 按 SIGTERM → 宽限 8 秒 → SIGKILL 逐步升级；需要 SIGKILL 时，CLI 写入 `killed` 事件，保证日志如实记录。

```bash
trellis channel kill impl-task --as codex-impl
trellis channel spawn impl-task --as codex-impl --provider codex \
  --resume "$(cat ~/.trellis/channels/<bucket>/impl-task/worker.session-id)"

echo "停止；新指令：……" \
  | trellis channel send impl-task --as dispatcher --to codex-impl --stdin
```

`kill` 参数：

- `--as <agent>` **（必填）**：指定 worker（位置参数 `<name>` 是频道名）。
- `--scope <project|global>`。
- `--force`：立即 SIGKILL，同时终止内部 worker PID。

副作用：清理 `pid`、`worker-pid`、`config`、`spawnlock` 辅助文件；保留 `log`、`session-id`、`thread-id`，用于取证和恢复。

`interrupt` 无法使行为收敛时，kill + `--resume` 是确保重定向的路径。

## worker OOM 防护

OOM 防护避免孤立 / 空闲 worker 累积并耗尽宿主资源。每次 `spawn` 都运行，并对每个项目存储桶执行两项策略：

- **空闲 TTL**：清理最后活动时间早于配置阈值的 worker（默认 `5m`；`0` 禁用）。
- **存活 worker 预算**：同一项目存储桶中已有超过 N 个存活 worker 时，拒绝新 spawn（默认 `6`；`0` 禁用）。

优先级从高到低：

1. `spawn` 的 CLI 参数：`--idle-timeout`、`--max-live-workers`。
2. 环境变量：`TRELLIS_CHANNEL_WORKER_IDLE_TIMEOUT`、`TRELLIS_CHANNEL_MAX_LIVE_WORKERS`。
3. `.trellis/config.yaml` 中的 `channel.worker_guard`。
4. 内置默认值（`5m`、`6`）。

spawn 时向 stderr 写入清理通知，便于操作人员看到清理了哪些空闲 worker，以及为何拒绝新 spawn。防护对临时 / `channel run` worker 没有例外；它们同样受空闲 TTL 和预算约束。

审计当前状态时，用 `channel list` 的 `WORKERS` 列查看 worker，并检查 `~/.trellis/channels/<bucket>/<channel>/` 下各频道的 `pid` / `worker-pid` 辅助文件。

## worker 收件箱 API

收件箱是唤醒 worker 的频道入口。路由由两项设置控制：

- **收件箱策略**（`spawn --inbox-policy`）：
  - `explicitOnly`（默认）：仅 `send --to <worker>` 或 `interrupt --to <worker>` 会唤醒 worker。
  - `broadcastAndExplicit`：广播（不带 `--to` 的 `send`）也会唤醒。
- **投递模式**（`send --delivery-mode`）：
  - `appendOnly`：无论 worker 状态如何，都追加事件。
  - `requireKnownWorker`：`--to` 指定的 worker 从未启动过时失败。
  - `requireRunningWorker`：指定 worker 当前不存活时失败。

调用方期待接收者正在运行时，更严格的投递模式能防止消息静默丢失。

收件箱相关子命令：

- `send <channel> [text]`：追加 `message` 事件。
  - `--as <agent>` **（必填）**：作者。
  - `--to <agents>`：CSV；单个接收者保存为字符串，多个保存为数组；省略则广播。
  - `--stdin` / `--text-file <path>` / `[text]`：正文来源。
  - `--delivery-mode <appendOnly|requireKnownWorker|requireRunningWorker>`。
- `interrupt <channel> [text]`：软中断重定向，见上文。
- `wait <channel>`：阻塞等待匹配事件。
  - `--as <agent>` **（必填）**：筛选上下文中的 `self`。
  - `--from <agents>`：作者 CSV。
  - `--kind <kind[,kind...]>`：CSV（OR 语义），支持 `done`、`progress` 等；中断事件名按安装版本核对，旧 `interrupt` 写法不应直接套用。
  - `--to <target>`：默认为自身代理（广播 + 显式发给自身）。
  - `--include-progress`：progress 事件也可唤醒。
  - `--all`：要求每个 `--from` 代理都匹配（超时 → 退出 **124**）。
  - `--timeout <duration>`：`30s` / `2m` / `1h` / `1000ms`。
- `messages <channel>`：查看 / 筛选 / 持续跟踪事件流。
  - `--follow` 持续跟踪；`--kind` / `--from` / `--to` 筛选；`--raw` 每行输出 JSON；`--no-progress` 隐藏进度噪声。

典型派发循环：

```bash
# 1. 唤醒 worker。
echo "运行失败的测试并回报。" \
  | trellis channel send impl-task --as dispatcher --to codex-impl --stdin \
      --delivery-mode requireRunningWorker

# 2. 阻塞直到完成。
trellis channel wait impl-task --as dispatcher \
  --from codex-impl --kind done,error --timeout 30m

# 3. 读取最终回答。
trellis channel messages impl-task --from codex-impl --kind message --last 1 --raw
```

所有发出事件的子命令（`send`、`interrupt`、`post`、`context add` / `delete`、`title set` / `clear`、`thread rename`）都会在 stdout 将追加的事件打印为单行 JSON，方便对收件箱层编写脚本。
