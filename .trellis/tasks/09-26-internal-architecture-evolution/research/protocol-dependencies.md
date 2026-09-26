# 协议依赖调查与首批最小边界

## 调查范围与结论

- 仅静态阅读当前源码与 `.trellis/spec/backend/{directory-structure,session-and-cache,error-handling}.md`，未运行在线请求、测试或修改产品代码。
- 现有组合入口、公开门面、协议、调度器、请求和传输分层可继续使用，不需要更换框架。
- 首批建议收敛吧身份解析及其缓存写入，并缩窄 Thread/User 的依赖。分类查询暂留 ForumProtocol，通过独立窄接口提供。
- 本文是规划建议，尚不代表用户已批准实施。

## 已有良好结构

- `AioTieba4DotNet/Clients/TiebaClientComposition.cs:32–53` 为各入口创建统一对象图，每客户端独立会话和一个 ForumInfoCache。
- `AioTieba4DotNet/Transport/TiebaOperationDispatcher.cs:21–109` 集中认证、TBS、传输选择、状态修改和有限的 WS 回退。取消不触发回退。
- HTTP 已分出执行策略、请求构造和签名；WS 已分出连接、编解码、握手及路由，不以文件较长作为继续拆分的充分理由。

## 三个已证实的维护问题

1. 吧名到 ID 的查询与缓存更新分别出现在 `ForumProtocol.cs:39–55` 和 `AdminProtocol.cs:314–333`；Thread/User 却依赖整个 IForumProtocol。这是首批目标。
2. Forum/Admin/User/Messages 中用户 ID、页码等基础校验有完全重复逻辑；但参数名和部分错误消息存在刻意保留的差异。可后续按相同语义小范围抽取，不能顺手统一错误行为。证据：`ForumProtocol.cs:638–684`、`AdminProtocol.cs:336–375`、`UserProtocol.cs:443–483`、`MessagesProtocol.cs:257–314`、`ThreadProtocol.cs:454–485`。
3. 会话初始化执行／回滚在 Session，成功提交分别在 ClientProtocol 的 ApplySessionMutation 和 MessagesProtocol 手动更新，而 TBS 服务直接提交。证据：`TiebaClientSession.cs:118–181`、`ClientProtocol.cs:29–50`、`MessagesProtocol.cs:144–150`、`TiebaSessionTbsService.cs:62–79`。锁与状态发布时间的调整会影响并发语义，应另行设计；当前仅确认维护复杂度，未证明运行缺陷，首批不涉及。

## Thread/User 实际使用范围

通过检索 `forums.` 的所有调用逐项确认：

| 使用方 | 方法及调用数量 | 代码位置 |
| --- | --- | --- |
| ThreadProtocol | GetFidAsync：15 处 | 97、128、196、215、232、250、268、285、303、319、336、356、372、390、413 |
| ThreadProtocol | GetFnameAsync：2 处 | 47、169 |
| ThreadProtocol | GetCidAsync(string, string, CancellationToken)：1 处 | 286，位于 GoodAsync，不是分类移动操作 |
| UserProtocol | GetFidAsync：1 处 | 336，位于 GetUserForumInfoAsync(string, string, CancellationToken) |

Thread 的分类依赖不能遗漏：`GoodAsync` 保持认证／TBS 前置 → GetFid → GetCid → Good 请求的顺序。`ForumProtocol.cs:242–257` 的 GetCid 直接调用 `Api/GetCid/GetCid.cs`，不调用 GetThreads 或其他类别查询协议。

## 建议内部接口和所有权

新增内容均保持 internal，建议放在既有 `Protocols/` 目录，不另建框架层。

```csharp
internal interface IForumIdentityResolver
{
    Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default);
    Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default);
}

internal interface IForumCategoryResolver
{
    Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default);
}
```

