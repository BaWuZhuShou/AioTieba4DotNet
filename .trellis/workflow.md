# 开发工作流

---

## 核心原则

1. **先规划，再编码** — 开始前先明确要做什么
2. **通过注入读取规范** — 由钩子或技能注入指南，不依赖记忆复述
3. **所有成果持久化** — 研究、决策和经验都写入文件；对话会被压缩，文件不会
4. **增量开发** — 每次处理一个任务
5. **沉淀经验** — 每个任务结束后，回顾并将新知识写回规范

---

## Trellis 系统

### 开发者身份

首次使用时，初始化身份：

```bash
python3 ./.trellis/scripts/init_developer.py <your-name>
```

该命令创建 `.trellis/.developer`（由 Git 忽略）和 `.trellis/workspace/<your-name>/`。

### 规范系统

`.trellis/spec/` 按包和层组织编码指南。

- `.trellis/spec/<package>/<layer>/index.md` — 包含**开发前检查清单**和**质量检查**的入口。具体指南在它链接的 `.md` 文件中。
- `.trellis/spec/guides/index.md` — 跨包思考指南。

```bash
python3 ./.trellis/scripts/get_context.py --mode packages   # 列出包和层
```

**更新规范的时机**：发现新模式或约定、需要固化缺陷预防规则、作出新技术决策。

### 任务系统

每个任务在 `.trellis/tasks/{MM-DD-name}/` 下拥有独立目录，保存 `task.json`、`prd.md`、可选的 `design.md`、`implement.md`、`research/`，以及供支持子代理的平台使用的上下文清单（`implement.jsonl`、`check.jsonl`）。

```bash
# 任务生命周期
python3 ./.trellis/scripts/task.py create "<任务标题>" [--slug <name>] [--parent <dir>]
python3 ./.trellis/scripts/task.py start <name>          # 设置活动任务（可用时限定到会话）
python3 ./.trellis/scripts/task.py current --source      # 显示活动任务及来源
python3 ./.trellis/scripts/task.py finish                # 清除活动任务（触发 after_finish 钩子）
python3 ./.trellis/scripts/task.py archive <name>        # 移至 archive/{year-month}/
python3 ./.trellis/scripts/task.py list [--mine] [--status <s>]
python3 ./.trellis/scripts/task.py list-archive

# 代码规范上下文（通过 JSONL 注入 implement/check 代理）。
# 支持子代理的平台在 task create 时创建空的 implement.jsonl / check.jsonl；
# AI 在规划期间填入真实的规范和研究条目。清单仍为空时，validate 失败且 start 拒绝启动，
# 避免子代理在没有规范上下文的情况下运行。确实有意使用空清单时，传入 start --allow-empty-context。
python3 ./.trellis/scripts/task.py add-context <name> <action> <file> <reason>
python3 ./.trellis/scripts/task.py list-context <name> [action]
python3 ./.trellis/scripts/task.py validate <name>

# 任务元数据
python3 ./.trellis/scripts/task.py set-branch <name> <branch>
python3 ./.trellis/scripts/task.py set-base-branch <name> <branch>    # PR 目标分支
python3 ./.trellis/scripts/task.py set-scope <name> <scope>

# 父子层级
python3 ./.trellis/scripts/task.py add-subtask <parent> <child>
python3 ./.trellis/scripts/task.py remove-subtask <parent> <child>

# 创建 PR
python3 ./.trellis/scripts/task.py create-pr [name] [--dry-run]
```

> 运行 `python3 ./.trellis/scripts/task.py --help` 查看权威且最新的命令列表。

**当前任务机制**：`task.py create` 创建任务目录，并在有会话身份时自动设置会话级活动任务指针，使规划状态提示立即生效。`task.py start` 写入同一指针（已经设置时保持幂等），并将 `task.json.status` 从 `planning` 改为 `in_progress`。状态保存在 `.trellis/.runtime/sessions/`。如果钩子输入、`TRELLIS_CONTEXT_ID` 和平台原生会话环境变量都没有提供上下文键，则没有活动任务，`task.py start` 会失败并提示补充会话身份。`task.py finish` 删除当前会话文件，但不改变任务状态。`task.py archive <task>` 写入 `status=completed`，将目录移至 `archive/`，并删除仍指向该归档任务的运行时会话文件。

### 工作区系统

在 `.trellis/workspace/<developer>/` 下记录每个 AI 会话，便于跨会话追踪。

- `journal-N.md` — 会话日志。**每个文件最多 2000 行**；超过后自动创建 `journal-(N+1).md`。
- `index.md` — 个人索引（总会话数、最后活跃时间）。

```bash
python3 ./.trellis/scripts/add_session.py --title "会话标题" --commit "hash" --summary "摘要"
```

### 上下文脚本

