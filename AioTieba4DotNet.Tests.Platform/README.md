# AioTieba4DotNet.Tests.Platform

这个项目承载活动测试拓扑里的 shared runtime support。它负责环境模板、repo-path helper、执行基类，以及场景和治理都会复用的 support utility。

## 它负责什么

- online 测试运行时支撑
- fail-closed 环境模板与加载约定
- 仓库路径与执行辅助
- 共享基类和 support primitive

在线测试运行模板 `online-test.safe.template.json` 和 `online-test.restricted.template.json` 必须保持空白并 fail-closed，不在仓库里填入真实凭据或线上资产。

## 它不负责什么

- 它不是 discoverability-scanned runnable scenario assembly
- 它不是 ordered-suite host
- 它不拥有 `safe` / `restricted` / `sequence-dry-run` wrapper

活动场景请看 `../AioTieba4DotNet.Tests.Online/README.md`，治理与 wrapper 请看 `../AioTieba4DotNet.Tests.Governance/README.md`。
