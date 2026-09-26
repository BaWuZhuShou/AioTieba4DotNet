# 研究：Trellis 人类可读内容中文化与运行契约边界

- 研究问题：盘点本项目全部本地 Trellis 人类可读内容，识别中文化会影响的解析器、生成模板、上下文注入及自动提交规则。
- 范围：内部；不修改全局 CLI，不研究产品功能实现。
- 日期：2026-09-26
- 研究方式：使用原生注入提供的任务路径；只读源文件；唯一写入文件为本研究文档。未执行任何 Git 操作，未读取 implement.jsonl/check.jsonl 内容。

## 研究发现

### 1. 可读范围与数量

建议将“人类可读”解释为中文正文、标题、说明性注释、文档字符串、提示、帮助文本、模板、技能 description、代理 developer_instructions，以及现有任务/归档/日志的自然语言；路径、命令、协议标记、机器键和枚举保持原样。代码示例内注释可翻译，示例的可执行语法不改。

盘点时排除了 `__pycache__/`、`.runtime/`，以下为当时文件快照，当前任务文件会随其他工作者继续写入而增加。

| 路径 | 数量 / 行数 | 内容与处理 |
| --- | --- | --- |
| `AGENTS.md` | 1 / 21 | 根 Trellis 入口，整段可读说明中文化，保留管理注释标记 |
| `.trellis/workflow.md` | 1 / 721 | 工作流、状态提示、阶段正文、技能路由、示例、维护说明 |
| `.trellis/spec/` | 17 / 1,254 | 后端 8、前端 5、指南 3、总索引 1；中文化及项目定位由对应规格负责人处理 |
| `.trellis/workspace/` | 3 / 205 | 总索引、Juicpt 个人索引、journal-1；翻译说明与历史自然语言，保留日期、哈希、分支、结果事实 |
| `.trellis/agents/` | 2 / 141 | channel 的 check/implement 运行代理；与 Codex 代理分属不同入口 |
| `.trellis/config.yaml` | 1 / 158 | 注释、提示示例、提交信息可译；键、布尔、枚举保持机器契约 |
| `.trellis/scripts/` | 28 / 11,568 | Python 注释、文档字符串、本地提示、帮助、生成模板；须分类处理字符串 |
| `.codex/hooks/` | 3 / 2,256 | 工作流、子代理、SessionStart 的人类可读注入文本及解析器 |
| `.codex/agents/` | 3 / 171 | TOML description、developer_instructions 中文化，机器设置不变 |
| `.codex/config.toml` | 1 / 39 | 解释性注释可译，配置项不变 |
| `.codex/hooks.json` | 1 / 27 | 仅机器配置，当前没有需翻译的自然语言值 |
| `.agents/skills/` | 46 / 5,071 | 12 个 SKILL.md 和 34 个引用文档；全部在本地可读范围 |
| `.trellis/.gitignore` | 1 / 32 | 说明性注释可译，匹配模式不变 |
| `.trellis/tasks/` | 快照 11 个文件 | 当前任务与一个已归档任务；本研究未读取上下文清单；其自然语言 reason/title/description 可由主会话或实施者处理 |

技能规模分布：`trellis-meta` 24 文件 / 1,998 行，`trellis-channel` 6 / 1,410，`trellis-spec-bootstrap` 5 / 321，`trellis-session-insight` 3 / 240，其余 8 个单文件技能。排除 tasks/runtime/cache 后，三个根目录共有 110 个文件，其中 69 个 Markdown、31 个 Python；另加根 AGENTS.md。文件总行数不是待翻译字符串数。

不应翻译 `.trellis/.version`、`.template-hashes.json`、`.developer` 的机器内容或临时运行状态。根 `.gitignore` 只需为 Trellis 技能持久化作窄调整，不应借此翻译整份 Visual Studio 模板。

### 2. 文件与现有规范

