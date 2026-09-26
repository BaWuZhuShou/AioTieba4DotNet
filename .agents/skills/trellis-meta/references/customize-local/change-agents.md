# 修改本地代理

用户希望改变 `trellis-research`、`trellis-implement` 或 `trellis-check` 的行为时，应编辑用户项目中的平台代理文件。

## 先读取这些文件

1. 目标平台的代理目录
2. `.trellis/workflow.md` 的阶段 2 / 研究路由
3. 当前任务的 `prd.md`
4. 当前任务的 `implement.jsonl` / `check.jsonl`
5. 相关钩子或代理前置指令

## 常见路径

| 平台 | 路径 |
| --- | --- |
| Claude Code | `.claude/agents/trellis-*.md` |
| Cursor | `.cursor/agents/trellis-*.md` |
| OpenCode | `.opencode/agents/trellis-*.md` |
| Codex | `.codex/agents/trellis-*.toml` |
| Kiro | `.kiro/agents/trellis-*.json` |
| Gemini CLI | `.gemini/agents/trellis-*.md` |
| Qoder | `.qoder/agents/trellis-*.md` |
| CodeBuddy | `.codebuddy/agents/trellis-*.md` |
| Factory Droid | `.factory/droids/trellis-*.md` |
| Pi Agent | `.pi/agents/trellis-*.md` |
| Reasonix | `.reasonix/skills/trellis-*/SKILL.md`（子代理 frontmatter） |
| ZCode | `.zcode/agents/trellis-*.md` |

以用户项目中的实际路径为准。

## 常见需求

| 需求 | 应编辑的代理 |
| --- | --- |
| 研究必须写入文件，不能只在聊天中回复 | `trellis-research` |
| 实施前必须读取某些本地规范 | `trellis-implement` + `implement.jsonl` 配置规则 |
| 检查时必须执行特定命令 | `trellis-check` |
| 代理不得修改某些目录 | 对应代理的写入边界说明 |
| 代理输出格式必须固定 | 对应代理的最终回复/报告说明 |

## 修改原则

1. **保留角色边界**：研究代理调查并保存结果；实施代理编写实现；检查代理审查并修复。
2. **不要将项目规范硬编码进代理**：长期规范属于 `.trellis/spec/`；代理负责读取这些规范。
3. **明确读取顺序**：实施和检查角色按派发提示的活动任务路径 → 对应 JSONL 及规范/研究 → `prd.md` → `design.md`（如存在）→ `implement.md`（如存在）读取。研究角色使用研究专属上下文，不加载 implement/check 清单。
4. **明确写入边界**：哪些目录可写，哪些不可写。
5. **跨平台同步**：用户配置了多个平台时，判断只改当前平台还是修改所有平台的代理。

## 代理主动读取的平台

如果代理文件包含“启动后读取任务/上下文”的前置步骤，编辑时不要删除。否则代理只会依赖聊天上下文，绕过 Trellis 的核心机制。

## 钩子推送的平台

即使上下文由钩子注入，代理文件也应保留职责边界。不要因为钩子会注入上下文，就移除代理中的 PRD/规范要求。
