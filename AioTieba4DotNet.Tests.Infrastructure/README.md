# AioTieba4DotNet.Tests.Infrastructure

这个项目承载在线测试家族的共享基础设施，包括环境模板、分类契约、执行门禁、补偿审计，以及仓库路径等支撑代码。它本身不是功能场景集合，主要服务于 `Safe`、`Restricted` 和 `Suite` 三个项目。

## 什么时候用

- 需要查看或调整在线测试的分类词汇时，比如 `Feature:*`、`Tier:*`、`Stage:*`、`Suite:*`，以及矩阵支持的首类 `Api:*` 集合。
- 需要确认本地在线测试环境的模板文件名、环境变量名和 fail-closed 门禁规则时。
- 需要给 Safe / Restricted / Suite 项目补共享执行基类、补偿审计或仓库辅助代码时。

## 本地怎么用

先看这里提供的跟踪模板文件，模板必须保持空白，不要把真实凭据或线上资产写回仓库：

- `online-test.safe.template.json`
- `online-test.restricted.template.json`

本地运行基础契约时，可以直接执行：

```bash
dotnet test AioTieba4DotNet.Tests.Infrastructure/AioTieba4DotNet.Tests.Infrastructure.csproj --configuration Release --nologo --filter "TestCategory=Contract:Architecture"
```

如果你只想看默认档位和受限档位的基础元数据，也可以用档位过滤：

```bash
dotnet test AioTieba4DotNet.Tests.Infrastructure/AioTieba4DotNet.Tests.Infrastructure.csproj --configuration Release --nologo --filter "TestCategory=Tier:Safe"
dotnet test AioTieba4DotNet.Tests.Infrastructure/AioTieba4DotNet.Tests.Infrastructure.csproj --configuration Release --nologo --filter "TestCategory=Tier:Restricted"
```

环境模板是否保持空白、Restricted 是否按显式 opt-in 和 capability 门禁失败关闭，主要由 `AioTieba4DotNet.Tests.Online.Suite` 里的契约测试验证。

## 关键说明

- Safe 和 Restricted 都依赖这里声明的共享分类与执行契约，不要在各项目里发散出另一套命名。
- `docs/related/public-api-coverage-matrix.md` 是公开覆盖声明的 canonical source，`Contracts/OnlineTestMetadata.cs` 是可执行分类词汇表；两者必须同步，只把直接、非 deferred 的在线行公开成首类 `Api:*` 分类。
- Root 构造器、根访问器、factory/DI surface、默认接口属性、离线 helper，以及 deferred 行都不应在这里被注册成首类 `Api:*` 常量。
- 跟踪模板只提供字段形状与默认空值，本地真实运行请用环境变量覆盖，例如 `TIEBA_ONLINE_SAFE__...` 或 `TIEBA_ONLINE_RESTRICTED__...`。
- 真实在线测试仍然是本地执行路径，CI 维持 build-only，不负责跑这些在线场景。
