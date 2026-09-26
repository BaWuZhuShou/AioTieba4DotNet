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

[ForumInfoCache](../../../AioTieba4DotNet/Internal/ForumInfoCache.cs) 在 `MemoryCache` 中同时保存名称到 ID 和 ID 到名称的映射。每个运行时只创建一份，由 [ForumIdentityResolver](../../../AioTieba4DotNet/Protocols/ForumIdentityResolver.cs) 持有，供 Forum/Admin/Thread/User 的查询路径共享。ID 未命中返回 `0`，名称未命中返回空字符串。复用此缓存，不要并行维护字典。

条目目前没有 TTL／大小限制；缓存没有公开释放接口。不要声称其提供持久存储、淘汰／新鲜度策略、自动刷新或跨客户端共享。

避免全局凭据／TBS 缓存、端点内部的生命周期门控、吞掉取消，以及在远程成功前发布状态。

## 吧身份解析协作契约

### 1. 范围与触发条件

修改身份解析、详情查询、管理操作的吧名重载或跨协议依赖时，遵循以下可观察顺序。分类查询继续由 ForumProtocol 通过 IForumCategoryResolver 提供，不并入身份服务。

### 2. 内部签名

- `IForumIdentityResolver.GetFidAsync(string fname, CancellationToken cancellationToken = default)` → `Task<ulong>`。
- `IForumIdentityResolver.GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)` → `Task<string>`。
- 具体服务另提供 `GetDetailAsync(ulong, CancellationToken)`、`ResolveFidForOperationAsync(string, TiebaOperationCapabilities, string, CancellationToken)` 和 `RememberForum(ulong, string)`；不把这些协作能力扩大成新的公共 API。

### 3. 状态与请求契约

普通 ID 查询先检查取消，再查缓存，未命中才以 `GetFidAsync` 描述符请求。管理专用解析先命中短路，未命中才执行原业务操作的认证/TBS 准备，随后以 `${operationName}ResolveFid` 查询。认证准备期间即使其他调用填入缓存，管理解析也不重查，保持原请求顺序。

数字详情请求仍使用 `GetDetailAsync` 描述符，按响应 ID/名称条件回填。名称查询未命中时复用详情读取，然后**另以请求 ID 无条件写入**；请求和响应 ID 不同也不能合并两次写入。GetForum 仍由 ForumProtocol 请求，仅委托 resolver 记录返回的身份。

### 4. 校验与错误矩阵

| 情况 | 保留行为 |
| --- | --- |
| 普通身份记录 ID 为 0 或名称空白 | RememberForum 不写入 |
| Admin ID 查询成功 | 保留其非 0 才写入的独立条件 |
| 名称缓存为空白字符串 | `IsNullOrEmpty` 判定下仍属命中，不改成 `IsNullOrWhiteSpace` |
| GetFname 完成详情读取 | 以请求 ID 执行第二次无条件写入，不套 RememberForum 过滤 |
| 缺凭据、取消或请求失败 | 按原阶段抛出原异常；无伪成功回填，不继续执行业务写请求 |
| Admin 辅助解析缓存命中 | 只跳过辅助阶段的认证准备，实际业务操作的认证仍保留 |

### 5. 良好、基准与错误场景

良好：一个客户端的 Forums 查询可以被 Thread/User/Admin 复用，另一客户端仍独立请求。基准：GetFname 请求 ID 为 A、响应 ID 为 B 时，两个 ID 都可得到名称，而最后的反向映射指向 A。错误：把 Admin 的准备与查询替换成普通 GetFid 调用，导致中途重新查缓存并改变请求次数。

### 6. 必需测试与评审

[ForumLookupBehaviorContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/ForumLookupBehaviorContractTests.cs) 覆盖共享/隔离、冷热缓存、两次回填、空值、失败/取消、管理认证和并发填充、Thread.Good 的分类顺序。有限结构检查保证 Thread/User 不重新依赖完整 IForumProtocol。

内部 descriptor 名称无直接公共观测点；除行为测试外，必须审阅 `GetFidAsync`、`GetDetailAsync` 和 `${operationName}ResolveFid` 字面值。Thread.Good 仍须按认证/TBS → ID → 分类 → 加精顺序执行，空分类短路先后不变。

### 7. 正误示例

错误：`EnsureCanExecuteAsync(...)` 后调用 `GetFidAsync(fname, ct)`，隐式重新查询缓存。正确：管理专用入口完成原有准备后直接调用只负责发送的私有 `RequestFidAsync(operationName, fname, ct)`；缓存策略留在两个明确入口，不能通过可选委托和布尔开关混合。