```bash
python3 ./.trellis/scripts/get_context.py                            # 完整会话运行上下文
python3 ./.trellis/scripts/get_context.py --mode packages            # 可用包及规范层
python3 ./.trellis/scripts/get_context.py --mode phase --step <X.Y>  # 某个工作流步骤的详细指南
```

---

<!--
  工作流状态提示契约（编辑下面的标记块前先阅读）

  下方“## 阶段索引”中的 [workflow-state:STATUS] 块，是各受支持 AI 平台的
  UserPromptSubmit 钩子读取每轮 <workflow-state> 提示的唯一事实来源。
  inject-workflow-state.py（Python 平台）和 inject-workflow-state.js
  （OpenCode 插件）只负责解析；从 v0.5.0-rc.0 起，脚本不再内置兜底字典。

  STATUS 字符集：[A-Za-z0-9_-]+。钩子找不到标记时，退回一条通用的
  “参阅 workflow.md 确认当前步骤”提示。该降级刻意可见，便于发现并修复损坏的 workflow.md。

  上游测试不变量（test/regression.test.ts，并非本仓库的测试路径）：
    每个带有 `[required · once]` 标记的详细步骤，都必须在所属阶段的
    [workflow-state:*] 块中有对应的强制执行说明。状态提示是唯一的逐轮通道；
    如果未提及必做步骤，AI 可能跳过它。阶段 1 规划门槛和阶段 3.4 提交
    都曾因这一缺口被跳过。保留这些行内机器兼容性标记，周围说明使用中文。

  标记与阶段对应关系：
    [workflow-state:no_task]      → 无活动任务，阶段 1 之前
    [workflow-state:task_error]   → 活动任务记录不可读，修复后才能继续
    [workflow-state:planning]     → 整个阶段 1（status='planning'）
    [workflow-state:planning-inline] → Codex 内联模式的阶段 1
    [workflow-state:in_progress]  → 阶段 2 和阶段 3.2–3.4
                                    （从 task.py start 到 task.py archive，
                                    status 始终为 in_progress）
    [workflow-state:in_progress-inline] → Codex 内联模式的阶段 2/3
    [workflow-state:completed]    → 当前不可达：cmd_archive 在一次调用中
                                    修改状态并移动目录，解析器随后失去指针。
                                    保留此块，供未来显式 in_progress→completed
                                    状态转换使用。

  编辑检查清单：
    - 修改 [workflow-state:STATUS] 块时，同步核对对应阶段的
      `[required · once]` 详细步骤。
    - Trellis 上游模板维护者可运行 trellis update，将新正文以块级受管替换
      分发至下游项目。本项目本轮不运行更新；项目定制遵循
      .trellis/spec/trellis-localization.md。
    - 本地解析实现：.codex/hooks/inject-workflow-state.py。
      完整上游契约位于 Trellis 上游仓库的
      .trellis/spec/cli/backend/workflow-state-contract.md，本项目不包含该文件。
-->

## 阶段索引

```
阶段 1：规划 → 分类、取得创建任务授权，再编写规划文档
阶段 2：执行 → 任务状态为 in_progress 后才实施
阶段 3：收尾 → 验证、更新规范、提交并完成收尾
```

### 请求分类

- 简单对话或小任务：只询问本轮是否创建 Trellis 任务。用户拒绝时，本会话跳过 Trellis。
- 复杂任务：询问是否可以创建 Trellis 任务并进入规划。用户拒绝时，不直接在主会话展开大范围实施；解释情况、澄清范围或建议拆小。
- 用户同意创建任务不等于同意开始实施。仍须先完成规划。

### 规划文档

- `prd.md` — 需求、约束和验收标准；不在这里放技术设计或执行清单。
- `design.md` — 复杂任务的技术设计：边界、契约、数据流、取舍、兼容性、发布和回滚方式。
- `implement.md` — 复杂任务的执行计划：有序检查清单、验证命令、评审门槛和回滚点。
- `implement.jsonl` / `check.jsonl` — 子代理上下文的规范和研究清单，不能替代 `implement.md`。
- 轻量任务可以只有 PRD。复杂任务在 `task.py start` 前必须具备 `prd.md`、`design.md` 和 `implement.md`。

### 父子任务树

一个请求包含多个可独立验证的交付物时，使用父任务。父任务负责原始需求集、任务映射、跨子任务验收标准和最终集成评审；除非自身也有直接实施工作，否则通常不作为实施目标。

可独立规划、实施、检查和归档的交付物使用子任务。父子结构不是依赖系统：若子任务需要等待另一个子任务，必须将顺序写入该子任务的 `prd.md` / `implement.md`，并保证每个子任务的验收标准可测试。

