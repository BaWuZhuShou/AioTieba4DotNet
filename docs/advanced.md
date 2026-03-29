# 高级用法

## 1. 依赖注入 (Dependency Injection)

生产环境推荐通过 `AddAioTiebaClient(...)` 集成。这样可以复用 `IHttpClientFactory`、统一配置 `TiebaOptions`，并让 `ITiebaClientFactory` 按相同规则创建隔离客户端。

### 注册服务

```csharp
using AioTieba4DotNet;

builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = builder.Configuration["Tieba:Bduss"];
    options.Stoken = builder.Configuration["Tieba:Stoken"];
    options.TransportMode = TiebaTransportMode.Auto;
    options.RequestTimeout = TimeSpan.FromSeconds(20);
    options.MaxReadRetryAttempts = 1;
});
```

### 使用服务

```csharp
public class MyWorker(ITiebaClient client)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var threads = await client.Threads.GetThreadsAsync("csharp", cancellationToken: cancellationToken);
        // ...
    }
}
```

## 2. 统一传输策略：`Auto` 默认，`Http` 为唯一公开覆盖

调用方只做业务调用，传输决策由统一 dispatcher 负责。

### 默认行为：`TiebaTransportMode.Auto`

- 对支持 WebSocket 的操作优先尝试 WebSocket。
- 当功能不支持 WebSocket，或链路在请求提交前不可用时，统一回退到 HTTP。
- 取消、超时、本地鉴权失败、协议错误、服务端业务错误都不会被当成“自动回退”的理由。

### 显式 HTTP-only

```csharp
using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "你的BDUSS",
    TransportMode = TiebaTransportMode.Http
});

var threads = await client.Threads.GetThreadsAsync("csharp");
```

### 需要提前建立 WS 链路时

如果场景需要提前预热链路，请调用保留的客户端模块方法：

```csharp
await client.Client.InitWebSocketAsync();
await client.Threads.AddPostAsync("csharp", 1234567890, "预热后发送的内容");
```

## 3. 自定义 `HttpClient`

DI 注册时会配置一个名为 `"TiebaClient"` 的专用 `HttpClient`。如果你需要代理、证书、连接策略或更细粒度的 handler 行为，请通过标准 `IHttpClientFactory` 管道配置，而不是依赖底层传输实现细节。

```csharp
using System.Net;

builder.Services.AddHttpClient("TiebaClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        Proxy = new WebProxy("http://127.0.0.1:8888"),
        UseProxy = true
    });
```

## 4. 多账户与生命周期

### 工厂模式（推荐）

```csharp
public class MultiAccountService(ITiebaClientFactory factory)
{
    public async Task RunAsync()
    {
        using var clientA = factory.CreateClient("BDUSS_A", "STOKEN_A");
        using var clientB = factory.CreateClient(new TiebaOptions
        {
            Bduss = "BDUSS_B",
            TransportMode = TiebaTransportMode.Http
        });

        await clientA.Forums.SignAsync("csharp");
        await clientB.Forums.SignAsync("dotnet");
    }
}
```

### 直接实例化

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");
```

> `ITiebaClient` / `TiebaClient` 实现了 `IDisposable`。直接创建的客户端请使用 `using` 或手动 `Dispose()`；DI 管理的实例让容器负责生命周期即可。

## 5. 鉴权与异常模型

### 本地鉴权失败：`TiebaAuthenticationException`

v2 的显式变化之一，是把“本地缺少必需凭据”从旧版的隐式失败路径改为稳定、可区分的本地异常。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Exceptions;

using var guestClient = new TiebaClient();

try
{
    await guestClient.Forums.SignAsync("csharp");
}
catch (TiebaAuthenticationException ex)
{
    Console.WriteLine(ex.Message);
}
```

### 配置错误：`TiebaConfigurationException`

例如只提供 `Stoken` 但未提供 `Bduss`，或传入非法超时/重试值时，会在配置验证或客户端创建阶段失败。

### 服务端业务错误：`TieBaServerException`

凭据有效但贴吧服务端拒绝请求时，仍然抛出服务端异常：

```csharp
using AioTieba4DotNet.Exceptions;

try
{
    await client.Forums.SignAsync("某个不存在的吧");
}
catch (TieBaServerException ex)
{
    Console.WriteLine($"错误码: {ex.Code}, 消息: {ex.Message}");
}
```

## 6. 迁移提示

如果你是从 v1 升级，请先阅读 [migration-v1-to-v2.md](./migration-v1-to-v2.md)。完整 breaking inventory、旧→新示例，以及 cutover 前检查项都集中维护在那份文档里。