- `AGENTS.md:1-21`：管理区采用 `<!-- TRELLIS:START -->` / `<!-- TRELLIS:END -->`；正文可中文化，标记必须保留。
- `.trellis/spec/frontend/index.md:5`：明确写着维护规格使用 English，应改为中文规则；站点 `zh-CN` 说明保留事实。
- `.trellis/workspace/index.md:125`：明确要求全部文档使用 English，与用户要求冲突。
- `.trellis/spec/backend/index.md:17-31`、`.trellis/spec/frontend/index.md:16-29`：开发前检查与质量检查是技能要定位的段落。
- `.agents/skills/trellis-before-dev/SKILL.md:29-31` 与 `.agents/skills/trellis-check/SKILL.md:31` 引用上述英文标题；本地 Python 未发现按这两个标题解析的逻辑，可将标题和技能文字一同中文化。
- `.agents/skills/trellis-meta/references/local-architecture/overview.md:23-42`：本地 workflow/spec/tasks/workspace/scripts 为定制入口，保留已有修改；全局 npm 包不是默认目标。

### 3. 工作流提取：必须同步修改两处消费者

1. `.trellis/scripts/common/workflow_phase.py:38,77-80` 精确匹配 `## Phase Index` 和 `## Phase 1: Plan`。直接将文档标题翻成中文会让索引为空，或无法识别结束位置而注入过长正文；`.trellis/scripts/common/git_context.py:85-97` 在为空时退出 2。
2. `.codex/hooks/session-start.py:423-440,468` 独立实现相同精确标题匹配。不能只修 common/workflow_phase.py。当前 `.codex/hooks.json:3-25` 仅注册 UserPromptSubmit、SubagentStart；SessionStart 文件目前未在此配置注册，但作为随仓库提供的入口仍需兼容。
3. `.trellis/scripts/common/workflow_phase.py:35,100-129` 通过 `#### X.Y` 识别步骤，依赖层级、数字步骤号和段落边界，不依赖步骤中文标题。保留步骤号：`1.0,1.1,1.2,1.3,1.4,1.5,2.1,2.2,2.3,3.2,3.3,3.4,3.5`。
4. `.trellis/scripts/common/workflow_phase.py:32,134-159` 识别平台标签并映射稳定平台 ID；`[Claude Code, Cursor, ...]`、闭合标签及 `codex` 等 ID 应保留。正文可译。
5. `.codex/hooks/inject-workflow-state.py:204-230` 只匹配成对 `[workflow-state:STATUS]` 标记，正文允许中文；`.trellis/scripts/common/workflow_phase.py:90-97` 与 `.codex/hooks/session-start.py:443-453` 同样依赖这些标记进行剔除。

建议：工作流可见标题全部中文化；以上两处索引解析器最小兼容旧英文与新中文标题，避免增加新的工作流格式。保留 `workflow-state` 标记及状态：`no_task`、`task_error`、`planning`、`planning-inline`、`in_progress`、`in_progress-inline`、`completed`。文件末尾的 `my-status` 是示例，当前正则也会读到它，验证时不要把它误认作新增故障。

`[required · once]` 等行内标记并非已发现的本地 Python 解析条件，但 workflow.md:116-120 提到上游测试依赖该字面量。稳妥做法是保留其代码形式，将周围说明译成中文；不要宣称本地不存在的上游测试已经通过。

### 4. 日志不是纯 Markdown：编号、日期、幂等与表头均有消费者

| 消费者 | 当前契约 | 中文化要求 |
| --- | --- | --- |
| `.trellis/scripts/add_session.py:100-101` | `**Date**: YYYY-MM-DD`、`## Session N:` | 日期和会话标题支持旧英文与新中文形式 |
| 同文件 `:162-175` | `Total Sessions` 计数、Session 最大编号 | 中文个人索引和中英混合日志均不得归零 |
| 同文件 `:611-635` | `git grep -E` 查询其他 refs 中的 Session / Total Sessions | 不仅改 Python 正则；Git 查询表达式与后续解析也须同时支持中英 |
| 同文件 `:701,749-758` | 标记归属到 Session 标题、读取 Date | 中文化后仍能识别旧记录重试与日期 |
| 同文件 `:978-1026` | `@@@auto:*` / `@@@/auto:*` 管理区 | HTML 标记完整保留 |
| 同文件 `:1037-1047` | 历史表头 Date / Title / Commits / Branch / Base Branch | 保留旧 4/5/6 列迁移能力，新增中文表头识别 |