使用 `task.py create "<任务标题>" --slug <name> --parent <parent-dir>` 创建子任务；使用 `task.py add-subtask <parent> <child>` 关联现有任务；错误关联使用 `task.py remove-subtask <parent> <child>` 解除。

<!-- 每轮状态提示：没有活动任务时显示（阶段 1 之前）。 -->

[workflow-state:no_task]
当前没有活动任务。先对本轮请求分类，取得创建任务授权后再创建 Trellis 任务。
简单对话或小任务：只询问本轮是否创建 Trellis 任务。用户拒绝时，本会话跳过 Trellis。
复杂任务：询问是否可以创建 Trellis 任务并进入规划。用户拒绝时，解释情况、澄清范围或建议拆小。
[/workflow-state:no_task]

<!-- 每轮状态提示：活动任务记录不可读时显示。 -->

[workflow-state:task_error]
无法读取活动任务记录。不要创建或激活另一个任务。
检查上方指定的任务目录，修复 task.json；它必须是有效的 JSON 对象，且 status 非空。
保留已有任务字段和文档。若无法安全确定正确状态，重建记录前先询问用户。
[/workflow-state:task_error]

### 阶段 1：规划
- 1.0 创建任务 `[required · once]`（仅在取得创建任务授权后）
- 1.1 需求探索 `[required · repeatable]`（`prd.md`；复杂任务还需 `design.md` 和 `implement.md`）
- 1.2 研究 `[optional · repeatable]`
- 1.3 配置上下文 `[required · once]` — Claude Code、Cursor、OpenCode、Codex、Kiro、Gemini、Qoder、CodeBuddy、Copilot、Droid、Pi、Oh My Pi、ZCode、Snow、Reasonix、Grok、Kimi Code（仅适用于派发子代理的平台；内联平台跳过）
- 1.4 激活任务 `[required · once]`（先评审，再执行 `task.py start`；状态变为 in_progress）
- 1.5 完成标准

<!-- 每轮状态提示：整个阶段 1 显示（status='planning'）。 -->

[workflow-state:planning]
加载 trellis-brainstorm，留在规划阶段。
轻量任务可以只用 prd.md；复杂任务须完成 prd.md、design.md 和 implement.md，在 task.py start 前请用户评审。
多个交付物：考虑父任务加可独立验证的子任务；依赖写入子任务文档，不能只靠树形位置暗示。
子代理模式：启动前将真实的规范和研究条目填入 implement.jsonl 和 check.jsonl。
[/workflow-state:planning]

<!-- 每轮状态提示：codex.dispatch_mode=inline 时，整个阶段 1 显示。
     这是仅供 Codex 主动选择的 [workflow-state:planning] 替代版本。
     主代理在阶段 2 直接编辑代码，因此跳过 JSONL 清单整理；内联工作流
     加载 trellis-before-dev，而不是向子代理注入 JSONL。 -->

[workflow-state:planning-inline]
加载 trellis-brainstorm，留在规划阶段。
轻量任务可以只用 prd.md；复杂任务须完成 prd.md、design.md 和 implement.md，在 task.py start 前请用户评审。
多个交付物：考虑父任务加可独立验证的子任务；依赖写入子任务文档，不能只靠树形位置暗示。
内联模式：跳过 JSONL 清单整理；阶段 2 通过 trellis-before-dev 读取文档和规范。
[/workflow-state:planning-inline]

### 阶段 2：执行
- 2.1 实施 `[required · repeatable]`
- 2.2 质量检查 `[required · repeatable]`
- 2.3 回退 `[on demand]`

<!-- 每轮状态提示：status='in_progress' 时显示。
     范围是整个阶段 2 和阶段 3.2–3.4（task.py start 到 task.py archive 期间
     状态始终为 in_progress，只有归档才改变状态）。正文必须覆盖从实施到提交的
     所有必做步骤，包括阶段 3.3 规范更新和阶段 3.4 提交。 -->

子代理派发协议适用于所有平台和子代理，包括带有子代理自行读取兜底的 Codex 原生 `SubagentStart` 上下文注入、第二类 Gemini/Qoder/Copilot/Reasonix/Trae/Grok/Kimi Code、由钩子支持的 ZCode/Snow，以及 `trellis-research`：每条派发提示必须以 `Active task: <task path from task.py current>` 开头，再写角色指令。在 Grok Build 上，使用 `spawn_subagent` 并将 `subagent_type` 设为 Trellis 代理名（如 `trellis-implement`）。在 Kimi Code 上，派发内置 `coder` / `explore` 子代理，并附上对应 `.kimi-code/skills/trellis-<role>/SKILL.md` 的指令。

