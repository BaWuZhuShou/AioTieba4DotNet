# 质量与验证

## 源码约定

[.editorconfig](../../../.editorconfig) 管理风格：四空格、CRLF、UTF-8、文件末尾换行、去除行尾空白、文件范围 C# 命名空间、`_camelCase` 私有字段、PascalCase 常量／静态只读字段，以及优先排列 system using。它倾向使用 `var`、主构造函数和集合表达式。遵循附近手写代码的风格，不要重排生成文件或无关文件。

[Directory.Build.props](../../../Directory.Build.props) 选择 C# 14／.NET 10 和确定性构建。当前禁用了构建时分析器执行和代码风格强制检查；编译成功不能证明编辑器建议已通过。可空引用／隐式 using 配置由项目分别决定，库与生成器启用这些配置。[Directory.Packages.props](../../../Directory.Packages.props) 管理依赖版本；共用 props 启用锁文件。

保持公开 XML 文档与默认值一致。使用局部警告抑制保留协议要求的算法，参见 [Session.Account](../../../AioTieba4DotNet/Session/Account.cs)。避免公开 DTO 数据字段、大范围警告抑制、仅存在于源码的对齐／认证标记，以及手改 protobuf 输出。

## 构建与文档检查

使用 [global.json](../../../global.json) 允许的 SDK，在仓库根目录运行。下列命令描述检查方式，不代表已执行。还原需要访问 NuGet 或拥有缓存包。

| 目的 | 命令 | 效果／前置条件 |
| --- | --- | --- |
| 锁定还原 | `dotnet restore AioTieba4DotNet.sln --locked-mode` | 清单与锁文件不一致时失败 |
| 编译 | `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore` | 需要已还原依赖，不运行测试 |
| 只读文档／证据验证 | `bash scripts/verify-local.sh --validate-only` | 需要 `python` 指向 Python 3 |
| PowerShell 等价检查 | `pwsh -File scripts/verify-local.ps1 -ValidateOnly` | 验证目的相同 |
| 查看有序执行计划 | `bash scripts/test-lane.sh sequence-dry-run` | 只打印计划，不执行测试 |

[verify-local.sh](../../../scripts/verify-local.sh) 和 [verify-local.ps1](../../../scripts/verify-local.ps1) 通常会重写清单；只读检查应使用仅验证模式。它们会打印文档命令，但不会安装／构建站点或执行测试。VitePress 验证参见[前端指南](../frontend/index.md)。

验证器中的 Bash `required_docs` 和 PowerShell `$requiredDocs` 列表管理必需文档路径。验证还要求 `.sisyphus/evidence/` 下保留四个产物：`parity-truth-freeze.json`、`parity-gap-ledger.json`、`local-verification.manifest.json` 和 `local-verification.manifest.schema.json`。这个被忽略的目录可能未随检出提供；本次规范初始化时四个文件均不存在。应报告缺失的前置条件，不得伪造证据或削弱验证。清单同步不会重建其他产物，历史任务证据也不能替代它们。

## 测试拓扑与选择

[OnlineTestProjectTopology](../../../AioTieba4DotNet.Tests.Governance/Contracts/OnlineTestProjectTopology.cs) 和共用构建文件定义：

- Platform：共用支持／环境／测试夹具门控／执行辅助逻辑，不是可运行场景程序集。
- Online：由可发现性扫描识别的 Safe／Restricted 场景程序集。
- Governance：有序在线套件及保留的离线契约。项目名和 `Contract:*` 分类都不能保证离线执行。[ThreadWebSocketOnlineContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketOnlineContractTests.cs) 同时属于 `Contract:Architecture` 和在线 `Tier:Safe`。

对于映射变更，已具体检查过的离线选择如下：

```bash
dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --filter "FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.MappingCoverageContractTests" -p:CollectCoverage=false
```

此类映射内存中的 protobuf／JSON 测试夹具；它会按需构建／还原，但不能证明整个套件成功或覆盖率达标。其他选择应先检查再执行。不要将未筛选的 Governance／整个解决方案测试当作离线工作运行。基于反射的契约可能需要最新的 Release 库／Online 程序集；解决方案构建会提供这些输出。

断言传输协议形态、公开属性、错误、回滚和传输选择。参照[映射](../../../AioTieba4DotNet.Tests.Governance/Contracts/MappingCoverageContractTests.cs)、[WS 测试替身](../../../AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketDirectContractTests.cs)和[组合](../../../AioTieba4DotNet.Tests.Governance/Contracts/ClientLifecycleAndCompositionContractTests.cs)示例。单条日志或未抛异常的调用本身不能证明行为。

## 内部重构的兼容性基线

### 1. 范围与触发条件

调整内部依赖、组合或共享实现时，先在未重构的产品上建立行为表征，再迁移。公开 API 静态基线由 [PublicApiBaselineContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/PublicApiBaselineContractTests.cs) 比较；固定文件位于 [public-api.txt](../../../AioTieba4DotNet.Tests.Governance/Contracts/Baselines/public-api.txt)。它保护指定元数据维度，不等于全部二进制、运行时或真实在线兼容性证明。

### 2. 验证入口

```bash
dotnet build AioTieba4DotNet.sln --configuration Release --no-restore
dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.PublicApiBaselineContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.ForumLookupBehaviorContractTests' -p:CollectCoverage=false
```

先完成锁定还原。整个解决方案的 Release 构建可避免既有反射测试从 Online 输出目录加载旧产品副本。检查实际发现和执行的用例，零用例或跳过不算通过。

### 3. 静态与行为契约

