# 高级用法

## 1. 依赖注入 (Dependency Injection)

在生产环境中，推荐使用 .NET 标准的依赖注入方式。这会自动处理 `IHttpClientFactory`，从而优化连接池管理。

### 注册服务

```csharp
using AioTieba4DotNet;

// 在 Program.cs 或 Startup.cs 中
builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = "你的BDUSS";
    options.Stoken = "你的STOKEN";
    options.RequestMode = TiebaRequestMode.Http; // 默认请求模式
});
```

### 使用服务

利用 C# 12 的 **Primary Constructors**，可以更简洁地注入服务：

```csharp
public class MyWorker(ITiebaClient client)
{
    public async Task RunAsync()
    {
        // 自动管理生命周期的 client
        var threads = await client.Threads.GetThreadsAsync("csharp");
        // ...
    }
}
```

## 2. 请求模式与 WebSocket

`AioTieba4DotNet` 支持多种请求模式，优先使用 Protobuf 协议以获得最佳性能。

### 全局模式
通过 `ITiebaClient.RequestMode` 可以切换全局默认模式。

```csharp
client.RequestMode = TiebaRequestMode.Websocket;
```

### 单次请求指定
大部分 API 方法都支持在调用时传入 `mode` 参数。

```csharp
// 即使全局是 WebSocket，这次请求强制使用 HTTP
await client.Threads.GetThreadsAsync("csharp", mode: TiebaRequestMode.Http);
```

> **注意**: 部分 API（如写操作）目前仅支持 HTTP 模式。如果请求方法不支持指定的模式，内部会自动回退到 HTTP。

## 3. 自定义 HttpClient

通过 `AddAioTiebaClient` 注册时，它会配置一个名为 `"TiebaClient"` 的专用 HttpClient。你可以配置它的行为（如添加代理、设置超时等）。

```csharp
builder.Services.AddHttpClient("TiebaClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        Proxy = new WebProxy("http://127.0.0.1:8888"),
        UseProxy = true
    });
```

## 4. 多账户支持 (Multi-Account)

### 4.1 工厂模式 (推荐)

在需要同时操作大量账号的场景下，推荐使用 `ITiebaClientFactory`。它能利用统一的连接池，同时为每个账户创建隔离的客户端。

```csharp
public class MultiAccountService(ITiebaClientFactory factory)
{
    public async Task RunAsync()
    {
        // 为不同账号创建隔离的客户端
        using var clientA = factory.CreateClient("BDUSS_A", "STOKEN_A");
        using var clientB = factory.CreateClient("BDUSS_B", "STOKEN_B");

        // 它们拥有独立的 Account 状态和 WebSocket 连接
        await clientA.Forums.SignAsync("csharp");
        await clientB.Forums.SignAsync("dotnet");
    }
}
```

### 4.2 简单模式 (手动实例化)

```csharp
using var client = new TiebaClient("你的BDUSS");
```

> **重要**: `ITiebaClient` 实现了 `IDisposable`。请务必使用 `using` 或手动调用 `Dispose()`，以释放 WebSocket 长连接和底层资源。

## 5. 异常处理与鉴权检查

### 本地鉴权检查
所有需要登录的 API（标记了 `[RequireBduss]`）在发起网络请求前，都会由 `HttpCore` 自动检查 `BDUSS`。如果未配置，将抛出 `TiebaException`。这能避免无效的网络请求并方便调试。

### 业务异常 (TieBaServerException)
当服务器返回业务错误（`error_code != 0`）时抛出。

```csharp
try
{
    await client.Forums.SignAsync("某个不存在的吧");
}
catch (TieBaServerException ex)
{
    // 处理特定业务逻辑，如：34001 表示已签到
    Console.WriteLine($"错误码: {ex.Code}, 消息: {ex.Message}");
}
```

### 账户安全性
`Account` 类内部对设备 ID (`AndroidId`, `Cuid` 等) 的初始化进行了加锁，多线程并发访问同一个 `Account` 实例是线程安全的。