[workflow-state:in_progress]
工具：trellis-implement / trellis-research 仅为子代理类型（Task/Agent 工具，不是 Skill；没有同名技能）。trellis-update-spec 是技能。trellis-check 同时有代理和技能；代码变更后验证优先使用代理。
流程：trellis-implement -> trellis-check -> trellis-update-spec -> 主会话自主本地提交（阶段 3.4）-> /trellis:finish-work。
主会话默认派发实施和检查子代理。子代理自豁免：已作为 trellis-implement 运行时，禁止再创建 trellis-implement 或 trellis-check；已作为 trellis-check 运行时，也禁止再创建这两类代理。只有主会话可以派发并统一提交，子代理不提交。
派发提示以 Active task: <task path from task.py current> 开头。读取顺序：JSONL 条目 -> prd.md -> design.md（若存在）-> implement.md（若存在）。
检查通过后，主会话按任务范围暂存并以中文说明提交，无需再次确认提交；随后自主完成归档和会话记录，不夹带无关暂存内容。
[/workflow-state:in_progress]

<!-- 每轮状态提示：codex.dispatch_mode=inline 且 status='in_progress' 时显示。
     这是仅供 Codex 主动选择的 [workflow-state:in_progress] 替代版本。
     主会话直接编辑代码，不派发子代理。 -->

[workflow-state:in_progress-inline]
流程：trellis-before-dev -> 编辑 -> trellis-check -> 验证 -> trellis-update-spec -> 主会话自主本地提交（阶段 3.4）-> /trellis:finish-work。
内联模式不派发实施和检查子代理。
读取顺序：prd.md -> design.md（若存在）-> implement.md（若存在），再读取技能加载的相关规范和研究。
检查通过后，主会话按任务范围暂存并以中文说明提交，无需再次确认提交；随后自主完成归档和会话记录，不夹带无关暂存内容。
[/workflow-state:in_progress-inline]

### 阶段 3：收尾
- 3.2 调试复盘 `[on demand]`
- 3.3 规范更新 `[required · once]`
- 3.4 提交变更 `[required · once]`
- 3.5 自主收尾

> 步骤 3.1 已并入 2.2（最后一轮全范围检查）和 3.4（提交前检查）。编号保持稳定，避免破坏外部引用。

<!-- 每轮状态提示：status='completed' 时显示。
     正常流程中当前不可达：cmd_archive 在同一调用中写入 completed 并将任务目录
     移到 archive/，活动任务解析器随后失去指针，归档任务不会再触发钩子。
     保留此块，供未来状态转换重设计（例如显式 in_progress→completed 命令）使用。
     与仍生效的状态块采用相同的规范维护流程。 -->

[workflow-state:completed]
代码已提交。由主会话运行 /trellis:finish-work；若仍有本任务未提交变更，先回到阶段 3.4 自主完成提交。
[/workflow-state:completed]

### 规则

1. 判断当前所处阶段，从该阶段下一步骤继续。
2. 每个阶段内按顺序执行；标记为 `[required]` 的步骤不能跳过。
3. 阶段允许回退，例如执行中发现 PRD 缺陷，则返回规划修复后再执行。
4. 标记为 `[once]` 的步骤，产物已经存在时跳过，不重复执行。
5. 根据文档是否存在判断下一步：轻量任务可以没有 `design.md` / `implement.md`；复杂任务缺少它们则表示规划未完成。

### 活动任务路由

活动任务中的用户请求符合以下意图时，先路由，再按需加载详细步骤。

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

- 规划或需求不清楚 -> `trellis-brainstorm`。
- `in_progress` 的实施或检查 -> 派发 `trellis-implement` / `trellis-check`。
- 反复调试 -> `trellis-break-loop`；规范更新 -> `trellis-update-spec`。

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

- 规划或需求不清楚 -> `trellis-brainstorm`。
- 编辑前 -> `trellis-before-dev`；编辑后 -> `trellis-check`。
- 反复调试 -> `trellis-break-loop`；规范更新 -> `trellis-update-spec`。

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

### 约束

- 创建任务授权不是实施授权；文档评审后执行 `task.py start`，再开始实施。
- 轻量任务可以只有 PRD；复杂任务需要 `design.md` 和 `implement.md`。
- 规划必须写入任务文档；报告完成前必须运行检查。

### 加载步骤详情

执行每个步骤时，运行以下命令获取详细指导：

```bash
python3 ./.trellis/scripts/get_context.py --mode phase --step <step>
# 例如：python3 ./.trellis/scripts/get_context.py --mode phase --step 1.1
```

---

## 阶段 1：规划

目标：对请求分类，需要任务时取得创建授权，产出实施前必需的规划文档。

#### 1.0 创建任务 `[required · once]`

取得创建授权后才创建任务目录。命令将状态设为 `planning`，写入 `task.json`，创建默认 `prd.md`，并在有会话身份时自动指向新任务：

```bash
python3 ./.trellis/scripts/task.py create "<任务标题>" --slug <name>
```

