# 传输与请求

## 操作调度

[TiebaOperationDescriptor](../../../AioTieba4DotNet/Transport/TiebaOperationDescriptor.cs) 携带名称、[能力](../../../AioTieba4DotNet/Transport/TiebaOperationCapabilities.cs)、HTTP／WS 执行器和可选的成功后会话修改。应在此声明认证／TBS 要求。需要先因认证失败退出时，在准备性查询前调用 `EnsureCanExecuteAsync`；参见 [ThreadProtocol.GetRecoversAsync](../../../AioTieba4DotNet/Protocols/ThreadProtocol.cs)。

[TiebaOperationDispatcher](../../../AioTieba4DotNet/Transport/TiebaOperationDispatcher.cs) 负责路径选择：

| 能力 | 当前行为 |
| --- | --- |
| `HttpOnly` | 无论配置模式为何都使用 HTTP |
| `WebSocketPreferred` | `Http` 模式或缺少 WS 执行器时使用 HTTP；否则预热 WS，`WebSocketOnly` 模式禁用回退 |
| `WebSocketOnly` | 必须具备 WS 执行器并完成预热，不会创建 HTTP 路径 |

在 `Auto` 模式下，优先使用 WS 的操作仅因 `WebSocketException` 或内部 `TiebaWebSocketUnavailableException` 回退。取消／服务端错误不会触发回退。业务错误后的 HTTP 重放可能重复执行操作。`ApplySessionMutation` 在成功后运行，参见[会话所有权](./session-and-cache.md)。

## 端点模式

打包上游字段，调用 `ITiebaHttpCore`／`ITiebaWsCore`，检查服务端错误，再映射为公开模型。上游支持时优先采用 protobuf。[GetThreads](../../../AioTieba4DotNet/Api/GetThreads/GetThreads.cs) 示例：

```csharp
Common = new CommonReq { ClientType = 2, ClientVersion = Const.MainVersion },
Pn = pn == 1 ? 0 : pn,
RnNeed = rn + 5,
```

这些是特定族系的传输协议规则。保留参数顺序、特殊值、命令 ID、打包和认证要求。复制端点前核对[对齐台账](../../../docs/related/parity.md)，并按[项目定位与上游对齐](../project-positioning.md)记录默认值、认证、回退等语义，不能自行改变。

[JsonApiBase](../../../AioTieba4DotNet/Api/JsonApiBase.cs) 委托 [ApiResponseValidator](../../../AioTieba4DotNet/Api/ApiResponseValidator.cs) 校验，支持配置错误字段名。对于 protobuf，应先检查族系错误再映射。不要假定存在通用响应封装。

## HTTP 所有权与兼容性

- [HttpCore](../../../AioTieba4DotNet/Transport/Http/HttpCore.cs) 提供 app-form、app-protobuf、web-GET、web-form 和自定义发送。辅助方法读取／释放响应；[执行策略](../../../AioTieba4DotNet/Transport/Http/TiebaHttpExecutionPolicy.cs) 创建／释放每次尝试的请求。复用这些边界。
- [描述符](../../../AioTieba4DotNet/Transport/Http/TiebaHttpRequestDescriptor.cs) 快照输入。[请求工厂](../../../AioTieba4DotNet/Transport/Http/TiebaHttpRequestFactory.cs) 负责编码／multipart 封装。不同请求使用自定义发送工厂，不要绕过流水线。
- [TiebaHttpParityHandler](../../../AioTieba4DotNet/Transport/Http/TiebaHttpParityHandler.cs) 归一化请求头、显式 web cookie 和图片 Referer。[TiebaHttpClientFactory](../../../AioTieba4DotNet/Transport/Http/TiebaHttpClientFactory.cs) 配置 cookie、gzip、代码页支持及无限的 `HttpClient.Timeout`；超时由操作策略提供。
- 保留 [TiebaHttpRequestSigner](../../../AioTieba4DotNet/Transport/Http/TiebaHttpRequestSigner.cs) 中按顺序签名的行为。保留兼容性哈希及有充分理由的局部警告抑制，不要替换算法或抑制整个类型。[签名契约](../../../AioTieba4DotNet.Tests.Governance/Contracts/SignatureParityContractTests.cs) 检测输入顺序漂移。

## 重试与超时限制

[TimeoutConfig](../../../AioTieba4DotNet/Contracts/TimeoutConfig.cs) 默认超时为 30 秒，重试次数为零。策略在多次发送尝试之间共用一个关联的超时预算。只有在允许重试、调用者未取消且策略未超时时，符合条件的发送异常才会重试。没有按状态码重试或退避机制。

`SendWebGetAsync` 主动启用所配置的重试；app-form、app-protobuf 和 web-form 描述符不启用。自定义发送默认 `allowRetry: false`。因此，`MaxReadRetryAttempts` 不保证每个只读公开 API 都会重试。不得对写入全局启用重试，也不要逐端点重建重试机制。

示例：[传输协议对齐契约](../../../AioTieba4DotNet.Tests.Governance/Contracts/WireParityContractTests.cs)和[直接 WS 契约](../../../AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketDirectContractTests.cs)使用受控传输。保留[错误边界](./error-handling.md)。
