# 研究：对外兼容性基线与离线验证门槛

- 研究问题：现有验证能否保障内部架构演进不改变公开 API 和调用者可观察行为；首批吧信息查询依赖收敛前需要补齐什么。
- 范围：混合；以当前仓库源码、规范和本机前置条件为主，补充 Microsoft 官方 API 兼容性工具文档。
- 日期：2026-09-26
- 活动任务：`.trellis/tasks/09-26-internal-architecture-evolution`
- 本次只研究，未改产品、规范、任务元数据，未执行 Git 操作、构建、测试或在线通道，未读取凭据。

## 研究发现

### 1. 结论与首批边界

现有测试可以保护部分组合、DTO 映射、请求打包及状态恢复，但没有发现覆盖全部公开类型与成员的固定元数据基线。公开覆盖矩阵也不是编译产物的完整签名比较器。首批建议：**先冻结公开元数据，再为吧信息查询及其调用路径补齐离线行为表征，最后收敛内部协议依赖**。

按主会话明确的首批范围，本轮不修改共享 HTTP/WS 传输、签名、重试或上游对齐策略；这些区域的测试缺口记录为后续演进条件，不要求本轮扩展为传输治理工程。

### 2. 找到的文件与实际保障

下表路径相对于仓库根目录。测试文件统一位于 `AioTieba4DotNet.Tests.Governance/Contracts/`。

| 文件与代码位置 | 实际职责和保护范围 | 限制 |
| --- | --- | --- |
| `ClientLifecycleAndCompositionContractTests.cs:23`、`:44`、`:102`、`:121` | 访客/认证构造，Account/Options 输入映射，工厂具体/接口重载，以及部分配置错误 | 共 8 个测试；通过反射读取 `_lifetime`、`_account` 等实现细节，不是完整公开签名快照 |
| 同文件 `:70`、`:161`、`:203` | 直接构造的 HTTP 释放；命名 HttpClient、singleton 工厂、scoped 客户端；DI 解析时的配置异常 | 没有完整证明注入 HttpClient 所有权、工厂多账号隔离、全部资源释放和并发行为 |
| 同文件 `:326`、`:550` | 固定六模块名称及返回实例实现对应接口 | 检查存在且类型兼容，未枚举并冻结全部公开成员、默认值、参数名、nullable 或新增泄漏 |
| `UserAndMessageSurfaceContractTests.cs:18`、`:33` | `UserContentCmd == 303002` 和 push payload 的 5 个映射字段 | 仅 2 个局部表面/行为契约 |
| `PublicApiCoverageMatrixContract.cs:150` | 读取 Markdown 表格、检查格式及覆盖分类 | 输入来自文档，不枚举产品程序集公开 API，也没有与全部实际重载做逐项对比 |
| `OnlineArchitectureContractTests.cs:130`、`:141`、`:163` | 覆盖矩阵、`Api:*` 声明和可发现 Online 测试的一致性 | 是测试治理，不是完整 ABI/API 冻结；可发现不等于测试实际成功 |
| `ThreadWebSocketDirectContractTests.cs:36`、`:54`、`:123`、`:148` | 11 个直接请求契约：GetThreads/GetPosts 的 HTTP/WS protobuf 一致、页码/数量规则、有效/空/坏载荷、服务端错误、GetComments 无 error 载荷 | 使用 `ITiebaHttpCore`/`ITiebaWsCore` 替身；证明请求层，不覆盖完整 dispatcher 的所有模式或取消 |
| `StateParityContractTests.cs:15`、`:69` | 2 个测试严格比较 10 行状态样本，包含 TBS/z-id/sync/WS 成功、回退和 4 类失败恢复，以及状态发布顺序 | 失败样本主要是 `InvalidOperationException`；不等于取消、并发、锁等待和关闭场景已覆盖 |
| `WireParityContractTests.cs:16`、`:70` | 3 个测试运行部分受控请求捕获，确认比较器能显式发现 scheme/header/fallback 等差异 | **主测试要求至少出现差异，而非所有行为匹配**；不能把绿色结果解释为当前请求形态已冻结 |
| `SignatureParityContractTests.cs:25`、`:35`、`:45`、`:60` | 2 个测试比较字段顺序和动态字段屏蔽，检测人为置换 | expected 的 `sign` 来自被测 signer 的输出；缺少独立已知摘要值，不保证算法/盐值无漂移 |
| `MappingCoverageContractTests.cs:15`、`:61`、`:130`、`:211` | 12 个内存 DTO 映射测试，覆盖用户共用字段、帖子/主题局部字段、无效数字默认值、链接/头像边界等 | 局部行为契约，不是全部 DTO 元数据和全部映射覆盖 |
| `RetainedTransportParitySupport.cs:136`、`:200`、`:227`、`:235` | 受控 HTTP handler、假 WS 回退、传输异常和超时异常精确类型断言 | 可复用的离线接缝；并非完整的重试/取消表征 |
| `RetainedSessionStateParitySupport.cs:283`、`:341`、`:473` | 构建内存会话、记录回退和失败前后状态 | 测试辅助代码，可以复用思路，避免为本轮再建一套复杂框架 |
| `AioTieba4DotNet/Properties/AssemblyInfo.cs:3` | 对 Governance 提供 `InternalsVisibleTo` | 新离线表征可以直接使用内部替身，无需为测试暴露产品公开 API |
| `AioTieba4DotNet/Protocols/ForumProtocol.cs:39`、`:58`、`:71` | 名称/ID 查询、缓存命中、详情查询和缓存写入的现有行为 | 本轮需要补齐的路径表征依据 |
| `AioTieba4DotNet/Protocols/AdminProtocol.cs:314` | 管理操作查 ID 前的缓存与认证/TBS 顺序 | 不能在统一查询服务时意外改变门控顺序或错误上下文 |
| `AioTieba4DotNet/Internal/ForumInfoCache.cs:17`、`:27`、`:37` | 双向缓存、0/空字符串 miss 哨兵 | 不具备 TTL、持久化、并发合并或公开释放承诺 |

