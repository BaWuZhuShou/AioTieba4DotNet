# How-to: Messages

v3 把消息相关能力明确放在 `client.Messages`。`@`、回复、私信、吧群消息和 push 解析都由这个模块公开承载，`client.Client` 只负责连接生命周期。

## 读取 @ 消息和回复消息

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var ats = await client.Messages.GetAtsAsync();
var replies = await client.Messages.GetRepliesAsync();

Console.WriteLine(ats.MessageList.Count);
Console.WriteLine(replies.MessageList.Count);
```

## 读取 websocket 私信消息组

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var groups = await client.Messages.GetGroupMessagesAsync();
Console.WriteLine(groups.Groups.Count);
```

如果你已经知道目标消息组 id，可以显式传入:

```csharp
var groups = await client.Messages.GetGroupMessagesAsync(new long[] { 1234567890 }, getType: 1);
```

## 发送私信

你可以按用户 id 发送，也可以按 portrait 或用户名发送。

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var id1 = await client.Messages.SendMessageAsync(123456789, "你好，这是一条私信");
var id2 = await client.Messages.SendMessageAsync("某个 portrait 或用户名", "第二条私信");

Console.WriteLine(id1);
Console.WriteLine(id2);
```

## 发送吧群消息

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Messages.SendChatroomMessageAsync(
    chatroomId: 1234567890,
    forumId: 73,
    text: "吧群里的消息",
    atUserIds: new long[] { 123456789 });
```

这个入口要求你已经知道聊天室 id 和 forum id。需要这类 fixture 的场景，建议先把安全数据准备成配置，而不是把真实 id 写死在代码里。

## 标记私信为已读

先取消息，再把其中某条传给 `SetMessageReadAsync(...)`。

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var groups = await client.Messages.GetGroupMessagesAsync();
var firstMessage = groups.Groups
    .SelectMany(group => group.Messages)
    .FirstOrDefault();

if (firstMessage is not null)
{
    await client.Messages.SetMessageReadAsync(firstMessage);
}
```

## 解析 `push_notify` 负载

v3 公开的是“解析入口”，不是后台事件总线。

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

byte[] payload = GetPayloadFromYourWebSocketSource();
var notifications = client.Messages.ParsePushNotifications(payload);

Console.WriteLine(notifications.Count);

static byte[] GetPayloadFromYourWebSocketSource()
{
    throw new NotImplementedException();
}
```

如果你需要提前建立 WebSocket 链路，可以配合 `client.Client.InitWebSocketAsync()`。

## 相关阅读

- [Getting Started](./getting-started.md)
- [Advanced](./advanced.md)
- [Troubleshooting](./troubleshooting.md)
- [Modules Reference](./modules.md)
