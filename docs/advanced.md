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

## 4. 碎片化内容 (IFrag)

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
