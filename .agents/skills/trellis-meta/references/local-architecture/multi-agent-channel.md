# 本地多代理通道运行时

`trellis channel` 是随 Trellis CLI 提供的本地多代理协作运行时。主 AI 会话可以启动对等工作进程（Claude Code、Codex 或 `.trellis/agents/` 下的任意代理定义），通过事件日志交换持久消息，并协调审查或需求探索循环，无需手工拼接 Shell 管道。

本参考说明通道如何接入用户项目，让定制项目的 AI 知道应修改什么。运行时用法（命令、论坛/主题模式、工作进程启动参数）请参阅捆绑的 `trellis-channel` 能力技能。

## 本地系统模型

通道运行时跨越三个本地部分：

1. 用户主目录中的**存储层**：持久事件日志与工作进程状态文件。
2. 项目 `.trellis/agents/` 中的**代理定义**：由 `trellis channel spawn --agent <name>` 读取的平台无关角色卡。
3. `.trellis/config.yaml` 中的**项目配置**：工作进程防护阈值及其他通道配置。

## 核心路径

| 路径 | 用途 |
| --- | --- |
| `~/.trellis/channels/<project>/<channel>/events.jsonl` | 每个通道的只追加事件日志。序号受锁保护，可安全重放。 |
| `~/.trellis/channels/<project>/<channel>/<channel>.lock` | 通道级写锁。 |
| `~/.trellis/channels/<project>/<channel>/<worker>.spawnlock` | OOM 防护使用的各工作进程启动锁。 |
| `~/.trellis/channels/<project>/<channel>/.seq` | 有序分配事件的序号旁文件。 |
| `~/.trellis/channels/_global/<channel>/...` | 使用 `--scope global` 创建的通道。项目分组被共享键替代。 |
| `.trellis/agents/check.md` | `--agent check` 读取的默认检查代理角色定义。 |
| `.trellis/agents/implement.md` | `--agent implement` 读取的默认实施代理角色定义。 |
| `.trellis/config.yaml`（`channel.*` 块） | 工作进程防护阈值与通道默认值。 |

项目分组名从项目绝对路径生成（斜杠展开为平面名称，非字母数字字符替换成 `-`），与 Claude Code 的 `~/.claude/projects/<sanitized-cwd>/` 约定一致。测试或沙箱场景可通过 `TRELLIS_CHANNEL_ROOT`（根目录）或 `TRELLIS_CHANNEL_PROJECT`（分组名）覆盖。

## 何时使用通道运行时

通道比单次 Bash 调用或一次性子代理派发更重。至少满足以下一项才使用：

- 工作需要**两个或更多代理进行多轮对话**（跨 AI 需求探索、同级审查、调度者 + 工作进程）。
- 工作进程需要作为**对等进程**运行，供主会话中断、查看进度或异步等待。
- 对话需要**持久保存并可事后检查**（论坛/主题通道、问题看板、决策记录）。
- 多个工作进程需要**共享事件日志**，以查看彼此报告。

以下情况优先使用成本更低的基础能力：

- 单次 Bash 命令或一次 Agent 工具调用足够 → 直接执行。
- 用户只需要针对文件的静态审查 → 读取文件并在当前会话回复。
- 需求是“回忆上周讨论过什么” → 使用 `trellis mem`，不使用通道。

## 定制位置

| 需求 | 修改位置 |
| --- | --- |
| 修改通道工作进程默认空闲超时 | `.trellis/config.yaml` 中的 `channel.worker_guard.idle_timeout`。支持 `5m`、`30s` 等；设为 `0` 禁用空闲清理。 |
| 修改运行中工作进程数量上限 | `.trellis/config.yaml` 中的 `channel.worker_guard.max_live_workers`。设为 `0` 禁用启动时数量检查。 |
| 按单次启动覆盖工作进程防护 | 在 `trellis channel spawn` 传入 `--idle-timeout` / `--max-live-workers`，或设置环境变量 `TRELLIS_CHANNEL_WORKER_IDLE_TIMEOUT` / `TRELLIS_CHANNEL_MAX_LIVE_WORKERS`。 |
| 修改默认检查或实施工作进程的职责 | 编辑 `.trellis/agents/check.md` 或 `.trellis/agents/implement.md`。它们是平台无关角色卡；传入 `--agent check|implement` 时由通道运行时注入。 |
| 添加新角色卡 | 在 `.trellis/agents/` 中放入 `<name>.md`，`trellis channel spawn --agent <name>` 会读取它。 |
| 迁移通道存储位置（CI 沙箱、临时运行） | 设置 `TRELLIS_CHANNEL_ROOT=/path/to/dir`。后续通道事件使用新位置；已有通道仍在旧根目录。 |
| 切换存储作用域 | 在每个通道子命令中传入 `--scope project`（默认）或 `--scope global`。只改变分组目录，其他行为不变。 |

工作进程防护的优先级：CLI 参数 > 环境变量 > `.trellis/config.yaml` > 内置默认值。内置默认值为 `idle_timeout: 5m` 和 `max_live_workers: 6`。

## 与其他本地层的关系

- **工作流层**：使用通道派发的工作流（如 `channel-driven-subagent-dispatch`）指示主代理调用 `trellis channel spawn --agent check` 或 `--agent implement`，而非平台代理。如果 `.trellis/agents/check.md` 或 `implement.md` 缺失，`trellis workflow --template <id>` 会在安装时输出非阻断警告。误删后可用 `trellis update` 恢复。
- **任务层**：通道工作进程不拥有任务状态。负责监督的主会话通过工作进程收件箱传入活动任务路径，工作进程再从磁盘读取任务产物。
- **规范层**：工作进程与主会话一样读取 `.trellis/spec/`。通道运行时不会绕过规范上下文加载。
- **平台集成层**：通道运行时与平台无关，不依赖 `.claude/`、`.codex/` 或其他平台目录。规范化提供方输出的适配器（Claude `stream-json`、Codex `app-server`）在 Trellis CLI 程序内部，不在用户项目中。
- **平台子代理文件与通道工作进程**：编辑 `.claude/agents/trellis-implement.md`（或其他平台 `.X/agents/` 中的同类文件）不会改变通道运行时工作进程的行为；通道工作进程加载 `.trellis/agents/<name>.md`。平台专用代理文件供主 AI 会话直接派发子代理使用，不供通道启动的工作进程使用。各平台代理入口见 `platform-files/agents.md`；`trellis-meta/SKILL.md` 中的规则明确了这一分工。

## 运行时用法

命令语法、论坛/主题模式、工作进程句柄、进度查看，以及 `--kind done` / `--kind turn_finished` 调度者等待模式，请加载捆绑的 `trellis-channel` 技能（在 `trellis init` / `trellis update` 后自动安装到各平台技能目录）。本参考仅覆盖本地文件结构与定制配置，不重复可能随版本变化的命令语法。