`--slug` 只填写可读名称，**不要**包含 `MM-DD-` 日期前缀；`task.py create` 会自动添加。中文任务标题应显式提供稳定的英文 `--slug`。

使用任务树时，先创建父任务，再用 `--parent <parent-dir>` 创建子任务。不要因为有子任务就启动父任务；应启动负责下一个可独立验证交付物的子任务。

命令成功后，每轮状态提示自动切换为 `[workflow-state:planning]`，要求 AI 留在规划阶段。

这里只运行 `create`，不要同时运行 `start`。`start` 会将状态改为 `in_progress`，导致规划文档尚未评审就切换到实施提示。留到步骤 1.4 再运行。

若 `python3 ./.trellis/scripts/task.py current --source` 已经指向一个任务，则跳过。

#### 1.1 需求探索 `[required · repeatable]`

加载 `trellis-brainstorm` 技能，遵循其指导，与用户交互探索需求。

该技能会引导你：
- 每次只问一个问题
- 能研究得出答案时优先研究，不向用户提问
- 优先提供选项，避免开放式提问
- 每次用户回答后立即更新 `prd.md`
- 交付物可以独立验证时，将大范围拆成父任务与子任务
- 保持 `prd.md` 聚焦需求和验收标准
- 复杂任务在实施前产出 `design.md` 和 `implement.md`

考虑父子任务拆分时：
- 一个请求包含多个可独立验证的交付物时使用父任务。
- 父任务负责原始需求、子任务映射、跨子任务验收标准和最终集成评审。
- 子任务负责可独立规划、实施、检查和归档的实际交付物。
- 父子结构不是依赖系统。子任务 B 依赖 A 时，将顺序写入 B 的 `prd.md` / `implement.md`。
- 启动负责下一个交付物的子任务。父任务自身没有直接实施工作时，不启动父任务。

需求变化时返回本步骤，修改对应文档。

#### 1.2 研究 `[optional · repeatable]`

需求探索期间可以随时研究，不限于本地代码。可使用所有可用工具（MCP 服务、技能、网页搜索等）查阅外部资料，包括第三方库文档、行业实践和 API 参考。

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

创建研究子代理：

- **代理类型**：`trellis-research`
- **任务描述**：研究某个具体问题
- **关键要求**：研究结果**必须**保存至 `{TASK_DIR}/research/`

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

直接在主会话研究，将发现写入 `{TASK_DIR}/research/`。`codex-inline` 是明确将工作保留在主会话的模式。

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

**研究文档约定**：
- 每个研究主题独立文件，例如 `research/auth-library-comparison.md`
- 将第三方库用法示例、API 参考和版本约束写入文件
- 记录发现的相关规范路径，供后续引用

需求讨论与研究可以交错进行：暂停讨论研究技术问题，再回到用户讨论。

**关键原则**：研究结果必须写入文件，不能只留在聊天中。对话会被压缩，文件不会。

#### 1.3 配置上下文 `[required · once]`

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

整理 `implement.jsonl` 和 `check.jsonl`，让阶段 2 子代理获得正确的规范和研究上下文。`task create` 已创建清单；旧版本可能留有单条自说明的 `_example` 示例行。本步骤负责填入真实条目。

**位置**：`{TASK_DIR}/implement.jsonl` 和 `{TASK_DIR}/check.jsonl`（已经存在）。

**格式**：每行一个 JSON 对象：`{"file": "<path>", "reason": "<原因>"}`。路径相对于仓库根目录。

**应包含**：
- **规范文件** — `.trellis/spec/<package>/<layer>/index.md` 和任务相关的具体指南，如 `error-handling.md`、`conventions.md`
- **研究文件** — 子代理需要查阅的 `{TASK_DIR}/research/*.md`

**不应包含**：
- 代码文件（`src/**`、`packages/**/*.ts` 等）— 子代理实施时自行读取，不在这里预注册
- 即将修改的文件 — 原因同上

**两个文件的分工**：
- `implement.jsonl` → 实施子代理正确编码所需的规范和研究
- `check.jsonl` → 检查子代理所需的规范（质量指南、检查约定，以及必要的相同研究）

这些清单不能替代 `implement.md`。后者是复杂任务的人类可读执行计划；JSONL 只列出需要注入或加载的上下文文件。

**查找相关规范**：

```bash
python3 ./.trellis/scripts/get_context.py --mode packages
```

命令列出每个包、规范层及路径，选择与任务领域匹配的条目。

**追加条目**：

可以直接编辑 JSONL，也可以运行：

```bash
python3 ./.trellis/scripts/task.py add-context "$TASK_DIR" implement "<path>" "<原因>"
python3 ./.trellis/scripts/task.py add-context "$TASK_DIR" check "<path>" "<原因>"
```

