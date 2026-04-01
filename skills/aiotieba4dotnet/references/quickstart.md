# AioTieba4DotNet 快速上手

## 包信息

- Package ID: `AioTieba4DotNet`
- 当前公开产品线：v3
- 目标框架：`net10.0`
- 定位：面向 .NET 10 的异步贴吧客户端

## 安装

推荐安装命令：

```bash
dotnet add package AioTieba4DotNet
```

可选的 NuGet 包管理器命令：

```powershell
NuGet\Install-Package AioTieba4DotNet -Version 3.0.0
```

## 客户端初始化方式

### 访客或只读模式

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();
```

### 登录态模式

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");
```

### 显式配置模式

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;
using System;

using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "你的BDUSS",
    Stoken = "你的STOKEN",
    TransportMode = TiebaTransportMode.Http,
    RequestTimeout = TimeSpan.FromSeconds(15),
    MaxReadRetryAttempts = 1,
});
```

### DI 或多账号模式

- 用 `AddAioTiebaClient(...)` 做依赖注入。
- 当调用方需要按账号或按任务临时创建客户端时，用 `ITiebaClientFactory` 或 `TiebaClientFactory`。

## 模块选择

| 需求 | 模块 |
| --- | --- |
| 查吧、关注、签到、搜索、统计 | `client.Forums` |
| 主题帖、楼层、楼中楼、回复、互动、帖子管理 | `client.Threads` |
| 用户资料、主页、关注关系、黑名单 | `client.Users` |
| Bawu、权限、日志、封禁、申诉 | `client.Admins` |
| @、回复、私信、吧群消息、push 解析 | `client.Messages` |
| WebSocket 预热、ZId 初始化、同步生命周期状态 | `client.Client` |

## 常见任务路由

- 读取贴吧信息、关注吧、签到、搜索：`Forums`
- 读取主题帖、楼层、楼中楼，或做回复、点赞、删帖、置顶、加精、移动、推荐：`Threads`
- 读取资料页、主页、关注关系，或维护黑名单、修改资料：`Users`
- 读取 @、回复、私信，发送私信或吧群消息，标记已读，解析 push：`Messages`
- 做封禁、申诉、Bawu、权限、吧务日志：`Admins`
- 提前初始化 websocket 或同步客户端状态：`Client`

## 公开配置与异常

- 公开传输模式由 `TiebaOptions.TransportMode` 控制。
- 默认模式是 `Auto`。
- 唯一公开覆盖模式是 `Http`。
- 缺少登录凭据却调用需要登录的操作时，可能抛出 `TiebaAuthenticationException`。
- 配置本身非法时，可能抛出 `TiebaConfigurationException`。
- 服务端按业务语义拒绝请求时，可能抛出 `TieBaServerException`。
- 其他公开运行时问题还可能表现为 `TiebaTransportException`、`TiebaTimeoutException`、`TiebaProtocolException`、`TiebaUnsupportedOperationException`。

## 使用边界

- 只围绕稳定公开入口和公开模块契约来使用。
- 不要把内部请求族、transport/session 机制暴露给调用方。
- `Messages` 和 `Client` 是故意拆开的两个模块。
- 要保持这些并列接口的区分：资料页 vs 主页、App user info vs Web user info、Blacklist vs BlacklistOld。

## 复用说明

这份引用文档是给 skill 导出用的。如果包后续演进，只更新公开名称、安装片段和模块说明，不要把这里膨胀成完整产品手册。
