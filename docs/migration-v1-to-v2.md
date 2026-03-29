# AioTieba4DotNet v1 -> v2 迁移指南

这份文档只关注 **intentional public breaking changes**。它的目的不是重述每一个内部重构，而是帮助现有调用方把 v1 代码迁移到 v2 的保留公开契约上。

## 适用范围

适用于以下调用方式的升级：

- `TiebaClient` 直接创建
- `AddAioTiebaClient(...)` DI 注册
- `ITiebaClientFactory` 多账户创建
- `AioTieba4DotNet.Client` 兼容 facade
- `RequestMode` / `TiebaRequestMode` / 每个业务方法的 `mode` 参数
- `HttpCore` / `WsCore` / 直接 core 注入

## 先看结论：保留什么，移除什么

### 保留的产品故事

- `TiebaClient`
- `ITiebaClient`
- `AddAioTiebaClient(...)`
- `ITiebaClientFactory`
- `client.Forums`
- `client.Threads`
- `client.Users`
- `client.Client`

### 明确移除或内化的 v1 入口

- `AioTieba4DotNet.Client` 兼容 facade：**移除**
- `TiebaOptions.RequestMode`：**移除**
- `TiebaRequestMode`：**移除出公开契约**
- `ITiebaClient.RequestMode`、`IThreadModule.RequestMode`、`IUserModule.RequestMode`：**移除**
- 业务方法中的 `mode: TiebaRequestMode?` 参数：**移除**
- `ITiebaClient.HttpCore` / `WsCore`：**内化**
- public `ITiebaHttpCore` / `ITiebaWsCore`：**不再是 v2 文档承诺的公开定制入口**
- `TiebaClient(ITiebaHttpCore ...)` 等直接 core 注入构造：**移除**

## breaking changes 一览

| v1 用法 | v2 去向 | 迁移影响 |
| --- | --- | --- |
| `new Client(...)` | `new TiebaClient(...)` | 必须改类型名，facade 已删除 |
| `options.RequestMode = ...` | `options.TransportMode = TiebaTransportMode.Auto/Http` | 必须改配置名与可选值 |
| `client.RequestMode = ...` | 无等价属性 | 迁移到客户端级 `TiebaOptions.TransportMode` |
| `GetThreadsAsync(..., mode: ...)` | `GetThreadsAsync(...)` | 删除 `mode` 参数 |
| `AddPostAsync(..., mode: ...)` | `AddPostAsync(...)` | 删除 `mode` 参数；由统一传输策略决定协议 |
| `client.HttpCore` / `client.WsCore` | 无公开替代 | 改为 DI / `TiebaOptions` / `client.Client.*` 能力 |
| `new TiebaClient(new HttpCore())` | `new TiebaClient(...)` 或 DI | 直接 core 注入不再受支持 |
| 缺少本地鉴权时走旧失败路径 | 直接抛 `TiebaAuthenticationException` | 需要更新异常处理逻辑 |

## 1. `AioTieba4DotNet.Client` facade 已移除

### 为什么移除

`AioTieba4DotNet.Client` 在 v1 只是一个薄包装层，把真正的模块能力重新包装成一组历史方法和默认字段。继续保留它会带来三个问题：

1. 让 facade 与真实模块 surface 长期双轨漂移。
2. 继续把旧默认属性（如 thread 默认参数）当成产品契约维护。
3. 让“`Client` 到底指 facade 还是模块”持续混淆。

### 旧 -> 新

```csharp
// v1
using AioTieba4DotNet;

var client = new Client("BDUSS", "STOKEN");
var fid = await client.GetFid("csharp");
var profile = await client.GetUserInfo("someone");
```

```csharp
// v2
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS", "STOKEN");
var fid = await client.Forums.GetFidAsync("csharp");
var profile = await client.Users.GetProfileAsync("someone");
```

### 常见 facade 方法替换表

