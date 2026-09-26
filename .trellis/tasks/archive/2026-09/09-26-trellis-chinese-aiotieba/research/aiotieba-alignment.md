# 研究：aiotieba Python → C# 定位与功能接口对齐规范

- 研究问题：核对仓库 C# 库定位、支持边界、现有对齐与验证治理；为 Trellis 中文规范提供可复用规则及插入位置。
- 范围：本仓库源码/文档/规范只读研究，上游 GitHub 官方仓库固定版本只读核验。
- 日期：2026-09-26
- 任务边界：仅产出本研究文件；不补接口、不改业务代码、不调整对齐基线、不修改治理状态、不执行在线测试或 Git 操作。

## 研究发现

### 1. 定位结论与建议用语

建议在 Trellis 项目入口明确写入：

> AioTieba4DotNet 是以 Python aiotieba 为功能与接口语义参照的 C# 实现，面向 C#/.NET 使用者提供异步贴吧客户端库。项目持续对齐上游能力、参数语义、请求协议、响应数据及业务行为，同时采用 .NET 的异步、类型、异常、资源生命周期、依赖注入和工厂习惯。当前维护线为 v3，支持 `net10.0` / C# 14；具体已实现范围、差异与验证证据以现有对齐台账和覆盖矩阵为准。

此表述将用户刚确认的核心定位写成项目约束，不把“参考上游”弱化为可有可无的背景，也不把目标写成已全面完成的事实。

依据：

- `README.md:3` 定义异步 .NET 10 贴吧库；`README.md:11` 起列出贴吧、主题、用户、消息和吧务等能力。
- `README.md:69` 明确 aiotieba 是上游 Python 实现；`README.md:75` 要求 API 参数、打包、解析、错误处理优先对齐上游。
- `AioTieba4DotNet/AGENTS.md:57` 要求在 C# 契约允许范围内镜像上游语义；`.junie/guidelines.md:33` 起规定导出身份、请求语义与差异边界。
- `Directory.Build.props:4` 固定 `net10.0`，`:5` 固定语言 `14.0`；`docs/adr-v3-contract.md:25` 起明确当前支持政策。
- `.trellis/spec/backend/index.md:3` 已说明这里的 backend 路由指客户端库、生成器和测试支持，不是 ASP.NET 服务，也没有数据库层。

“方便 C# 用户各个情况下使用”宜转化为接入场景与示例覆盖：直接创建客户端、宿主应用依赖注入、多账号独立客户端、访客读取、登录后操作、消息/吧务等能力场景。不能据此自动承诺旧 .NET、.NET Standard、Unity、浏览器、Native AOT 或所有操作系统/架构已验证支持。

### 2. 已找到的文件与责任归属

