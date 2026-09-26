# 目录结构与公开契约

## 包边界

| 仓库路径 | 职责 |
| --- | --- |
| `AioTieba4DotNet/Clients/` | 根命名空间中的客户端／工厂接口、实现与组合 |
| `AioTieba4DotNet/Contracts/` | 模块接口、选项、公开账号输入和传输模式 |
| `AioTieba4DotNet/Modules/` | 委托内部协议执行的公开门面 |
| `AioTieba4DotNet/Protocols/` | 内部编排、校验和能力声明 |
| `AioTieba4DotNet/Api/` | 内部上游请求族系、打包／解析和 protobuf 文件 |
| `AioTieba4DotNet/Transport/` | 调度器及 HTTP/WebSocket 机制 |
| `AioTieba4DotNet/Session/` | 内部凭据、设备标识、认证和生命周期状态 |
| `AioTieba4DotNet/Models/` | 面向使用者的 DTO 与枚举 |
| `AioTieba4DotNet/Internal/` | 共用辅助逻辑和 `Mapping/` 转换 |
| `AioTieba4DotNet/Exceptions/` | 声明在根命名空间中的公开异常 |
| `ProtoGenerator/` | 手写的发现、规划、执行和控制台报告逻辑 |
| `AioTieba4DotNet.Tests.Platform/` | 共用运行时、环境模板、执行基类和支持逻辑 |
| `AioTieba4DotNet.Tests.Online/` | `Tiers/` 下由可发现性扫描识别的场景 |
| `AioTieba4DotNet.Tests.Governance/` | 有序套件宿主、治理契约和保留的离线测试 |
| `aiotieba/` | 上游对照源码目录，不属于 .NET 交付和覆盖率范围；当前检出未包含此目录 |

SDK 基线见 [global.json](../../../global.json)；[Directory.Build.props](../../../Directory.Build.props) 选择 `net10.0` 和 C# 14。[Directory.Packages.props](../../../Directory.Packages.props) 管理包版本。不要借顺手清理引入多目标框架。

## 公开入口

[TiebaClient](../../../AioTieba4DotNet/Clients/TiebaClient.cs)、[ITiebaClient](../../../AioTieba4DotNet/Clients/ITiebaClient.cs)、[AddAioTiebaClient](../../../AioTieba4DotNet/DependencyInjection.cs) 和[工厂](../../../AioTieba4DotNet/Clients/TiebaClientFactory.cs) 是受支持的入口。保留六个模块属性：`Forums`、`Threads`、`Users`、`Admins`、`Messages`、`Client`。

- `Messages` 负责消息读取、发送、已读状态更新和推送解析；`Client` 负责 WebSocket 初始化、z-id 初始化和同步等生命周期辅助操作。
- 公开契约使用 `Models/` 与 `Contracts/` 中的 DTO／枚举。请求、会话／传输类型及生成的 protobuf 类型保持内部可见。
- 保留名称、默认值、重载、异常和命名空间。目录位置不决定命名空间：客户端／异常使用 `AioTieba4DotNet`；模块实现当前使用 `AioTieba4DotNet.Modules`。
- 既有库规范和跨目录指南提及保留 `Client.cs` 兼容门面，但当前检出没有该文件。不要依据指南文字宣传或重建这个不存在的入口。删除已承诺的契约仍需明确的兼容性／迁移决策。

## 功能调用流程

遵循现有主题读取路径（以下路径相对于库目录）：

```text
Contracts/IThreadModule.cs -> Modules/ThreadModule.cs
  -> Protocols/ThreadProtocol.cs -> Transport/TiebaOperationDispatcher.cs
  -> Api/GetThreads/GetThreads.cs -> Internal/Mapping/Models/Threads/ThreadsMapper.cs
  -> Models/Threads/Threads.cs
```

[ThreadModule](../../../AioTieba4DotNet/Modules/ThreadModule.cs) 提供默认值，并转发至 [ThreadProtocol](../../../AioTieba4DotNet/Protocols/ThreadProtocol.cs)。协议选择能力与执行器，API 族系负责传输协议细节。避免在门面中加入 HTTP 调用、protobuf 解析或重复认证。

[TiebaClientComposition.CreateRuntime](../../../AioTieba4DotNet/Clients/TiebaClientComposition.cs) 为所有入口构建会话、调度器、协议、缓存和模块。DI 将 `ITiebaClient` 注册为 scoped，将 `ITiebaClientFactory` 注册为 singleton；工厂创建的客户端获得独立运行时。组合逻辑应统一收敛于此，参见[组合契约](../../../AioTieba4DotNet.Tests.Governance/Contracts/ClientLifecycleAndCompositionContractTests.cs)。

跨协议的吧信息协作使用既有 `Protocols/` 层内的窄接口：[IForumIdentityResolver](../../../AioTieba4DotNet/Protocols/IForumIdentityResolver.cs) 提供吧 ID/名称查询，[IForumCategoryResolver](../../../AioTieba4DotNet/Protocols/IForumCategoryResolver.cs) 提供字符串分类查询。Thread 依赖两者，User 只依赖身份接口，不依赖完整 `IForumProtocol`。分类实现仍由 ForumProtocol 提供。

[ForumIdentityResolver](../../../AioTieba4DotNet/Protocols/ForumIdentityResolver.cs) 统一身份查询、详情读取及对应缓存写入；Forum/Admin 按其协作需要使用具体服务。组合根每客户端创建一份 resolver 和原 ForumInfoCache。resolver 不依赖业务协议，避免 GetFname → GetDetail 产生反向循环；缓存与认证顺序见[会话与缓存规范](./session-and-cache.md)。

## 代码归属规则

- 扩展公开行为时，修改对应接口、模块和协议；传输协议操作放在内部 `Api/<UpstreamFamily>/` 中。
- 增加层次前先查找已有 mapper／请求辅助逻辑。保留上游导出语义；[parity.md](../../../docs/related/parity.md) 是映射的权威来源。
- 不添加仅存在于源码的对齐／认证属性标记，不公开生成类型，也不只凭方法名推断认证需求。
- 将旧代码删除与无关清理分开；规范初始化不构成删除受支持契约的授权。