- 新增 `ForumIdentityResolver`：实现 IForumIdentityResolver，依赖 TiebaOperationDispatcher 和既有 ForumInfoCache；持有缓存引用并负责本次迁移涉及的全部缓存写入。它不依赖任何业务协议。
- `ForumIdentityResolver` 另提供内部的明确操作：`GetDetailAsync(ulong, CancellationToken)`、`ResolveFidForOperationAsync(string operationName, TiebaOperationCapabilities capabilities, string fname, CancellationToken)`、`RememberForum(ulong forumId, string forumName)`。这三个成员不加入 Thread/User 使用的窄接口。
- `ForumProtocol`：构造时接收具体 ForumIdentityResolver，以调用详情和缓存记录等内部操作；继续实现 IForumProtocol，增加实现 IForumCategoryResolver。分类方法本体保留，无新增分类服务。
- `AdminProtocol`：接收具体 ForumIdentityResolver，调用专用 ResolveFidForOperationAsync；自身不再拥有缓存，也不再组装 GetFid 请求。
- `ThreadProtocol`：接收 IForumIdentityResolver 与 IForumCategoryResolver，分别代替现有 `forums.GetFid/GetFname` 与 `forums.GetCid`。
- `UserProtocol`：仅接收 IForumIdentityResolver。首批不为一个方法再额外建立 ID 单成员接口。
- `TiebaClientComposition`：每客户端创建同一个 ForumInfoCache 和一个 ForumIdentityResolver，之后创建 ForumProtocol，再将 ForumProtocol 作为 IForumCategoryResolver 传给 Thread。无全局缓存、单例共享或所有权转移。

```text
TiebaClientComposition
  ├─ ForumInfoCache ─> ForumIdentityResolver ─> dispatcher
  ├─ ForumProtocol ─> ForumIdentityResolver
  │                 └─ dispatcher
  ├─ AdminProtocol ─> ForumIdentityResolver
  │                 └─ dispatcher
  ├─ ThreadProtocol ─> IForumIdentityResolver
  │                  ├─ IForumCategoryResolver（同一 ForumProtocol）
  │                  └─ dispatcher
  └─ UserProtocol ─> IForumIdentityResolver
                     └─ dispatcher
```

不让 ForumIdentityResolver 回调 ForumProtocol，不给它注入 GetDetail 委托，也不让分类查询依赖 ThreadProtocol。因此对象图无反向循环。具体服务只用于需要其完整内部协作能力的 Forum/Admin；面向消费者的 Thread/User 保持窄接口，不必为所有内部成员再建立大接口。

## GetFname、GetDetail 和缓存回填的精确迁移

### GetFid

将 `ForumProtocol.cs:39–55` 按原顺序迁入 ForumIdentityResolver：取消检查 → 缓存查询 → 未命中时以 `GetFidAsync` 为 descriptor 名称执行 HttpOnly → 原缓存过滤规则 → 返回。

### GetDetail

将 `ForumProtocol.cs:71–83` 的 `GetDetailAsync(ulong)` 整体迁入 ForumIdentityResolver，保留取消检查、操作名 `GetDetailAsync`、HttpOnly、`(long)fid` 转换和返回后缓存写入。ForumProtocol 两个公开协议重载继续保留：数字重载转发；名称重载仍依次 GetFid 和 GetDetail。

不要把详情读取抽到单独的“查询框架”。它是 GetFname 未命中时既有必要数据来源；迁入同一服务可使名称解析与对外详情查询继续共用实现，无需反向依赖。

### GetFname 的两次映射写入都要保留

`ForumProtocol.cs:58–68` 的当前行为是：

1. 取消检查后，查询传入 fid 的名称；非空字符串即命中，空白字符串仍属于命中。
2. 调用 GetDetailAsync(fid)。该调用按响应的 `detail.Fid/detail.Fname` 执行有过滤条件的 CacheForum。
3. 再无条件执行 `_cache.SetForumName(fid, detail.Fname)`，以请求 fid 写入映射。