| 文件 | 一行说明与关键锚点 |
| --- | --- |
| `README.md` | 面向用户的产品入口、能力和上游链接；`:3`、`:69`、`:75`。 |
| `AioTieba4DotNet/AGENTS.md` | 库目录规则、六模块、公开/内部边界及对齐义务；`:28`、`:51`。 |
| `ProtoGenerator/AGENTS.md` | `.proto` 为源、生成 `.cs` 为派生产物，禁止手改；`:8`。 |
| `.junie/guidelines.md` | v3、支持矩阵、对齐真源、跨目录文档和测试约束；`:5`、`:18`、`:33`、`:46`。 |
| `.trellis/spec/index.md` | 现有规范总入口、权威文件分工与验证边界；`:3`、`:34`。 |
| `.trellis/spec/backend/index.md` | 开发前和质量检查入口；`:17`、`:25`。 |
| `.trellis/spec/backend/directory-structure.md` | 客户端库入口与 contract/module/protocol/request 分层；`:25`、`:34`。 |
| `.trellis/spec/backend/transport-guidelines.md` | HTTP/WS 选择、回退、协议打包、签名和重试规则；`:3`、`:17`、`:38`。 |
| `.trellis/spec/backend/mapping-and-codegen.md` | 公开 DTO、内部映射、默认值与代码生成规则；`:3`、`:11`、`:23`。 |
| `.trellis/spec/backend/error-handling.md` | 公开异常、取消、错误传播和回退边界；`:3`、`:21`、`:27`。 |
| `.trellis/spec/backend/session-and-cache.md` | 独立客户端状态、生命周期、多账号、认证及缓存所有权；`:5`、`:15`。 |
| `.trellis/spec/backend/quality-guidelines.md` | 可执行检查、证据文件、测试分层与在线副作用；`:11`、`:27`、`:45`。 |
| `.trellis/spec/frontend/content-guidelines.md` | 用户文档与 parity/coverage 各自责任、示例类型及机器读取 Markdown；`:12`、`:24`、`:43`。 |
| `docs/related/parity.md` | 当前唯一对齐台账：上游冻结身份、内部映射、认证、命名归一化、能力族系、审计行；`:10`、`:38`、`:78`、`:183`。 |
| `docs/related/public-api-coverage-matrix.md` | 每个公开成员/行为的证据、运行通道、处置和直接 `Api:*` 可发现性；`:15`、`:24`、`:31`。 |
| `docs/guide/getting-started.md` | 访客、认证、配置、DI、多账号工厂使用路径；`:41`、`:59`、`:80`、`:106`、`:136`。 |
| `AioTieba4DotNet.Tests.Governance/README.md` | 三项目测试拓扑中的治理、离线契约和有序在线套件职责；`:3`、`:22`。 |
| `AioTieba4DotNet.Tests.Governance/Contracts/ParityTruthFreezeContract.cs` | 可执行上游 repo/tag/SHA 与本地快照信任策略；`:14`、`:39`、`:79`。 |
| `AioTieba4DotNet.Tests.Governance/Contracts/ParityEvidenceSchemaContract.cs` | 对齐状态、精确 Markdown 表头、逐行必需字段及解析器；`:16`、`:22`、`:46`、`:69`。 |
| `AioTieba4DotNet.Tests.Governance/Contracts/PublicApiCoverageMatrixContract.cs` | 覆盖矩阵允许值、精确表头与在线分类正则；`:16`、`:25`、`:28`。 |
| `AioTieba4DotNet.Tests.Governance/Contracts/MappingCoverageContractTests.cs` | 内存 fixture 映射及默认值验证模式；`:15`、`:61`。 |
| `AioTieba4DotNet.Tests.Governance/Contracts/SignatureParityContractTests.cs` | 签名字段顺序与显式动态掩码契约；`:14`、`:50`、`:79`。 |

### 3. 上游版本与来源：保留已有冻结基线

现有有效基线是：

| 字段 | 当前值 |
| --- | --- |
| 仓库 | `lumina37/aiotieba` |
| 官方 URL | `https://github.com/lumina37/aiotieba` |
| 标签 | `v4.6.4` |
| 固定提交 | `04f8e431f87507a6228b42061c70d298b34317ff` |
| 比较树 | `https://github.com/lumina37/aiotieba/tree/04f8e431f87507a6228b42061c70d298b34317ff` |

`docs/related/parity.md:16` 与 `ParityTruthFreezeContract.cs:16` 至 `:21` 同时冻结该组值。2026-09-26 只读 GitHub REST API 核验 `refs/tags/v4.6.4` 仍直接指向上述提交，该提交时间为 `2026-03-23T08:41:25Z`。

同次只读核验看到 `master` 为 `e12cc165abd53127ad27a307b94b4f567712be2d`，提交时间 `2026-09-15T16:16:04Z`；主分支浏览页面显示当前目录为 `src/aiotieba`。这是“上游已演进”的观察，不是升级本仓库基线的授权或对齐完成证据。

固定 `v4.6.4` 树内源码路径是 `aiotieba/client.py`、`aiotieba/api/**` 和 `aiotieba/__init__.py`。台账中的 `aiotieba/aiotieba/...` 包含过去仓库内参考快照目录前缀；写固定 GitHub 链接时不要误用这个双重前缀。升级基线需单独比较导出、接口、行为、证据和兼容性，不属于本次中文设置。

### 4. 应记录的语义对齐清单

建议新增的规范按以下顺序要求研究和验收，不另建第二份能力台账：