需同步的生成端：

- `.trellis/scripts/common/developer.py:81-138`：新开发者初始日志、个人索引模板。
- `.trellis/scripts/add_session.py:304`：轮转后的新 journal 标题。
- `.trellis/scripts/add_session.py:885-941`：正文、日期、摘要、提交证据、测试、完成状态、下一步；`No commits - planning session` 也是人类可读提示。
- `.trellis/scripts/add_session.py:995-1008,1041-1047`：个人索引状态和表头；计数字段不能生成后又读不回。
- `.trellis/workspace/index.md:83-125`：文档示例与实际生成格式应一致。

`<!-- trellis-session: ... -->`、版本号、fp/key 等身份字段保持不变（add_session.py:93-107）。记录指纹还依赖输入自然语言（add_session.py:481-512）；翻译既有记录不能伪造或重算历史身份标记，不能改写提交哈希、真实提交 subject、日期和原始验证结果。已有英文证据可作为原始机器/外部证据保留，周围解释中文化。

### 5. 新任务、帮助输出、注入提示

- `.trellis/scripts/common/task_store.py:299-322` 是 `task.py create` 的 PRD 模板，应中文化 Goal/Requirements/Acceptance Criteria/Notes、TBD、默认说明；否则新任务会重新出现英文。
- `.trellis/scripts/common/task_store.py:104-110,392-398`：自动 slug 仅由 ASCII 标题生成；中文任务必须继续显式传稳定英文 `--slug`。无需为本次中文化扩展目录命名规则。
- `.trellis/scripts/common/task_store.py:529-552` 的任务 JSON 键以及 `planning` 等状态保留，title/description/notes 等描述值可中文化。
- `.trellis/scripts/common/git_context.py:50-70` 的 argparse 描述/help 可译，`--mode`、`--step`、`--platform` 及 choices 不译。Python argparse 内置 `usage/options/error` 不是本地文案，不值得通过自定义解析器扩大本任务。
- `.trellis/scripts/common/paths.py:28-42,93-95`：目录常量、文件名、环境变量与 `.developer` 的 `name=` 是机器契约。
- `.codex/hooks/inject-workflow-state.py:309-336,359-380`：mode 说明、默认提示、Status/Task 显示可译；模式枚举、XML 标签保留。
- `.codex/hooks/inject-subagent-context.py:875-910,1046-1089`：宿主 hook 字段名、事件名、role ID 必须不变；可见角色说明、标题、搜索提示、截断提示可译。
- `Active task: <path>`、`<!-- trellis-hook-injected -->`、`Full hook output saved to: <path>` 是跨提示的契约（`.codex/agents/trellis-research.toml:18-29`，其他两个代理 `:20-24`）；保留字面量，可加中文解释。宿主的截断提示不是本地输出。
- `.codex/agents/*.toml` 的 `name`、`sandbox_mode`、`model`、`model_reasoning_effort`、`developer_instructions` 键保留；description / developer_instructions 的自然语言翻译。
- `.trellis/agents/*.md:1-7` 的 `name/provider/labels` 及值是运行时标识，description 与正文中文化。`.agents/skills/*/SKILL.md` 的 `name` 值与目录名不变，description 翻译。

### 6. 自动本地 Git 提交：用户新增长期授权的落点

本节基于主会话转达的新增要求：以后完成检查后自动 Git 提交，不逐次要求用户确认；最终提交由主会话承担，子代理不提交。

