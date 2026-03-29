# AioTieba4DotNet v2 发布说明

## 发布定位

`AioTieba4DotNet` v2 是一次真正的 **SemVer major** 升级。包名与根命名空间保持不变，但公开契约做了刻意收口：保留 `TiebaClient`、DI、工厂与四个业务模块，移除 v1 时代的 facade / request-mode / public core 使用故事。

## 支持矩阵

- `net8.0`
- `net9.0`
- `net10.0`

## 这次升级保留了什么

- `TiebaClient` 直接创建
- `AddAioTiebaClient(...)` DI 注册
- `ITiebaClientFactory` 多账户创建
- `Forums` / `Threads` / `Users` / `Client` 四个根模块
- HTTP + WebSocket 双传输能力（但 transport 选择改为策略驱动）

## 这次升级改变了什么

### 1. 传输选择改为策略驱动

- 移除：`TiebaRequestMode`
- 移除：`RequestMode` 属性
- 移除：业务方法 `mode` 参数
- 新入口：`TiebaOptions.TransportMode`
  - `Auto`：默认值
  - `Http`：唯一公开覆盖

### 2. 鉴权失败可被稳定区分

- v2 对本地缺少必需凭据的情况直接抛出 `TiebaAuthenticationException`
- 服务器已经处理请求后返回的业务错误仍然使用 `TieBaServerException`

### 3. 旧 facade 与 public core 不再属于用户迁移目标

- 移除：`AioTieba4DotNet.Client`
- 内化：`HttpCore` / `WsCore` 作为用户可依赖入口的故事
- 保留但需区分：`client.Client` 模块

## 升级前建议

1. 先阅读 [v1 到 v2 迁移指南](./migration-v1-to-v2.md)
2. 搜索并删除所有 `RequestMode` / `TiebaRequestMode` 用法
3. 搜索并替换所有 `new Client(...)` facade 调用
4. 搜索并移除 `HttpCore` / `WsCore` 直连逻辑
5. 更新异常处理：新增 `TiebaAuthenticationException`

## preview -> GA 发布节奏

v2 的发布线固定为：

1. `2.0.0-preview.N`
2. `2.0.0-rc.N`
3. `2.0.0`

每个阶段都继续使用相同包身份 `AioTieba4DotNet`，因此迁移说明必须和实际公开 API 保持一致，不能依赖“后面再解释”的隐式兼容。

## GA cutover 条件

正式以 v2 替代当前主线前，至少要满足：

1. `v2` 分支的 CI 为绿色。
2. parity matrix 已完成，未覆盖能力已被明确标记为 `Deferred` 或 `Dropped`。
3. `README.md`、`docs/modules.md`、`docs/advanced.md`、`docs/migration-v1-to-v2.md` 已与保留公开契约对齐。
4. release workflow 已在发布前验证 restore/build/tests/generator consistency/migration docs。
5. 预发布（preview / rc）验证完成，且用户可见 breaking list 已公开。

## 对维护者的发布提醒

- v2 维护者发布说明已经切换到 **Trusted Publishing** 口径。仓库内的发布入口仍然是 `.github/workflows/publish.yml` 这条 tag 驱动流水线，但 **nuget.org 侧 Trusted Publishing policy 的创建与维护是外部前置条件，不属于仓库自动化范围**。
- 在第一次使用 v2 发布线前，先确认 nuget.org policy 已按下面的固定字段配置完成，否则不要执行正式发布：
  - repository owner：`BaWuZhuShou`
  - repository name：`AioTieba4DotNet`
  - workflow filename：`publish.yml`
  - GitHub Environment：默认 **不绑定**，即 no GitHub Environment binding
- `.github/workflows/publish.yml` 的文件名本身属于外部 trusted publishing 绑定的一部分。只要 workflow filename 不是 `publish.yml`，维护者就必须先更新 nuget.org policy，再继续发版。
- 本项目继续保留当前 tag 约定，不改为别的发布模型：
  - 触发标签仍然是 `v*.*.*`
  - 合法版本仍然是 `vX.Y.Z`、`vX.Y.Z-preview.N`、`vX.Y.Z-rc.N`
  - 这份清单不新增 preview rehearsal lane
- 发布前运行：
  - `dotnet restore --nologo`
  - `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore --nologo`
  - `dotnet test AioTieba4DotNet.Tests/AioTieba4DotNet.Tests.csproj -f net8.0 --configuration Release --no-build --filter "TestCategory!=Integration&TestCategory!=Live" --nologo`
  - `dotnet test AioTieba4DotNet.Tests/AioTieba4DotNet.Tests.csproj -f net9.0 --configuration Release --no-build --filter "TestCategory!=Integration&TestCategory!=Live" --nologo`
  - `dotnet test AioTieba4DotNet.Tests/AioTieba4DotNet.Tests.csproj -f net10.0 --configuration Release --no-build --filter "TestCategory!=Integration&TestCategory!=Live" --nologo`
  - `dotnet run --project ProtoGenerator/ProtoGenerator.csproj`
- 如需本地确认打包产物，再运行：
  - `dotnet pack --configuration Release --no-build --output ./nupkg -p:Version=<version> --nologo`
- 上述本地检查通过后，再推送符合规则的 `v*.*.*` tag，让 `publish.yml` 执行仓库侧发布流程。不要把 nuget.org policy 创建、Environment 绑定或旧的长期 NuGet API key secret 轮换写成仓库内自动步骤。
- 任何一次 major 相关预发布都应该同时附带迁移指南链接。

## 用户可见 breaking list（摘要）

- 删除旧 facade `AioTieba4DotNet.Client`
- 删除 `RequestMode` / `TiebaRequestMode` / 每方法 `mode`
- 删除 public core 作为主要使用故事
- 引入 `TiebaTransportMode.Auto` / `Http`
- 鉴权失败改为显式 `TiebaAuthenticationException`

完整示例见 [migration-v1-to-v2.md](./migration-v1-to-v2.md)。
