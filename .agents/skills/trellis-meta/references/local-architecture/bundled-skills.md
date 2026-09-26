# 捆绑技能

“捆绑技能”是随 Trellis CLI npm 包发布的多文件内置技能。与用户单独安装到 `.claude/skills/` 或其他平台技能根目录的市场技能不同，捆绑技能由 `trellis init` 自动写入所有支持平台的技能根目录，并由 `trellis update` 保持同步。它们属于 Trellis 本身，不是第三方内容。

捆绑技能在 **Trellis 上游源码**中位于 `packages/cli/src/templates/common/bundled-skills/<skill>/`，该目录包含自有的 `SKILL.md`（带 YAML frontmatter），以及可选的 `references/`、资源或其他支持文件。Trellis 将整个目录树原样复制到各平台技能根目录，使引用资料保持按需加载，无需压成一个过大的 `SKILL.md`。

## 捆绑技能与相邻概念的区别

下表的 `templates/` 路径均指 Trellis 上游模板源码。

| 源路径 | 类型 | 分发方式 |
| --- | --- | --- |
| `templates/common/bundled-skills/<name>/` | 捆绑技能（多文件） | 将整个目录复制到每个平台技能根目录 |
| `templates/common/skills/<name>.md` | 单文件工作流技能 | 添加 frontmatter，写为 `<root>/<name>/SKILL.md` |
| `templates/common/commands/<name>.md` | 斜杠命令 / 提示 | 写入各平台命令目录（`.claude/commands/trellis/`、`.cursor/commands/trellis-*.md`、`.gemini/commands/trellis/*.toml` 等） |
| `templates/<platform>/skills/` | 平台专用技能 | 只写入对应平台目录（例如 `.codex/skills/`） |
| `.claude/skills/<my-skill>/` 等位置的用户技能 | 市场技能或用户自编技能 | 不由 Trellis 管理 |

Trellis CLI 不会触碰自身模板加载器未生成的内容。用户手动放进平台技能根目录的文件会保持原样。

## 当前捆绑技能（v0.6.0）

运行时通过列出 `templates/common/bundled-skills/` 下的目录发现技能集合：

| 技能 | 用途 |
| --- | --- |
| `trellis-meta` | 本技能。向用户项目中的 AI 解释本地 Trellis 架构与定制入口。 |
| `trellis-session-insight` | 封装 `trellis mem` CLI，让 AI 知道何时、如何查阅过去的 Claude Code / Codex / Pi Agent 对话日志。 |
| `trellis-spec-bootstrap` | 根据实际代码创建或刷新 `.trellis/spec/` 的平台无关工作流，可选集成 GitNexus / ABCoder。 |
| `trellis-channel` | 能力技能，指导 AI 何时使用 `trellis channel` 进行多代理协作、论坛/主题持久看板和调度者等待。 |

列表在运行时发现，因此只需在 `bundled-skills/` 下添加新目录即可注册技能（见下文“添加新的捆绑技能”）。

## 各平台的捆绑技能落点

一个平台的完整文件集——命令、工作流技能、代理、钩子、捆绑技能——只在上游 `packages/cli/src/configurators/<platform>.ts` 的 `collect<Platform>Templates()` 中描述一次。捆绑技能的描述涉及两个调用：`resolveBundledSkills(ctx)` 读取 `templates/common/bundled-skills/` 下的所有目录、解析占位符并返回展平的 `{relativePath, content}` 条目列表；`collectSkillTemplates(<skillsRoot>, <workflowSkills>, <bundledSkills>)` 将其合并到平台的 `Map<filePath, content>`，路径为 `<skillsRoot>/<skill>/<relativePath>`。

全部 21 个平台都会收到完整捆绑技能集合：

| 平台 | 捆绑技能根目录 |
| --- | --- |
| Claude Code | `.claude/skills/<skill>/` |
| Cursor | `.cursor/skills/<skill>/` |
| OpenCode | `.opencode/skills/<skill>/` |
| Codex | `.agents/skills/<skill>/` |
| Gemini CLI | `.agents/skills/<skill>/` |
| Pi | `.agents/skills/<skill>/` |
| Kimi | `.agents/skills/<skill>/` |
| Kilo | `.kilocode/skills/<skill>/` |
| Kiro | `.kiro/skills/<skill>/` |
| Antigravity | `.agent/skills/<skill>/` |
| Devin | `.devin/skills/<skill>/` |
| Qoder | `.qoder/skills/<skill>/` |
| Codebuddy | `.codebuddy/skills/<skill>/` |
| Copilot | `.github/skills/<skill>/` |
| Droid | `.factory/skills/<skill>/` |
| Reasonix | `.reasonix/skills/<skill>/` |
| ZCode | `.zcode/skills/<skill>/` |
| Trae | `.trae/skills/<skill>/` |
| OMP | `.omp/skills/<skill>/` |
| Grok | `.grok/skills/<skill>/` |
| Snow | `.snow/skills/<skill>/` |

