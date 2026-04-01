# Modules Reference

这页是 v3 的参考索引，不是推荐学习顺序。第一次接入请先看 [getting-started.md](./getting-started.md)，按任务落地请看四份 how-to 页面。

## 根客户端和组合方式

### `TiebaClient`

`TiebaClient` 是直接实例化入口，适合脚本、控制台工具和少量自托管任务。

```csharp
using var guestClient = new TiebaClient();
using var authClient = new TiebaClient("你的BDUSS", "你的STOKEN");
```

### `ITiebaClient`

`ITiebaClient` 是统一根接口，公开以下模块属性:

- `Forums`
- `Threads`
- `Users`
- `Admins`
- `Messages`
- `Client`

### `AddAioTiebaClient(...)`

DI 集成入口，适合 ASP.NET Core 和 Worker Service。

```csharp
builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = builder.Configuration["Tieba:Bduss"];
    options.Stoken = builder.Configuration["Tieba:Stoken"];
});
```

### `ITiebaClientFactory`

多账户或按任务临时创建客户端时使用。

```csharp
using var client = factory.CreateClient("BDUSS", "STOKEN");
using var accountClient = factory.CreateClient(new Account("BDUSS", "STOKEN"));
```

## `TiebaOptions`

| 属性 | 说明 | 默认值 |
| --- | --- | --- |
| `Bduss` | 登录态 BDUSS | `null` |
| `Stoken` | 登录态 STOKEN | `null` |
| `TransportMode` | 传输模式 | `TiebaTransportMode.Auto` |
| `RequestTimeout` | 单次请求超时 | `30s` |
| `MaxReadRetryAttempts` | 只读 HTTP 请求最大重试次数 | `0` |
| `Timeout` | 独立超时配置视图 | `RequestTimeout=30s, MaxReadRetryAttempts=0` |

### `TiebaTransportMode`

| 值 | 说明 |
| --- | --- |
| `Auto` | 默认模式，支持 WebSocket 的请求优先走 WebSocket，否则回退 HTTP |
| `Http` | 强制走 HTTP |

## 模块索引

## 1. `client.Forums`

适用任务: 查吧、关注、签到、搜索、统计、图片辅助。

### 基础读取

| 方法 | 用途 |
| --- | --- |
| `GetFidAsync(string)` | 按吧名查 `fid` |
| `GetFnameAsync(ulong)` | 按 `fid` 查吧名 |
| `GetForumAsync(string)` | 读取 `forumInfo` |
| `GetDetailAsync(string)` / `GetDetailAsync(ulong)` | 读取贴吧详情 |

### 关注和签到

| 方法 | 用途 |
| --- | --- |
| `FollowAsync(string)` / `FollowAsync(ulong)` | 关注贴吧 |
| `UnfollowAsync(string)` / `UnfollowAsync(ulong)` | 取消关注 |
| `SignAsync(string)` | 单吧签到 |
| `SignForumsAsync()` | 一键签到当前账号关注的贴吧 |
| `SignGrowthAsync()` | 成长任务签到 |

### 发现、搜索和统计

| 方法 | 用途 |
| --- | --- |
| `SearchExactAsync(...)` | 精确搜索 |
| `GetLastReplyersAsync(...)` | 获取带最后回复人的帖子列表 |
| `GetMemberUsersAsync(...)` | 获取会员列表 |
| `GetRankForumsAsync(...)` | 获取签到排行 |
| `GetRecomStatusAsync(...)` | 获取推荐配额状态 |
| `GetSquareForumsAsync(...)` | 获取吧广场列表 |
| `GetStatisticsAsync(...)` | 获取贴吧统计时间序列 |
| `GetForumLevelAsync(...)` | 获取当前账号在某吧的等级信息 |
| `GetCidAsync(...)` | 获取精华分类 id |
| `GetRoomListByFidAsync(...)` | 获取某吧聊天室列表 |

### 图片和列表辅助