### 3. 公开 API：应冻结什么

当前源码搜索没有找到 `PublicAPI.Shipped`、`ApiCompat`、`EnablePackageValidation`、`GetExportedTypes` 或 `NullabilityInfoContext` 的现有兼容性检查接入。局部反射测试不能替代整体基线。

首批基线以**重构前实际可编译程序集**为准，记录固定的源码起点和生成命令。至少覆盖：

1. 所有对外可见类型及嵌套类型的完整名称、类型种类、基类/接口、泛型参数与约束、abstract/sealed/static 等修饰。
2. 对外可见构造器、方法、属性/索引器、事件、常量/字段，包含签名、成员类型、访问器可见性及适用的 protected 面。
3. 参数名、顺序、类型、`ref`/`out`/`in`、`params`、可选性、默认值、泛型返回类型与可空标注；参数名会影响具名参数源码兼容。
4. enum 底层类型和值、常量值，以及调用者会依赖的属性元数据，如 obsolete、序列化/必需成员标记。不能只拍六个接口的方法名。
5. 禁止新增公开 `Api`、`Transport`、`Session` 或生成 protobuf 类型泄漏。根命名空间、Contracts/Models 的既有公开内容需全部枚举；目录名称不能决定可见性。

首批可以在 Governance 内实现一个有限、确定性的元数据输出和固定文本比较，沿用 MSTest、不引入产品依赖。输出必须排序、使用稳定类型表示；**测试只比较，不自动更新基线**。基线更新是显式维护步骤，内部重构应要求零差异。对生成器要选有代表性的自校验样本，防止漏记默认值、nullable、访问器或 enum 值后产生虚假的绿色结果。

若采用成熟 ApiCompat 工具替代大部分自研元数据比较，可固定工具版本并保留重构前程序集；但它不能代替运行时行为表征，而且仍需确认默认值、nullable、常量等本项目要求的覆盖，不把默认工具配置视为“所有对外语义已冻结”。二者择一作为主要形状门槛，避免本轮同时搭建多套维护体系。官方工具参考见第 8 节。

### 4. 首批吧信息查询的离线行为表征

这些是内部重构前后的相同行为断言，不是新增查询语义：