Codex、Gemini CLI、Pi 和 Kimi 共享 `.agents/skills/` 根目录（上游 Agent Skills 工作区别名）。凡是多个收集器会写入此处的同一路径，内容必须逐字节一致。

一份描述由两个消费者使用：

1. `trellis init` → `configurePlatform(platformId, cwd)` → `writeTemplateMap(cwd, collect<Platform>Templates())`。21 个平台中有 18 个在 `configurators/index.ts` 的注册项直接使用 `fromTemplates(collect<Platform>Templates)`，就是上述组合。Claude Code、Codex 和 ZCode 各自显式定义 `configure`，处理 `Map<path, content>` 无法表达的工作（可选 `--with-statusline` 参数、刻意保留为空的 `.codex/skills/` 目录、一次性控制台提示）；它们都不会重复声明文件列表。
2. `trellis update` → `collectPlatformTemplates(platformId)`（位于 `configurators/index.ts`）→ 同一个映射，用于检测偏离并填充 `.trellis/.template-hashes.json`。

两个消费者读取同一份描述，因此 init 与 update 不会对捆绑技能生成哪些文件产生分歧。

## 分发接线（代码路径）

自动将捆绑技能分发到平台技能根目录的机制位于两个上游文件：

1. `packages/cli/src/templates/common/index.ts`
   - `listDirectories("bundled-skills")` 枚举磁盘上的技能。
   - `listBundledSkillFiles(skillDir)` 递归遍历各技能目录，为每个文件返回 `{relativePath, content}`。
   - `getBundledSkillTemplates()` 返回缓存的 `CommonBundledSkill[]`。

2. `packages/cli/src/configurators/shared.ts`
   - `resolveBundledSkills(ctx)` 将列表展平为 `ResolvedSkillFile[]`，使用 `<skill>/<relativePath>` 路径并解析占位符。
   - `collectSkillTemplates(skillsRoot, workflowSkills, bundledSkills)` 将工作流技能和捆绑技能文件一起返回为以 `skillsRoot` 为根的 `Map<filePath, content>`。
   - `writeTemplateMap(cwd, files)` 是将收集到的映射写入磁盘的唯一写入器。

所有支持技能的平台都会从各自 `collect<Platform>Templates()` 调用这两个辅助函数：有的直接调用（`claude.ts`、`codex.ts`、`copilot.ts`、`gemini.ts`、`grok.ts`、`kimi.ts`、`kiro.ts`、`omp.ts`、`opencode.ts`、`pi.ts`、`reasonix.ts`、`snow.ts`、`zcode.ts`）；有的通过 `shared.ts` 中的 `collectBothTemplates(ctx, cmdPath, skillRoot)` 调用。后者代替同时拥有命令目录和技能根目录的平台执行相同两次调用（`antigravity.ts`、`codebuddy.ts`、`cursor.ts`、`devin.ts`、`droid.ts`、`kilo.ts`、`qoder.ts`、`trae.ts`）。

## 添加新的捆绑技能

目录结构与分发接线已经通用化，因此向 Trellis 上游添加技能只需修改文件并验证分发。

1. **创建目录树。**

   ```
   packages/cli/src/templates/common/bundled-skills/<my-skill>/
     SKILL.md                     # YAML frontmatter + 正文
     references/                  # 可选
       <topic>.md
     assets/                      # 可选（任何可按 UTF-8 读取的内容）
   ```

2. **编写有效的 `SKILL.md` 头部。** frontmatter 至少应包含：

   ```yaml
   ---
   name: <my-skill>
   description: "AI 何时应使用此技能；在此写明触发表述。"
   ---
   ```

   各平台的自动触发机制匹配 `description`，因此应描述用户意图触发条件，而非技能内部实现。