1. **身份与版本**：记录固定 repo/tag/SHA、上游包路径和导出符号；上游 `Replys`、`get_uinfo_getUserInfo_web` 等特殊拼写保留为检索身份。C# 友好名必须通过现有台账可反查，上游新主分支不得替换冻结版本。依据 `parity.md:16`、`:23`、`:183`。
2. **能力与入口**：将上游 client 方法/导出族系映射到现有六模块和公开契约；区分内部 `Api/**` 实现与供使用者调用的表面。只实现内部请求不能证明公开入口已可用。依据 `AGENTS.md:30`、`:55`，`parity.md:26`、`:78`。
3. **输入**：逐项核对含义、类型、范围、单位、空值/空集合、默认值、页码、每页条数、排序、特殊哨兵值、同名重载；C# `long`/`ulong`/`int`/`uint` 不能因为 Python 都写 `int` 就统一。依据 `IThreadModule.cs:22`、`:36`、`:56`，`content-guidelines.md:30`。
4. **认证与状态**：区分访客、BDUSS/STOKEN、TBS、WS/z-id/sync 状态和顺序；记录何时校验凭据、是否进行准备查询、成功后何时提交本地状态、失败与取消如何恢复。依据 `session-and-cache.md:15`、`transport-guidelines.md:5`。
5. **线协议**：核对 URL/方法、HTTP 表单/JSON/protobuf/WS/BLCP、命令 ID、编码、公共字段、cookie/header、签名输入顺序及必须保留的兼容算法；不按“更现代”理由改变既定线上语义。依据 `transport-guidelines.md:17`、`:31`。
6. **响应模型**：检查数据字段、枚举、列表顺序、分页/游标、缺失/空值/默认值及错误字段，映射为协议无关的公开 DTO。JSON/protobuf 类型不进入公开签名，`.proto` 与生成代码同步。依据 `mapping-and-codegen.md:3`、`:11`、`:23`。
7. **失败、取消和回退**：记录上游与 .NET 各自对调用者呈现的结果；保留服务端错误信息和业务失败语义，明确 .NET 异常适配；取消仍为取消。回退仅按既有规则进行，不吞业务异常或重复执行写请求。依据 `error-handling.md:17`、`:29`，`transport-guidelines.md:15`。
8. **差异与证据**：记录“来源/预期/实际/验证命令/证据/状态/限制”；新差异需要明确说明，不能因为采用 .NET 就默认合理，也不能在没有批准记录时标成 `intentional-divergence-approved`。能力台账、公开覆盖矩阵、执行报告分工保持清晰。

### 5. 可直接复用的 .NET 适配模式

| Python 语义或用户场景 | 现有 .NET 表达 | 代码依据与边界 |
| --- | --- | --- |
| `async def` / `await` | `Task<T>`、`Async` 后缀、末尾可选 `CancellationToken` | `Contracts/IThreadModule.cs:22`；`Modules/ThreadModule.cs:20` 直接转发取消令牌。不要为语法一致改成 Python 命名或同步阻塞包装。 |
| `str \| int` 的吧名/吧 ID 输入 | 强类型重载 | `IThreadModule.cs:22`、`:36` 对应 `string` / `ulong`；保留两条输入语义，不丢重载证明。 |
| Python 根客户端的大量方法 | 六个业务模块 | `Clients/ITiebaClient.cs:6` 至 `:36`；消息归 `Messages`，初始化/同步归 `Client`。 |
| `async with Client()` 的资源作用域 | 当前 `IDisposable` / `using var` | `Clients/ITiebaClient.cs:6`、`Clients/TiebaClient.cs:68`；不要在设置任务中改成或承诺 `IAsyncDisposable`。 |
| 凭据与配置 | 公开 `Contracts.Account`、`TiebaOptions`，独立客户端状态 | `TiebaClient.cs:15`、`:24`、`:33`，`TiebaOptions.cs:13`、`:18`、`:23`。 |
| 宿主应用集成 | `AddAioTiebaClient` 与 `ITiebaClient` | `DependencyInjection.cs:22`；客户端注册为 scoped（`:40`），共享组合逻辑（`:37`）。 |
| 多账号 | `ITiebaClientFactory` 创建独立客户端 | `DependencyInjection.cs:56`、`Clients/TiebaClientFactory.cs:25`；不以共享 singleton 上替换凭据来表达多账号。 |
| Python 模型和字段 | 公开 DTO 属性及内部 mapper | `Models/Threads/Threads.cs:13`、`:23`、`:28`；不泄漏生成 protobuf。 |
| Python 错误包装 | 已有 .NET 异常契约 | `Api/ApiResponseValidator.cs:7` 非零错误抛 `TieBaServerException`，异常保留 `Code`（`Exceptions/TieBaServerException.cs:13`）。 |

