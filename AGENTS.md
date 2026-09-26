<!-- TRELLIS:START -->
# Trellis 项目指引

本指引用于在本项目工作的 AI 助手。

项目由 Trellis 管理，工作所需知识位于 `.trellis/`：

- `.trellis/workflow.md` — 开发阶段、创建任务的时机和技能路由
- `.trellis/spec/` — 按包和层组织的编码指南（修改对应层前必须阅读）
- `.trellis/workspace/` — 每位开发者的日志和会话轨迹
- `.trellis/tasks/` — 活动及归档任务（PRD、研究和 JSONL 上下文）

如果当前平台提供 Trellis 命令（例如 `/trellis:finish-work`、`/trellis:continue`），优先使用命令而不是手动操作。并非所有平台都提供全部命令。

使用 Codex 或其他支持代理的工具时，项目级辅助内容还可能位于：
- `.agents/skills/` — 可复用的 Trellis 技能
- `.codex/agents/` — 可选的自定义子代理

本区块由 Trellis 管理。区块外的修改会保留；区块内的修改可能被后续 `trellis update` 覆盖。

<!-- TRELLIS:END -->

## 本项目长期约定

- Trellis 人类可读内容统一使用简体中文，包括需求、设计、执行计划、研究、规范、提示和会话记录。命令、路径、代码标识、配置键、状态枚举、协议标记及真实历史证据保留原样。维护与升级规则见 [.trellis/spec/trellis-localization.md](.trellis/spec/trellis-localization.md)。
- AioTieba4DotNet 的核心定位是参考并对齐 [aiotieba Python 版](https://github.com/lumina37/aiotieba) 的 C#/.NET 实现，便于 C# 用户在不同宿主和使用场景中调用。功能语义、上游固定基线、.NET 使用习惯及验证边界以 [.trellis/spec/project-positioning.md](.trellis/spec/project-positioning.md) 为准；目标不能当作已经完整对齐的证明。
- 用户已授权主会话在相关检查通过后，按任务范围自主创建本地 Git 提交，无需再次确认提交。提交说明使用 Conventional Commit 类型前缀和中文说明；子代理只回报，由主会话统一提交。先工作提交，再归档和会话记录提交，不夹带无关修改、凭据或缓存。该授权不包含远程推送、历史改写或跳过检查，具体顺序见工作流阶段 3.4。