| 方法 | 用途 |
| --- | --- |
| `GetImageBytesAsync(string)` | 下载原始图片字节 |
| `GetImageAsync(string)` | 按 URL 获取图片信息 |
| `GetImageByHashAsync(string, ForumImageSize)` | 按图片 hash 获取贴吧图片 |
| `GetPortraitAsync(string, ForumImageSize)` | 按 portrait 获取头像 |
| `GetFollowForumsAsync(...)` | 获取指定用户关注吧列表 |
| `GetSelfFollowForumsAsync(...)` | 获取当前账号关注吧列表，返回 `SelfFollowForums`，包含 `IsSigned` 状态 |
| `GetSelfFollowForumsV1Async(...)` | 获取当前账号关注吧列表的 V1 版本接口，返回 `SelfFollowForumsV1` 与显式分页信息 |
| `GetDislikeForumsAsync(...)` | 获取首页推荐屏蔽贴吧列表 |
| `DislikeAsync(...)` / `UndislikeAsync(...)` | 设置或取消首页推荐屏蔽 |

## 2. `client.Threads`

适用任务: 读帖、读楼中楼、回复、互动、吧务帖子操作。

| 方法族 | 代表方法 |
| --- | --- |
| 主题帖读取 | `GetThreadsAsync(...)` |
| 楼层读取 | `GetPostsAsync(...)` |
| 楼中楼读取 | `GetCommentsAsync(...)` |
| 回收站读取 | `GetRecoversAsync(...)`, `GetRecoverInfoAsync(...)` |
| 分区映射 | `GetTabMapAsync(...)` |
| 互动 | `AgreeAsync(...)`, `DisagreeAsync(...)`, `UnagreeAsync(...)`, `UndisagreeAsync(...)` |
| 发帖和管理 | `AddPostAsync(...)`, `DelThreadAsync(...)`, `DelPostAsync(...)`, `DelThreadsAsync(...)`, `DelPostsAsync(...)`, `GoodAsync(...)`, `UngoodAsync(...)`, `TopAsync(...)`, `UntopAsync(...)`, `MoveAsync(...)`, `RecommendAsync(...)`, `RecoverAsync(...)`, `SetThreadPrivacyAsync(...)` |

## 3. `client.Users`

适用任务: 查资料、查主页、关注用户、黑名单、资料修改。

### 常用入口

- `GetUserInfoAppAsync(...)`
- `GetUserInfoWebAsync(...)`
- `GetProfileAsync(...)`
- `GetHomepageAsync(...)`
- `GetSelfInfoAsync()`
- `GetSelfInfoInitNicknameAsync()`
- `GetSelfInfoMoIndexAsync()`
- `LoginAsync()`
- `SetProfileAsync(...)`
- `SetNicknameAsync(...)`
- `GetBlacklistAsync()`
- `SetBlacklistAsync(...)`
- `GetBlacklistOldAsync(...)`
- `AddBlacklistOldAsync(...)`
- `RemoveBlacklistOldAsync(...)`

`GetUserInfoAppAsync(...)` 和 `GetUserInfoWebAsync(...)` 是两组并列支持的 `user_info` 接口，分别对应 aiotieba `get_uinfo_getuserinfo_app` / `UserInfo_guinfo_app` 与 `get_uinfo_getuserinfo_web` / `UserInfo_guinfo_web`，并返回 `UserInfoGuInfoApp` 与 `UserInfoGuInfoWeb`。`GetProfileAsync(...)` 读取资料页信息，`GetHomepageAsync(...)` 读取主页内容和主页快照。它们也是分开的用户读取接口，不会合并成同一个入口。

### 补充读取和社交关系

- `GetHomepageAsync(...)`
- `GetFollowsAsync(...)`
- `GetFansAsync(...)`
- `FollowAsync(...)`
- `UnfollowAsync(...)`
- `GetUserForumInfoAsync(...)`
- `GetRankUsersAsync(...)`
- `GetPostsAsync(...)`
- `GetThreadsAsync(...)`
- `GetUserByTiebaUidAsync(...)`

### 并列支持的接口组

这些入口都是当前公开契约的一部分。它们对应不同的 aiotieba 接口，或者保留了不同的返回结果，按你的数据需求选择即可。