错误表达需要特别写清楚：上游固定版 `client.py:417` 对 `get_threads` 使用 `handle_exception`；`helper/utils.py:93` 至 `:99` 捕获异常、构造结果并写入 `.err`。因此“行为对齐”不能简单等同于所有语言层面的错误呈现完全相同。当前 .NET 规范反而明确禁止未经契约变更把业务异常改成 `false`/空 DTO（`error-handling.md:17`）。建议描述为“保留业务成功/失败信息，使用既定 .NET 异常契约表达，并记录可观察差异”；本研究不认定现有台账已正式批准所有此类差异。

### 6. 小型源码对应示例：`get_threads`

以下只是规范所需证据粒度的例子，不等于本次完成该族系的全量审计：

- 上游 `aiotieba/client.py:419` 接受吧名或 ID，默认 `pn=1`、`rn=30`、回复排序、非精品；C# 对应 `IThreadModule.GetThreadsAsync` 两个重载（`:22`、`:36`）。
- 上游 `aiotieba/api/get_threads/_api.py:9` 命令为 `301001`，`:17` 首页打包为 `pn=0`，`:19` 为 `rn_need=rn+5`；C# 对应 `Api/GetThreads/GetThreads.cs:17`、`:27`、`:29`。
- 上游 `_api.py:30` 在非零错误码抛服务端错误；C# `GetThreads.cs:41` 交给公共错误校验，再于 `:45` 映射公开模型。
- 上游 `_api.py:39` / `:51` 存在 HTTP 与 WS 请求；C# 对应 `GetThreads.cs:81` / `:101`。是否进行了真实 WS 调用仍要看独立执行证据，两个方法存在本身不能证明。
- `docs/related/parity.md:142` 已有内部映射；`:280` 已有能力族系行。新增 Trellis 规范应链接这些记录，避免复制一张会漂移的新映射表。

### 7. 对齐状态、覆盖状态和执行证据不能合并

现有标准对齐状态必须保留精确 token：

| token | 可加的中文解释 |
| --- | --- |
| `match` | 本审计单元已有符合要求的匹配证据，不代表整个库全部对齐。 |
| `requires-remediation` | 已识别差异，需修正。 |
| `upstream-gap` | 上游存在需单独记录的能力或证据缺口。具体原因必须写在该行。 |
| `blocked-by-verification` | 实现可能存在，但所需验证受明确条件阻断。 |
| `intentional-divergence-approved` | 有明确批准记录的有意差异。不能自行用来消除未证实问题。 |

`docs/related/parity.md:40` 至 `:46` 和 `ParityEvidenceSchemaContract.cs:16` 至 `:31` 共同限定格式。标准审计行必须提供上游锚点、.NET 锚点、传输、认证前置条件、证明产物、状态、验证命令和说明（后者由代码 `:58` 要求）。

覆盖矩阵另有 `Current coverage`、`Target lane`、`Disposition`，不能将 `Disposition` 当已覆盖标志（`public-api-coverage-matrix.md:17`）。同一 `Api:*` 家族分类不能替代每个重载的直接证明（`:34`）；只有明确公布直接运行命令的行才属于首类可筛选入口（`:35`）。

应写入验收规则：

- 构建成功、文档构建成功、离线 fixture 通过、在线成功、覆盖率达标分别报告；任何一种不能代替另一种。
- 先复用离线签名、线协议、状态与 mapper 契约。测试必须断言可观察结果，不能只输出日志或断言“不抛异常”。
- `safe` 是真实在线通道，包含消息/帖子写入及补偿；文案设置不应触发它。`restricted` 需要显式选择与专用能力/资产门禁，缺条件的 inconclusive 结果不是通过。
- 不按 Governance 项目名或 `Contract:*` 类别断言测试一定离线；具体测试需检查代码与依赖。现有规范已指出其中存在 live `Tier:Safe` 测试（`quality-guidelines.md:33`）。
- 当前治理约定要求保留四个证据文件：`parity-truth-freeze.json`、`parity-gap-ledger.json`、`local-verification.manifest.json`、`local-verification.manifest.schema.json`，预期位于 `.sisyphus/evidence/`，本次检查均不存在。不复活旧证据、不伪造缺失材料、不降低门禁。

