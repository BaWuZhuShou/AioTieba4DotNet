---
name: trellis-start
description: "初始化 AI 开发会话，读取 .trellis/ 中的工作流、开发者身份、Git 状态、活动任务及项目指南；对请求分类并路由到需求探索、直接修改或任务流程。用于开始开发会话、恢复工作、启动新任务或重新建立项目上下文。"
---

# 开始会话

初始化由 Trellis 管理的开发会话。当前平台没有会话启动钩子，因此按以下步骤手动加载等效的精简上下文。

---

## 步骤 1：当前状态

获取身份、Git 状态、当前任务、活动任务和日志位置。

```bash
python3 ./.trellis/scripts/get_context.py
```

若输出包含以 `发现 Trellis 更新：` 开头的行，汇总会话上下文时完整原样复制该行。旧版或外部 CLI 的 `Trellis update available:` 提示也按此处理，不缩短操作命令提示。

## 步骤 2：工作流概览

读取精简阶段索引、请求分类规则、规划文档契约和步骤详情命令。

```bash
python3 ./.trellis/scripts/get_context.py --mode phase
```

完整指南位于 `.trellis/workflow.md`，按需阅读。

## 步骤 3：指南索引

发现包和规范层，再阅读相关索引文件。

```bash
python3 ./.trellis/scripts/get_context.py --mode packages
cat .trellis/spec/guides/index.md
cat .trellis/spec/<package>/<layer>/index.md   # 对每个相关层执行
```

索引列出实际开始编码时需要读取的具体指南。

## 步骤 4：决定下一步

步骤 1 已提供当前任务和状态。检查任务目录：

- **活动任务状态为 `planning`，没有 `prd.md`** → 阶段 1.1，加载 `trellis-brainstorm` 技能。
- **活动任务状态为 `planning`，存在 `prd.md`** → 留在阶段 1。轻量任务可以只有 PRD；复杂任务需要 `design.md` 和 `implement.md`。执行 `task.py start` 前加载相应阶段 1 步骤详情。
- **活动任务状态为 `in_progress`** → 阶段 2 的步骤 2.1。加载详情：
  ```bash
  python3 ./.trellis/scripts/get_context.py --mode phase --step 2.1 --platform codex
  ```
- **没有活动任务** → 先分类。简单对话或小任务，只询问本轮是否创建 Trellis 任务；复杂任务，询问是否可以创建任务并进入规划。用户拒绝时，本会话跳过 Trellis。

---

## 技能路由速查

| 用户意图 | 技能 |
|---|---|
| 新功能或需求不清楚 | `trellis-brainstorm` |
| 即将编写代码 | `trellis-before-dev` |
| 编码完成或质量检查 | `trellis-check` |
| 遇到阻碍或同一缺陷反复修复 | `trellis-break-loop` |
| 学到值得记录的知识 | `trellis-update-spec` |

完整规则及约束见 `.trellis/workflow.md`。