有真实条目后删除旧 `_example` 示例行（可选，读取方会自动跳过它）。

就绪门槛：`task.py start` 前，`implement.jsonl` 和 `check.jsonl` 各至少有一条真实的 `{"file": "...", "reason": "..."}` 条目。只有 `_example` 行不算就绪。

仅在两个清单都已有整理好的真实条目时跳过本步骤。

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

跳过本步骤。阶段 2 由 `trellis-before-dev` 技能直接加载上下文。

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

#### 1.4 激活任务 `[required · once]`

文档评审通过后，将任务状态改为 `in_progress`：

```bash
python3 ./.trellis/scripts/task.py start <task-dir>
```

轻量任务可以只用 `prd.md`。复杂任务的 `prd.md`、`design.md` 和 `implement.md` 必须齐全且已评审，才能启动。派发子代理的平台还要求 `implement.jsonl` 和 `check.jsonl` 都有真实条目。运行时读取方为了兼容，容许清单缺失或仅有示例，但这种容错不代表规划已经就绪。

命令成功后，状态提示自动切换为 `[workflow-state:in_progress]`，随后执行阶段 2 和 3。

若 `task.py start` 提示会话身份错误（钩子输入、`TRELLIS_CONTEXT_ID` 或平台原生会话环境变量均无上下文键），按报错提示配置会话身份后重试。

#### 1.5 完成标准

| 条件 | 必需 |
|------|:---:|
| `prd.md` 存在 | ✅ |
| 用户确认进入实施 | ✅ |
| 已运行 `task.py start`（status = in_progress） | ✅ |
| `research/` 有研究文档（复杂任务） | 建议 |
| `design.md` 存在（复杂任务） | ✅ |
| `implement.md` 存在（复杂任务） | ✅ |

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

| `implement.jsonl` 和 `check.jsonl` 各至少有一条真实条目（示例行不计） | ✅ |

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

---

## 阶段 2：执行

目标：依据已评审的规划文档实施，并通过质量检查。

#### 2.1 实施 `[required · repeatable]`

[Claude Code, Cursor, OpenCode, codex-sub-agent, CodeBuddy, Droid, Pi, ZCode, Snow, Oh My Pi]

创建实施子代理：

- **代理类型**：`trellis-implement`
- **任务描述**：依据已评审的任务文档实施，查阅 `{TASK_DIR}/research/` 的资料；完成前运行项目 lint 和类型检查
- **派发提示约束**：必须以 `Active task: <task path>` 开头，并告知子代理它已经是 `trellis-implement`，应直接实施，不得再创建 `trellis-implement` / `trellis-check`。

平台钩子或插件自动处理：
- 读取 `implement.jsonl`，将所引用的规范和研究文件注入代理提示
- 注入 `prd.md`，以及存在时的 `design.md` 和 `implement.md`
- Codex 通过 `SubagentStart` 提供原生上下文注入；代理配置保留子代理自行读取作为兜底

[/Claude Code, Cursor, OpenCode, codex-sub-agent, CodeBuddy, Droid, Pi, ZCode, Snow, Oh My Pi]

[Gemini, Qoder, Copilot, Reasonix, Trae, Grok, Kimi Code]

创建实施子代理：

- **代理类型**：`trellis-implement`
- **任务描述**：依据已评审的任务文档实施，查阅 `{TASK_DIR}/research/` 的资料；完成前运行项目 lint 和类型检查
- **派发提示约束**：必须以 `Active task: <task path>` 开头，并明确告知子代理它已经是 `trellis-implement`，应直接实施，不得再创建 `trellis-implement` / `trellis-check`。

采用主动读取方式的子代理定义自动处理上下文加载：
- 用 `task.py current --source` 解析活动任务，再读取 `prd.md` 以及存在时的 `design.md` 和 `implement.md`
- 读取 `implement.jsonl`，要求代理在编码前加载每个引用的规范和研究文件

[/Gemini, Qoder, Copilot, Reasonix, Trae, Grok, Kimi Code]

[Kiro]

创建实施子代理：

- **代理类型**：`trellis-implement`
- **任务描述**：依据已评审的任务文档实施，查阅 `{TASK_DIR}/research/` 的资料；完成前运行项目 lint 和类型检查
- **派发提示约束**：告知子代理它已经是 `trellis-implement`，应直接实施，不得再创建 `trellis-implement` / `trellis-check`。

平台前置上下文自动处理加载：
- 读取 `implement.jsonl`，将所引用的规范和研究文件注入代理提示
- 注入 `prd.md`，以及存在时的 `design.md` 和 `implement.md`

[/Kiro]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

