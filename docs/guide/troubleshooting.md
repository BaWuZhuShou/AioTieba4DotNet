# 排障

这页按现象、原因、处理方式整理。先找最接近你的症状，不需要从头读完。

本页示例里的 `FORUM_NAME_PLACEHOLDER`、`BDUSS_PLACEHOLDER` 等值统一遵循[示例占位符词汇表](/guide/getting-started#example-placeholder-glossary)。

## 创建客户端就报配置错误

### 常见表现

- `TiebaConfigurationException`
- DI 解析 `ITiebaClient` 时立即失败

### 常见原因

- 只提供了 `Stoken`，没有 `Bduss`
- `RequestTimeout` 或 `MaxReadRetryAttempts` 配置非法
- 配置源里键名拼错，导致实际读到的是空值

### 建议处理

1. 先确认 `Bduss` 和 `Stoken` 是否都来自同一个账号
2. 在 DI 模式下，确认 `Tieba:Bduss` 和 `Tieba:Stoken` 这两个配置键存在
3. 把超时和重试配置先恢复成默认值，再逐步收紧

## 访客模式调用写接口直接失败

### 常见表现

- `TiebaAuthenticationException`
- 调用 `SignAsync(...)`、`SendMessageAsync(...)`、`SetProfileAsync(...)` 时在本地就失败

### 原因

这是当前客户端的行为。缺少必需凭据时，客户端会在本地直接失败，而不是把请求发出去再等一个不稳定的服务端错误形状。

### 建议处理

- 只读操作继续用访客客户端
- 写操作改用 `new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER")`
- 如果是宿主应用，确认 DI 读取到了真实配置值

## 明明有凭据，但服务端还是拒绝

### 常见表现

- `TieBaServerException`
- 错误码看起来像吧不存在、无权限或请先登录

### 说明

这通常不是客户端构造问题，而是服务端业务条件不满足。例如:

- 目标贴吧或帖子不存在
- 当前账号没有吧务权限
- 目标接口对环境、账号状态或时间窗口有额外要求

### 建议处理

1. 先检查目标 `fname`、`fid`、`tid`、`pid` 是否正确
2. 如果是后台管理或消息写操作，确认当前账号具备权限
3. 对状态型接口，准备安全夹具，不要把真实线上目标写死在示例代码里

## 想读消息，但 `Client` 模块里找不到消息 API

### 原因

这是当前公开边界。`client.Client` 现在只保留生命周期能力，消息读写统一在 `client.Messages`。

### 正确入口

- `client.Messages.GetAtsAsync()`
- `client.Messages.GetRepliesAsync()`
- `client.Messages.GetGroupMessagesAsync(...)`
- `client.Messages.SendMessageAsync(...)`
- `client.Messages.SendChatroomMessageAsync(...)`
- `client.Messages.SetMessageReadAsync(...)`
- `client.Messages.ParsePushNotifications(...)`

### 生命周期相关入口

- `client.Client.InitWebSocketAsync()`
- `client.Client.InitZIdAsync()`
- `client.Client.SyncAsync()`

> 模块边界
> - 消息业务 API 去 `client.Messages` 找。
> - 连接预热、标识初始化和同步 helper 去 `client.Client` 找。

## 想读取 `@` 或回复消息，但不知道该用哪个模块

直接使用 `client.Messages.GetAtsAsync(...)` 和 `client.Messages.GetRepliesAsync(...)`。消息读取属于 `Messages`，不属于 `Users` 或 `Client`。

## 想强制不用 WebSocket

把 `TiebaOptions.TransportMode` 设成 `TiebaTransportMode.Http` 即可。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "BDUSS_PLACEHOLDER",
    Stoken = "STOKEN_PLACEHOLDER",
    TransportMode = TiebaTransportMode.Http
});
```

## 想提前建立 WebSocket 连接

调用 `client.Client.InitWebSocketAsync()`。如果你的目标只是普通只读论坛查询，一般不用这么做。它更适合消息组或连接敏感场景，而且不会改变消息 API 仍然归属 `client.Messages` 这一点。

## 从 v2 升级后找不到入口

先对照下面这组变化。

- `Messages` 现在负责消息读取、私信、吧群消息、已读状态和推送通知解析
- `Admins` 现在负责吧务和后台管理操作
- `Client` 只保留生命周期方法
- v3 只支持 `net10.0`

## 文档路径改了之后，应该从哪里继续找

文档现在按三类入口组织。

1. `/how-to/*`，面向直接任务操作
2. `/reference/modules`，面向完整公开 API 查询
3. `/guide/advanced` 与 `/guide/troubleshooting`，面向设计解释和问题排查

## 下一步

- 想直接完成消息任务，继续看 [消息相关](/how-to/messages)
- 想直接完成吧务团队、权限、封禁或申诉处理任务，继续看 [吧务相关](/how-to/admins)
- 想确认六个模块和根客户端入口，继续看 [API 参考](/reference/modules)
- 想理解传输和生命周期设计，继续看 [进阶](/guide/advanced)
