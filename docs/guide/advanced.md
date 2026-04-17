# 进阶

这页解释几个设计重点，适合已经跑通基本调用、开始关心传输策略、生命周期边界和宿主集成的时候阅读。

本页示例会直接写成“你的吧名”“你的 BDUSS”“账号 A 的 BDUSS”这类示意值，阅读时按自己的实际参数替换即可。

## 传输策略为什么公开 `Auto`、`Http` 和 `WebSocketOnly`

当前公开层的目标，是让调用方表达业务意图，而不是在每个业务方法里手动决定这次到底用 HTTP 还是 WebSocket。因此传输选择继续收敛到 `TiebaOptions.TransportMode`。

- `Auto` 是默认值：支持 WebSocket 的调用优先走 WebSocket，链路不可用时再回退到 HTTP
- `Http` 用来全局关闭 WebSocket
- `WebSocketOnly` 用来要求支持 WebSocket 的调用必须走 WebSocket；链路不可用时直接失败，不回退到 HTTP

这让三个结果保持稳定。

1. 业务 API 的签名不会因为传输实现细节而膨胀
2. 调用方可以全局切换行为，而不是到处传 `mode`
3. 需要强约束时，可以显式把支持 WebSocket 的调用锁定在 WebSocket 路径上

## 什么时候要显式预热 WebSocket

大部分场景不需要预热，直接调用业务方法即可。只有当你希望把首个连接成本前置，或者你准备马上读消息组、发送私信、发送吧群消息时，预热才有意义。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的 BDUSS", "你的 STOKEN");

await client.Client.InitWebSocketAsync();

var groups = await client.Messages.GetGroupMessagesAsync();
Console.WriteLine(groups.Count);
```

> 模块边界
> - `client.Client.InitWebSocketAsync()` 只负责连接预热。
> - `client.Messages.GetGroupMessagesAsync(...)`、`SendMessageAsync(...)`、`SendChatroomMessageAsync(...)` 和 `ParsePushNotifications(...)` 仍然属于消息业务面。

## DI、named `HttpClient` 和宿主集成

`AddAioTiebaClient(...)` 会注册一个名为 `TiebaClient` 的专用 `HttpClient`。这意味着你可以继续使用标准 `IHttpClientFactory` 管道来做代理、证书、handler 或连接层策略，而不用碰内部 transport 实现。

```csharp
using System.Net;
using AioTieba4DotNet;

builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = builder.Configuration["Tieba:Bduss"];
    options.Stoken = builder.Configuration["Tieba:Stoken"];
});

builder.Services.AddHttpClient("TiebaClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        Proxy = new WebProxy("http://127.0.0.1:7890"),
        UseProxy = true
    });
```

## 多账户策略

一个宿主里有多个贴吧账号时，不建议手动维护很多长生命周期实例。`ITiebaClientFactory` 更适合这种情况，因为它会沿用同样的底层组合规则，但让每个账号的客户端生命周期保持清晰。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

public sealed class BotHost(ITiebaClientFactory factory)
{
    public async Task RunAsync()
    {
        using var accountA = factory.CreateClient("账号 A 的 BDUSS", "账号 A 的 STOKEN");
        using var accountB = factory.CreateClient(new TiebaOptions
        {
            Bduss = "账号 B 的 BDUSS",
            Stoken = "账号 B 的 STOKEN",
            TransportMode = TiebaTransportMode.Http
        });

        await accountA.Forums.SignAsync("你的吧名");
        await accountB.Messages.GetRepliesAsync();
    }
}
```

## 异常模型

### `TiebaAuthenticationException`

表示当前操作需要的凭据或会话条件不满足。例如访客客户端去做签到，或者消息写操作缺少必要登录态。

```csharp
using AioTieba4DotNet;

using var guestClient = new TiebaClient();

try
{
    await guestClient.Forums.SignAsync("你的吧名");
}
catch (TiebaAuthenticationException ex)
{
    Console.WriteLine(ex.Message);
}
```

### `TiebaConfigurationException`

表示客户端配置本身就不合法，例如超时或重试配置非法，或者只传了 `Stoken` 没传 `Bduss`。

### `TieBaServerException`

表示请求已经到达服务端，但业务被拒绝。它和本地配置错误、凭据缺失不是一类问题。

## 为什么把 `Users`、`Messages` 和 `Client` 拆开

早期实现里，用户模块顺带承载了部分 inbox 读取能力。现在消息读写和推送通知解析显式归到 `Messages`，原因是这条业务线已经不只是用户资料的补充信息。

现在的边界是:

- `Users` 负责资料、社交关系、黑名单、主页、资料修改
- `Messages` 负责 `@`、回复、私信、吧群消息、已读状态和推送通知解析
- `Client` 负责初始化 WebSocket、ZId、ClientId / SampleId

这个拆分让消息文档、异常语义和后续扩展都更清楚，也能避免把生命周期 helper 和业务 API 混在同一个模块里。

## `Admins` 模块什么时候该用

如果你的动作会触及吧务团队、吧务权限、删帖日志、用户管理日志、封禁列表或解封申诉，那就应该进入 `Admins` 模块，而不是继续堆在 `Forums` 或 `Users` 里。

这类操作统一放在这个模块下。

- `client.Admins.BlockAsync(...)`
- `client.Admins.AddBawuAsync(...)`
- `client.Admins.DelBawuAsync(...)`
- `client.Admins.GetBawuInfoAsync(...)`

其中 `AddBawuAsync(...)` / `DelBawuAsync(...)` 直接对应 upstream `add_bawu` / `del_bawu`，只是在公开 C# 名称上保留了统一的 `Bawu` 根。

## 下一步

- 想查完整公开 API，继续看 [API 参考](/reference/modules)
- 想直接完成吧务团队、权限或封禁任务，继续看 [吧务相关](/how-to/admins)
- 想直接完成消息读写和推送通知解析任务，继续看 [消息相关](/how-to/messages)
- 想排查传输、凭据和生命周期问题，继续看 [排障](/guide/troubleshooting)
