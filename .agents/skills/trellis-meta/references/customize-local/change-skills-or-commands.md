# 修改本地技能、命令、提示与工作流

用户希望改变 AI 入口、自动触发规则或显式命令行为时，应编辑本地平台目录中的技能、命令、提示或工作流。

编辑前，先判断将要修改的技能属于哪一类：

- **上游捆绑技能**——`trellis-meta`、`trellis-spec-bootstrap`、`trellis-session-insight`、`trellis-channel`。权威源位于 Trellis CLI 仓库的 `packages/cli/src/templates/common/bundled-skills/<name>/`；`trellis init` / `trellis update` 时由 `getBundledSkillTemplates()` 自动分发到每个平台的技能根目录。本地修改由 `.trellis/.template-hashes.json` 跟踪，并会在下次更新时提示。
- **项目本地技能**——`.{platform}/skills/` 下的其他技能。由用户拥有；不会被 `trellis update` 刷新。

下文中的“技能”均指本地文件；这两类文件的覆盖与冲突规则不同。

## 先读取这些文件

1. `.trellis/workflow.md`
2. 目标平台的技能/命令/提示/工作流目录
3. 相关代理或钩子文件
4. 检查 `.trellis/spec/` 中是否已有项目规则
5. `.trellis/.template-hashes.json`——确认待编辑技能是上游所有（有条目）还是项目本地（无条目）

## 选择哪种入口

| 目标 | 建议 |
| --- | --- |
| AI 应自动了解某项能力 | 添加或修改技能。 |
| 用户希望通过命令手动触发 | 添加或修改命令/提示/工作流。 |
| 团队项目约定 | 优先放入 `.trellis/spec/` 或项目本地技能，绝不要放进捆绑技能目录。 |
| 为用户自己的项目微调捆绑技能（如 `trellis-meta`） | 创建名称不同的同级项目本地技能以覆盖意图，或编辑 `.trellis/spec/`。捆绑技能目录中的修改只能保留到下一次 `trellis update`，每次都需要选择“保留”。 |
| 将变更贡献到上游 | 编辑 Trellis CLI 仓库中的 `packages/cli/src/templates/common/bundled-skills/<name>/`，而不是已部署副本。 |
| 改变 Trellis 流程语义 | 同步 `.trellis/workflow.md`。 |

## 修改技能

技能通常采用以下结构：

```text
<skill-name>/
├── SKILL.md
└── references/
```

`SKILL.md` 应简短，负责触发和路由。较长内容放进 `references/`，供 AI 按需读取。

frontmatter 的 description 应说明何时使用该技能。例如：

```yaml
description: "定制本项目的部署流程和发布检查清单时使用。"
```

不要使用“有用的项目技能”等含糊描述，这类描述容易误触发。

### 捆绑技能与项目本地技能

相同的目录结构对应两种完全不同的所有权模型：

| 方面 | 捆绑技能（`trellis-meta`、`trellis-spec-bootstrap`、`trellis-session-insight`、`trellis-channel`） | 项目本地技能 |
| --- | --- | --- |
| 权威源 | Trellis CLI 仓库中的 `packages/cli/src/templates/common/bundled-skills/<name>/` | 用户项目内部 |
| 分发 | `trellis init` / `trellis update` 时，由 `getBundledSkillTemplates()`（`packages/cli/src/templates/common/index.ts`）自动分发到所有平台技能根目录 | 由用户（或其他技能）创建，不会被移动 |
| 哈希跟踪 | 每个文件都记录在 `.trellis/.template-hashes.json`；更新时提示冲突 | 不跟踪 |
| 本地编辑 | 允许，但下次更新会标记为“用户已修改” | 可自由编辑 |
| 合适的定制方式 | 新增一个名称不同的项目本地技能，补充（或替代）捆绑技能 | 直接编辑文件 |

如果目标是“让本项目的 AI 在讨论发布说明时采用不同做法”，通常应使用项目本地技能，而不是修改 `trellis-meta/`。

## 修改命令/提示/工作流

显式入口应说明：

- 用户如何触发。
- 读取哪些 `.trellis/` 文件。
- 执行哪些脚本。
- 完成后如何报告。

如果命令只是重复工作流规则，应让它引用/读取 `.trellis/workflow.md`，避免维护第二份流程。

## 常见路径

| 平台 | 入口目录 |
| --- | --- |
| Claude Code | `.claude/skills/`、`.claude/commands/` |
| Cursor | `.cursor/skills/`、`.cursor/commands/` |
| OpenCode | `.opencode/skills/`、`.opencode/commands/` |
| Codex | `.agents/skills/`、`.codex/skills/` |
| Gemini CLI | `.agents/skills/`、`.gemini/commands/` |
| Kiro | `.kiro/skills/` |
| Qoder | `.qoder/skills/`、`.qoder/commands/` |
| CodeBuddy | `.codebuddy/skills/`、`.codebuddy/commands/` |
| GitHub Copilot | `.github/skills/`、`.github/prompts/` |
| Factory Droid | `.factory/skills/`、`.factory/commands/` |
| Pi Agent | `.agents/skills/` |
| Reasonix | `.reasonix/skills/`（无独立命令目录；斜杠命令内置于平台） |
| ZCode | `.zcode/skills/`、`.zcode/commands/` |
| Kilo / Antigravity / Devin | 工作流 + 技能 |

上述平台目录体系都是四个捆绑技能的部署目标。每个平台都会在 `trellis init` 时收到完整副本，在 `trellis update` 时刷新；无需手动接线。

## 添加项目本地技能

用户希望记录团队私有定制时，应创建项目本地技能。不要把项目私有内容放进捆绑技能目录，因为 `trellis update` 会覆盖它。

```text
.claude/skills/project-trellis-local/
└── SKILL.md
```

多平台项目应在各平台技能目录中添加等价版本，或在支持共享层的平台（Codex、Gemini CLI）使用 `.agents/skills/`。

名称不能与以下捆绑技能冲突：

- `trellis-meta`
- `trellis-spec-bootstrap`
- `trellis-session-insight`
- `trellis-channel`

重名会导致 `getBundledSkillTemplates()` 在下次更新时覆盖项目本地副本。常见约定是加上项目名前缀，例如 `acme-trellis-deploy`、`acme-trellis-onboarding`。

## 注意事项

- 不要把所有平台的语法混进同一个文件。
- 不要只改一个平台入口却声称支持所有平台。
- 不要把长期工程约定藏在命令中；应写入 `.trellis/spec/`。
- 不要手动编辑任何 `.{platform}/skills/` 目录下的 `trellis-meta/`、`trellis-spec-bootstrap/`、`trellis-session-insight/` 或 `trellis-channel/` 后就期望变更永久保留；这些都是捆绑技能，会被 `trellis update` 刷新。应贡献到上游，或新增项目本地技能进行补充。
- `trellis update` 对捆绑技能文件报告“用户已修改”（原始提示 `modified by you`）冲突后，只有愿意手动维护差异时才选择 **keep**；否则接受覆盖，并以项目本地技能重新表达定制意图。
