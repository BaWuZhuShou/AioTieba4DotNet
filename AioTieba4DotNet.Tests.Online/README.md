# AioTieba4DotNet.Tests.Online

这个项目是当前唯一的 discoverability-scanned runnable scenario assembly。所有可发现的在线场景都收敛到这里，Safe 和 Restricted tiers 都位于 `Tiers/` 下。

## 它负责什么

- 可执行在线 scenario
- `Tier:Safe` 与 `Tier:Restricted` discoverability
- matrix-backed 直接 `Api:*` 场景分类

## 它不负责什么

- 它不提供共享环境模板或 execution base，那个归 `AioTieba4DotNet.Tests.Platform`
- 它不是 ordered-suite host，那个归 `AioTieba4DotNet.Tests.Governance`
- 它不拥有 wrapper 路由，`safe` / `restricted` / `sequence-dry-run` 由 Governance 持有

按矩阵里的直接分类运行时，请以 `docs/related/public-api-coverage-matrix.md` 为 discoverability truth。
