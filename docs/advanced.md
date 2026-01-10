# 高级用法

## 1. 依赖注入 (Dependency Injection)

在生产环境中，推荐使用 .NET 标准的依赖注入方式。这会自动处理 `IHttpClientFactory`，从而优化连接池管理。

### 注册服务

```csharp
using AioTieba4DotNet;

// 在 Program.cs 中
builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = "你的BDUSS";
    options.Stoken = "你的STOKEN";
    options.RequestMode = TiebaRequestMode.Http; // 默认请求模式
});
```

### 使用服务

```csharp
public class MyWorker(ITiebaClient client)
{
    public async Task RunAsync()
    {
        var threads = await client.Threads.GetThreadsAsync("csharp");
        // ...
    }
}
```

## 2. 请求模式与 WebSocket

`AioTieba4DotNet` 支持多种请求模式。

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

通过 `AddAioTiebaClient` 注册时，它会默认配置一个名为 `"TiebaClient"` 的 HttpClient。你可以通过标准的 `IHttpClientFactory` 配置来拦截或修改它的行为（例如添加代理）。

```csharp
builder.Services.ConfigureHttpClientDefaults(builder =>
{
    // 配置所有 HttpClient 使用代理等
});

// 或者针对性配置
builder.Services.AddHttpClient("TiebaClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        Proxy = new WebProxy("http://127.0.0.1:8888"),
        UseProxy = true
    });
```

## 4. 多账户支持 (Multi-Account)

在需要同时操作多个账号（如吧务机器人、批量处理）的场景下，推荐使用 `ITiebaClientFactory`。

### 使用工厂创建客户端

`ITiebaClientFactory` 是单例服务，它可以利用 DI 容器中统一配置的 `IHttpClientFactory` 来高效管理网络连接，同时为每个账户创建独立的 `ITiebaClient` 实例。

```csharp
public class MultiAccountService(ITiebaClientFactory factory)
{
    public async Task RunAsync()
    {
        // 实时为不同账号创建客户端
        var clientA = factory.CreateClient("BDUSS_A", "STOKEN_A");
        var clientB = factory.CreateClient("BDUSS_B", "STOKEN_B");

        // 它们彼此隔离，拥有独立的 Account 状态和 WebSocket 连接
        await clientA.Forums.SignAsync("csharp");
        await clientB.Forums.SignAsync("dotnet");

        // 记得释放资源（特别是如果你开启了 WebSocket 连接）
        clientA.Dispose();
        clientB.Dispose();
    }
}
```

> **提示**: `ITiebaClient` 实现了 `IDisposable`。如果你通过工厂频繁创建并弃用客户端，请务必显式调用 `Dispose()` 或使用 `using` 语句，以确保 WebSocket 连接和底层资源被及时关闭。

### 并发与线程安全

- **账户安全**：`Account` 对象内部对设备 ID (`AndroidId`, `Cuid` 等) 的初始化进行了加锁，多线程并发访问同一个 `Account` 实例是安全的。
- **WebSocket 隔离**：每个通过工厂或手动 `new` 出来的 `TiebaClient` 都持有私有的 `WebsocketCore` 和独立的 `ClientWebSocket` 连接。这保证了不同账号之间的实时长连接互不干扰。
- **发送锁**：`WebsocketCore` 内部实现了发送信号量，确保在高并发请求下不会违反 WebSocket 底层“同一时间只能有一个发送操作”的限制。

## 5. 碎片化内容 (IFrag)

在发表帖子时，如果需要混合文本、表情、艾特等内容，需要使用 `IFrag` 列表。

```csharp
using AioTieba4DotNet.Api.Entities.Contents;

var contents = new List<IFrag>
{
    new FragText { Text = "你好 " },
    new FragAt { Text = "测试账号", UserId = 12345 },
    new FragEmoji { Id = "滑稽" }
};

await client.Threads.AddThreadAsync("csharp", "标题", contents);
```