### 8. 中文化范围中的机器契约

本任务可把 Trellis 说明、规范、标题、操作指引和解释性文本设为中文；但机器读取的身份与格式必须保留：

- `ParityEvidenceSchemaContract.cs:22` 精确匹配英文标准审计表头，`:80` 使用 ordinal 比较。
- `PublicApiCoverageMatrixContract.cs:25` 精确匹配英文覆盖矩阵表头，`:16` 至 `:23` 固定通道/处置 token，`:28` 固定 `direct-api-*` 和 `Api:<Module>.<Method>` 语法。
- `ParityTruthFreezeContract.cs:21` 的 `CanonicalSourcePathPolicy` 虽然是英文句子，也属于 `:76` 精确比较的证据字符串，不能因“人类可读”直接翻译。
- C# 标识符、命名空间、路径、JSON 键、状态值、测试分类、命令参数、固定 SHA、Python 导出名保留原文；需要读者理解时在旁边补中文解释。

这些治理文件属于参考边界。仅为 Trellis 中文设定，不需要扩大到翻译业务文档、测试代码常量和接口 XML 注释；如果主任务扩大范围，应先把机器契约列入保护清单。

### 9. 推荐的规范插入位置

| 位置 | 建议增补 | 保持的权威分工 |
| --- | --- | --- |
| `.trellis/spec/index.md` 开头 | 写明确的“aiotieba Python 版的 C# 实现”定位、当前 .NET 支持边界、语义对齐目标、文案不等于完成证明。 | 不复制模块/API 全量清单。 |
| `.trellis/spec/backend/upstream-alignment.md`（建议新增） | 统一描述固定来源、对齐清单、Python → .NET 适配、差异处理、证据层级及开发/审查步骤。 | 这是操作规范；唯一能力台账仍是 `docs/related/parity.md`。 |
| `.trellis/spec/backend/index.md` 的规范索引与开发前检查 | 链接上述对齐规范，要求接口、协议、模型、错误改动先检查它及现有台账。 | 保留其他专题规范的详细实现规则。 |
| `.trellis/spec/backend/transport-guidelines.md` | 链接语义清单，中文说明默认值、特殊值、认证与回退不能自行改变。 | 不重复所有族系打包规则。 |
| `.trellis/spec/backend/mapping-and-codegen.md` | 强调保留数据语义、强类型/属性适配和缺省值证据。 | `.proto`/生成器仍各自负责源与输出。 |
| `.trellis/spec/backend/error-handling.md` | 简述上游 `.err` 与 .NET 异常是需说明的语言适配，不能无审查改成空结果。 | 继续以现有 C# 公开异常和取消契约为准。 |
| `.trellis/spec/backend/quality-guidelines.md` | 链接新规范，明确“目标/实现/离线/在线/覆盖”区分与缺失证据处理。 | coverage matrix / retained artifacts 仍是当前治理来源。 |
| `.trellis/spec/frontend/content-guidelines.md` | 项目定位一致、按 C# 接入场景组织例子、按当前实际类型/重载编写。 | 用户使用说明、对齐台账、覆盖矩阵互相链接，不混成一份表。 |
| `.trellis/spec/guides/cross-layer-thinking-guide.md` | 将通用层次例子贴合本库：契约 → 模块 → 协议 → 传输/请求 → mapper → DTO → 文档/证据。 | 不引入本项目没有的数据库/服务端应用架构。 |

本轮只是研究；具体插入由主会话在获准实施后的 spec 更新流程处理。

### 10. 外部引用与本次访问方式

以下均为 `lumina37/aiotieba` 官方 GitHub 仓库内容，只读获取，未下载或执行上游依赖。