这两个 fid 不一定相同，两个写入的过滤条件也不同。新 resolver 必须保留两次写入及顺序，不能合并为一次 RememberForum，不能将 `IsNullOrEmpty` 改成 `IsNullOrWhiteSpace`。

### 其他缓存来源

- `ForumProtocol.cs:181–193` 的 GetForumAsync 网络请求仍留在 ForumProtocol，仅把末尾 `CacheForum((ulong)forum.Fid, forum.Fname)` 改成 resolver.RememberForum，保留转换及调用时机。
- 原 `ForumProtocol.cs:630–635` 的 CacheForum 过滤条件原样迁成 RememberForum：ID 为 0 或名称空白时不写缓存。
- ForumInfoCache 的字符串键规则、名称／ID 双向映射、未命中值、无 TTL／无容量限制和当前释放行为均保持原样，不顺手修复潜在键冲突或引入缓存策略。

## Admin 的特殊路径如何保留

不要让 Admin 调用普通 GetFidAsync，也不要使用包含可选认证委托、可选名称和布尔策略的万能 GetOrResolve 方法。

把 `AdminProtocol.cs:314–333` 迁为 resolver 的明确方法 ResolveFidForOperationAsync，执行顺序保持：

1. `_cache.GetForumId(fname)`；命中即返回，不额外插入认证或取消检查。
2. 仅未命中时执行 `dispatcher.EnsureCanExecuteAsync(operationName, capabilities, cancellationToken)`。
3. 执行 ID 查询，descriptor 名称严格为 `$"{operationName}ResolveFid"`，能力仍是 `HttpOnly()`。
4. 返回 ID 非 0 时按原条件写缓存；返回 ID。各 Admin 入口仍保留现有参数校验、开头取消检查及后续业务 ExecuteAsync。

其中“命中不认证”仅描述**解析辅助阶段**；业务 ExecuteAsync 仍会执行其现有能力检查，不能据此声称 Admin 操作可以绕过认证。

普通 GetFidAsync 与 Admin ResolveFidForOperationAsync 可以共同调用一个**必选 operationName 参数的私有 RequestFidAsync**，只封装 descriptor 与 GetFid 请求，不做缓存命中判断或认证前置。两个显式入口各自保留必要顺序与缓存写入条件。私有 HTTP 请求复用与清晰的不同入口，比一个带可选委托和策略开关的方法更简单。

特别保留：Admin 做完 EnsureCanExecuteAsync 后，不再调用会重新查询缓存的 GetFidAsync，否则并发填充缓存时会减少原本会发出的请求，产生额外行为差异。

## 分类查询的保留边界

- IForumCategoryResolver 只声明 Thread 实际使用的字符串重载；`ForumProtocol.cs:242–257` 的方法继续原样实现。
- 空白 cname 时 GetCid 在取消检查之后直接返回 0，先于吧名校验和认证；不得移动前置检查。
- `ForumProtocol.cs:260–270` 的数字重载继续留在 ForumProtocol，其 GetFname 调用改为委托同一身份 resolver；保留空白 cname 短路和认证检查顺序。
- 不修改 IForumModule、公开 DTO、Good 请求、Api/GetCid 或 IForumProtocol 中已有方法／默认实现。

## 首批范围及应提交给验证代理的行为边界

建议仅新增两个窄接口与一个身份服务，修改组合入口及 Forum/Admin/Thread/User 构造依赖与上述迁移方法；不要同时修改 Session、Transport、基础校验、API 族系或缓存实现。

后续验证应覆盖：同客户端跨 Forum/Admin/Thread/User 共享缓存、不同客户端隔离；Admin 命中／未命中认证和 TBS 顺序；普通／Admin descriptor 名称；GetFname 请求 fid 与响应 fid 不同的映射回填；GetForum 填充缓存；Good 的分类查询顺序与空白分类短路；已有取消和失败传播。本文未调查已有测试覆盖，具体契约和执行方案由验证研究负责。