| v1 facade 方法 | v2 替代 |
| --- | --- |
| `Client.GetFid(...)` | `client.Forums.GetFidAsync(...)` |
| `Client.GetFname(...)` | `client.Forums.GetFnameAsync(...)` |
| `Client.GetForumDetail(...)` | `client.Forums.GetDetailAsync(...)` |
| `Client.GetThreads(...)` | `client.Threads.GetThreadsAsync(...)` |
| `Client.GetUserInfo(...)` | `client.Users.GetProfileAsync(...)` |
| `Client.Block(...)` | `client.Users.BlockAsync(...)` |

## 2. `client.Client` 模块仍然保留，但它不是旧 facade

这是 v1 -> v2 迁移里最容易误解的一点：

- **已删除**：`AioTieba4DotNet.Client` 类型（facade）
- **仍保留**：`ITiebaClient.Client` / `client.Client` 模块（客户端元数据与连接能力）

```csharp
// v2 中这是合法且保留的用法
await client.Client.InitWebSocketAsync();
var zid = await client.Client.InitZIdAsync();
var (clientId, sampleId) = await client.Client.SyncAsync();
```

## 3. `RequestMode` / `TiebaRequestMode` 被 `TransportMode` 取代

### 为什么移除

v1 公开了三层 transport 旋钮：

- `TiebaOptions.RequestMode`
- `client.RequestMode`
- 业务方法中的 `mode` 参数

这让业务 API 必须承担协议选择、回退和默认值传播，导致文档承诺与内部行为都过于分散。v2 把这件事收口到一个客户端级配置：`TiebaOptions.TransportMode`。

### 旧 -> 新：客户端配置

```csharp
// v1
var client = new TiebaClient(new TiebaOptions
{
    Bduss = "BDUSS",
    RequestMode = TiebaRequestMode.Http
});
```

```csharp
// v2
var client = new TiebaClient(new TiebaOptions
{
    Bduss = "BDUSS",
    TransportMode = TiebaTransportMode.Http
});
```

### 旧 -> 新：移除每次调用的 `mode`

```csharp
// v1
await client.Threads.GetThreadsAsync("csharp", mode: TiebaRequestMode.Http);
await client.Threads.AddPostAsync("csharp", tid, "hello", mode: TiebaRequestMode.Websocket);
```

```csharp
// v2
await client.Threads.GetThreadsAsync("csharp");
await client.Threads.AddPostAsync("csharp", tid, "hello");
```

### v2 传输规则

- `TiebaTransportMode.Auto`：默认值。支持 WebSocket 的操作优先走 WebSocket；若功能不支持或链路在请求提交前不可用，则统一回退到 HTTP。
- `TiebaTransportMode.Http`：整个客户端实例只走 HTTP。
- **没有**公开的 “强制 WebSocket-only” 模式。

## 4. `HttpCore` / `WsCore` 不再是公开迁移目标

### 为什么移除/内化

这些类型在 v1 暴露了过多底层细节：账号状态、原始发送方法、连接生命周期、以及手工 transport 选择。v2 的设计目标是让调用方面向业务模块，而不是面向 transport core。

### 旧 -> 新：不要再读 `client.HttpCore` / `client.WsCore`

```csharp
// v1
var account = client.HttpCore.Account;
await client.WsCore.ConnectAsync();
```

```csharp
// v2
// 无公开替代。请改为：
// 1. 使用 TiebaOptions / DI 配置客户端行为
// 2. 使用 client.Client.InitWebSocketAsync() 预热链路
// 3. 通过模块 API 执行业务操作
await client.Client.InitWebSocketAsync();
await client.Threads.AddPostAsync("csharp", tid, "hello");
```

### 旧 -> 新：不要再直接注入 core

```csharp
// v1
var client = new TiebaClient(new HttpCore());
```

```csharp
// v2
var client = new TiebaClient(new TiebaOptions
{
    Bduss = "BDUSS",
    TransportMode = TiebaTransportMode.Http
});
```

如果你需要自定义 HTTP handler、代理或超时，请改为配置 DI：

