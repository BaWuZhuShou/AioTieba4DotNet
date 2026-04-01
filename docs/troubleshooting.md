# Troubleshooting

这页按“现象 -> 原因 -> 处理方式”来整理。先看最接近你的症状，不需要从头读完。

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

这是 v3 的设计。缺少必需凭据时，客户端会在本地直接失败，而不是把请求发出去再等一个不稳定的服务端错误形状。

### 建议处理

- 只读操作继续用访客客户端
- 写操作改用 `new TiebaClient("BDUSS", "STOKEN")`
- 如果是宿主应用，确认 DI 读取到了真实配置值

## 明明有凭据，但服务端还是拒绝

### 常见表现

- `TieBaServerException`
- 错误码看起来像“吧不存在”“无权限”“请先登录”

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

这是 v3 的公开边界调整。`client.Client` 现在只保留生命周期能力，消息读写统一在 `client.Messages`。

### 替代路径

- `client.Messages.GetAtsAsync()`
- `client.Messages.GetRepliesAsync()`
- `client.Messages.GetGroupMessagesAsync(...)`
- `client.Messages.SendMessageAsync(...)`
- `client.Messages.SendChatroomMessageAsync(...)`
- `client.Messages.ParsePushNotifications(...)`

## 想读取 `@` 或回复消息，但不知道该用哪个模块

直接使用 `client.Messages.GetAtsAsync(...)` 和 `client.Messages.GetRepliesAsync(...)`。消息读取属于 `Messages`，不属于 `Users` 或 `Client`。

## 想强制不用 WebSocket

把 `TiebaOptions.TransportMode` 设成 `TiebaTransportMode.Http` 即可。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "你的BDUSS",
    Stoken = "你的STOKEN",
    TransportMode = TiebaTransportMode.Http
});
```

## 想提前建立 WebSocket 连接

调用 `client.Client.InitWebSocketAsync()`。如果你的目标只是普通只读论坛查询，一般不用这么做。它更适合消息组或连接敏感场景。

## 从 v2 升级后找不到入口

先对照下面这组变化:

- `Messages` 现在负责消息读取、私信和 push 解析
- `Admins` 现在负责吧务和后台管理操作
- `Client` 只保留生命周期方法
- v3 只支持 `net10.0`

更完整的迁移说明见 [migration-v2-to-v3.md](./migration-v2-to-v3.md)。

## 只通过了文件存在校验，但文档还是不好找

这是 Task 20 要修正的问题。当前 v3 的建议导航是:

1. `README.md`
2. `docs/getting-started.md`
3. 任务导向 how-to 页面
4. `docs/modules.md` 参考索引
5. `docs/advanced.md` / `docs/troubleshooting.md`
6. `docs/migration-v2-to-v3.md` / `docs/release-notes-v3.md` / `docs/parity-v3.md`

## 相关阅读

- [Getting Started](./getting-started.md)
- [Modules Reference](./modules.md)
- [Advanced](./advanced.md)
- [Migration v2 to v3](./migration-v2-to-v3.md)