| 行为 | 最低断言 | 当前依据 |
| --- | --- | --- |
| 名称/ID 缓存命中 | 返回值与既有相同；受控 HTTP/WS 调用数为 0；两方向共享同一客户端缓存 | `ForumProtocol.cs:39-68`、`ForumInfoCache.cs:17-40` |
| 缓存未命中 | 只调用原有 GetFid/GetForumDetail 路径；透传参数和 token；返回后按原有规则写入双向缓存 | `ForumProtocol.cs:47-82` |
| 无效/空结果 | 保留 0/空名称等现有哨兵和缓存写入规则，不“顺便修复”协议输入或新增缓存规则 | `ForumInfoCache.cs:19`、`:29`；各调用点现有分支 |
| 详情与查名称的关联 | 查详情成功后的缓存可被名称/ID 查询复用；注意 `GetFnameAsync` 同时涉及返回详情 ID 和输入 ID 的缓存逻辑 | `ForumProtocol.cs:66-82` |
| 失败 | 原异常类型、错误码/消息保持；不会缓存伪成功，也不会继续执行业务操作 | 现有异常规范及请求层行为 |
| 取消 | 调用前取消保持取消且不发请求；查询进行中取消向受控执行器传播；不当作成功或回退 | `ForumProtocol.cs:41`、`:60`、`:73`；dispatcher `:26`、`:104` |
| 管理操作门控 | 未认证/缺 TBS 条件下，在与当前实现相同的位置失败；缓存命中/未命中分别观察调用顺序；原操作名错误上下文不漂移 | `AdminProtocol.cs:317-328` |
| 调用链 | 选取 Thread、User、Admin 的代表路径，比较查询次数、认证准备、最终调用及参数顺序 | 如 `ThreadProtocol.cs:94-100`，具体路径以首批设计清单为准 |
| 客户端隔离 | 同一客户端协议共享查询缓存，不同直接构造/工厂/DI scope 的客户端不共享可变查询状态 | 组合规范；现有 composition 测试只能部分证明 |

不要为“更先进”而在本阶段增加 TTL、去重并发请求、改变负结果缓存、提前加载、重试或选项快照；它们都可能改变调用次数、时序和错误行为，需要独立范围与验证。

建议复用现有内部 transport 接口和可记录 handler，每个用例返回固定 protobuf/JSON 并断言请求序列；不要以日志、“不抛异常”或比对私有字段名称为主要成功标准。现有 `_lifetime`/`_account` 反射可在以后逐步减少，不能在行为证据未替代前简单删掉。

### 5. 现有可离线执行的白名单

已检查以下 7 个类的入口与辅助路径，没有加载真实 Online 环境或调用 OnlineExecutionGate；共 **40 个 `[TestMethod]` 声明**，这个数字不是执行结果：

| 类名 | 声明数 |
| --- | ---: |
| `ClientLifecycleAndCompositionContractTests` | 8 |
| `UserAndMessageSurfaceContractTests` | 2 |
| `ThreadWebSocketDirectContractTests` | 11 |
| `WireParityContractTests` | 3 |
| `SignatureParityContractTests` | 2 |
| `StateParityContractTests` | 2 |
| `MappingCoverageContractTests` | 12 |

注意生命周期测试 `ClientLifecycleAndCompositionContractTests.cs:79` 对**已释放** HttpClient 调用 `http://localhost`，正常实现会在发送前抛 `ObjectDisposedException`。如果释放回归，会尝试本地地址；它不访问真实贴吧，但也不是在任意产品缺陷下都数学保证零 socket 调用的测试。新增行为表征应始终注入阻止真实网络的 handler。

以下验证命令供实施阶段使用，**本研究没有执行**：

```bash
dotnet restore AioTieba4DotNet.sln --locked-mode
dotnet build AioTieba4DotNet.sln --configuration Release --no-restore
```

解决方案构建仅编译，包括 Online 项目，不运行在线场景。之后按明确类名筛选：

```bash
offline_filter='FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.ClientLifecycleAndCompositionContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.UserAndMessageSurfaceContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.ThreadWebSocketDirectContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.WireParityContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.SignatureParityContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.StateParityContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.MappingCoverageContractTests'
dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --no-build --no-restore --filter "$offline_filter" -p:CollectCoverage=false
```