```csharp
builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = builder.Configuration["Tieba:Bduss"];
});

builder.Services.AddHttpClient("TiebaClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = true
    });
```

## 5. 鉴权失败现在是显式的本地异常

### 为什么变化

Task 4 的基线记录了 v1 的一个问题：某些写操作在本地缺少 BDUSS 时并不会稳定地先失败，而是可能继续走到 transport，再以服务端形状错误返回。v2 故意修正了这件事，让调用方能稳定地区分：

- **本地没资格发请求**：`TiebaAuthenticationException`
- **已经发出请求但服务端拒绝**：`TieBaServerException`

### 旧 -> 新

```csharp
// v1：部分调用路径可能只在服务端返回后才表现为鉴权失败
try
{
    await client.Threads.AddPostAsync("csharp", tid, "hello");
}
catch (TieBaServerException)
{
    // 旧代码可能在这里处理“其实是未登录”的情况
}
```

```csharp
// v2：缺少本地必需凭据时先抛 TiebaAuthenticationException
using AioTieba4DotNet.Exceptions;

try
{
    await client.Threads.AddPostAsync("csharp", tid, "hello");
}
catch (TiebaAuthenticationException)
{
    // 处理本地未登录/缺少凭据
}
catch (TieBaServerException)
{
    // 处理服务端业务拒绝
}
```

## 6. DI 与工厂故事保留，但配置点变了

### DI 注册

```csharp
// v1
services.AddAioTiebaClient(options =>
{
    options.Bduss = "BDUSS";
    options.Stoken = "STOKEN";
    options.RequestMode = TiebaRequestMode.Http;
});
```

```csharp
// v2
services.AddAioTiebaClient(options =>
{
    options.Bduss = "BDUSS";
    options.Stoken = "STOKEN";
    options.TransportMode = TiebaTransportMode.Http;
    options.RequestTimeout = TimeSpan.FromSeconds(20);
});
```

### 工厂创建

```csharp
// v1
using var client = factory.CreateClient("BDUSS", "STOKEN");
```

```csharp
// v2
using var client = factory.CreateClient("BDUSS", "STOKEN");
// 或
using var client2 = factory.CreateClient(new TiebaOptions
{
    Bduss = "BDUSS",
    TransportMode = TiebaTransportMode.Http
});
```

## 7. 升级清单

升级到 v2 前，请至少完成以下检查：

1. 全局搜索 `Client(`、`new Client(`，把旧 facade 实例化改成 `TiebaClient`。
2. 全局搜索 `RequestMode`、`TiebaRequestMode`、`mode:`，迁移到 `TiebaOptions.TransportMode` 或直接删除。
3. 全局搜索 `HttpCore`、`WsCore`、`SetAccount`、`ConnectAsync` 等 core 直连用法，改成 DI、`TiebaOptions` 或模块调用。
4. 更新异常处理逻辑，区分 `TiebaAuthenticationException` 与 `TieBaServerException`。
5. 复核文档和示例，确认不再引用 v1 的 request-mode / facade / public-core 模式。

## 8. 没有直接替代项的变更与理由

| 变更 | 是否有直接替代 | 理由 |
| --- | --- | --- |
| `AioTieba4DotNet.Client` facade | 无，改为 `TiebaClient` + 模块 | facade 只是兼容包装层，不再作为产品表面维护 |
| `client.HttpCore` / `client.WsCore` | 无公开替代 | raw core 会泄露 transport/session 细节，破坏统一策略 |
| 业务方法 `mode` 参数 | 无一对一替代 | 业务方法不再负责协议选择 |
| public `TiebaRequestMode` | 无直接替代枚举值映射 | v2 只保留 `Auto` 和 `Http` 两个公开策略 |

## 9. 参考文档

- [README](../README.md)
- [modules.md](./modules.md)
- [advanced.md](./advanced.md)
- [release-notes-v2.md](./release-notes-v2.md)

如果你需要核对这份迁移指南是否覆盖了全部 intentional breaks，请参阅 `.sisyphus/evidence/task-5-public-api-inventory.md`。
