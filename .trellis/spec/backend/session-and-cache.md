# 会话与缓存所有权

本库没有数据库、ORM、迁移或持久化仓储。有状态的边界是每个客户端的会话、传输和吧信息查询缓存。

## 客户端与账号生命周期

[TiebaClientComposition](../../../AioTieba4DotNet/Clients/TiebaClientComposition.cs) 为每个客户端创建全新的会话／协议对象图。[TiebaClient.Dispose](../../../AioTieba4DotNet/Clients/TiebaClient.cs) 释放该会话；[TiebaClientSession](../../../AioTieba4DotNet/Session/TiebaClientSession.cs) 释放其门控、TBS 服务和可释放的传输对象。

[HttpCore](../../../AioTieba4DotNet/Transport/Http/HttpCore.cs) 拥有自己创建的客户端；注入的 `HttpClient` 实例仅在明确要求转移所有权时才会被释放。保留直接构造／DI 生命周期。多账号使用工厂创建独立客户端，不要在共享 singleton 上替换凭据。

区分公开的 [Contracts.Account](../../../AioTieba4DotNet/Contracts/Account.cs) 凭据输入与内部的 [Session.Account](../../../AioTieba4DotNet/Session/Account.cs)；后者拥有凭据、延迟创建的设备 ID 和加密状态。[GlobalUsings](../../../AioTieba4DotNet/GlobalUsings.cs) 将 `Account` 别名指向内部类型。公开签名必须使用公开契约。

会话保留可变的 `TiebaOptions`；HTTP 策略在构造时快照超时／重试值，而调度时从选项读取传输模式。目前没有统一的实时重新配置契约。应在构造客户端前完成配置，不要宣称后续修改属于受支持的重新配置。

## 认证与状态修改

[TiebaSessionAuthPolicy](../../../AioTieba4DotNet/Session/TiebaSessionAuthPolicy.cs) 区分缺少凭据与缺少已初始化状态。支持访客；需要认证的操作通过会话／调度器门控失败，不应发出畸形远程请求。

[TiebaSessionState](../../../AioTieba4DotNet/Session/TiebaSessionState.cs) 区分访客／已认证会话，以及 `Unavailable`、`Pending`、`Initializing`、`Ready` 资源状态。[TiebaSessionStateStore](../../../AioTieba4DotNet/Session/TiebaSessionStateStore.cs) 对快照加锁。保留状态转换及同步映射到账号上的值：

- [TiebaSessionTbsService](../../../AioTieba4DotNet/Session/TiebaSessionTbsService.cs) 检查缓存 TBS、串行化初始化、再次检查，然后保存非空结果。刷新会强制加载。失败时恢复状态并重新抛出；`finally` 释放门控。
- WS 预热、z-id 初始化和同步使用会话拥有的门控，失败时恢复先前状态。
- [ClientProtocol](../../../AioTieba4DotNet/Protocols/ClientProtocol.cs) 通过 `ApplySessionMutation` 应用成功的 z-id／同步值；执行器不得提前发布部分成功状态。

这些门控针对具体操作，不保证所有字段／调用之间的原子性。[StateParityContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/StateParityContractTests.cs) 记录回滚、账号映射值和修改顺序，包括先发生 WS 回退，再执行 HTTP 状态修改。

## 吧信息查询缓存

[ForumInfoCache](../../../AioTieba4DotNet/Internal/ForumInfoCache.cs) 在 `MemoryCache` 中同时保存名称到 ID 和 ID 到名称的映射。同一个运行时实例供 `ForumProtocol` 和 `AdminProtocol` 使用。ID 未命中返回 `0`，名称未命中返回空字符串。复用此缓存，不要并行维护字典。

条目目前没有 TTL／大小限制；缓存没有公开释放接口。不要声称其提供持久存储、淘汰／新鲜度策略、自动刷新或跨客户端共享。

避免全局凭据／TBS 缓存、端点内部的生命周期门控、吞掉取消，以及在远程成功前发布状态。
