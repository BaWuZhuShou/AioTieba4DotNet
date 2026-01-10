# AioTieba4DotNet

🚀 C# 版本的高性能贴吧异步操作库。基于 [aiotieba](https://github.com/lumina37/aiotieba) 的理念重新构建，提供更现代的软件工程体验。

[![NuGet version (AioTieba4DotNet)](https://img.shields.io/nuget/v/AioTieba4DotNet.svg?style=flat-square)](https://www.nuget.org/packages/AioTieba4DotNet/)
[![Qodana](https://github.com/BaWuZhuShou/AioTieba4DotNet/actions/workflows/code_quality.yml/badge.svg)](https://github.com/BaWuZhuShou/AioTieba4DotNet/actions/workflows/code_quality.yml)
[![QQ Group](https://img.shields.io/badge/QQ%E7%BE%A4-278662447-blue)](https://qm.qq.com/q/a0I1RepoA2)

---

## ✨ 项目特色

- **现代工程化**：支持 .NET 8/9/10，原生支持依赖注入 (DI)，内置 `ITiebaClientFactory` 支持多账户动态管理。
- **高性能**：全面采用异步编程模式，底层使用 Protobuf 序列化，性能优异。
- **模块化设计**：按业务功能拆分为 `Forums`, `Threads`, `Users`, `Client` 等模块，入口清晰。
- **功能丰富**：支持查看帖子、发布内容、点赞、签到、封禁等常用功能。
- **多协议支持**：支持 HTTP 和 WebSocket 双协议切换。

---

## 📦 安装

```shell
dotnet add package AioTieba4DotNet
```

---

## 🚀 快速开始

### 1. 简单模式 (推荐快速脚本)

```csharp
using AioTieba4DotNet;

// 1. 无账号初始化 (仅限读操作)
var client = new TiebaClient();

// 2. 带账号初始化 (推荐)
var clientWithAccount = new TiebaClient("你的BDUSS", "你的STOKEN");

// 3. 多账户简单用法 (快速脚本/少量账户)
using var clientA = new TiebaClient("BDUSS_A");
using var clientB = new TiebaClient("BDUSS_B");
await Task.WhenAll(
    clientA.Forums.SignAsync("csharp"),
    clientB.Forums.SignAsync("dotnet")
);

// 获取贴吧信息
var fid = await client.Forums.GetFidAsync("csharp");
Console.WriteLine($"贴吧ID: {fid}");

// 签到
await clientWithAccount.Forums.SignAsync("csharp");

// 获取帖子列表
var threads = await client.Threads.GetThreadsAsync("csharp");
foreach (var thread in threads.Objs)
{
    Console.WriteLine($"标题: {thread.Title} | 作者: {thread.User?.ShowName}");
}
```

### 2. 依赖注入模式 (推荐生产环境)

在 `Program.cs` 注册：
```csharp
services.AddAioTiebaClient(options =>
{
    options.Bduss = "你的BDUSS";
    options.Stoken = "你的STOKEN";
});
```

在服务中使用：
```csharp
public class MyService(ITiebaClient tiebaClient)
{
    public async Task DoWork()
    {
        var profile = await tiebaClient.Users.GetProfileAsync("某个ID");
        // ...
    }
}
```

### 3. 多账户模式 (推荐机器人/多账号场景)

通过 `ITiebaClientFactory` 可以动态创建多个隔离的客户端：

```csharp
public class MyBot(ITiebaClientFactory factory)
{
    public async Task Run()
    {
        var client1 = factory.CreateClient("BDUSS_1");
        var client2 = factory.CreateClient("BDUSS_2");

        // client1 和 client2 拥有完全隔离的连接和账号状态
        await client1.Forums.SignAsync("csharp");
        await client2.Forums.SignAsync("dotnet");
    }
}
```

---

## 📖 详细文档

为了保持 README 简洁，更多详细内容请参阅：
- [功能模块详细说明](./docs/modules.md) - 包含 Forum, Thread, User 模块的所有 API 列表。
- [高级用法](./docs/advanced.md) - 包含 WebSocket 配置、多账户模式、**异常处理**、自定义 HttpClient 等。

---

## 🛠️ 功能模块概览

### 贴吧模块 (`client.Forums`)
- 吧资料获取 (fid, fname, detail, forumInfo)
- 关注/取消关注 (`LikeAsync`, `UnlikeAsync`)
- 签到 (`SignAsync`)
- 吧务管理 (`DelBaWuAsync`)

### 帖子模块 (`client.Threads`)
- 帖子列表、回复列表、楼中楼获取
- 点赞/点踩 (`AgreeAsync`, `DisagreeAsync`)
- 发布主题帖、回复帖子 (`AddThreadAsync`, `AddPostAsync`)
- 删除帖子、删除回复 (`DelThreadAsync`, `DelPostAsync`)

### 用户模块 (`client.Users`)
- 用户详细资料、基础信息、面板信息获取
- 关注/取消关注用户、关注列表获取
- 用户发表的主题/回复列表获取
- 登录与 TBS 获取 (`LoginAsync`, `GetTbsAsync`)
- 封禁用户 (`BlockAsync`)

### 客户端模块 (`client.Client`)
- ZID 初始化
- 客户端配置同步 (ClientId, SampleId)

---

## 🛠️ 开发与贡献

如果你想为本项目贡献代码，请注意以下事项：

- **代码风格**：项目配置了 `.editorconfig`，请确保你的 IDE 加载了该配置。我们偏好使用 C# 12+ 的现代特性，如文件范围命名空间 (File-scoped Namespaces) 和主构造函数 (Primary Constructors)。
- **质量检查**：项目集成了 [Qodana](https://www.jetbrains.com/qodana/) 进行静态代码分析。在提交 PR 前，建议确保 CI 中的 Qodana 检查通过。

---

## 🤝 友情链接

- 原版 Python 实现: [aiotieba](https://github.com/lumina37/aiotieba)
- 吧务管理器: [TiebaManager](https://github.com/dog194/TiebaManager)
- Protobuf 定义: [tbclient.protobuf](https://github.com/n0099/tbclient.protobuf)

## 📄 开源协议

[Unlicense](LICENSE)
