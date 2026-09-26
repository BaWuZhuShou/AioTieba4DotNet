# 错误处理

## 公开异常契约

[Exceptions](../../../AioTieba4DotNet/Exceptions/) 下的类型公开于根命名空间 `AioTieba4DotNet`，派生自 `TiebaException`：

| 类型 | 既有职责 |
| --- | --- |
| `TiebaAuthenticationException` | 缺少操作所需凭据 |
| `TiebaConfigurationException` | 选项无效或缺少必需的会话状态 |
| `TieBaServerException` | 非零服务端错误，保留 `Code` 与消息 |
| `TiebaTransportException` | 归一化后的传输失败 |
| `TiebaTimeoutException` | `TiebaTransportException` 的超时特化 |
| `TiebaProtocolException` | 已识别的畸形数据，例如无效的 WS 帧头 |
| `TiebaUnsupportedOperationException` | 没有有效执行器／路径 |

保留公开的 `TieBaServerException` 拼写。未经明确的契约变更，不得重命名它，也不得将业务失败变为 `false`／空 DTO。

上游 Python 的部分方法通过 `.err` 包装错误；本库沿用既定 .NET 异常契约表达业务失败，保留服务端错误信息，并按[项目定位与上游对齐](../project-positioning.md)记录调用者可观察到的适配差异。语义对齐不要求复制 Python 的错误包装，也不意味着这些差异已自动获得批准。

[TiebaOptionsValidator](../../../AioTieba4DotNet/Internal/Mapping/TiebaOptionsValidator.cs) 拒绝只有 STOKEN 而没有 BDUSS、重试次数为负、有限超时值非正等配置。[DI](../../../AioTieba4DotNet/DependencyInjection.cs) 在解析 scoped 服务时将选项校验失败转换为 `TiebaConfigurationException`。并非所有无效输入都会被包装：空引用检查和账号凭据长度检查也使用标准参数异常。

## 响应与传输边界

[ApiResponseValidator](../../../AioTieba4DotNet/Api/ApiResponseValidator.cs) 在服务端错误码非零时抛出异常。JSON 默认字段为 `error_code`／`error_msg`；当前缺失错误码视为零。保留各族系特有的字段选择。这不是严格的通用响应封装校验器，畸形 JSON 也不会统一转换为 `TiebaProtocolException`。

[HTTP 执行策略](../../../AioTieba4DotNet/Transport/Http/TiebaHttpExecutionPolicy.cs) 通过 [TiebaHttpErrorNormalizer](../../../AioTieba4DotNet/Transport/Http/TiebaHttpErrorNormalizer.cs) 归一化选定的发送失败。既有 `TiebaException` 保持原样；支持处理的失败转换为带内部异常的传输／超时异常。解析发生在发送边界之后，因此并非所有解析器／流失败都会归一化。[HttpCore](../../../AioTieba4DotNet/Transport/Http/HttpCore.cs) 也不施加通用的 HTTP 成功状态码检查；各族系的解析行为仍然重要。

## 取消与回退

- 将 `CancellationToken` 传递至协议、API、传输与锁。调用者请求导致的 `OperationCanceledException` 保持取消语义，不得变成超时、成功或回退。
- 会话初始化可以捕获异常以恢复状态，但必须重新抛出，并在 `finally` 中释放门控；参见 [TBS 服务](../../../AioTieba4DotNet/Session/TiebaSessionTbsService.cs)。
- [调度器](../../../AioTieba4DotNet/Transport/TiebaOperationDispatcher.cs) 的回退仅限已识别的 WS 不可用情形。[GetThreads](../../../AioTieba4DotNet/Api/GetThreads/GetThreads.cs) 将空／畸形 WS 载荷转换为对应内部信号，同时让服务端错误保持可观察。不要为所有协议失败添加一概捕获的处理。
- 在后台任务边界保留取消，使观察者能够识别预期关闭。不要增加空的取消捕获；遵循 [WebSocket 引擎](../../../AioTieba4DotNet/Transport/WebSockets/TiebaWebSocketEngine.cs) 的所有权和跨目录规则。

[直接 WebSocket](../../../AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketDirectContractTests.cs)、[传输协议对齐](../../../AioTieba4DotNet.Tests.Governance/Contracts/WireParityContractTests.cs)和[状态对齐](../../../AioTieba4DotNet.Tests.Governance/Contracts/StateParityContractTests.cs)测试断言错误身份、路径行为和回滚。记录异常日志或只检查没有异常，不能验证此契约。
