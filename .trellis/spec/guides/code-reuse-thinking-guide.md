# 代码复用思考指南

> **目的**：创建新代码前停下来想一想——相同能力是否已经存在？

---

## 问题

**重复代码是行为不一致缺陷的首要来源。**

复制粘贴或重新编写已有逻辑时：

- 缺陷修复无法同步传播。
- 行为随时间分化。
- 代码库越来越难理解。

---

## 编写新代码前

### 第一步：先搜索

```bash
# 搜索相似函数名
rg -n "functionName" .

# 搜索相似逻辑
rg -n "keyword" .
```

### 第二步：提出这些问题

| 问题 | 如果答案是“是” |
| --- | --- |
| 是否存在相似函数？ | 使用或扩展它 |
| 其他地方是否采用了这一模式？ | 遵循既有模式 |
| 是否适合成为共用工具？ | 在正确位置创建它 |
| 是否正在从另一个文件复制代码？ | **停下**，提取为共用逻辑 |

---

## 常见重复模式

### 模式一：复制粘贴函数

**不推荐**：将校验函数复制到另一个文件。

**推荐**：提取为共用工具，在需要处引用。

### 模式二：相似实现

**不推荐**：创建一个与现有实现有 80% 相同的新实现，例如复制大部分请求辅助逻辑。

**推荐**：通过参数、选项或适当变体扩展既有实现；通用组件场景可通过属性／变体复用。本库应先检查请求工厂、描述符和 mapper 的既有边界，不引入不存在的应用组件层。

### 模式三：重复常量

**不推荐**：在多个文件中定义同一个常量。

**推荐**：使用唯一权威定义，各处引用。

### 模式四：重复提取载荷字段

**不推荐**：多个使用方分别在本地对同一 JSON／事件字段进行类型断言：

```typescript
const description = (ev as { description?: string }).description;
const context = (ev as { context?: ContextEntry[] }).context;
```

即使只有两行，这也是重复的契约逻辑。每个使用方都分别定义了什么才是有效载荷。这里的 TypeScript 事件代码是通用／Trellis 示例，不表示本库存在此运行时。

**推荐**：将解码器、类型守卫或投影放在数据所有者附近：

```typescript
if (isThreadEvent(ev)) {
  renderThreadEvent(ev);
}
```

**规则**：同一个无类型载荷字段在两个或更多位置被读取时，增加第三个读取方之前，应创建共用类型守卫／归一化器／投影。

---

## 何时抽象

**适合抽象的情况**：

- 相同代码出现三次或更多次。
- 逻辑复杂到容易出现缺陷。
- 多个人可能需要使用它。

**不适合抽象的情况**：

- 只使用一次。
- 只是简单的一行代码。
- 抽象比重复本身更复杂。

---

## 批量修改后

对多个文件作类似修改后：

1. **审查**：是否覆盖了所有实例？
2. **搜索**：运行 `rg` 找出遗漏。
3. **考虑**：是否应提取抽象？

### 归约器应采用穷尽结构

状态从 `action`、`kind`、`status`、`phase` 等动作类值派生时，优先使用一个 `switch` 归约器，不要散落在多处 `if/else` 更新中。

```typescript
// 不推荐：按动作分散的状态转换难以审计
if (action === "opened") { ... }
else if (action === "comment") { ... }
else if (action === "status") { ... }

// 推荐：由一个归约器管理转换表
switch (event.action) {
  case "opened":
    ...
    return;
  case "comment":
    ...
    return;
}
```

当事件日志是权威来源时，这一点尤为重要。归约器是明确的重放模型；展示代码和命令不应重复实现模型的部分逻辑。

---

## 提交前检查清单

- [ ] 已搜索相似的既有代码。
- [ ] 没有本应共享的复制粘贴逻辑。
- [ ] 共用解码器之外没有重复提取无类型载荷字段。
- [ ] 常量只有一处定义。
- [ ] 相似模式遵循相同结构。
- [ ] 归约器／动作转换位于一个归约器或命令调度器中。

---

## 易错点：Python if/elif/else 的穷尽检查

