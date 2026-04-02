# ADR: v3 产品契约、支持矩阵与现代化边界冻结

- Status: Accepted
- Date: 2026-03-29
- Canonical for: v3 support policy, public-surface policy, coverage scope, and migration-guide structure
- Related documents:
  - `docs/related/parity-v3.md`
  - `docs/related/migration-v2-to-v3.md`

## 背景

Task 1 已经用 `docs/related/parity-v3.md` 冻结了 upstream parity truth，但 v3 仍缺少一个**明确的产品契约冻结**：

- 当前仓库仍然多目标构建 `net8.0` / `net9.0` / `net10.0`
- 现有文档仍主要描述 v2 故事
- 后续 Tasks 3-22 需要实现 build、coverage、session/transport、public API、docs 和 migration 工作
- 如果不先冻结支持矩阵、coverage scope 和 public-surface policy，后续任务会重复做产品决策，而不是实现既定决策

本 ADR 的目标不是列出所有 v3 breaking changes，而是先冻结**哪些事情已经决定**，让后续任务只补实现与 inventory，不再重新定义产品边界。

## 1. 冻结结论摘要

| 主题 | v3 冻结决策 | 直接含义 |
| --- | --- | --- |
| 支持 TFM | **`net10.0` only** | v3 明确放弃 `net8.0` / `net9.0` 兼容责任。 |
| 语言版本策略 | **显式固定 `LangVersion=14.0`** | 使用稳定 C# 14；不得使用 `latest`、`preview` 或“跟随 SDK 漂移”的语言策略。 |
| 语法/BCL 使用策略 | **允许直接使用稳定 .NET 10 / C# 14 语法与 BCL** | 不再为了 `net8` / `net9` 保留兼容性折中写法、polyfill 偏好或 API 回避。 |
| coverage gate scope | **只统计维护中的手写产品/工具代码** | 只覆盖 `AioTieba4DotNet/**` 与 `ProtoGenerator/**` 的非生成代码；排除 generated protobuf、tests、docs、`aiotieba/`。 |
| 核心产品形态 | **保留 `TiebaClient`、DI 注册、factory、modules 作为核心 user-facing shape** | v3 仍然是 root client + DI/factory + module-oriented business API 的产品，而不是 raw transport/core 产品。 |
| breaking-change policy | **允许重塑 v2 签名/契约，但不得在后续任务里再发明新的 public-surface principle** | 后续任务可以 reshape API，但不能把产品模型改成另一种哲学。 |

## 2. 支持矩阵与语言策略

### 2.1 目标框架

v3 的支持矩阵冻结为：

- `net10.0`

以下结论同样属于冻结范围：

- `net8.0` **不是** v3 支持矩阵的一部分
- `net9.0` **不是** v3 支持矩阵的一部分
- 后续任务不得把 “暂时继续保留 net8/net9，等以后再说” 当成默认策略
- 任何为了 `net8` / `net9` 留下的兼容性折中代码、条件分支、替代 API 选择、文档措辞或测试矩阵，都不再属于 v3 policy

### 2.2 语言版本

仓库语言版本策略冻结为：

- `<LangVersion>14.0</LangVersion>`

具体规则：

1. 必须使用**显式稳定 pin**，而不是 `latest`。
2. 必须使用稳定语言版本，而不是 `preview`。
3. 后续 build/solution 任务不得把“由安装的 SDK 自动决定语言级别”当成可接受策略。

### 2.3 .NET 10 / C# 14 现代化边界

v3 明确允许直接使用：

- 稳定的 .NET 10 BCL
- 稳定的 C# 14 语法与语言特性
- 只对 `net10.0` 成立的简化实现与 API 选择

这意味着：

- 不需要为了旧 TFM 保留低版本替代实现
- 不需要为了旧 TFM 避免使用稳定新 API
- 不需要在文档中继续承诺 `net8` / `net9` 兼容 cutover

## 3. Coverage scope 冻结

