# 添加项目本地约定

用户通常不需要改变 Trellis 的机制，而是希望本地 AI 理解团队约定。这时应优先使用 `.trellis/spec/` 或项目本地技能，不要编辑 `trellis-meta`。

## 内容放在哪里

| 内容类型 | 位置 |
| --- | --- |
| 代码必须遵守的规则 | `.trellis/spec/<layer>/` |
| 跨层思考方法 | `.trellis/spec/guides/` |
| 项目特定流程所需的 AI 能力 | 平台本地技能 |
| 一次性任务资料 | `.trellis/tasks/<task>/` |
| 会话摘要 | `.trellis/workspace/<developer>/journal-N.md` |

## 创建项目本地技能

如果用户希望 AI 了解“本项目如何定制 Trellis”，可创建本地技能：

```text
.claude/skills/trellis-local/
└── SKILL.md
```

示例：

```md
---
name: trellis-local
description: "本仓库的项目本地 Trellis 定制。修改本项目的 Trellis 工作流、钩子、本地代理或团队专属约定时使用。"
---

# Trellis 本地定制

## 本地范围

本技能仅记录本仓库的 Trellis 定制。

## 自定义工作流规则

- ...

## 本地钩子变更

- ...

## 本地代理变更

- ...
```

对于多平台项目，在其他平台技能目录中放置等价版本；支持共享层的平台也可使用 `.agents/skills/`。

## 写入 `.trellis/spec/`

如果内容属于编码约定，应写入规范。例如：

```text
.trellis/spec/backend/error-handling.md
.trellis/spec/frontend/components.md
.trellis/spec/guides/cross-platform-thinking-guide.md
```

写完后更新对应的 `index.md`，让 AI 能从入口找到新规则。

## 让当前任务使用新约定

规范写好后，将其添加到当前任务的上下文：

```bash
python3 ./.trellis/scripts/task.py add-context <task> implement ".trellis/spec/backend/error-handling.md" "错误处理约定"
python3 ./.trellis/scripts/task.py add-context <task> check ".trellis/spec/backend/error-handling.md" "审查错误处理"
```

## 不要把项目私有规则放进 `trellis-meta`

`trellis-meta` 是用于理解 Trellis 架构和本地定制入口的公共技能。项目私有内容应放在：

- `.trellis/spec/`
- 项目本地技能
- 当前任务
- 工作区日志

这样可以避免未来更新 Trellis 内置 `trellis-meta` 时覆盖团队自身的约定。
