# 消息相关

这页按常见消息任务组织。要查完整方法和签名，请同时对照 [API 参考](/reference/modules)。

> 模块边界
> - `client.Messages` 负责 `GetAtsAsync(...)`、`GetRepliesAsync(...)`、`GetGroupMessagesAsync(...)`、`SendMessageAsync(...)`、`SendChatroomMessageAsync(...)`、`SetMessageReadAsync(...)` 和 `ParsePushNotifications(...)`。
> - `client.Client` 只负责 `InitWebSocketAsync()`、`InitZIdAsync()` 和 `SyncAsync()` 这类生命周期入口。

## 读取 `@` 消息和回复消息

最直接的入口是 `client.Messages`。这两类 inbox 读取不再挂在 `Users` 或 `Client` 上。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var ats = await client.Messages.GetAtsAsync();
var replies = await client.Messages.GetRepliesAsync();

Console.WriteLine(ats.Count);
Console.WriteLine(replies.Count);
```

## 读取 WebSocket 私信消息组

如果你想先拉取当前账号的私信消息组，直接调用 `GetGroupMessagesAsync(...)`。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var groups = await client.Messages.GetGroupMessagesAsync();
Console.WriteLine(groups.Count);
```

如果你已经知道目标消息组 id，可以显式传入过滤条件。

```csharp
var groups = await client.Messages.GetGroupMessagesAsync(new long[] { 1234567890 }, getType: 1);
```

如果你希望把首个链路成本前置，可以先调用 `client.Client.InitWebSocketAsync()`，但消息读取本身仍然属于 `Messages`。

## 发送私信

私信支持按用户 id 发送，也支持按 portrait 或用户名发送。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var id1 = await client.Messages.SendMessageAsync(123456789, "你好，这是一条私信");
var id2 = await client.Messages.SendMessageAsync("某个 portrait 或用户名", "第二条私信");

Console.WriteLine(id1);
Console.WriteLine(id2);
```

## 发送吧群消息

吧群消息同样由 `client.Messages` 提供。这个入口要求你已经知道聊天室 id 和 forum id。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Messages.SendChatroomMessageAsync(
    chatroomId: 1234567890,
    forumId: 73,
    text: "吧群里的消息",
    atUserIds: new long[] { 123456789 });
```

需要这类 fixture 的场景，建议把安全数据放进配置，而不是把真实 id 写死在示例代码里。

## 标记私信为已读

先读取消息组，再把其中一条消息传给 `SetMessageReadAsync(...)`。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var groups = await client.Messages.GetGroupMessagesAsync();
var firstMessage = groups
    .SelectMany(group => group.Messages)
    .FirstOrDefault();

if (firstMessage is not null)
{
    await client.Messages.SetMessageReadAsync(firstMessage);
}
```

## 解析 `push_notify` 负载

v3 公开的是解析入口，不是后台事件总线。调用方需要自己决定 payload 的来源和消费方式。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

byte[] payload = /* 来自你自己的 WebSocket push_notify 负载 */ [];
var notifications = client.Messages.ParsePushNotifications(payload);

Console.WriteLine(notifications.Count);
```

如果你需要提前建立 WebSocket 链路，可以配合 `client.Client.InitWebSocketAsync()`。这属于连接预热，不会改变 `Messages` 对推送通知解析的所有权。

## 下一步

- 想确认六个模块的完整边界，继续看 [API 参考](/reference/modules)
- 想理解传输策略、生命周期和多账户设计，继续看 [进阶](/guide/advanced)
- 想排查凭据、配置或链路问题，继续看 [排障](/guide/troubleshooting)