这里冻结的是 **v3 coverage gate 的统计边界**，不是“哪些文件可以被测试”。

### 3.1 计入 coverage 的范围

以下代码属于 v3 coverage scope：

| 范围 | 是否计入 | 说明 |
| --- | --- | --- |
| `AioTieba4DotNet/**` 中的维护中手写代码 | Yes | 主产品代码；以非生成、非临时产物为准。 |
| `ProtoGenerator/**` 中的维护中手写代码 | Yes | 受维护的工具代码，也属于 gate 范围。 |

### 3.2 不计入 coverage 的范围

以下内容明确**不属于** v3 coverage denominator：

| 范围 | 是否计入 | 说明 |
| --- | --- | --- |
| generated protobuf 输出 | No | 包括 `*/Protobuf/*.cs` 与其他由 `.proto` 生成的代码。 |
| `AioTieba4DotNet.Tests/**` | No | 测试项目用于验证，不进入产品/工具 coverage denominator。 |
| `docs/**` | No | 文档不属于 coverage gate 范围。 |
| `.sisyphus/**` | No | 计划、证据、notepad 等协作工件不属于 coverage gate 范围。 |
| `aiotieba/**` | No | upstream Python reference tree 不是维护中的 .NET 产品代码。 |

后续 coverage / CI 任务必须以这个边界设计 line/branch gate，而不是把 generated code、tests 或 docs 混进总体分母。

## 4. 冻结的 v3 public product shape

v3 的 user-facing 产品故事冻结为**client-first、module-oriented** 模型。

### 4.1 保留的核心产品概念

| 概念 | v3 状态 | 冻结规则 |
| --- | --- | --- |
| `TiebaClient` root client story | Retained | v3 仍以 root client 作为直接使用入口。 |
| DI registration story | Retained | `AddAioTiebaClient(...)` 所代表的宿主集成故事仍然保留。 |
| Factory story | Retained | 多账户 / 多实例创建仍然通过 factory 故事暴露，而不是让调用方手工拼 transport core。 |
| Module-oriented business API | Retained | 用户通过 modules 访问论坛、主题帖、用户、客户端元数据，以及后续 parity-required 业务域。 |
| Current baseline modules (`Forums` / `Threads` / `Users` / `Client`) | Retained as baseline concepts | 这些业务域仍然属于 root client 导航模型的一部分。 |

### 4.2 允许的 v3 reshape 范围

v3 **不是** “保留 v2 每一个签名不动”的版本。后续任务可以进行 intentional reshape，但必须留在同一产品模型里。

允许 reshape 的范围包括：

- public method signatures
- option / configuration contracts
- public model / enum / exception shape
- module partitioning、命名、拆分、合并
- interface 与 concrete type 的组织方式

但这些 reshape 必须同时满足：

1. 仍然服务于 `docs/related/parity-v3.md` 冻结的 capability scope。
2. 仍然遵循 root client + DI/factory + module-oriented public API 的形态。
3. 仍然把 raw transport/session/generated seams 留在内部。
4. 仍然可以在 `docs/related/migration-v2-to-v3.md` 里被记录为 intentional break，而不是事后发明的新产品原则。

### 4.3 后续新增业务域的公开方式

如果 parity work 需要补齐当前尚缺的消息、管理、搜索、统计等能力，允许的公开方式只有一种：

- 作为 `TiebaClient` 体系下的业务模块 family 暴露

不允许的方式包括：

- 新发明一个与 root client 平行的“transport-first” 产品入口
- 暴露 raw HTTP / WebSocket core 作为消费者主入口
- 以 generated protobuf DTO / request builder 作为迁移目标
- 把模块能力改成“先拿到底层 session/core，再自己发送请求”的使用模式

### 4.4 公开命名规范化的文档发布规则

Task 1 已经把允许的 public rename / removal rows 冻结在 `.sisyphus/evidence/task-1-naming-matrix.md`。后续 parity、migration、release 文档必须按同一套发布规则写，不能再各自发明命名故事。