快照以确定性格式记录对外可见类型、成员、参数名/默认值/修饰、泛型约束、可空性、访问器、枚举/常量及调用或序列化相关属性。参考 [提取器](../../../AioTieba4DotNet.Tests.Governance/Contracts/PublicApiSnapshot.cs) 和 [维护说明](../../../AioTieba4DotNet.Tests.Governance/Contracts/Baselines/README.md) 的具体维度与局限。测试只比较，不自动接受变化；内部重构要求零差异。

吧信息查询使用 [ForumLookupBehaviorContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/ForumLookupBehaviorContractTests.cs) 和拒绝未声明请求的受控 handler，通过实际组合入口断言结果、错误、取消、顺序和缓存。Governance 已有 `InternalsVisibleTo`，优先使用该接缝，不为测试新增公开产品类型或重复构建一套协议对象图。

### 4. 错误矩阵

| 情况 | 处理 |
| --- | --- |
| 公开元数据出现差异 | 测试失败并显示首处差异；调查变更，不重写预期以通过 |
| 基线不存在、空白或读取失败 | 失败；不能从当前产品自动生成后继续成功 |
| 新增签名形状暂不支持 | 扩展提取规则和有效性样本，再评审基线 |
| 缺 SDK、还原资产或治理证据 | 记录具体前置缺失，不降低版本/门槛或伪造结果 |

### 5. 场景

良好场景：新旧内部实现通过同一份 API 基线和查询行为用例。基准场景：表征在原实现上先通过，并记录源码提交与程序集来源。错误场景：将旧实现的实际输出改成“更合理”的值，再重新生成快照掩盖变化。

### 6. 必需断言

公开快照的有效性样本应能区分默认值、可空性、访问器和枚举变化。查询表征覆盖冷热缓存、跨协议共享/跨客户端隔离、认证先后、取消和失败，保留不同入口的缓存条件和操作名。静态快照不能替代行为断言，WireParity/SignatureParity 的既有通过也不能单独证明所有请求/签名已冻结。

### 7. 正误示例

相对 URL 夹具应使用 `relative/path` 或 `./relative`；`/relative` 在 Linux 上可被 `UriKind.Absolute` 解析为文件 URI，不能据此断言它在所有平台都走相对 URL 回退。修正夹具时保留原断言与产品行为，并记录重构前既有失败。

错误做法：失败时写回 `public-api.txt` 再比较。正确做法：从独立保存的未重构程序集显式生成基线，记录来源；后续测试仅比较并由人工审阅任何改变。

## 在线通道会真实执行操作

[test-lane.sh](../../../scripts/test-lane.sh)／[test-lane.ps1](../../../scripts/test-lane.ps1) 要求显式提供通道参数。指南中关于默认 safe 通道的说法描述的是规则，不是包装脚本无参数时的行为。

| 通道 | 执行内容 |
| --- | --- |
| `safe` | `Suite:SafeOrdered`：ForumFoundation、ForumExtensions、ThreadRead、UserSocial、Messaging、ThreadWrite |
| `restricted` | `Suite:RestrictedOrdered`：ModerationRestricted、AdminRestricted |
| `sequence-dry-run` | 仅计划 |

仅在真实在线执行属于任务范围且满足测试夹具门控时，使用 `bash scripts/test-lane.sh safe` 或 `pwsh -File scripts/test-lane.ps1 -Lane safe`；仅在明确选择时使用 `restricted`。Safe 包含真实消息／主题写入及补偿，并非只读。缺少凭据也不能保证没有网络请求，因为访客可执行的安全能力仍可能运行。复用 [OnlineExecutionGate](../../../AioTieba4DotNet.Tests.Platform/Execution/OnlineExecutionGate.cs) 及既有环境加载方式，不要临时拼凑密钥。被门控阻止／inconclusive 的结果不能证明真实在线行为成功。

`CompensationAudit` 是套件综合报告，不是可运行通道／筛选条件。只有[公开 API 覆盖矩阵](../../../docs/related/public-api-coverage-matrix.md)支持且不属于延期行时，才能公布直接 `Api:*` 筛选条件。

## 覆盖率与 CI

[Directory.Build.targets](../../../Directory.Build.targets) 声明仓库总行／分支覆盖率目标为 100%，仅对生成代码作有限排除。规则范围是维护中的手写库／生成器代码，不包括测试／文档／证据／上游 Python。不要为使检查通过而降低阈值或扩大排除范围。

声明的规则不等于已达成／已强制执行的覆盖率证据：中央包列表列有 `coverlet.msbuild` 版本，但当前有效项目没有引用它，包装脚本也禁用了采集。宣称覆盖率前，核对采集器接入和实际报告。

[GitHub 工作流](../../../.github/workflows/) 的验证集中于还原／构建／代码生成／打包，同时包含 CodeQL 分析和发布。它们不运行 `dotnet test` 或需要密钥的通道。本地真实在线证据与 CI 构建成功保持分开。

按[项目定位与上游对齐](../project-positioning.md)分别报告对齐目标、实现记录、离线契约、在线行为和覆盖率证据；这些层次不能相互替代。

## 公开变更审查

公开行为变更需审查 [README](../../../README.md)、[模块参考](../../../docs/reference/modules.md)、任务指南、[对齐台账](../../../docs/related/parity.md)及发布／迁移说明。用法／包身份变化还要求同步[使用者技能](../../../skills/aiotieba4dotnet/SKILL.md)及其引用。长期跨目录规则写入最近的规则文件和 [.junie/guidelines.md](../../../.junie/guidelines.md)，不要重复整份指南。

依据签名、异常契约、默认值、文档行为和受支持的 TFM 评估 SemVer。仅修改规范需要文字／引用／导航／空白格式检查，不需要真实在线测试夹具或 protobuf 重新生成。
