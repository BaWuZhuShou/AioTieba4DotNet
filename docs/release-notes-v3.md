# AioTieba4DotNet v3 发布说明

v3 是一次“公开边界更清楚，文档入口更清楚，发布治理更清楚”的发布线。它没有把客户端根故事推倒重来，但把长期混杂的模块职责和用户文档路径重新整理成了稳定形态。

## 用户可见变化

### 1. 支持矩阵冻结到 .NET 10

v3 只支持 `net10.0`。这让文档、示例和发布契约终于可以围绕同一条技术线来写，而不用继续在 README 里混放多个过渡时代的叙事。

### 2. 六个公开模块成为主叙事

v3 的根客户端稳定为:

- `Forums`
- `Threads`
- `Users`
- `Admins`
- `Messages`
- `Client`

其中最值得升级方注意的是:

- `Messages` 成为消息业务 canonical 入口
- `Admins` 成为吧务和后台管理 canonical 入口
- `Client` 只保留生命周期和初始化职责

### 3. 用户文档 IA 改成“从旅程出发”

主文档链路现在是:

1. `README.md`
2. `docs/getting-started.md`
3. 四份按任务组织的 how-to 页面
4. `docs/modules.md` 参考索引
5. `docs/advanced.md` / `docs/troubleshooting.md`
6. `docs/migration-v2-to-v3.md` / `docs/parity-v3.md`

## 发布治理

v3 发布线继续采用 **build/codegen/docs/packaging/evidence contract only** 的治理模型。

- GitHub Actions **不运行 `dotnet test`**
- deterministic、integration、live 验证都通过本地或 agent 环境执行
- release gate 在 pack / publish 之前检查 restore、build、codegen、docs-contract、packaging 和 evidence presence

## 本地验证契约

发版前需要保证以下文件齐备并且非空:

- `README.md`
- `docs/getting-started.md`
- `docs/how-to-forums.md`
- `docs/how-to-threads.md`
- `docs/how-to-users.md`
- `docs/how-to-messages.md`
- `docs/modules.md`
- `docs/advanced.md`
- `docs/troubleshooting.md`
- `docs/migration-v2-to-v3.md`
- `docs/release-notes-v3.md`
- `docs/parity-v3.md`

此外还要保留本地验证 manifest 与三份 lane evidence:

- `.sisyphus/evidence/local-verification.manifest.json`
- `.sisyphus/evidence/local-deterministic-verification.md`
- `.sisyphus/evidence/local-integration-verification.md`
- `.sisyphus/evidence/local-live-verification.md`

## 发布前最小检查清单

1. `dotnet restore --nologo`
2. `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore --nologo`
3. `dotnet run --project ProtoGenerator/ProtoGenerator.csproj`
4. `pwsh ./scripts/verify-local.ps1 -ValidateOnly`
5. 完整检查 README 和 docs 导航链路

## 相关文档

- [Getting Started](./getting-started.md)
- [Migration v2 to v3](./migration-v2-to-v3.md)
- [Parity v3](./parity-v3.md)