固定规则如下：

1. `docs/related/parity-v3.md` 的 row key 继续锚定 upstream family identity，比如 `aiotieba.api.get_blacklist_old`、`aiotieba.api.get_replys`、`aiotieba.api.del_bawu`。
2. parity row 的 `C# Surface` 列只写**当前规范化后的公开名字**，不能把已移除 alias、bridge 或旧 DTO 名重新写回 active surface。
3. 已移除的 alias 和 old-home bridge 只能保留在显式 mapping section 里，作用是 searchable migration context，不是继续宣称它们仍是支持中的入口。
4. 仍然保留的 peer families 必须写成 parallel supported APIs，而不是 deprecated shim、legacy shim 或 temporary bridge。
5. migration 和 release 文档必须按 module / family 分组列出 mapping，不能把全部 rename row 扁平堆成一张无上下文的大表。

这组规则还固定了三个 ownership anchor：

- `Messages` owns inbox and message work, including `GetAtsAsync(...)`, `GetRepliesAsync(...)`, private messages, chatroom messages, and push parsing.
- `Admins` owns admin, bawu, and block-management work.
- `Client` stays lifecycle-only, for websocket and client/session initialization flows.

## 5. retain / remove / reshape / internalize taxonomy

后续 Tasks 3-22 一律使用以下 taxonomy，而不是再次争论边界。

| 分类 | 定义 | v3 适用对象 |
| --- | --- | --- |
| Retain | 保留为产品故事或对外承诺的一部分 | root client、DI registration、factory、module-oriented business API |
| Remove | 从 v3 policy 中明确移除，不再承担兼容责任 | `net8.0` / `net9.0` 支持义务、为旧 TFM 保留的折中策略、把 `docs/archive/todo.md` 当 scope truth |
| Reshape | 仍保留为产品能力，但允许 intentional breaking redesign | public signatures、module 边界、options/models/exceptions/enums 的具体 shape |
| Internalize | 可以存在于实现中，但不再是 consumer contract | raw transport cores、session/auth internals、dispatcher internals、generated protobuf/request artifacts |

### 5.1 必须 retain 的内容

- root client 作为直接使用故事
- DI registration 作为宿主集成故事
- factory 作为多实例/多账户故事
- module-oriented business API 作为功能组织方式
- parity-driven capability families 作为产品能力范围

### 5.2 明确 remove 的内容

- `net8.0` / `net9.0` 兼容责任
- “为了多 TFM，先不用 .NET 10 / C# 14 稳定特性”的默认约束
- “后续任务再决定到底支持哪些 TFM / 语言版本 / coverage 范围”的自由度
- 把 `docs/todo.md` 继续当作 parity 或 contract truth

### 5.3 允许 reshape 的内容

- 具体 public signatures
- module property / interface 的组织方式
- 选项对象、异常、模型、枚举的细节
- v2 -> v3 migration inventory 中的 intentional API cleanup

### 5.4 必须 internalize 的内容

- raw HTTP / WebSocket core
- transport dispatcher internals
- session/auth internal state machines
- generated protobuf C# types
- low-level request packers / response parsers
- compatibility-only facade 或 temporary bridge types

## 6. 允许的 breaking-change 边界

### 6.1 明确允许的 v3 breaks

| breaking area | 是否允许 | 规则 |
| --- | --- | --- |
| 从多 TFM 收口到 `net10.0` | Yes | 这是 v3 已冻结的产品决策，不是待讨论事项。 |
| 从 floating / implicit language policy 收口到 `LangVersion=14.0` | Yes | 必须显式稳定 pin。 |
| 清理 v2 public signatures / options / models / exceptions | Yes | 允许 intentional break，但必须纳入 migration inventory。 |
| 调整 module 边界、拆分/合并/新增 parity-driven module family | Yes | 前提是仍属于 client-first、module-oriented 模型。 |
| 重做 transport/session/auth internal architecture | Yes | 只要不把 raw internals 暴露成新的 consumer contract。 |

