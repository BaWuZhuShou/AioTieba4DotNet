# 论坛频道

论坛频道是持久、按主题组织的频道，在创建时用 `--type forum` 指定，创建后类型不可更改。它不是普通聊天流；默认读取顺序是：**论坛概览 → 单个主题时间线 → 当前上下文**。

## 论坛与普通频道

频道类型通过 `channel create` 的 `--type` 设置，之后不会改变：

- `chat`（默认）：平铺消息时间线。`channel messages` 始终渲染事件流。`--thread`、`--action` 等论坛专用参数会被拒绝。
- `forum`：按主题组织。无筛选条件的 `channel messages` 展示主题看板概览，而非原始事件。`post`、`forum`、`thread` 和 `thread rename` 子命令仅适用于论坛频道。

两种类型共享相同范围模型（默认 `--scope project`；`--scope global` 将频道放入跨项目存储桶）。

## 创建论坛频道

```bash
trellis channel create design-feedback \
  --type forum \
  --scope global \
  --description "跨项目设计反馈看板。" \
  --context-raw "每个设计话题建立一个主题；解决后关闭。" \
  --by main
```

单仓库看板使用 `--scope project`，跨项目看板使用 `--scope global`。

## 主题：打开、评论、状态、摘要

主题存在于论坛频道内，以稳定的 `--thread <key>` 标识（通常采用小写 kebab-case）。主题的第一个动作是 `opened`；后续都使用相同的 `--thread` 键。

```bash
trellis channel post design-feedback opened \
  --scope global \
  --as main \
  --thread login-empty-state \
  --title "登录页面的空状态" \
  --description "跟踪新登录空状态的设计反馈。" \
  --labels design,login \
  --context-raw "在 0.4 发布评审中发现。" \
  --text-file /tmp/thread-open.md

trellis channel post design-feedback comment \
  --scope global \
  --as reviewer \
  --thread login-empty-state \
  --text-file /tmp/review.md

trellis channel post design-feedback status \
  --scope global \
  --as main \
  --thread login-empty-state \
  --status closed

trellis channel post design-feedback summary \
  --scope global \
  --as main \
  --thread login-empty-state \
  --summary "采用方案 B 布局；由工单 TRELLIS-123 负责修复。"
```

关键区别：

- `--description` 是**持久**主题说明，回答“这个主题讨论什么”。在 `opened` 时设置，后续通过再次运行带 `--description` 的 `post` 修改。
- `--text` / `--stdin` / `--text-file` 是**事件正文**，即附在本条时间线记录上的评论或载荷。
- `--labels` 和 `--assignees` 接收 CSV，并**替换**当前值，不是追加。
- `--summary` 是持续更新的主题摘要。在 `status closed` 时设置它，是带上下文标记主题已解决的标准方式。

除 `opened` 外，每种动作都要求 `--thread`（实际使用中 `opened` 同样需要，因为不存在匿名主题）。

## 阅读论坛

```bash
trellis channel messages design-feedback --scope global
trellis channel forum design-feedback --scope global --status open
trellis channel thread design-feedback login-empty-state --scope global
trellis channel messages design-feedback --scope global --raw --thread login-empty-state
```

同级代理说“我在论坛评论了”时，先运行 `channel forum` 看哪个主题有变化，再用 `channel thread <name> <thread>` 深入。不要直接临时解析 `events.jsonl`。

## 上下文

上下文条目是读取频道或主题时应始终考虑的持久背景。它们**不是**时间线事件，而是单独投影，并为每位读取者重放。

使用 `context` 子命令。`create` 和 `post` 的旧参数 `--linked-context-file` / `--linked-context-raw` 是已弃用别名，会归并到标准参数 `--context-file` / `--context-raw`。

### 添加上下文

```bash
# 频道级上下文（整个论坛）
trellis channel context add design-feedback \
  --scope global \
  --raw "上游反馈看板；打开主题前请关联任务。"

# 主题级上下文（单个主题）
trellis channel context add design-feedback \
  --scope global \
  --thread login-empty-state \
  --file "$PWD/.trellis/tasks/05-13-login-redesign/design.md"
```

- `--thread <key>` 切换频道级与主题级上下文。
- `--file` 路径**必须为绝对路径**；相对路径会被拒绝。
- `--raw` 是内联纯文本内容。
- 两个参数均可重复；`add` / `delete` 至少需要其中一个。
- `--as <agent>` 记录作者，默认 `main`。

### 列出上下文

```bash
trellis channel context list design-feedback --scope global
trellis channel context list design-feedback --scope global --thread login-empty-state --raw
```

`list` 的 `--raw` 每行输出一条 JSON（方便管道处理）；不带它则输出人类可读的 `file <path>` / `raw <truncated text>` 列表。存储为空时打印 `(no context)`。

### 删除上下文

```bash
trellis channel context delete design-feedback \
  --scope global \
  --thread login-empty-state \
  --raw "过期笔记"
```

按**值**删除，而非按 ID：传入添加时相同的 `--file` 或 `--raw` 值。重复参数可在一次调用中删除多个条目。

### 阅读顺序

读取主题时，自上而下：

1. 主题 `description`（持久的“这个主题讨论什么”）。
2. 上下文条目（频道级 + 主题级）。
3. 时间线（`opened`、`comment`、`status`、`summary`）。

上下文文件缺失或不可读时，明确说明，并继续使用剩余数据；不要编造内容。

## 标题投影

`title` 为频道设置稳定的显示标题，不更改存储地址。所有命令使用的频道 `name` 保持不变。

```bash
trellis channel title set design-feedback \
  --scope global \
  --title "设计反馈看板"

trellis channel title clear design-feedback --scope global
```

- `title set` 必须提供 `--title`。
- `--as <agent>` 记录作者，默认 `main`。
- 这只是显示层改动。工具和脚本继续使用原频道名。

## 主题重命名

创建主题时键有误（拼写错误、不符 slug 约定等），通过 `thread rename` 修正。主题不支持硬删除；受支持的修正方式是重命名。

```bash
trellis channel thread rename design-feedback old-key new-key \
  --scope global \
  --as main
```

- `--as <agent>` **必填**。
- `post <name> rename` 会被拒绝；必须使用 `thread rename`。

## 删除约束

不要把删除单条评论或硬删除主题视为常规流程。论坛主题是只能追加的协作历史。修正状态时使用：

- `post ... status` 将主题标记为 closed / blocked 等状态。
- `post ... summary` 记录解决结果。
- `post ... --labels` 重新设置标签（替换整个集合）。
- `thread rename` 修正错误的主题键。

## 内部变更日志模式

全局论坛频道常用于内部发布 / 运行时变更日志。每项重要变更建立一个主题，便于检索历史：

```bash
trellis channel create release-notes \
  --type forum \
  --scope global \
  --description "内部发布与运行时变更日志。" \
  --context-raw "每项重要变更建立一个主题；发布后关闭。" \
  --by main

trellis channel post release-notes opened \
  --scope global \
  --as main \
  --thread release-2026-q1 \
  --title "0.6 中的频道主题与论坛体验" \
  --description "论坛频道体验已在 0.6 版本线发布。" \
  --labels channel,release \
  --text-file /tmp/release-notes.md
```

使用稳定且能说明内容的主题键（如 `release-2026-q1`、`runtime-event-schema-change`），方便后续按名称查找。
