# 日志规范

## 已实现的功能

[TiebaLogging](../../../AioTieba4DotNet/TiebaLogging.cs) 是基于 `Microsoft.Extensions.Logging` 的公开辅助类，作用于整个进程，由调用者主动启用：

- 初始工厂：`NullLoggerFactory.Instance`。
- `GetLogger(string)`／`GetLogger<T>()` 创建分类日志记录器。
- `EnableFileLog(path, minimumLevel)` 替换并释放工厂；默认最低级别为 `Information`。
- `Reset()` 释放当前工厂并恢复空日志工厂。

运行时没有内部调用此辅助类，也没有自动请求日志流水线。启用它不会自动提供 HTTP 跟踪、DI 日志集成或逐客户端遥测。不要在端点内部切换这一全局设施。

调用者自行记录日志的示例：

```csharp
using AioTieba4DotNet;
using Microsoft.Extensions.Logging;

TiebaLogging.EnableFileLog("logs/client.log");
try
{
    TiebaLogging.GetLogger("Example").LogInformation("开始 {Operation}", "GetThreads");
}
finally
{
    TiebaLogging.Reset();
}
```

## 格式与限制

日志提供程序追加 UTF-8 文本，包含 UTC ISO 时间戳、级别、分类、格式化消息，以及可选的异常类型／消息。进程级锁使文件写入串行化，并会创建目标目录。日志作用域不执行实际操作；结构化属性转为文本，不会成为 JSON 字段。没有轮转、保留策略、异步队列或自动脱敏功能。文件 I/O 错误可能向外传播。

目前没有事件 ID 注册表或项目专用的日志级别分类。新增调用应保留过滤行为，并使用常规 `ILogger` 消息模板；不要编造承诺的格式契约或强制运行时日志依赖。

## 诊断边界

凭据、cookie、TBS、加密材料及私信不得进入日志。这是调用者的责任：[Session.Account](../../../AioTieba4DotNet/Session/Account.cs) 持有凭据／设备状态，[HTTP 对齐处理](../../../AioTieba4DotNet/Transport/Http/TiebaHttpParityHandler.cs) 构建凭据 cookie，而日志记录器直接写入未经筛选的格式化文本。避免原样输出选项／请求／响应；异常消息也要检查，因为 [HTTP 错误](../../../AioTieba4DotNet/Transport/Http/TiebaHttpErrorNormalizer.cs) 可能包含 URI。

[ProtoGeneratorApp](../../../ProtoGenerator/ProtoGeneratorApp.cs) 将 CLI 进度／编译器诊断写入传入的 `TextWriter`，与库日志分离。保留便于处理的失败信息和退出状态。测试证据应通过断言及既有报告机制表达；`Console.WriteLine` 不能替代行为断言。
