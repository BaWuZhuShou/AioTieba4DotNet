# AioTieba4DotNet

🚀 面向 .NET 8/9/10 的异步贴吧客户端。v2 保留 `TiebaClient`、DI 注册、`ITiebaClientFactory` 与模块化入口，并统一采用策略驱动的传输与显式的鉴权异常模型。

[![NuGet version (AioTieba4DotNet)](https://img.shields.io/nuget/v/AioTieba4DotNet.svg?style=flat-square)](https://www.nuget.org/packages/AioTieba4DotNet/)
[![CodeQL](https://github.com/BaWuZhuShou/AioTieba4DotNet/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/BaWuZhuShou/AioTieba4DotNet/actions/workflows/codeql-analysis.yml)
[![QQ Group](https://img.shields.io/badge/QQ%E7%BE%A4-278662447-blue)](https://qm.qq.com/q/a0I1RepoA2)

---

## ✨ v2 公开契约

- **保留的入口**：`TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)`、`ITiebaClientFactory`。
- **保留的模块**：`Forums`、`Threads`、`Users`、`Client` 四个模块。
- **传输策略收口**：公开层只保留 `TiebaOptions.TransportMode`；默认 `Auto`，唯一公开覆写为 `Http`。
- **显式鉴权失败**：缺少本地必需凭据时直接抛出 `TiebaAuthenticationException`，不再依赖旧版“发出请求后拿到服务端形状错误”的路径。
- **多协议仍然可用**：WebSocket 仍是内部优先路径，但业务方法不再暴露 `mode` 参数，也不再要求调用方手动切协议。

---

## 📦 安装

```shell
dotnet add package AioTieba4DotNet
```

---

## 🚀 快速开始

### 1. 直接创建客户端

```csharp
using AioTieba4DotNet;

// 访客模式：适合只读脚本
using var guestClient = new TiebaClient();

var fid = await guestClient.Forums.GetFidAsync("csharp");
Console.WriteLine($"贴吧 ID: {fid}");

// 鉴权模式：适合签到、发帖、管理等操作
using var authenticatedClient = new TiebaClient("你的BDUSS", "你的STOKEN");
await authenticatedClient.Forums.SignAsync("csharp");

var threads = await authenticatedClient.Threads.GetThreadsAsync("csharp");
foreach (var thread in threads.Objs)
{
    Console.WriteLine($"标题: {thread.Title} | 作者: {thread.User?.ShowName}");
}
```

### 2. 用 `TiebaOptions` 控制传输与超时策略

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "你的BDUSS",
    Stoken = "你的STOKEN",
    TransportMode = TiebaTransportMode.Http,
    RequestTimeout = TimeSpan.FromSeconds(15),
    MaxReadRetryAttempts = 1
});

await client.Threads.AddPostAsync("csharp", 1234567890, "这是一条通过统一传输策略发送的回复");
```

> `TransportMode = Auto` 是默认行为：支持 WebSocket 的请求会优先尝试 WebSocket，不支持或当前不可用时统一回退到 HTTP。业务方法本身不再暴露 `mode` 参数。

### 3. 依赖注入模式

```csharp
using AioTieba4DotNet;

builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = builder.Configuration["Tieba:Bduss"];
    options.Stoken = builder.Configuration["Tieba:Stoken"];
    options.RequestTimeout = TimeSpan.FromSeconds(20);
});
```

```csharp
public class MyService(ITiebaClient tiebaClient)
{
    public async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        var profile = await tiebaClient.Users.GetProfileAsync("某个ID", cancellationToken);
        // ...
    }
}
```

### 4. 多账户模式

```csharp
public class MyBot(ITiebaClientFactory factory)
{
    public async Task RunAsync()
    {
        using var client1 = factory.CreateClient("BDUSS_1", "STOKEN_1");
        using var client2 = factory.CreateClient(new TiebaOptions
        {
            Bduss = "BDUSS_2",
            TransportMode = TiebaTransportMode.Http
        });

        await client1.Forums.SignAsync("csharp");
        await client2.Forums.SignAsync("dotnet");
    }
}
```

---

## 📖 文档入口

- [功能模块详细说明](./docs/modules.md) - 当前保留的 `Forums` / `Threads` / `Users` / `Client` 模块能力。
- [高级用法](./docs/advanced.md) - DI、传输策略、异常模型、自定义 `HttpClient`、多账户与生命周期。
- [v1 到 v2 迁移指南](./docs/migration-v1-to-v2.md) - 所有 intentional breaking changes、旧→新示例与移除理由。
- [v2 发布说明](./docs/release-notes-v2.md) - major 升级摘要、发布节奏与 cutover 清单。
- [待实现功能清单 (TODO)](./docs/todo.md) - 冻结 upstream 基线之外仍未覆盖的能力。

---

## 🛠️ 模块概览

### `client.Forums`

- 贴吧 ID / 吧名 / 详情 / forumInfo 查询
- 关注 / 取消关注 / 签到
- 吧务移除 (`DelBaWuAsync`)

### `client.Threads`

- 主题帖、回帖、楼中楼读取
- 点赞 / 点踩 / 取消点赞 / 取消点踩
- 回复、删除、批量删除、加精、置顶、移动、推荐、恢复、隐私设置

### `client.Users`

- TBS、自身信息、资料页、关注/粉丝/黑名单、@/回复消息
- 关注 / 取关 / 封禁 / 拉黑 / 移除粉丝
- 用户主题帖 / 回复列表查询

### `client.Client`

- `InitWebSocketAsync()`：按需预热 WebSocket 链路
- `InitZIdAsync()`：初始化 ZID
- `SyncAsync()`：同步客户端标识

> `client.Client` 表示客户端元数据与连接相关能力。

---

## ⚠️ 从 v1 升级

v2 是一次 major 升级。请在修改现有代码前先阅读 [migration-v1-to-v2.md](./docs/migration-v1-to-v2.md)，其中包含完整的 breaking inventory、旧→新示例、以及 cutover 前的升级清单。

---

## 🛠️ 开发与贡献

- **对齐原版**：任何 API 的参数语义、请求打包、响应解析和错误处理都必须严格参照 Python 版 [aiotieba](https://github.com/lumina37/aiotieba)。
- **生成链路**：修改 `.proto` 后请运行 `dotnet run --project ProtoGenerator/ProtoGenerator.csproj`；不要手改生成的 `.cs` 文件。
- **质量门禁**：发布和主要验证都以 `dotnet restore`、`dotnet build`、多 TFM 确定性测试、generator consistency、migration docs 检查为准。

---

## 🤝 友情链接

- 原版 Python 实现: [aiotieba](https://github.com/lumina37/aiotieba)
- 吧务管理器: [TiebaManager](https://github.com/dog194/TiebaManager)
- Protobuf 定义: [tbclient.protobuf](https://github.com/n0099/tbclient.protobuf)

## 📄 开源协议

[Unlicense](LICENSE)
