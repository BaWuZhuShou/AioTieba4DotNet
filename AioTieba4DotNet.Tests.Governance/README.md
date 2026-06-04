# AioTieba4DotNet.Tests.Governance

这个项目是当前活动测试拓扑里的 governance owner。它承载 ordered suite host、topology/discoverability/docs/artifact/wrapper/environment/cleanup contracts、retained offline contract tests，以及 wrapper-owned `safe` / `restricted` / `sequence-dry-run`。

## 它负责什么

- `Suite:SafeOrdered` 与 `Suite:RestrictedOrdered`
- topology、discoverability、docs、artifact、wrapper、environment、cleanup contracts
- retained offline contract tests
- `scripts/test-lane.*` 路由到的 wrapper 入口

## 常用入口

```bash
dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Suite:SafeOrdered"
dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Suite:RestrictedOrdered"
pwsh ./scripts/test-lane.ps1 safe
pwsh ./scripts/test-lane.ps1 restricted
pwsh ./scripts/test-lane.ps1 sequence-dry-run
```

Discoverability ownership 仍然在 `docs/related/public-api-coverage-matrix.md`，parity ownership 仍然在 `docs/related/parity.md`。