实施时把新增且已审阅确认为离线的具体类名追加到白名单；记录测试发现数、通过/失败/跳过数，零测试或跳过不能当通过。`-p:CollectCoverage=false` 与现有局部验证惯例一致，该命令不提供覆盖率证明。

不使用未筛选的 `dotnet test`、解决方案测试或 `Contract:Architecture` 分类作为离线入口：`ThreadWebSocketOnlineContractTests.cs:23-34` 同时属于 Architecture/Safe，且会读取在线配置并运行真实 WS。`scripts/test-lane.sh:20-26` 的 safe/restricted 则执行有真实副作用的有序套件。

反射类 `ClientLifecycleAndCompositionContractTests.cs:453-478` 与 `UserAndMessageSurfaceContractTests.cs:69-94` 优先加载 Online 的 Release 副本，其次产品 Release DLL。只重建 Governance 或产品可能留下旧 Online 副本；每轮基线应由完整 Release 构建产物驱动，记录实际程序集来源，不能测试到陈旧副本后宣称本轮代码通过。

### 6. 本机前置条件：已实际核验

| 项目 | 2026-09-26 现场结果 |
| --- | --- |
| SDK 要求 | `global.json:3-5`：`10.0.201`、`latestFeature`、不接受 prerelease |
| 已安装 SDK | `dotnet --info`：仅 `10.0.112`，路径 `/usr/lib/dotnet/sdk` |
| 仓库内 SDK 解析 | `dotnet --version` 退出码 155，明确报告找不到兼容 SDK |
| Runtime | `dotnet --info`：.NET / ASP.NET Runtime `10.0.12`；runtime 可用不代表 SDK 符合仓库要求 |
| 5 个项目锁文件 | 产品、ProtoGenerator、Platform、Online、Governance 的 `packages.lock.json` 均存在 |
| 还原资产 | 上述 5 个项目的 `obj/project.assets.json` 全部不存在 |
| Release DLL | 上述 5 个项目的 `bin/Release/net10.0/<project>.dll` 全部不存在 |
| 固定上游本地目录 | `aiotieba/` 不存在 |
| 保留证据 | 下列四个 `.sisyphus/evidence/` 文件全部不存在 |

缺失文件：

- `parity-truth-freeze.json`
- `parity-gap-ledger.json`
- `local-verification.manifest.json`
- `local-verification.manifest.schema.json`

开始实施前先提供兼容 SDK 并完成锁定还原/Release 构建。不能为了通过而降低 `global.json` 基线。NuGet 缓存和网络还原能否成功未验证。

文档/证据验证入口是 `bash scripts/verify-local.sh --validate-only`；脚本 `:125-149` 强制四个产物。当前缺失不应伪造或降低门槛；恢复可信证据属于独立前置工作。此研究角色禁止 Git 操作，脚本内部含 `git rev-parse HEAD`（`:191`），因此本次未运行；这里报告的是文件存在性，不是该脚本的实际失败输出。无参数脚本默认 `--sync` 并可写 manifest（`:5`、`:221-224`），研究时不能误用。

### 7. 分阶段验收门槛

1. **环境就绪**：兼容 SDK、locked restore、完整 Release build 成功；记录基线源码身份与产物。未完成时只能称规划/源码研究完成。
2. **冻结**：新公开元数据基线覆盖完整对外面；固定文件来自未重构版本；选定白名单通过；已知 pre-existing failure 与环境失败单独列出，不能默默接受为重构通过。
3. **查询表征**：第 4 节的受影响路径用受控替身形成可重复结果，重构前通过。
4. **内部迁移**：重构后公开基线无差异，查询表征和既有白名单通过；核对无额外公开类型、默认值/取消/认证/缓存时序变化。
5. **记录边界**：只声称构建和所列离线测试通过；未执行在线、未测全覆盖率、缺失治理产物均明确保留。按阶段保留独立可回退提交，不以测试迁就实现的方式改写固定 expected。