### 6.2 明确不允许的边界漂移

以下不是“以后再看”的设计空间，而是**禁止重新打开的决策**：

| 行为 | 状态 | 原因 |
| --- | --- | --- |
| 继续保留 `net8.0` / `net9.0` 作为 v3 支持矩阵 | Forbidden | 与本 ADR 冻结结论冲突。 |
| 使用 `LangVersion=latest` / `preview` 作为仓库语言策略 | Forbidden | 会让 build policy 随 SDK 漂移。 |
| 因为旧 TFM 兼容顾虑而禁止使用稳定 .NET 10 / C# 14 | Forbidden | v3 已明确放弃这类 compromise behavior。 |
| 把 generated protobuf、tests、docs、`aiotieba/` 纳入 coverage denominator | Forbidden | 与 coverage scope freeze 冲突。 |
| 重新发明 transport-first、core-first、generated-type-first public API | Forbidden | 与 v3 public product shape 冲突。 |
| 后续任务单方面更改 migration guide 的章节骨架 | Forbidden | 迁移结构由本任务冻结。 |

## 7. 文档与 source-of-truth 层级

v3 文档层级冻结为：

1. `docs/related/parity-v3.md` = **parity truth**
2. `docs/adr-v3-contract.md` = **canonical v3 contract decision source**
3. `docs/related/migration-v2-to-v3.md` = **stable migration skeleton and later breaking inventory host**
4. `README.md`、`docs/index.md`、`docs/guide/getting-started.md`、`docs/reference/modules.md`、`docs/guide/advanced.md`、`docs/guide/troubleshooting.md` 等 = downstream explanatory/consumer docs，必须与上面三者保持一致

命名规范化 wave 的额外发布要求：

- `docs/related/parity-v3.md` 保留 upstream family 可检索性，但其 `C# Surface` 列必须使用规范化后的 public names。
- `docs/related/migration-v2-to-v3.md` 和 `docs/related/release-notes-v3.md` 必须为每个 removed alias、removed bridge、renamed peer-family symbol、renamed canonical symbol、renamed public DTO shape 提供 module-grouped mapping context。
- `docs/adr-v3-contract.md` 负责冻结这些写法规则，本身不再充当另一个 rename matrix。

特别说明：

- `docs/archive/todo.md` **不是** parity truth
- 旧 v2 文档措辞如果与本 ADR 冲突，以本 ADR 为准
- 后续 docs / guide 任务必须围绕本 ADR 与 migration skeleton 对齐，而不是重新决定产品边界

## 8. 对 downstream tasks 的直接约束

| Tasks | 现在已经不需要重新决定的事情 | 约束结果 |
| --- | --- | --- |
| 3 / 7 / 22 | TFM 与语言版本策略 | 可以直接按 `net10.0` + `LangVersion=14.0` 设计 solution/build/release，不必保留 `net8` / `net9` compromise。 |
| 4 | coverage denominator | 可以只围绕维护中的手写产品/工具代码设计 total line/branch gates。 |
| 6 / 10 / 11-17 | public-surface policy 与 internalization boundary | 可以重塑 API 与 transport/session internals，但不能离开 client/module/factory/DI 形态，也不能把 raw seams 重新公开。 |
| 20 / 21 | docs truth hierarchy 与 migration guide structure | 可以直接围绕 parity ledger + 本 ADR + migration skeleton 更新文档，无需再争论 scope authority。 |

## 9. 结果

本 ADR 之后，后续任务仍然要做大量实现工作，但它们不再拥有以下自由度：

- 不再决定 v3 是否保留 `net8` / `net9`
- 不再决定语言版本是否 floating
- 不再决定 coverage gate 到底算哪些代码
- 不再决定产品是否改成 raw core / transport-first 模型
- 不再决定迁移指南是否另起一套结构

这些事情已经在本 ADR 中冻结；后续任务只负责实现、验证和记录具体 breaking inventory。