- `GetUserInfoAppAsync(...)` / `GetUserInfoWebAsync(...)`：并列支持的 App 与 Web `user_info` 接口
- `GetBlacklistAsync(...)` / `SetBlacklistAsync(...)` 与 `GetBlacklistOldAsync(...)` / `AddBlacklistOldAsync(...)` / `RemoveBlacklistOldAsync(...)`：并列支持的两组黑名单接口，分别返回 `BlacklistUsers` 与 `BlacklistOldUsers`，其中 `Old` 直接对应 upstream `_old` 这一组接口
- `SetProfileAsync(...)` 与 `SetNicknameAsync(...)`：分开的资料写入接口，一组用于整组资料写入，一组用于单字段昵称写入

消息读取能力已经完全归属 `client.Messages`；吧务封禁和 Bawu 写操作已经完全归属 `client.Admins`。

## 根级辅助导出

- `Account`：公开凭据模型，可转换为 `TiebaOptions`，也可直接用于 `TiebaClient` / `ITiebaClientFactory`
- `TimeoutConfig`：公开超时配置模型，对应 `TiebaOptions.Timeout`
- `VersionInfo.Version`：公开运行时版本字符串
- `TiebaLogging`：根级日志 helper，支持 `GetLogger(...)` 与 `EnableFileLog(...)`

## 4. `client.Admins`

适用任务: 吧务团队、权限、日志、申诉、封禁管理。

| 方法族 | 代表方法 |
| --- | --- |
| 吧务团队 | `AddBawuAsync(...)`, `DelBawuAsync(...)`, `GetBawuInfoAsync(...)`（分别对应 upstream `add_bawu` / `del_bawu`） |
| 吧务权限 | `GetBawuPermAsync(...)`, `SetBawuPermAsync(...)` |
| 吧务黑名单 | `AddBawuBlacklistAsync(...)`, `DelBawuBlacklistAsync(...)`, `GetBawuBlacklistAsync(...)` |
| 吧务日志 | `GetBawuPostLogsAsync(...)`, `GetBawuUserLogsAsync(...)` |
| 申诉和封禁 | `GetUnblockAppealsAsync(...)`, `HandleUnblockAppealsAsync(...)`, `GetBlocksAsync(...)`, `BlockAsync(...)`, `UnblockAsync(...)` |

> 这类写操作通常要求吧务权限和安全夹具。日常消息或资料读取不需要进这个模块。

## 5. `client.Messages`

适用任务: inbox、私信、吧群消息、push 解析。

| 方法 | 用途 |
| --- | --- |
| `GetAtsAsync(...)` | 获取 @ 消息 |
| `GetRepliesAsync(...)` | 获取回复消息 |
| `GetGroupMessagesAsync(...)` | 获取 websocket 私信消息组 |
| `SendMessageAsync(long, ...)` / `SendMessageAsync(string, ...)` | 发送私信 |
| `SendChatroomMessageAsync(...)` | 发送吧群消息 |
| `SetMessageReadAsync(...)` | 标记私信为已读 |
| `ParsePushNotifications(byte[])` | 解析 `push_notify` 载荷 |

## 6. `client.Client`

适用任务: 生命周期初始化和链路预热。

| 方法 | 用途 |
| --- | --- |
| `InitWebSocketAsync()` | 预热 WebSocket 链路 |
| `InitZIdAsync()` | 初始化 ZId |
| `SyncAsync()` | 同步 `ClientId` 和 `SampleId` |

## 异常参考

| 异常 | 含义 |
| --- | --- |
| `TiebaAuthenticationException` | 当前操作需要的登录凭据或会话条件不满足 |
| `TiebaConfigurationException` | 客户端配置不合法，或初始化得到的关键值为空 |
| `TieBaServerException` | 服务端接受到请求，但按业务语义拒绝 |

## 下一步

- 上手示例: [getting-started.md](./getting-started.md)
- 任务导向文档: [how-to-forums.md](./how-to-forums.md), [how-to-threads.md](./how-to-threads.md), [how-to-users.md](./how-to-users.md), [how-to-messages.md](./how-to-messages.md)
- 深入说明: [advanced.md](./advanced.md)
- 问题排查: [troubleshooting.md](./troubleshooting.md)
