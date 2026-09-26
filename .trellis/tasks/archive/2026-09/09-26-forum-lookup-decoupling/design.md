# 吧信息查询解耦设计

## 内部结构

在既有 `Protocols/` 下增加三个 internal 类型，沿用手动组合方式：

- `IForumIdentityResolver`：`GetFidAsync(string, CancellationToken)`、`GetFnameAsync(ulong, CancellationToken)`。
- `IForumCategoryResolver`：`GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default)`，返回 `Task<int>`。
- `ForumIdentityResolver`：身份接口实现，依赖原 dispatcher 与 ForumInfoCache，不依赖业务协议。

具体身份服务额外提供 `GetDetailAsync(ulong, CancellationToken)`、`ResolveFidForOperationAsync(operationName, capabilities, fname, cancellationToken)` 和 `RememberForum(forumId, forumName)`，仅供有完整协作需要的 Forum/Admin 使用，不加入 Thread/User 使用的身份接口。

ForumProtocol 保持 IForumProtocol 并实现分类窄接口；Admin/Forum 接收具体身份服务；Thread 接收身份与分类两个接口；User 接收身份接口。分类查询保持在 ForumProtocol，不为单方法增加新服务。也不为 User 的单次 ID 查询继续细分一个额外接口。

## 组合与所有权

`TiebaClientComposition.CreateRuntime` 创建 session → dispatcher → 原 ForumInfoCache → 身份服务 → Forum/Admin → Thread/User。Thread 的分类接口由同一个 ForumProtocol 提供。三种公共接入方式沿用这一对象图，不改变 DI 生命周期或任何 IDisposable 行为。

服务不得回调 ForumProtocol。GetDetail 的数字重载读取与回填下沉至身份服务，使 GetFname 可以直接复用。ForumProtocol 的详情重载保留为转发/编排；GetForum 请求保留，只将现有 CacheForum 调用迁到 RememberForum。

## 保留不同入口策略

| 入口 | 固定顺序 |
| --- | --- |
| GetFid | 取消 → 缓存 → 未命中请求（`GetFidAsync`）→ 原过滤回填 |
| GetDetail | 取消 → 原详情请求（`GetDetailAsync`、原 `(long)fid` 转换）→ 按响应 ID/名称过滤回填 |
| GetFname | 取消 → 非空名称命中 → GetDetail → 以请求 ID 无条件 SetForumName → 返回 |
| Admin 专用解析 | 缓存命中即返回；仅未命中进行原操作认证/TBS 前置 → `${operationName}ResolveFid` 请求 → 非 0 回填 |

普通与 Admin 路径只共享一个必传操作名的私有 `RequestFidAsync`，它只组装描述符并发请求，不查缓存、不认证、不决定缓存写入。Admin 前置后不再重查缓存，保留并发期间也仍发送原请求的现状。

普通 RememberForum 的 `fid != 0 && !IsNullOrWhiteSpace(fname)` 条件、Admin 的 `fid != 0` 条件、GetFname 的无条件第二次写入互不混用。缓存命中条件 `IsNullOrEmpty` 不能变为 `IsNullOrWhiteSpace`。

GetCid 两个现有重载的空分类短路、取消检查和认证顺序保持；Thread.GoodAsync 继续先认证/TBS，再 ID、分类和加精。

## 兼容性与测试

所有新增类型 internal，不改公开 Contracts/Models/Modules 签名。内部构造签名可以变化，现有测试支持只调整对象构造，不改变行为期望。使用 A 的固定快照和行为表征，补一个目标明确的结构约束来阻止 Thread/User 再次引入 IForumProtocol。

结构收益可直接审阅：Thread/User 的依赖面从完整论坛协议缩小为 2/1 个查询能力；GetFid 请求创建由两处收敛一处；解析所需的详情读取与缓存归属明确；接口和服务数量保持最小。

## 变更范围与回退

产品修改限 Clients/TiebaClientComposition、Protocols 中四个协议及三个新增类型。允许调整引用这些内部构造器的测试支持。Session、Transport、Api、ForumInfoCache 实现、公开 DTO 和模块保持原样。

组合、服务和消费方作为一个工作提交。测试失败先核查行为差异，不改基线；若无法保持原语义则回到规划评审。整体撤销 B 后，A 的基线仍是原实现有效保护。
