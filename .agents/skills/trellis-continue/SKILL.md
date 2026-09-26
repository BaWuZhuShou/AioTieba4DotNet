---
name: trellis-continue
description: "继续当前任务：加载工作流阶段索引，判断应从哪个阶段和步骤恢复，再通过 get_context.py --mode phase 获取步骤详情。用于返回进行中的任务并确定下一步。"
---

# 继续当前任务

恢复当前任务，从 `.trellis/workflow.md` 中正确的阶段和步骤继续。

---

## 步骤 1：加载当前上下文

```bash
python3 ./.trellis/scripts/get_context.py
```

确认当前任务、Git 状态和近期提交。

## 步骤 2：加载阶段索引

```bash
python3 ./.trellis/scripts/get_context.py --mode phase
```

显示规划、执行、收尾三个阶段，以及路由和技能映射。

## 步骤 3：判断当前进度

`get_context.py` 显示活动任务的 `status`。结合状态和文档是否存在进行路由。本命令帮助用户无需记忆 Trellis 流程，但本身不授予实施许可。

- `status=planning` 且无 `prd.md` → **1.1**（加载 `trellis-brainstorm`）
- `status=planning` 且只有 `prd.md` → 判断轻量或复杂。轻量任务可进入 **1.4** 评审；复杂任务返回 **1.1**，补齐 `design.md` 和 `implement.md`。
- `status=planning`，复杂任务文档齐全，但子代理 JSONL 未整理（为空或只有旧 `_example` 占位行）→ **1.3**
- `status=planning`，必需文档齐全，JSONL 已整理或采用内联模式 → **1.4**（请求启动评审；只有用户确认后才运行 `task.py start`）
- `status=in_progress`，尚未实施 → **2.1**
- `status=in_progress`，实施完成但未检查 → **2.2**
- `status=in_progress`，检查通过 → **3.3**（规范更新）→ **3.4**（主会话自主提交）
- `status=completed`（少见，通常立即归档）→ 归档流程

阶段规则（完整内容见 `.trellis/workflow.md`）：

1. 阶段内**按顺序**执行，不能跳过 `[required]` 步骤。
2. `[once]` 步骤的必需产物已存在时视为完成。只有轻量任务可以只用 `prd.md`；复杂任务还需 `design.md` 和 `implement.md`。
3. 新发现需要时，可返回先前阶段。

## 步骤 4：加载具体步骤

确定恢复点后：

```bash
python3 ./.trellis/scripts/get_context.py --mode phase --step <X.X> --platform codex
```

遵循加载的指令。每个 `[required]` 步骤完成后，继续下一步。

---

## 参考

完整工作流及各阶段详情位于 `.trellis/workflow.md`。本命令只是入口，权威指导以该文件为准。