- `.trellis/workflow.md:626-650` 明确要求一次确认、确认后提交、修改 message 后再确认。应改成主会话检查通过后归类任务文件、报告简要提交意图并自主完成本地提交；中文提交说明，保留约定的类型前缀即可。未知或无关改动不纳入本任务提交，不能因该授权扩大到 push/amend。
- `.trellis/workflow.md:654` 只提醒用户运行 finish-work；结合新长期授权，应主会话自动衔接已有收尾流程，避免在已授权的提交之后再次停下。
- `.agents/skills/trellis-finish-work/SKILL.md:3` 的 description 仍写“提醒用户提交”；`:42-45` 遇到本任务未提交文件时让用户返回阶段 3.4。应中文化并改为主会话回 3.4 完成提交后继续；仍不需要将工作提交塞进归档函数。
- `.agents/skills/trellis-finish-work/SKILL.md:32,56-71` 已区分成果提交、归档提交、日志提交，应保留顺序与提交哈希语义；不能把归档提交哈希冒充成果提交。
- `.trellis/scripts/common/config.py:22-24,118-136`：`session_auto_commit` 默认 true，只控制 add_session 的日志提交和 task_store 的归档提交。显式设置 `.trellis/config.yaml` 的 `session_auto_commit: true` 可以表达偏好，但不等于自动提交全部代码，仍要改主会话工作流。
- `.trellis/config.yaml:12` 的 `session_commit_message: "chore: record journal"` 与 `.trellis/scripts/common/config.py:22` 默认值可中文化；归档 subject 在 `.trellis/scripts/common/task_store.py:1549` 硬编码 `chore(task): archive ...`，恢复提示 `:1604` 也需同步。
- `.trellis/scripts/add_session.py:1149-1153` 与 `.trellis/scripts/common/task_store.py:1550-1555` 用显式 pathspec 提交，避免把别人已暂存内容带进去；主会话工作提交也应保留此边界，不用裸 `git commit` 吞并未知 staged 改动。
- `.trellis/agents/implement.md:30-36`、check.md:32-38 已明确子代理禁止 commit/push/merge；继续保留。`.codex/agents/trellis-implement.toml:26-31` 只有“禁止破坏性 Git 操作”，check.toml:26-39 没有提交归属约束；可明确补充由主会话提交以防宽泛继承长期授权。
- `.trellis/scripts/common/safe_commit.py:16-30,49-58` 的窄暂存和忽略保护保留，不能为了自动提交改成强制加入整个 `.trellis/`。

### 7. 版本控制、更新与本地定制边界

- 根 `.gitignore:274` 的 `.agents` 忽略整个技能目录。主会话已确认：`git check-ignore .agents/skills/trellis-start/SKILL.md` 命中该行，`git ls-files .agents` 为空。仅编辑技能会留在本机，需窄规则纳入 `.agents/skills/trellis-*/`，继续忽略其他本地代理文件；不要简单移除所有 `.agents` 忽略保护。
- 对 `.trellis/.template-hashes.json` 所列路径执行只读 SHA-256 对比：本研究快照无不匹配。此事实只说明这些模板文件与记录的模板哈希相同，不代表整个仓库干净，也不覆盖 spec/任务/日志。Git 既有改动由主会话核验，研究者未执行 Git。
- `.trellis/.template-hashes.json:4-90` 跟踪技能、Codex、AGENTS、脚本、workflow 的原始哈希；本次定制不应重写这些基线，否则更新器可能失去“用户修改”的判断依据。
- `.trellis/.version:1` 为本地生成版本 `0.6.17`；本研究未查询全局安装版本。
- `.codex/agents/trellis-implement.toml:30` 要求同步 `packages/cli/src/templates/trellis/scripts/`，但这是 Trellis 上游仓库路径，本项目不存在该模板树；应按本地定制范围改成仅当模板源在当前仓库存在时同步，避免实施者创建虚假上游路径。
- workflow.md:720-721 引用了本项目不存在的 `.trellis/spec/cli/backend/workflow-state-contract.md` 与 `.trellis/scripts/inject-workflow-state.py`；中文化时应明确上游参考或修正为本地 `.codex/hooks/inject-workflow-state.py`，不应新增空规格来满足链接。

### 8. 外部 CLI 和英语证据边界

不修改全局 npm 安装目录、`node_modules/@mindfoldhq/trellis*`、用户级 Codex 配置、原始会话日志或 `~/.trellis/channels/`。依据 `.agents/skills/trellis-meta/SKILL.md:21-23,84-88` 及 local-architecture/overview.md:38-42。

