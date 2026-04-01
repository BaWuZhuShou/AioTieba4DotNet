# Getting Started

这份文档按第一次接入 v3 的顺序来写。目标很简单，先装包，再跑通只读调用，然后再接入登录态、DI 和多账户。

## 1. 先确认运行环境

- SDK: .NET 10
- 包版本: `AioTieba4DotNet` v3
- 如果你是从 v2 升级，请同时打开 [migration-v2-to-v3.md](./migration-v2-to-v3.md)

安装命令:

```shell
dotnet add package AioTieba4DotNet
```

## 2. 先跑通一个访客只读调用

访客模式适合做论坛发现、帖子读取、用户资料读取这类不需要凭据的操作。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var forum = await client.Forums.GetForumAsync("csharp");
var threads = await client.Threads.GetThreadsAsync("csharp");

Console.WriteLine($"吧名: {forum.Name}");
Console.WriteLine($"当前页主题数: {threads.Objs.Count}");
```

如果这里只读调用就失败，先去看 [troubleshooting.md](./troubleshooting.md) 里的环境和网络部分。

## 3. 需要登录能力时，显式提供 BDUSS 和 STOKEN

签到、私信、吧群消息、吧务操作这类调用都需要登录态。v3 会在本地先做校验，不会等到请求发出去以后再给你一个模糊错误。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Forums.SignAsync("csharp");

var selfInfo = await client.Users.GetSelfInfoAsync();
Console.WriteLine(selfInfo.NameShow);
```

常见结果:

- 缺少必需凭据，抛出 `TiebaAuthenticationException`
- 配置本身非法，抛出 `TiebaConfigurationException`
- 服务端拒绝请求，抛出 `TieBaServerException`

## 4. 用 `TiebaOptions` 管理超时和传输策略

绝大多数场景可以直接用默认配置。只有当你需要强制 HTTP 或收紧超时时，才需要显式配置 `TiebaOptions`。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "你的BDUSS",
    Stoken = "你的STOKEN",
    TransportMode = TiebaTransportMode.Http,
    RequestTimeout = TimeSpan.FromSeconds(20),
    MaxReadRetryAttempts = 1
});

var homepage = await client.Users.GetHomepageAsync(1);
Console.WriteLine(homepage.Threads.Count);
```

v3 的传输规则是:

- `Auto` 是默认值
- 支持 WebSocket 的操作会优先走 WebSocket
- 当前链路不可用或该操作不支持 WebSocket 时，会回退到 HTTP
- 取消、鉴权失败、配置错误、服务端业务错误不会被当成自动回退信号

## 5. 接入 ASP.NET Core 或 Worker Service

如果应用本来就用 `Microsoft.Extensions.DependencyInjection`，推荐直接用 DI 接入。这样你可以复用 `IHttpClientFactory`，也能统一管理默认配置。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = builder.Configuration["Tieba:Bduss"];
    options.Stoken = builder.Configuration["Tieba:Stoken"];
    options.RequestTimeout = TimeSpan.FromSeconds(20);
});
```

注入后直接使用:

```csharp
public sealed class ForumWorker(ITiebaClient client)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var detail = await client.Forums.GetDetailAsync("csharp", cancellationToken);
        Console.WriteLine(detail.Name);
    }
}
```

## 6. 一个进程里需要多个账号时，用 factory

`ITiebaClientFactory` 适合 bot、定时任务、客服号切换这类场景。它保留同样的默认 wiring，但允许你为每次调用创建隔离客户端。

```csharp
public sealed class MultiAccountJob(ITiebaClientFactory factory)
{
    public async Task RunAsync()
    {
        using var signer = factory.CreateClient("BDUSS_A", "STOKEN_A");
        using var reader = factory.CreateClient(new AioTieba4DotNet.Contracts.TiebaOptions
        {
            Bduss = "BDUSS_B",
            Stoken = "STOKEN_B",
            TransportMode = AioTieba4DotNet.Contracts.TiebaTransportMode.Http
        });

        await signer.Forums.SignAsync("csharp");
        var messages = await reader.Messages.GetAtsAsync();
        Console.WriteLine(messages.MessageList.Count);
    }
}
```

## 7. 下一步去哪里

- 想做论坛相关任务，去看 [how-to-forums.md](./how-to-forums.md)
- 想读帖、回帖或做吧务帖子操作，去看 [how-to-threads.md](./how-to-threads.md)
- 想查资料、关注用户或维护黑名单，去看 [how-to-users.md](./how-to-users.md)
- 想收发消息，去看 [how-to-messages.md](./how-to-messages.md)
- 想看完整模块索引，去看 [modules.md](./modules.md)
- 想排查异常和配置问题，去看 [troubleshooting.md](./troubleshooting.md)