**问题**：Python 的 if/elif/else 链没有编译期穷尽检查。当给 `Literal` 类型（如 `Platform`）增加新值时，既有分支链会静默落入 `else`，使用错误的默认值。

**症状**：新平台只能部分正常工作——有些方法返回 Claude 默认值，而非平台专用值，且不报错。

**示例**（Trellis 上游 `cli_adapter.py`，不是本仓库文件）：

```python
# 不推荐：“gemini”落入 else，返回“claude”
@property
def cli_name(self) -> str:
    if self.platform == "opencode":
        return "opencode"
    else:
        return "claude"  # gemini 静默获得“claude”！

# 推荐：为每个平台显式设置分支
@property
def cli_name(self) -> str:
    if self.platform == "opencode":
        return "opencode"
    elif self.platform == "gemini":
        return "gemini"
    else:
        return "claude"
```

**预防**：给 Python `Literal` 类型增加新值时，搜索所有根据该类型切换的 if/elif/else 链，并添加显式分支。不要假定 `else` 对新值也正确。

---

## 易错点：不同机制产生相同输出

**问题**：当两个不同机制必须产生相同文件集合时，例如初始化递归复制目录，而更新手工调用 `files.set()`，重命名、移动或新增子目录等结构变化只会自动传递至前者，手工维护的路径会静默漂移。

**症状**：初始化完全正常，但更新将文件放到错误路径，或直接漏掉文件。

**预防**：

- **优先**：消除机制差异，让手工路径调用自动路径；例如 `collectTemplateFiles()` 调用 `getAllScripts()`，不再自行维护列表。
- **无法避免差异时**：增加比较两条机制输出的回归测试。
- 迁移目录结构时，搜索所有引用旧结构的代码路径。

**实际案例**：Trellis 上游的 `trellis update` 曾手工维护 11 个脚本的 `files.set()` 列表，而 `getAllScripts()` 已跟踪它们。修复方法是将手工列表替换为 `for..of getAllScripts()` 循环，参见 v0.4.0-beta.3 的 `update.ts` 重构。

---

## 模板文件注册（仅适用于 Trellis 上游开发）

本节路径属于 Trellis 上游仓库。本项目仅含安装后的本地 Trellis 文件，没有这些模板源目录；不要为了满足示例创建它们。

向上游 `src/templates/trellis/scripts/` 添加新文件时：

**唯一注册点**：`src/templates/trellis/index.ts`。

1. 添加 `export const xxxScript = readTemplate("scripts/path/file.py");`。
2. 加入 `getAllScripts()` Map。

这样即可。`commands/update.ts` 直接使用 `getAllScripts()`，无需手工同步另一份列表。

**原因**：未在 `getAllScripts()` 注册时，`trellis update` 不会将文件同步至使用者项目，缺陷修复和新功能也无法传递。

**历史**：v0.4.0-beta.3 之前，`update.ts` 有独立手工文件列表，经常与 `getAllScripts()` 不同步。这导致 `trellis update` 静默跳过 11 个 Python 文件。修复方法是取消重复列表，以 `getAllScripts()` 为唯一权威来源。

### 新脚本快速检查清单

以下命令只在确实包含该模板源树的 Trellis 上游仓库执行：

```bash
# 添加新的 .py 文件后，确认其已加入 getAllScripts()
rg -l "newFileName" src/templates/trellis/index.ts  # 应匹配到该文件
```

### 模板同步约定

在同时维护两份源树的 Trellis 上游仓库，`.trellis/scripts/`（项目自身使用）与 `packages/cli/src/templates/trellis/scripts/`（模板）必须保持一致。修改前者后同步后者。本项目没有该模板树，不执行以下上游同步示例：

```bash
rsync -av --delete --exclude='__pycache__' .trellis/scripts/ packages/cli/src/templates/trellis/scripts/
```

**易错点**：rsync 的源／目标路径错误会创建嵌套垃圾目录，例如 `.trellis/scripts/packages/cli/...`。运行前必须复核路径；`--delete` 还会删除目标独有内容，应先检查差异和授权范围。