1. 加载 `trellis-before-dev` 技能，读取项目指南
2. 读取 `{TASK_DIR}/prd.md`，再读取存在时的 `design.md` 和 `implement.md`
3. 查阅 `{TASK_DIR}/research/` 的资料
4. 按已评审文档实施代码
5. 运行项目 lint 和类型检查

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

#### 2.2 质量检查 `[required · repeatable]`

[Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

创建检查子代理：

- **代理类型**：`trellis-check`
- **任务描述**：对照规范和任务文档评审所有代码变更；直接修复发现的问题；确保 lint 和类型检查通过
- **派发提示约束**：必须以 `Active task: <task path>` 开头，并告知子代理它已经是 `trellis-check`，应直接评审和修复，不得再创建 `trellis-check` / `trellis-implement`。

检查代理负责：
- 对照规范评审代码变更
- 对照 `prd.md` 以及存在时的 `design.md` 和 `implement.md` 评审代码变更
- 自动修复发现的问题
- 运行 lint 和类型检查进行验证

[/Claude Code, Cursor, OpenCode, codex-sub-agent, Kiro, Gemini, Qoder, CodeBuddy, Copilot, Droid, Pi, Oh My Pi, ZCode, Snow, Reasonix, Trae, Grok, Kimi Code]

[codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

加载 `trellis-check` 技能，按其指导验证代码：
- 规范符合性
- lint、类型检查和测试
- 跨层一致性（变更跨层时）

发现问题后，修复并重新检查，直到通过。

[/codex-inline, Kilo, Antigravity, Devin, DeepSeek Harness]

**最终检查（阶段 3.4 提交前）**：任务的最后一次 2.2 必须覆盖完整变更范围，不能只检查最近一块实施内容。使用 `python3 ./.trellis/scripts/get_context.py --mode packages` 列出全部受影响的包，加载各包规范索引的“质量检查”章节，以发现迭代中局部检查无法覆盖的跨层或跨包问题。

#### 2.3 回退 `[on demand]`

- 检查发现 PRD 缺陷 → 返回阶段 1，修复 `prd.md` 后重做 2.1
- 实施出错 → 回退相关代码，重做 2.1
- 需要更多研究 → 按阶段 1.2 研究，并写入 `research/`

---

## 阶段 3：收尾

目标：确保代码质量、沉淀经验并记录工作。

#### 3.2 调试复盘 `[on demand]`

任务经历反复调试（同一问题多次修复）时，加载 `trellis-break-loop` 技能：
- 分类根因
- 解释此前修复失败的原因
- 提出预防措施

目标是记录调试经验，避免同类问题再次发生。

#### 3.3 规范更新 `[required · once]`

加载 `trellis-update-spec` 技能，判断任务是否产生值得记录的新知识：
- 新发现的模式或约定
- 遇到的陷阱
- 新技术决策

据此更新 `.trellis/spec/` 下的文档。即使结论是“无需更新”，也必须完成这一步判断。

#### 3.4 提交变更 `[required · once]`

**规范同步前置检查**：准备提交前，判断本任务修复的缺陷或发现的隐含知识是否应写入 `.trellis/spec/`，避免后续开发者或 AI 重复踩坑。若需要，先回到阶段 3.3；规范应纳入本任务提交批次，不留作容易忘记的后续事项。

用户已授权在相关检查通过后自动进行本地 Git 提交。由**主会话**统一审阅、暂存和提交本任务变更，无需再次询问是否提交；实施和检查子代理只回报结果。先产生工作提交，再产生归档和日志提交，不交错执行。

**执行步骤**：

1. **检查变更状态**：
   ```bash
   git status --porcelain
   git diff --cached --name-only
   ```
   记录全部变更路径及既有暂存内容。工作区没有变更时跳到 3.5，不创建空提交。

2. **查看近期提交约定**：
   ```bash
   git log --oneline -5
   ```
   沿用 Conventional Commit 类型前缀（`feat:` / `fix:` / `chore:` / `docs:` 等）和合适长度；新提交说明使用中文，真实历史提交说明保持原样。

3. **按任务归属分类**：
   - **本任务变更** — 本会话及其子代理修改、且可依据任务文档说明用途的文件；已有初始化内容只有属于任务范围时才能纳入。
   - **无关或来源不明的变更** — 用户手工修改、先前未完成工作、其他并行任务或无法确定归属的内容；不得默默夹带，也不覆盖或撤销。

4. **形成提交批次并简要说明**：按完整逻辑变更单元分组，不按每个文件单独提交。每批记录中文提交说明和精确文件清单；另外列出保留在工作区的无关或来源不明内容。常规提交无需再次确认；只有确实无法依据已有授权确定某项内容的归属时，才澄清该内容。

5. **检查通过后按顺序提交**：精确执行 `git add -- <本批文件>`，提交时使用显式路径范围（例如 `git commit --only -m "<类型: 中文说明>" -- <本批文件>`），避免把其他任务已经暂存的文件带入。提交前再次核对差异与范围，提交后核对实际路径和哈希。若同一文件混有无关修改，先隔离本任务改动；无法安全隔离时，不将整份文件提交。

6. **处理失败或用户变更指示**：检查失败先修复，无法解决则明确报告阻碍。用户明确要求暂停自动提交或自行提交时，遵循该要求，不继续提交；用户调整说明或分组时按已授权范围修改，不额外制造重复确认。

**规则**：
- 顺序保持为工作提交 → 归档提交 → 日志提交。
- 不执行 `git commit --amend`，不改写历史。
- 此授权不包含向远程推送，也不允许跳过检查。
- 不把凭据、运行缓存、无关已暂存或未暂存工作纳入提交。
- 主会话在最终报告中提供实际提交哈希、检查结果及剩余工作区状态。

#### 3.5 自主收尾

完成上述步骤后，主会话继续执行 `trellis-finish-work`（平台提供时使用 `/trellis:finish-work`），归档当前已完成任务并记录会话，无需再次询问是否执行已授权的收尾。其他不属于本会话的任务仍遵循该技能的单独确认规则。

---

## 定制 Trellis（分支与本地项目）

本节面向需要修改 Trellis 工作流的开发者。流程文字在本文件维护，脚本只负责解析；改变可见标题等解析边界时，须同步相应消费者。本项目的长期中文和提交约定见 `.trellis/spec/trellis-localization.md`。

### 修改步骤含义

编辑上方阶段 1 / 2 / 3 对应步骤的详细正文。关键不变量：
- 没有活动任务时，先分类并取得创建任务授权，再创建 Trellis 任务。
- 规划必须区分仅需 PRD 的轻量任务，以及启动前需要 `prd.md`、`design.md`、`implement.md` 的复杂任务。
- 所有必经执行路径都必须在 `/trellis:finish-work` 之前经过阶段 3.4 的主会话提交。

所有状态块都位于上方 `## 阶段索引`，紧随各阶段摘要：

| 范围 | 对应标记 |
|---|---|
| 无活动任务（阶段 1 之前） | `[workflow-state:no_task]`（阶段索引流程图之后） |
| 活动任务记录不可读 | `[workflow-state:task_error]`（修复已有任务后再继续） |
| 整个阶段 1（创建任务至实施就绪） | `[workflow-state:planning]`（阶段 1 摘要之后） |
| Codex 内联模式阶段 1 | `[workflow-state:planning-inline]` |
| 阶段 2 和阶段 3.2–3.4（实施、检查、收尾） | `[workflow-state:in_progress]`（阶段 2 摘要之后） |
| Codex 内联模式阶段 2 和阶段 3.2–3.4 | `[workflow-state:in_progress-inline]` |
| 阶段 3.5 之后（已归档） | `[workflow-state:completed]`（阶段 3 摘要之后；**当前不可达**） |

### 修改每轮提示正文

直接编辑对应 `[workflow-state:STATUS]` 块。模板维护者可运行 `trellis update` 分发更新；定制本项目时重启 AI 会话即可使用新正文，不需修改解析脚本。可见标题或格式改变仍须检查解析兼容性。本任务不升级全局 Trellis。

### 添加自定义状态

添加一个块：

```
[workflow-state:my-status]
此处填写每轮提示正文
[/workflow-state:my-status]
```

约束：
- STATUS 字符集为 `[A-Za-z0-9_-]+`（可用下划线和连字符，例如 `in-review`、`blocked-by-team`）
- 必须由生命周期钩子将 `task.json.status` 写成该自定义值，否则永远不会读取这个块
- 生命周期钩子位于 `task.json.hooks.after_*`，绑定到 `after_create / after_start / after_finish / after_archive` 之一

### 添加生命周期钩子

在 `task.json` 添加 `hooks` 字段：

```json
{
  "hooks": {
    "after_finish": [
      "your-script-or-command-here"
    ]
  }
}
```

支持事件：`after_create / after_start / after_finish / after_archive`。注意 `after_finish` 不等于状态改变，它只清除活动任务指针；发送“任务已完成”通知应使用 `after_archive`。

### 完整契约

工作流状态机的运行契约、状态写入位置、伪状态（`no_task` / `stale_<source_type>`）、钩子可达性矩阵等详细信息，可按以下归属查阅：

- Trellis 上游仓库的 `.trellis/spec/cli/backend/workflow-state-contract.md` — 上游运行契约、写入点表和测试不变量；本项目没有该文件。
- `.codex/hooks/inject-workflow-state.py` — 本项目实际解析器，只读取 workflow.md，不内置提示正文。
- `.trellis/spec/trellis-localization.md` — 本地中文定制、兼容性和升级保留约定。
