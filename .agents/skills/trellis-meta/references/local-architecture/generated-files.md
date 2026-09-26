# 初始化后生成的本地文件

`trellis init` 将 Trellis 运行脚本写入用户项目。之后，`trellis update` 尝试更新 Trellis 托管的模板文件，并使用 `.trellis/.template-hashes.json` 判断哪些文件已被用户修改。

本页仅说明用户项目中可见、可编辑的文件。

## `.trellis/`

```text
.trellis/
├── workflow.md
├── config.yaml
├── .developer
├── .version
├── .template-hashes.json
├── .runtime/
├── scripts/
├── spec/
├── tasks/
└── workspace/
```

| 路径 | 通常可编辑？ | 说明 |
| --- | --- | --- |
| `.trellis/workflow.md` | 是 | 本地工作流文档与 AI 路由规则。 |
| `.trellis/config.yaml` | 是 | 项目配置、钩子、包、日志行数限制及相关设置。 |
| `.trellis/spec/` | 是 | 项目规范，供用户与 AI 定期更新。 |
| `.trellis/tasks/` | 是 | 任务资料与研究产物，由任务工作流维护。 |
| `.trellis/workspace/` | 是 | 会话记录，通常由 `add_session.py` 写入。 |
| `.trellis/scripts/` | 谨慎编辑 | 本地运行脚本。可定制，但应先理解调用链。 |
| `.trellis/.runtime/` | 否 | 运行状态，通常由钩子/脚本自动写入。 |
| `.trellis/.developer` | 谨慎编辑 | 当前开发者身份。 |
| `.trellis/.version` | 否 | 更新/迁移逻辑使用的 Trellis 版本记录。 |
| `.trellis/.template-hashes.json` | 否 | 模板哈希记录。不要在此手写业务规则。 |

## 平台目录

不同平台生成不同目录。常见类别如下：

| 类别 | 示例路径 | 用途 |
| --- | --- | --- |
| 钩子 | `.claude/hooks/`、`.codex/hooks/`、`.cursor/hooks/` | 注入会话上下文、工作流状态和子代理上下文。 |
| 设置 | `.claude/settings.json`、`.codex/hooks.json`、`.qoder/settings.json`、`.trae/hooks.json` | 告知平台何时运行钩子或插件。 |
| 代理 | `.claude/agents/`、`.codex/agents/`、`.kiro/agents/`、`.zcode/agents/` | 定义 `trellis-research`、`trellis-implement`、`trellis-check` 等代理。 |
| 技能 | `.claude/skills/`、`.agents/skills/`、`.qoder/skills/`、`.zcode/skills/` | 自动触发或可供 AI 读取的技能。 |
| 命令/提示/工作流 | `.cursor/commands/`、`.github/prompts/`、`.devin/workflows/`、`.zcode/commands/` | 用户显式调用的命令或工作流入口。 |

修改平台目录时，也应确认 `.trellis/workflow.md` 是否仍描述相同流程。

## 模板哈希的含义

`.trellis/.template-hashes.json` 记录 Trellis 上次写入模板文件时的内容哈希。`trellis update` 据此区分三种情况：

| 情况 | 更新行为 |
| --- | --- |
| 文件未被用户修改 | 可自动更新。 |
| 文件已被用户修改 | 提示用户覆盖、保留或生成 `.new`。 |
| 文件已不属于当前模板 | 根据迁移规则删除、重命名或保留。 |

AI 定制本地 Trellis 文件时，无需手动维护哈希。Trellis 更新将结果识别为“用户已修改”是正常行为。

## 本地定制边界

默认可编辑：

- `.trellis/workflow.md`
- `.trellis/config.yaml`
- `.trellis/spec/**`
- `.trellis/scripts/**`
- 平台钩子、设置、代理、技能、命令、提示和工作流

默认不编辑：

- 全局 npm 安装目录
- `node_modules/@mindfoldhq/trellis`
- Trellis GitHub 仓库源码
- `.trellis/.runtime/**` 下的具体状态文件
- `.trellis/.template-hashes.json` 中的哈希内容

只有用户明确希望向上游贡献时，才切换到 Trellis CLI 源码视角。