后续若开始修改共享传输，才要求补齐当前请求形态的确定性断言、独立签名已知向量、调用者取消与策略超时区分、可重试读/不可重放写、HTTP 请求响应释放、dispatcher 模式/错误不回退等矩阵。当前产品行为依据为 `TiebaHttpExecutionPolicy.cs:39-51`、`:95-129`，`TiebaHttpErrorNormalizer.cs:8-17`，`HttpCore.cs:49-94`，`TiebaOperationDispatcher.cs:60-109`。这不是本轮已经完成的测试清单。

### 8. 外部参考与版本

- 本地构建基线：.NET SDK `10.0.201` 起的允许 feature band、TFM `net10.0`、C# `14.0`，以 `global.json` 和 `Directory.Build.props:4-5` 为准。MSTest `4.0.2`、Microsoft.NET.Test.Sdk `18.0.1` 由 `Directory.Packages.props` 声明。
- [Microsoft 官方 ApiCompat 工具文档](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/global-tool)：2026-09-26 已读取。支持程序集/包与基线比较，提供 strict mode、参数名规则和属性匹配开关。本次未安装，未选定/验证工具版本。
- [Microsoft 官方包基线验证文档](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/baseline-version-validator)：2026-09-26 已读取。说明 `EnablePackageValidation` 与 `PackageValidationBaselineVersion` 的已发布包比较方式。当前研究未确认可用的历史稳定 NuGet 包；不能直接假定 `3.0.0` 就是已发布且合适的基线。
- 上游冻结身份仅据 `.trellis/spec/project-positioning.md` 与仓库契约记录为 `lumina37/aiotieba`、`v4.6.4`、`04f8e431f87507a6228b42061c70d298b34317ff`。本研究不展开上游源码比对，也不把这些身份记录当成本轮完整对齐证明。

### 9. 相关规范

- `.trellis/workflow.md`：先规划/评审；研究写入任务目录。
- `.trellis/spec/project-positioning.md`：v3、.NET 10、六模块、三入口及证据分层。
- `.trellis/spec/backend/index.md`、`directory-structure.md`：公开边界、内部职责、按受影响层验证。
- `.trellis/spec/backend/quality-guidelines.md`：locked restore、Release build、离线过滤、保留证据、覆盖率和 CI 的限制。
- `.trellis/spec/backend/error-handling.md`、`transport-guidelines.md`：异常身份、取消、有限回退和不重放写请求。
- `.trellis/spec/backend/session-and-cache.md`：每客户端状态/缓存归属，不扩张缓存或重新配置承诺。
- `AioTieba4DotNet/AGENTS.md`、`.junie/guidelines.md`：公开面与 build-only CI。后者明确禁止 GitHub Actions 运行任何 `dotnet test`；不能把本轮离线测试自动塞入 CI 作为隐含附带变更。
- `Directory.Build.props:14-15` 禁用构建期分析器/风格强制；编译成功不能代表 lint/静态建议全通过。`Directory.Build.targets:11-20` 声明 100/100 覆盖率，但现有项目未引用采集器；声明不能当实际报告。

## 限制与未找到的内容

- 本研究没有运行构建或任何测试；SDK 已确认不兼容、还原/产物均缺失，因此不能报告基线测试通过。
- 没有完整公开 API 元数据快照、独立 ApiCompat 接入，亦未发现针对吧信息查询缓存/取消/门控的现成离线行为矩阵。结论基于当前测试与构建代码搜索，不代表历史上从未存在。
- 现有 wire/signature 契约有上述证明范围限制；本轮不改共享传输，不顺带改变既存上游差异或升级上游基线。
- 未读取真实凭据、未运行 safe/restricted、未恢复 .sisyphus 证据、未安装 SDK/工具、未核验 NuGet 历史发布包；需要分别恢复或核验。
- 现有文档提到兼容 `Client.cs`，但当前 `AioTieba4DotNet/` 文件搜索未找到该文件；公开基线应以实际程序集为准，不能照抄规范中的历史文件名构造不存在的 API 承诺。
- 7 类白名单仅证明其已检查的代码路径可用于离线验证；新增类/初始化器变化后需要重新审阅，不能只依赖 `Contract:*` 或项目名字。
