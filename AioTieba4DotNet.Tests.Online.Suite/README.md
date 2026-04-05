# AioTieba4DotNet.Tests.Online.Suite

这个项目承载 ordered suite 主机和在线测试治理契约。它负责把 Safe 与 Restricted 场景按既定阶段顺序串起来，也负责验证环境模板、项目布局、样式，以及 ordered-suite 执行约束。

## 什么时候用

- 你要跑完整的 ordered suite，而不是单个特性。
- 你要检查当前在线测试治理契约，比如 `Contract:Environment`、`Contract:ProjectLayout`、`Contract:Style`。
- 你要确认 Safe 默认只跑 `01` 到 `06` 阶段，Restricted 只在显式选择时跑 `07` 和 `08` 阶段。

## 本地怎么用

跑完整 Safe ordered suite：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Suite/AioTieba4DotNet.Tests.Online.Suite.csproj --configuration Release --nologo --filter "TestCategory=Suite:SafeOrdered" -p:CollectCoverage=false
```

跑完整 Restricted ordered suite：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Suite/AioTieba4DotNet.Tests.Online.Suite.csproj --configuration Release --nologo --filter "TestCategory=Suite:RestrictedOrdered" -p:CollectCoverage=false
```

只跑治理契约时，可以直接按契约分类过滤：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Suite/AioTieba4DotNet.Tests.Online.Suite.csproj --configuration Release --nologo --filter "TestCategory=Contract:Environment"
dotnet test AioTieba4DotNet.Tests.Online.Suite/AioTieba4DotNet.Tests.Online.Suite.csproj --configuration Release --nologo --filter "TestCategory=Contract:Style"
```

兼容脚本只是这个项目的包装层：

```bash
pwsh ./scripts/test-lane.ps1 safe
pwsh ./scripts/test-lane.ps1 restricted
pwsh ./scripts/test-lane.ps1 sequence-dry-run
```

## 关键说明

- Safe ordered suite 是默认路径，阶段顺序固定为 `Stage:01-ForumFoundation` 到 `Stage:06-ThreadWrite`。
- Restricted ordered suite 不是默认路径，只在显式选择 `Suite:RestrictedOrdered` 时运行 `Stage:07-ModerationRestricted` 和 `Stage:08-AdminRestricted`。
- 如果你只是想重跑某个功能，不要从这里入手，直接去 `AioTieba4DotNet.Tests.Online.Safe` 或 `AioTieba4DotNet.Tests.Online.Restricted` 用 `dotnet test --filter` 更准确；只有矩阵里已有直接在线证据的 `Api:*` 入口才会在那里被宣称为首类过滤面。
- Root 构造器、factory/DI surface、离线 contract rows，以及仍处于 deferred 的公开 API，都不属于 ordered suite 的首类 `Api:*` 过滤承诺。
- 这个项目会验证当前在线测试仓库现实，但真实在线场景依旧是本地执行，CI 仍然只做 build-only。