3. **适当使用占位符。** 捆绑技能内容会经过 `resolvePlaceholders(file.content, ctx)`。`resolvePlaceholders` 支持的 `{{platform_name}}`、`{{python_cmd}}` 等标记都会按平台替换。

4. **无需添加分发接线。** `listDirectories("bundled-skills")` 自动发现新目录，所有平台会在下次 `trellis init` 或 `trellis update` 时收到它。

5. **发布前验证分发路径。** 历史上跳过以下任一步骤，曾导致文档声称已捆绑某功能，而发布的 npm 压缩包实际缺少文件：

   - 待打标签的分支上存在源文件。
   - `pnpm --filter @mindfoldhq/trellis build` 将资源复制到 `dist/templates/common/bundled-skills/<skill>/`。
   - `npm pack --dry-run --json` 包含预期的 `dist/**` 路径。
   - 在全新临时项目中，`trellis init` 写入 `.claude/skills/<skill>/SKILL.md`、`.agents/skills/<skill>/SKILL.md`、`.zcode/skills/<skill>/SKILL.md` 等。
   - `.trellis/.template-hashes.json` 列出生成文件。
   - 在该临时项目中，`trellis update --dry-run` 报告 `Already up to date!`（已是最新）。

6. **添加迁移清单条目**：如果其他项目会升级到新增该技能的版本，应添加明确迁移条目。没有条目，文件仍会通过 `trellis update` 的标准“文件缺失”分支落地；但迁移清单可以让变更出现在更新日志中。

## 在本地覆盖捆绑技能

没有正式的“项目本地技能”专用机制（例如 `.trellis/skills/`）。捆绑技能以平台目录为根，因此覆盖也以平台目录为根。

支持的模式依赖 `trellis update` 现有的模板哈希比较：

1. 直接编辑本地文件，例如 `.claude/skills/trellis-meta/SKILL.md`。
2. 文件哈希与 `.trellis/.template-hashes.json` 中的条目不同。
3. 下次 `trellis update` 检测到用户修改后，保持文件不变（未显式使用 `--force` 时，Trellis 不会覆盖用户修改的文件）。

注意：

- 覆盖仅适用于被编辑目录所属的平台。例如要同时覆盖 Claude Code 与 Codex 的同一技能，必须同时编辑 `.claude/skills/<name>/` 和 `.agents/skills/<name>/`。
- 未来执行 `trellis update --force` 会覆盖本地编辑。应将覆盖内容纳入版本控制，方便需要时重新应用。
- 同一平台技能根目录下、使用不同目录名安装的市场技能（如 `.claude/skills/my-custom-meta/`）不会被 Trellis 修改。目标是增加行为而非修改捆绑技能时，这是更清晰的方式。
- 团队私有约定属于 `.trellis/spec/` 或独立的市场式本地技能，不应写进 `trellis-meta` 自身的修改。见 `customize-local/add-project-local-conventions.md`。

## 从项目中移除捆绑技能

没有按项目停用捆绑技能的参数。有两种选择：

1. **删除各平台技能根目录中的对应目录。** `trellis update` 会发现文件缺失，与 `.template-hashes.json` 比较，并将删除视为普通用户修改；除非传入 `--force`，否则不会静默重建目录。

2. **固定到未包含该技能的 Trellis 版本。** 捆绑技能集合在构建时确定，因此安装较早的 CLI 版本，是永久排除当前版本所带技能的唯一方式。

不支持第三种选择，即全局禁用所有捆绑技能。分发是无条件的：`collect<Platform>Templates()` 不接收参数，因此没有传入开关的位置。如需增加，必须修改全部 21 个平台的函数签名，以及 `configurators/index.ts` 中的 `collectPlatformTemplates`。

## 操作规则

- 以 Trellis 上游 `templates/common/bundled-skills/` 为捆绑技能集合的唯一权威源。不要手工维护逐平台技能列表。
- 不要在捆绑 `SKILL.md` 中添加平台专用逻辑。平台专用行为应放入 `templates/<platform>/skills/`。
- 不要让捆绑技能依赖某个特定 CLI 命令（如 `trellis mem`）却不在 description 与参考资料中说明依赖；旧版本用户可能没有该命令。
- 不要在捆绑技能中保存项目私有内容。捆绑技能是面向所有用户发布的公共内容；项目规则属于 `.trellis/spec/` 或本地技能。
