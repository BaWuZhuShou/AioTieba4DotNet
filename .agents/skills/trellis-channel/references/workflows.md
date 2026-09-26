# 工作流

按意图选择以下模式。多轮工作优先使用持久频道，单次问题使用 `channel run`。

版本核对：2026-09-26 本机 Trellis 为 0.6.17；`messages --help` 没有 `--tag`。下方读取评审结果的命令已移除旧 `--tag final_answer`，按作者与 `message` 类型读取。

## 模式 A：多轮需求讨论

用户说“和 codex/claude 讨论一下”“梳理需求”或“拉一个代理进来一起看”时使用。

```bash
trellis channel create brainstorm-storage-layer --by main \
  --task .trellis/tasks/05-XX-storage-adapter

trellis channel spawn brainstorm-storage-layer \
  --agent architect --provider codex \
  --file .trellis/tasks/05-XX-storage-adapter/prd.md \
  --file .trellis/tasks/05-XX-storage-adapter/design.md \
  --as cx-arch --timeout 30m

trellis channel send brainstorm-storage-layer \
  --as main --to cx-arch --text-file /tmp/brainstorm-r1.md

trellis channel wait brainstorm-storage-layer \
  --as main --kind done --from cx-arch --timeout 10m
```

不要得到一次回答就停止。阅读回答，找出模糊之处，提出新问题，重复直到结果可执行。

至少覆盖这些轮次：

1. 方向选择：应该放入已有机制，还是新建机制？
2. MVP 边界：v1、v2 各包含什么，什么情况会迫使 v2 内容回到 v1？
3. 数据契约：事件、schema、元数据、状态真源、兼容性。
4. CLI / UX 契约：命令名、参数、错误、默认值、歧义。
5. 跨层风险与测试：共享辅助函数、漂移点、阻断发布的测试。

可选轮次：

- 运维：日志、排查、worker 停滞、终止/重启、恢复。
- 迁移/发布：破坏性变更状态、manifest、变更日志、文档站。
- 反方评审：要求同级代理反驳当前方案。

每轮问题都应要求具体文件路径、命令、schema、已排除的替代方案和发布阻断项。需要决策时，不接受含糊回避。

## 模式 B：实施 / 检查代理

用户要求派发实施或评审工作时使用。

```bash
TASK=.trellis/tasks/05-12-foo
trellis channel create cr-foo --task "$TASK" --by main

trellis channel spawn cr-foo \
  --agent check \
  --jsonl "$TASK/check.jsonl" \
  --file "$TASK/prd.md" \
  --file "$TASK/design.md" \
  --file "$TASK/implement.md" \
  --cwd "$PWD" --timeout 15m

trellis channel send cr-foo --as main --to check --text-file /tmp/cr-brief.md
trellis channel wait cr-foo --as main --kind done --from check --timeout 15m
trellis channel messages cr-foo --kind message --from check
```

实施工作使用 `--agent implement` 并发送实施说明。检查工作应提供精确差异范围、相关规范和已经运行的验证。

## 模式 C：并行评审者

使用同一个频道，并为 worker 指定不同名称。

```bash
trellis channel create cr-feature --by main --ephemeral

trellis channel spawn cr-feature --agent check \
  --jsonl "$TASK/check.jsonl" --file "$TASK/prd.md" --file "$TASK/design.md" \
  --timeout 15m

trellis channel spawn cr-feature --agent check --provider codex --as check-cx \
  --jsonl "$TASK/check.jsonl" --file "$TASK/prd.md" --file "$TASK/design.md" \
  --timeout 15m

trellis channel send cr-feature --as main --to check --text-file /tmp/cr-brief.md
trellis channel send cr-feature --as main --to check-cx --text-file /tmp/cr-brief.md
trellis channel wait cr-feature --as main --kind done --from check,check-cx --all --timeout 15m
```

`--all` 表示列出的每个 worker 都必须发出匹配事件。

## 模式 D：单次 worker

```bash
trellis channel run --provider codex --message "用三个词打招呼" --timeout 1m
trellis channel run --agent plan --message-file /tmp/plan-question.md --timeout 10m
```

成功时，`run` 删除临时频道。出错、超时或被终止时，保留频道并打印路径，供检查。

## 模式 E：论坛频道

适用于问题论坛、按主题组织的反馈、发布待办、代理发现和内部变更日志。完整模型见 `forum.md`。

## 模式 F：接手已有主题

用户提供论坛/主题名时，自行恢复上下文：

```bash
trellis channel forum <board> --scope global
trellis channel thread <board> <thread> --scope global --raw
trellis channel context list <board> --scope global --thread <thread>
trellis channel messages <board> --scope global --raw --thread <thread>
```

输出约束摘要，而非原样倾倒对话：

- 用户层面的问题。
- 影响当前仓库的上下文文件。
- 当前版本与未来版本的需求。
- 当前代码/设计是否满足要求。
- 下一步动作或待追加评论。
