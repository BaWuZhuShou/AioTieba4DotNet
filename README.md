# AioTieba4DotNet

🚀 C# 版本的高性能贴吧异步操作库。基于 [aiotieba](https://github.com/lumina37/aiotieba) 的理念重新构建，提供更现代的软件工程体验。

[![NuGet version (AioTieba4DotNet)](https://img.shields.io/nuget/v/AioTieba4DotNet.svg?style=flat-square)](https://www.nuget.org/packages/AioTieba4DotNet/)
[![QQ Group](https://img.shields.io/badge/QQ%E7%BE%A4-278662447-blue)](https://qm.qq.com/q/a0I1RepoA2)

---

## ✨ 项目特色

- **现代工程化**：支持 .NET 8/9/10，原生支持依赖注入 (DI) 和 IHttpClientFactory。
- **高性能**：全面采用异步编程模式，底层使用 Protobuf 序列化，性能优异。
- **模块化设计**：按业务功能拆分为 `Forums`, `Threads`, `Users` 等模块，入口清晰。
- **密码学一致性**：与 [aiotieba](https://github.com/lumina37/aiotieba) 高度一致的签名和加解密算法。

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

// 1. 无账号初始化
var client = new TiebaClient();

// 2. 带账号初始化 (推荐)
var clientWithAccount = new TiebaClient("你的BDUSS", "你的STOKEN");

// 获取贴吧信息
var fid = await client.Forums.GetFidAsync("csharp");
Console.WriteLine($"贴吧ID: {fid}");

// 获取帖子列表 (默认使用 HTTP)
var threads = await client.Threads.GetThreadsAsync("csharp");
foreach (var thread in threads.ThreadList)
{
    Console.WriteLine($"标题: {thread.Title} | 作者: {thread.Author.ShowName}");
}
```

### 2. 高级配置 (WebSocket 支持)

```csharp
using AioTieba4DotNet.Enums;

// 全局设置使用 WebSocket 模式
var client = new TiebaClient { RequestMode = TiebaRequestMode.Websocket };

// 或者在具体请求时临时指定模式
var threads = await client.Threads.GetThreadsAsync("csharp", mode: TiebaRequestMode.Http);
```

### 3. 依赖注入模式 (推荐生产环境)

在 `Program.cs` 或 `Startup.cs` 中注册：

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

---

## 🛠️ 功能模块说明

### 贴吧模块 (`client.Forums`)
- `GetFidAsync(fname)`: 通过吧名获取 ID
- `GetFnameAsync(fid)`: 通过 ID 获取吧名
- `GetDetailAsync(fid/fname)`: 获取贴吧详细资料

### 帖子模块 (`client.Threads`)
- `GetThreadsAsync(fname/fid, pn, rn, sort, isGood, mode)`: 分页获取帖子列表。`mode` 可选 `Http` 或 `Websocket`，若 WS 未实现会自动回退。

### 用户模块 (`client.Users`)
- `GetProfileAsync(userId/portrait)`: 获取用户详细资料
- `GetBasicInfoAsync(userId)`: 获取用户基础信息
- `BlockAsync(fid, portrait, day, reason)`: 封禁用户

---

## 🤝 友情链接

- 原版 Python 实现: [aiotieba](https://github.com/lumina37/aiotieba)
- 吧务管理器: [TiebaManager](https://github.com/dog194/TiebaManager)
- Protobuf 定义: [tbclient.protobuf](https://github.com/n0099/tbclient.protobuf)

## 📄 开源协议

[Unlicense](LICENSE)