- [v4.6.4 标签引用 API](https://api.github.com/repos/lumina37/aiotieba/git/ref/tags/v4.6.4)：本次核验标签指向固定 SHA。
- [固定对齐提交](https://github.com/lumina37/aiotieba/commit/04f8e431f87507a6228b42061c70d298b34317ff)：本仓库现行比较基线。
- [固定版 Client](https://github.com/lumina37/aiotieba/blob/04f8e431f87507a6228b42061c70d298b34317ff/aiotieba/client.py#L417)：方法参数、装饰器和传输路由；资源生命周期见同文件 `:203` 至 `:222`。
- [固定版 get_threads 请求实现](https://github.com/lumina37/aiotieba/blob/04f8e431f87507a6228b42061c70d298b34317ff/aiotieba/api/get_threads/_api.py#L9)：命令、打包、解析及 HTTP/WS 方法。
- [固定版错误包装](https://github.com/lumina37/aiotieba/blob/04f8e431f87507a6228b42061c70d298b34317ff/aiotieba/helper/utils.py#L61)：捕获异常并写入 `.err` 的行为。
- [固定版顶层导出](https://github.com/lumina37/aiotieba/blob/04f8e431f87507a6228b42061c70d298b34317ff/aiotieba/__init__.py#L11)：Client、TimeoutConfig、Account、枚举、日志等导出身份。
- [固定版项目元数据](https://github.com/lumina37/aiotieba/blob/04f8e431f87507a6228b42061c70d298b34317ff/pyproject.toml#L3)：版本 `4.6.4`；Python 支持范围是上游信息，不是本库 .NET 支持范围。
- [本次看到的主分支提交](https://github.com/lumina37/aiotieba/commit/e12cc165abd53127ad27a307b94b4f567712be2d)：仅作上游变化观察，不作为本仓库本轮比较来源。

## 限制与未发现项

- 本次没有逐项审计上游全部 API，没有运行构建、测试、在线行为或覆盖率采集，不应从这份研究得出“功能全面对齐”结论。
- 2026-09-26 实际只读检查发现本地 `aiotieba/` 目录不存在，四个 `.sisyphus/evidence/` 保留产物也均不存在。源码/台账中写有历史证明不等于当前 checkout 具备可复核证明；禁止据此合成通过记录。
- `AioTieba4DotNet/AGENTS.md:36`、`.junie/guidelines.md:16` 仍提及保留 `Client.cs` 兼容层，但本次文件存在性检查未找到 `AioTieba4DotNet/Client.cs`；`.trellis/spec/frontend/content-guidelines.md:9` 已记录这一陈旧指引。新中文规范应以实际 `Clients/TiebaClient.cs` 为当前入口，不能复活过时示例。
- 部分业务 XML 文档仍混有英文（如 `Contracts/IThreadModule.cs:75`）；这不是 Trellis 配置本身。本次无权据此扩大到全库注释翻译。
- `README.md:38` 示例使用 `threads.Count`，而当前 `Models/Threads/Threads.cs` 仅公开 `Objs` 等属性；这是源码/使用文档交叉检查时发现的潜在示例漂移，未修复、未编译验证，不应在新规范中直接复制该示例。
- 现有台账已经包含 `blocked-by-verification` 行，例如签到和群消息/已读状态操作；“已实现”与“已在线证实”应分别叙述。中文设置任务不负责自动消除这些缺口。
- 内存快速检索未找到与本仓库/aiotieba 相关的历史记忆；本研究结论取自本轮仓库读取与官方固定版本访问。

### 2026-09-26 补充复核：新项目定位规范

- 研究问题：只读复核 `.trellis/spec/project-positioning.md` 的项目定位、固定基线、当前 .NET 行为和证明边界；未修改该规范。
- 发现一处小范围措辞问题：`:60` 只要求使用 `Task<T>`，而 `AioTieba4DotNet/Contracts/IClientModule.cs:12` 的 `InitWebSocketAsync` 现行返回 `Task`。建议写为“异步调用使用 `Task` 或 `Task<T>`”，避免把无返回值操作读成必须人为增加结果类型。
- 其余本轮检查未发现事实性误述或范围扩张：固定标签/SHA 与可执行冻结常量一致，三种入口共享 `TiebaClientComposition.CreateRuntime`（`:27` 至 `:53`），相对文档链接存在；目标、实现、离线/在线和覆盖率的区分明确。
- 主会话已采用上述措辞修正，将规范改为 `Task` 或 `Task<T>`。
