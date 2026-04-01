# AioTieba4DotNet

面向 .NET 10 的异步贴吧客户端。v3 保留 `TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)` 和 `ITiebaClientFactory`
这条主线，同时把公开能力稳定在六个模块上：`Forums`、`Threads`、`Users`、`Admins`、`Messages`、`Client`。

[![NuGet version (AioTieba4DotNet)](https://img.shields.io/nuget/v/AioTieba4DotNet.svg?style=flat-square)](https://www.nuget.org/packages/AioTieba4DotNet/)
[![CodeQL](https://github.com/BaWuZhuShou/AioTieba4DotNet/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/BaWuZhuShou/AioTieba4DotNet/actions/workflows/codeql-analysis.yml)
[![QQ Group](https://img.shields.io/badge/QQ%E7%BE%A4-278662447-blue)](https://qm.qq.com/q/a0I1RepoA2)

## v3 公开契约速览

- **支持矩阵**: v3 只支持 `net10.0`。
- **客户端入口**: 继续使用 `TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)`、`ITiebaClientFactory`。
- **模块边界**: `Forums`、`Threads`、`Users`、`Admins`、`Messages`、`Client`。
- **传输策略**: 公开层继续通过 `TiebaOptions.TransportMode` 控制，默认 `Auto`，唯一公开覆盖为 `Http`。
- **消息能力**: 消息读写与 push 解析统一在 `client.Messages`，`client.Client` 只保留生命周期与连接初始化职责。
- **异常模型**: 本地凭据缺失抛出 `TiebaAuthenticationException`，配置错误抛出 `TiebaConfigurationException`，服务端业务拒绝抛出
  `TieBaServerException`。

## 安装

```shell
dotnet add package AioTieba4DotNet
```

## 30 秒快速开始

### 访客读取

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var fid = await client.Forums.GetFidAsync("csharp");
var threads = await client.Threads.GetThreadsAsync("csharp");

Console.WriteLine($"fid = {fid}");
Console.WriteLine($"当前页主题数 = {threads.Objs.Count}");
```

### 已登录任务

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Forums.SignAsync("csharp");

var replies = await client.Messages.GetRepliesAsync();
Console.WriteLine($"回复消息页数 = {replies.Page?.CurrentPage}");
```

### 用 `TiebaOptions` 控制超时和传输

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "你的BDUSS",
    Stoken = "你的STOKEN",
    TransportMode = TiebaTransportMode.Http,
    RequestTimeout = TimeSpan.FromSeconds(15),
    MaxReadRetryAttempts = 1
});

var profile = await client.Users.GetProfileAsync("某个 portrait 或用户名");
Console.WriteLine(profile.ShowName);
```

## 文档导航

### 1. 上手

- [Getting Started](./docs/getting-started.md): 从安装、创建客户端到 DI 注册的完整入门路径。

### 2. 按任务找答案

- [How-to: Forums](./docs/how-to-forums.md): 查吧、关注、签到、搜索、统计，以及 `SelfFollowForums` / `SelfFollowForumsV1`
  这两组并列支持的接口。
- [How-to: Threads](./docs/how-to-threads.md): 读帖、回复、楼中楼、互动、吧务操作。
- [How-to: Users](./docs/how-to-users.md): 用户资料、社交关系、对应 aiotieba `user_info` 的两组用户信息接口、黑名单两组接口，以及主页与资料修改。
- [How-to: Messages](./docs/how-to-messages.md): @、回复、私信、吧群消息、push 解析。

### 3. 参考与深入说明

- [Modules Reference](./docs/modules.md): 根客户端、模块、DI、factory、选项与模块方法索引。
- [Advanced](./docs/advanced.md): 传输策略、DI、自定义 `HttpClient`、多账户、生命周期、异常模型。
- [Troubleshooting](./docs/troubleshooting.md): 凭据、配置、超时、消息边界与常见问题排查。

### 4. 迁移、发布与范围说明

- [Migration v2 to v3](./docs/migration-v2-to-v3.md): 从 v2 升级到 v3 时最需要关注的 breaking changes 和替代路径。
- [Release Notes v3](./docs/release-notes-v3.md): v3 的发布定位、亮点和本地验证要求。
- [Parity v3](./docs/parity-v3.md): upstream 对齐账本和 v3 范围说明。

## 公开模块概览

### `client.Forums`

- 贴吧 ID、名称、详情、`forumInfo`
- 关注、取消关注、签到、批量签到、成长签到
- `GetSelfFollowForumsAsync(...)` 与 `GetSelfFollowForumsV1Async(...)` 这两组并列支持的关注吧读取接口
- 精确搜索、排行、统计、图片与头像辅助

### `client.Threads`

- 主题帖、回帖、楼中楼、分区映射、回收站读取
- 点赞、点踩、取消点赞、取消点踩
- 回复、删除、批量删除、加精、置顶、移动、推荐、恢复、隐私设置

### `client.Users`

- TBS、自身信息、资料页、主页、吧内用户信息
- 关注、粉丝、黑名单、移除粉丝
- `GetUserInfoAppAsync(...)` 与 `GetUserInfoWebAsync(...)`，分别对应 aiotieba `get_uinfo_getuserinfo_app` 和
  `get_uinfo_getuserinfo_web`
- `GetBlacklistAsync(...)` 与 `GetBlacklistOldAsync(...)` / `AddBlacklistOldAsync(...)` / `RemoveBlacklistOldAsync(...)`
  ，分别对应当前黑名单接口和 `_old` 这一组接口
- `GetProfileAsync(...)` 与 `GetHomepageAsync(...)` 这两类分开的用户读取操作
- 资料修改、黑名单与昵称写入

### `client.Admins`

- 吧务团队、吧务权限、吧务黑名单
- 吧务日志、封禁列表、解封申诉
- `AddBawuAsync(...)`、`DelBawuAsync(...)`、`BlockAsync(...)` 等后台管理写操作，其中 `Bawu` 直接对应 upstream `add_bawu` /
  `del_bawu`
- 需要明确权限和安全夹具的后台管理写操作

### `client.Messages`

- `GetAtsAsync()`、`GetRepliesAsync()`
- `GetGroupMessagesAsync(...)`、`SendMessageAsync(...)`
- `SendChatroomMessageAsync(...)`、`SetMessageReadAsync(...)`
- `ParsePushNotifications(...)`

### `client.Client`

- `InitWebSocketAsync()`
- `InitZIdAsync()`
- `SyncAsync()`

## 开发与贡献

- **对齐 upstream**: 任何 API 的参数语义、请求打包、响应解析和错误处理都必须严格参照 Python
  版 [aiotieba](https://github.com/lumina37/aiotieba)。
- **生成链路**: 修改 `.proto` 后请运行 `dotnet run --project ProtoGenerator/ProtoGenerator.csproj`，不要手改生成的 `.cs`
  文件。
- **本地验证**: GitHub Actions 只做 restore、build、codegen 和 packaging 检查。deterministic、integration、live 验证，以及本地 docs / evidence contract，通过 `scripts/test-lane.*` 与 `scripts/verify-local.*` 在本地或 agent 环境执行。

## 友情链接

- 原版 Python 实现: [aiotieba](https://github.com/lumina37/aiotieba)
- 吧务管理器: [TiebaManager](https://github.com/dog194/TiebaManager)
- Protobuf 定义: [tbclient.protobuf](https://github.com/n0099/tbclient.protobuf)

## 开源协议

[Unlicense](LICENSE)
