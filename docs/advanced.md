# Advanced

这页解释 v3 的几个设计重点，适合你已经跑通基本调用，开始关心生命周期、传输策略和宿主集成的时候阅读。

## 1. 传输策略为什么只公开 `Auto` 和 `Http`

v3 的目标是让调用方表达业务意图，而不是在每个业务方法里手动决定“这次到底用 HTTP 还是 WebSocket”。因此公开层继续把传输选择收敛到 `TiebaOptions.TransportMode`。

- `Auto` 是默认值
- `Http` 是唯一公开覆盖
- 支持 WebSocket 的调用优先走 WebSocket
- 当前链路不可用或能力本身不支持 WebSocket 时，再回退到 HTTP

这让两个好处变得稳定:

1. 业务 API 的签名不会因为传输实现细节而膨胀
2. 调用方可以全局切换行为，而不是到处传 `mode`

## 2. 什么时候要显式预热 WebSocket

大部分场景不需要预热，直接调用业务方法即可。只有当你希望把首个连接成本前置，或者你准备马上读消息组、发送私信、发送吧群消息时，预热才有意义。

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Client.InitWebSocketAsync();

var groups = await client.Messages.GetGroupMessagesAsync();
Console.WriteLine(groups.Groups.Count);
```

注意，`client.Client` 只负责生命周期。消息业务本身在 `client.Messages`。

## 3. DI、named `HttpClient` 和宿主集成

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
        Proxy = new WebProxy("http://127.0.0.1:8888"),
        UseProxy = true
    });
```

## 4. 多账户策略

一个宿主里有多个贴吧账号时，不建议手动维护很多长生命周期实例。`ITiebaClientFactory` 更适合这种情况，因为它会沿用同样的底层组合规则，但让每个账号的客户端生命周期保持清晰。

```csharp
public sealed class BotHost(ITiebaClientFactory factory)
{
    public async Task RunAsync()
    {
        using var accountA = factory.CreateClient("BDUSS_A", "STOKEN_A");
        using var accountB = factory.CreateClient(new AioTieba4DotNet.Contracts.TiebaOptions
        {
            Bduss = "BDUSS_B",
            Stoken = "STOKEN_B",
            TransportMode = AioTieba4DotNet.Contracts.TiebaTransportMode.Http
        });

        await accountA.Forums.SignAsync("csharp");
        await accountB.Messages.GetRepliesAsync();
    }
}
```

## 5. v3 的异常模型

### `TiebaAuthenticationException`

表示当前操作需要的凭据或会话条件不满足。例如访客客户端去做签到，或者消息写操作缺少必要登录态。

```csharp
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

### `TiebaConfigurationException`

表示客户端配置本身就不合法，例如超时或重试配置非法，或者只传了 `Stoken` 没传 `Bduss`。

### `TieBaServerException`

表示请求已经到达服务端，但业务被拒绝。它和本地配置错误、凭据缺失不是一类问题。

## 6. `Users` 和 `Messages` 的边界为什么要拆开

v2 时期，用户模块顺带承载了部分 inbox 读取能力。v3 把消息读写和 push 解析显式归到 `Messages`，原因是这条业务线已经不只是“用户资料的补充信息”。

现在的边界是:

- `Users` 负责资料、社交关系、黑名单、主页、资料修改
- `Messages` 负责 @、回复、私信、吧群消息、push 解析
- `Client` 负责初始化 WebSocket、ZId、ClientId / SampleId

这个拆分能让消息文档、异常语义和后续扩展都更清楚。

## 7. `Admins` 模块什么时候该用

如果你的动作会触及吧务团队、吧务权限、删帖日志、用户管理日志、封禁列表或解封申诉，那就应该进入 `Admins` 模块，而不是继续堆在 `Forums` 或 `Users` 里。

这类操作统一放在这个模块下:

- `client.Admins.BlockAsync(...)`
- `client.Admins.AddBawuAsync(...)`
- `client.Admins.DelBawuAsync(...)`
- `client.Admins.GetBawuInfoAsync(...)`

其中 `AddBawuAsync(...)` / `DelBawuAsync(...)` 直接对应 upstream `add_bawu` / `del_bawu`，只是在公开 C# 名称上保留了统一的 `Bawu` 根。

## 8. 继续阅读

- [Modules Reference](./modules.md)
- [Troubleshooting](./troubleshooting.md)
- [Migration v2 to v3](./migration-v2-to-v3.md)
