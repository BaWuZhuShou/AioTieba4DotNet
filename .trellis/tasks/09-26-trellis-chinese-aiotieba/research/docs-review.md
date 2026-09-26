# 文档独立审查

## 范围与结论

本轮以 `/tmp/trellis-zh-20260926-01a0dd3b-baseline` 为原始文件基线，审查全部 19 份规范、既有归档任务和开发日志、根 `AGENTS.md`、工作流与配置、Trellis/Codex 代理说明、46 份技能 Markdown，以及根忽略规则和 `.gitattributes`。原有未跟踪文件也纳入比较，没有只依赖 `git diff`。

结论：文档范围没有未解决的交付阻碍。自然语言已中文化；保留英文属于技术名称、命令、配置或协议标记、状态枚举、路径及真实历史证据。未增加 C# 产品功能、上游对齐完成声明或未验证平台支持承诺。

## 已直接修复

| 文件 | 问题与修复 |
| --- | --- |
| `.agents/skills/trellis-session-insight/references/triggering-patterns.md` | 移除本机 CLI 不支持的 `trellis mem list --task` 示例，改用工作目录限定和任务名搜索，再核对真实对话。 |
| `.agents/skills/trellis-session-insight/references/cli-quick-reference.md` | 明确 `--task` 不受支持；将旧 OpenCode beta 占位限制标为历史信息，并按 0.6.17 帮助准确说明 `--phase` 的限制，不把帮助文本当作实际会话可读性的验证。 |
| `.agents/skills/trellis-channel/references/workers.md` | 读取最终答复时补充 `--kind message`，避免 `--last 1` 取到随后写入的 `done` 事件。 |
| `.agents/skills/trellis-channel/references/command-reference.md` | 根据安装版本源码区分 `messages` 与 `wait` 的默认事件过滤；明确 `messages --follow` 配合 `--raw` 才输出 JSON，而 `wait` 输出 JSON。 |
| `.agents/skills/trellis-channel/SKILL.md` | 将全局项目键从错误的 `__global__` 修正为实际导出的 `_global`。 |
| `.agents/skills/trellis-meta/SKILL.md` | 将当前派发模式写为 `auto` / `inline`，说明旧 `sub-agent` 是 `auto` 的兼容别名；同步 OpenCode 的安装版本边界。 |
| `.agents/skills/trellis-meta/references/customize-local/change-agents.md` | 同步实际子代理上下文读取顺序与研究代理的独立上下文范围，避免沿用旧的简化读取顺序。 |
| `.agents/skills/trellis-meta/references/local-architecture/workflow.md` | 修正状态块位于阶段索引的说明，并准确说明归档清除活动指针后通常不会进入保留的 `completed` 状态块。 |
| `.agents/skills/trellis-meta/references/customize-local/change-workflow.md` | 提醒收尾规则必须同步活动实施状态与阶段 3，不能仅修改通常不可达的 `completed` 状态块。 |

命令修正的证据来自本机 Trellis 0.6.17 帮助、只读安装源码及导出的常量，没有修改全局安装，也没有启动实际工作进程。`trellis mem` 的参数过滤实现不读取 `--task`；Codex 适配器会在最终 `message` 后写入 `done`；`messages` 的输出模式由 `--raw` 控制。派发模式依据本项目 `common/config.py` 的实际解析逻辑。

## 关键约束核对

- 项目定位规范固定 `v4.6.4` / `04f8e431f87507a6228b42061c70d298b34317ff`，保留 .NET 10、C# 14、六模块及现有异步、取消、资源释放、异常和 DTO 契约，明确区分目标、实现台账、离线验证和真实在线证据。
- 17 份原有规范的关键规则保留；历史“规范使用英文”作为当时事实翻译，当前中文约定另行生效。
- 历史日期、真实提交哈希与提交标题、会话指纹及原有日志/索引机器标记保留。新增说明中的示例标记不计作历史标记变化。
- `.gitignore` 精确放行 46 份 Trellis 技能 Markdown；验证的 7 类其他 `.agents` 路径仍被忽略。`.gitattributes` 仅翻译说明，`merge=union` 规则未变。
- 12 个技能的 `name` 和 frontmatter 键、4 份 TOML 的非文本字段、13 个工作流步骤、25 个必需/可选/按需标记和 42 个平台/状态标记与基线一致。
- 主会话自主提交授权在根指引、工作流、技能和代理职责间一致：检查和范围审阅后工作提交，再归档和会话记录；子代理只回报；未授权远程推送或历史改写。
- 模板哈希保留；上游模板树与本地文件的边界明确，没有为了补链接创建虚构目录。

## 验证

- `git diff --check`：通过。
- `/tmp/check-trellis-localization.py`：通过；35 份 Python、4 份 TOML、4 份 JSON、4 份 JSONL 均可解析，Markdown 结构与本地链接无新增错误，637 个范围外受保护文件内容一致。该工具的英文候选仅用于人工分类，未作为禁止英文字符的规则。
- 独立文档/元数据核对：通过；中文规范与历史文件的代码围栏、UTF-8、链接和非文本字段保持有效，技能名称、工作流标记、原有历史标记保持稳定。
- `python3 -B -m unittest discover -s .trellis/scripts/tests -p 'test_*.py'`：31 项通过。
- `python3 -B .trellis/scripts/task.py validate .trellis/tasks/09-26-trellis-chinese-aiotieba`：通过，实施上下文 2 项、检查上下文 4 项。
- `python3 -B .trellis/scripts/get_context.py --mode packages`：通过，返回本仓库的 `backend` / `frontend` 规范入口。
- 配置读取验证：`session_auto_commit` 为真，日志提交说明为 `chore: 记录开发日志`，默认派发模式为 `auto`。

本轮没有适用的独立 Markdown 类型检查命令；以结构解析、元数据比较和对应回归检查验证。没有执行产品构建、线上请求、依赖安装、protobuf 生成、任务归档或 Git 提交。最终提交及完整组合回归由主会话汇总。

## 未修复项

无本次变更引入且尚未修复的文档问题。原规范中已存在的重复检查清单保留原意，本任务不借翻译重构规范结构。