`trellis channel/mem/update --help` 属于已安装 CLI，其输出不会因翻译本地技能而变化。Git、linearis、宿主 hook、Python 标准库、OS 异常的原始英文也不是本地翻译目标；本地包装说明可中文化，原始诊断保留原文。

特别注意：`.trellis/scripts/common/session_context.py:350-359` 解析全局 CLI 的 `Trellis update available:`，这个输入识别正则要保留；`:484-486` 是本地生成的可读更新提示，可以中文化，但要同步 `.agents/skills/trellis-start/SKILL.md:19` 对提示前缀的识别指令。`.trellis/scripts/common/safe_commit.py:202-207` 的 `ignored by`、common/git.py:117 的 `HEAD branch:`、`:173` 的 `worktree ` 等是外部输出消费者，不能作为英语文案直接替换。

## 建议验证（实施阶段执行）

本次研究未创建测试、未变更运行文件。适量验证集中在翻译真正触及的解析/输出边界，无需跑 .NET 构建或线上贴吧请求。

1. **语法及结构**：用 Python `ast.parse` 检查 31 个本地 Python 文件，`tomllib` 检查 `.codex/**/*.toml`，`json.loads` 检查 hooks 配置与任务 JSON，项目自带 `parse_simple_yaml` 检查 config。不应为了检查翻译安装无关工具。可用 `python3 -B` 或 `PYTHONDONTWRITEBYTECODE=1` 避免缓存。
2. **阶段提取与路由**：执行 `python3 -B .trellis/scripts/get_context.py --mode phase --platform codex`，以及 `--step 1.4`、`--step 2.1`、`--step 3.4`；断言输出为中文、无多余下一阶段内容、含正确实现/提交职责。针对两个提取器以旧英文和新中文小样本作函数级回归；十三个步骤均非空。
3. **Hook 协议**：导入 hook 模块，构造 no_task/planning/in_progress 与 inline 样本，检查七个状态模板、JSON 外层键/事件名和三类角色分发不变；research 注入保持角色隔离。用测试替身或临时夹具，不修改当前真实 task/runtime 指针。验证中文大文件截断仍遵守 UTF-8 和字节预算。
4. **日志与任务模板**：在临时目录测试新中文 PRD、新开发者索引、新日志条目、轮转、旧英文与中英混合最大编号、日期解析、4/5/6 列表头迁移、幂等重试。跨 refs 编号探测使用模拟 Git 输出覆盖中英表达式；若测试真实 Git，必须独立临时仓库，不能向当前仓库生成假任务/日志/提交。
5. **自动提交契约**：确认 config 实读 true、日志/归档 subject 为中文、主会话文档无需重复确认、所有子代理仍不提交。对自动提交函数用捕获参数的替身确认显式 pathspec 不变；无需为文案验证在真实仓库做测试提交。
6. **完整性与残留审查**：`rg -n --hidden 'English|All documentation|remind user to commit|one-shot confirmation|On confirmation' AGENTS.md .trellis .codex .agents/skills` 列出候选，逐个区分遗留事实、机器契约和未译文案；不能把“没有 ASCII”当验收。主会话执行 `git diff --check`、`git check-ignore` / `git ls-files .agents/skills` 确认技能可持久化，检查管理标记和 Markdown 链接未破坏。

## 外部参考

- 无外部检索；本课题以实际安装到仓库内的 Trellis 文件为依据，版本见 `.trellis/.version`。
- 用户指定的 aiotieba 上游和 Python → C# 接口对齐由另一研究课题处理，本文件不作功能对齐结论。

## 限制与未发现项

- 不将根 AGENTS.md 的 Trellis 说明范围扩展到产品源码、产品 API 中文命名或全站内容重写。
- 当前没有发现本地 Trellis 专项测试文件；需要为上述解析契约增加小型回归检查，不引用不存在的上游测试作为本地通过证据。
- 本研究未读取 implement/check 上下文清单，未运行 Git，因此清单 reason 的中文残留与仓库 dirty 状态由主会话/实施者核验。
- 父会话正在并行更新规划，行号和数量均为研究读取时快照；实施前按目标符号重新定位。
