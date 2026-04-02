# API 参考

这页汇总当前公开 API，适合查入口、签名、职责边界和公开模型类型。第一次接入时先看 [快速开始](/guide/getting-started)；已经知道自己要调用什么时，再把这页当成字典使用。

除特别说明的同步成员外，下面列出的异步方法都带有尾部参数 `CancellationToken cancellationToken = default`。

## 根入口

### `TiebaClient`

`TiebaClient` 是直接实例化入口，适合控制台工具、脚本和少量自托管任务。

| 构造方式 | 用途 |
| --- | --- |
| `TiebaClient(string? bduss = null, string? stoken = null)` | 直接按凭据创建客户端；不传凭据时就是访客客户端。 |
| `TiebaClient(Account account)` | 用公开账户对象创建客户端。 |
| `TiebaClient(TiebaOptions options)` | 用完整配置创建客户端。 |

### `ITiebaClient`

`ITiebaClient` 是统一根接口，公开以下模块属性。

| 属性 | 说明 |
| --- | --- |
| `Forums` | 查吧、关注、签到、搜索、统计、图片和关注列表相关能力。 |
| `Threads` | 读帖、楼层、楼中楼、互动、删帖、加精、置顶、恢复和分区操作。 |
| `Users` | 用户资料、主页、关注关系、黑名单、资料修改和用户内容。 |
| `Admins` | 吧务团队、权限、封禁、日志和申诉处理。 |
| `Messages` | `@`、回复、私信、吧群消息、已读状态和推送通知解析。 |
| `Client` | WebSocket 预热、ZId 初始化和客户端标识同步。 |

### `AddAioTiebaClient(...)`

`AddAioTiebaClient(this IServiceCollection services, Action<TiebaOptions>? configureOptions = null)` 是 DI 集成入口。它会注册：

- `ITiebaClient`
- `ITiebaClientFactory`
- 名为 `TiebaClient` 的专用 `HttpClient`
- `TiebaOptions` 校验

### `ITiebaClientFactory`

适合多账户、按任务延迟创建实例，或者不想把某个账户长期挂成作用域服务的场景。

| 方法 | 说明 |
| --- | --- |
| `CreateClient(TiebaOptions options)` | 按完整配置创建客户端。 |
| `CreateClient(string bduss, string? stoken = null)` | 按凭据创建客户端。 |
| `CreateClient(Account account)` | 按公开账户对象创建客户端。 |

`TiebaClientFactory` 是这个接口的公开实现类型。

## 配置与辅助类型

### `TiebaOptions`

| 属性 | 说明 | 默认值 |
| --- | --- | --- |
| `Bduss` | 登录态 BDUSS。 | `null` |
| `Stoken` | 登录态 STOKEN。 | `null` |
| `TransportMode` | 传输模式。 | `TiebaTransportMode.Auto` |
| `RequestTimeout` | 单次请求超时。 | `30s` |
| `MaxReadRetryAttempts` | 只读 HTTP 请求最大重试次数。 | `0` |
| `Timeout` | `TimeoutConfig` 视图。 | `RequestTimeout=30s, MaxReadRetryAttempts=0` |

### `TiebaTransportMode`

| 值 | 说明 |
| --- | --- |
| `Auto` | 默认模式；支持 WebSocket 的调用优先走 WebSocket，否则回退 HTTP。 |
| `Http` | 强制走 HTTP。 |

### `TimeoutConfig`

| 属性 | 说明 | 默认值 |
| --- | --- | --- |
| `RequestTimeout` | 单次请求超时。 | `30s` |
| `MaxReadRetryAttempts` | 只读 HTTP 请求最大重试次数。 | `0` |

### `Account`

| 成员 | 说明 |
| --- | --- |
| `Account(string bduss = "", string stoken = "")` | 创建公开账户对象。 |
| `Bduss` | 账户 BDUSS。 |
| `Stoken` | 账户 STOKEN。 |
| `ToTiebaOptions()` | 转成 `TiebaOptions`。 |

## 常用输入类型、枚举和查询选项

这些类型会直接出现在公开方法签名里。查方法时，如果不清楚参数该怎么传，先看这一节。

### Forums 相关枚举

| 类型 | 可选值 | 用途 |
| --- | --- | --- |
| `ForumSearchType` | `All` / `Time` / `Relation` | `SearchExactAsync(...)` 的搜索排序方式。 |
| `ForumRankType` | `Today` / `Yesterday` / `Weekly` / `Monthly` | `GetRankForumsAsync(...)` 的排行范围。 |
| `ForumImageSize` | `Small` / `Medium` / `Large` | `GetImageByHashAsync(...)`、`GetPortraitAsync(...)` 的图片尺寸。 |

### Threads 相关枚举

| 类型 | 可选值 | 用途 |
| --- | --- | --- |
| `ThreadSortType` | `Reply` / `Create` / `Hot` / `Follow` | `GetThreadsAsync(...)`、`GetLastReplyersAsync(...)` 的主题排序方式。 |
| `PostSortType` | `Asc` / `Desc` / `Hot` | `GetPostsAsync(...)` 的楼层排序方式。 |

### Users 相关枚举

| 类型 | 可选值 | 用途 |
| --- | --- | --- |
| `BlacklistType` | `None` / `Follow` / `Interact` / `Chat` / `All` | `SetBlacklistAsync(...)` 的权限型黑名单设置。 |
| `Gender` | `Unknown` / `Male` / `Female` | `SetProfileAsync(...)` 的性别字段。 |

### Admins 相关枚举和查询选项

| 类型 | 可选值 / 关键字段 | 用途 |
| --- | --- | --- |
| `BawuType` | `Manager` / `ImageEditor` / `VoiceEditor` | `AddBawuAsync(...)`、`DelBawuAsync(...)` 的吧务类型。 |
| `BawuPermType` | `None` / `Unblock` / `UnblockAppeal` / `Recover` / `RecoverAppeal` / `All` | `SetBawuPermAsync(...)` 的权限集合。 |
| `BawuPostLogQueryOptions` | `PageNumber`、`SearchValue`、`SearchType`、`StartTime`、`EndTime`、`OperationType` | `GetBawuPostLogsAsync(...)` 的删帖日志查询参数。 |
| `BawuUserLogQueryOptions` | `PageNumber`、`SearchValue`、`SearchType`、`StartTime`、`EndTime`、`OperationType` | `GetBawuUserLogsAsync(...)` 的用户管理日志查询参数。 |

### Messages 与日志相关类型

| 类型 | 关键成员 | 用途 |
| --- | --- | --- |
| `WsMessage` | `GroupId`、`GroupTypeValue`、`MsgId`、`MsgTypeValue`、`Text`、`User`、`CreateTime` | `SetMessageReadAsync(...)` 需要的私信消息对象。 |
| `LogLevel` | `Trace` / `Debug` / `Information` / `Warning` / `Error` / `Critical` / `None` | `TiebaLogging.EnableFileLog(...)` 的日志级别；类型来自 `Microsoft.Extensions.Logging`。 |

## `client.Forums`

适合查吧、关注、签到、搜索、排行、统计、图片和关注列表相关任务。

| 方法 | 说明 |
| --- | --- |
| `GetFidAsync(string fname)` | 按吧名查 `fid`。 |
| `GetFnameAsync(ulong fid)` | 按 `fid` 查吧名。 |
| `GetDetailAsync(string fname)` / `GetDetailAsync(ulong fid)` | 读取贴吧详情。 |
| `GetForumAsync(string fname)` | 读取贴吧基础信息。 |
| `FollowAsync(string fname)` / `FollowAsync(ulong fid)` | 关注贴吧。 |
| `UnfollowAsync(string fname)` / `UnfollowAsync(ulong fid)` | 取消关注贴吧。 |
| `SignAsync(string fname)` | 单吧签到。 |
| `SignForumsAsync()` | 一键签到当前账号关注的贴吧。 |
| `SignGrowthAsync()` | 完成成长任务签到。 |
| `GetFollowForumsAsync(long userId, int pn = 1, int rn = 50)` | 获取指定用户的关注吧列表。 |
| `GetSelfFollowForumsAsync(int pn = 1, int rn = 200)` | 获取当前账号关注吧列表，返回 `SelfFollowForums`，包含 `IsSigned`。 |
| `GetSelfFollowForumsV1Async(int pn = 1, int rn = 20)` | 获取当前账号关注吧 V1 列表，返回 `SelfFollowForumsV1`。 |
| `GetCidAsync(string fname, string cname = "")` / `GetCidAsync(ulong fid, string cname = "")` | 按吧名或 `fid` 获取精华分类 ID。 |
| `GetImageBytesAsync(string imageUrl)` | 按 URL 获取图片原始字节。 |
| `GetImageAsync(string imageUrl)` | 按 URL 获取图片信息。 |
| `GetImageByHashAsync(string rawHash, ForumImageSize size = ForumImageSize.Small)` | 按图片 hash 获取贴吧图片。 |
| `GetPortraitAsync(string portrait, ForumImageSize size = ForumImageSize.Small)` | 按 portrait 获取头像。 |
| `SearchExactAsync(string fname, string query, int pn = 1, int rn = 30, ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false)` | 按吧名精确搜索。 |
| `SearchExactAsync(ulong fid, string query, int pn = 1, int rn = 30, ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false)` | 按 `fid` 精确搜索。 |
| `GetLastReplyersAsync(string fname, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply, bool isGood = false)` / `GetLastReplyersAsync(ulong fid, ...)` | 获取带最后回复人的主题列表。 |
| `GetMemberUsersAsync(string fname, int pn = 1)` / `GetMemberUsersAsync(ulong fid, int pn = 1)` | 获取吧会员列表。 |
| `GetRankForumsAsync(string fname, int pn = 1, ForumRankType rankType = ForumRankType.Weekly)` / `GetRankForumsAsync(ulong fid, ...)` | 获取签到排行。 |
| `GetRecomStatusAsync(string fname)` / `GetRecomStatusAsync(ulong fid)` | 获取推荐配额状态。 |
| `GetSquareForumsAsync(string cname, int pn = 1, int rn = 20)` | 获取吧广场列表。 |
| `GetStatisticsAsync(string fname)` / `GetStatisticsAsync(ulong fid)` | 获取贴吧统计数据。 |
| `GetForumLevelAsync(string fname)` / `GetForumLevelAsync(ulong fid)` | 获取当前账号在某吧的等级信息。 |
| `GetRoomListByFidAsync(ulong fid)` | 获取指定贴吧的房间列表。 |
| `DislikeAsync(string fname)` / `DislikeAsync(ulong fid)` | 屏蔽贴吧首页推荐。 |
| `UndislikeAsync(string fname)` / `UndislikeAsync(ulong fid)` | 解除贴吧首页推荐屏蔽。 |
| `GetDislikeForumsAsync(int pn = 1, int rn = 20)` | 获取首页推荐屏蔽贴吧列表。 |

## `client.Threads`

适合主题列表、楼层、楼中楼、互动、回帖和帖子管理。

| 方法 | 说明 |
| --- | --- |
| `GetThreadsAsync(string fname, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply, bool isGood = false)` / `GetThreadsAsync(ulong fid, ...)` | 获取主题帖列表。 |
| `GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc, bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0, bool commentSortByAgree = false)` | 获取楼层列表。 |
| `GetCommentsAsync(long tid, long pid, int pn = 1, bool isComment = false)` | 获取楼中楼回复列表。 |
| `GetRecoversAsync(string fname, int pn = 1, int rn = 10, long? userId = null)` / `GetRecoversAsync(ulong fid, ...)` | 获取回收站列表。 |
| `GetRecoverInfoAsync(string fname, long tid, long pid = 0)` / `GetRecoverInfoAsync(ulong fid, ...)` | 获取回收站条目的正文详情。 |
| `GetTabMapAsync(string fname)` / `GetTabMapAsync(ulong fid)` | 获取贴吧分区映射。 |
| `AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false, bool isUndo = false)` | 统一的互动入口；可点赞、点踩或撤销。 |
| `DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false)` | 点踩内容。 |
| `UnagreeAsync(long tid, long pid = 0, bool isComment = false)` | 取消点赞。 |
| `UndisagreeAsync(long tid, long pid = 0, bool isComment = false)` | 取消点踩。 |
| `AddPostAsync(string fname, long tid, string content, string? showName = null)` | 回复主题帖。 |
| `DelThreadAsync(string fname, long tid)` | 删除主题帖。 |
| `DelPostAsync(string fname, long tid, long pid)` | 删除回复。 |
| `DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block = false)` | 批量删除主题帖。 |
| `DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block = false)` | 批量删除回复。 |
| `GoodAsync(string fname, long tid, string cname = "")` | 加精主题帖。 |
| `UngoodAsync(string fname, long tid)` | 取消加精。 |
| `TopAsync(string fname, long tid, bool isVip = false)` | 置顶主题帖。 |
| `UntopAsync(string fname, long tid, bool isVip = false)` | 取消置顶。 |
| `MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0)` | 移动主题帖分区。 |
| `RecommendAsync(string fname, long tid)` | 推荐主题帖。 |
| `RecoverAsync(string fname, long tid = 0, long pid = 0, bool isHide = false)` | 恢复主题帖或回复。 |
| `SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate = true)` | 设置主题帖回复隐私。 |

## `client.Users`

适合用户资料、主页、关注关系、黑名单、资料修改和用户内容读取。

| 方法 / 属性 | 说明 |
| --- | --- |
| `GetTbsAsync()` | 获取当前会话的 TBS。 |
| `GetUserInfoAppAsync(int userId)` | 通过 App `user_info` 接口获取用户信息，返回 `UserInfoGuInfoApp`。 |
| `GetUserInfoWebAsync(int userId)` | 通过 Web `user_info` 接口获取用户信息，返回 `UserInfoGuInfoWeb`。 |
| `GetProfileAsync(int userId)` / `GetProfileAsync(string portraitOrUserName)` | 读取资料页信息。 |
| `GetHomepageAsync(int userId, int pn = 1)` | 读取用户主页内容和主页快照。 |
| `FollowAsync(string portrait)` | 关注用户。 |
| `UnfollowAsync(string portrait)` | 取消关注用户。 |
| `GetFollowsAsync(long userId, int pn = 1)` | 获取关注列表。 |
| `GetFansAsync(long userId, int pn = 1)` | 获取粉丝列表。 |
| `RemoveFanAsync(long userId)` | 移除粉丝。 |
| `GetPanelInfoAsync(string nameOrPortrait)` | 获取用户面板信息。 |
| `GetUserInfoJsonAsync(string username)` | 获取用户 JSON 信息。 |
| `GetSelfInfoAsync()` | 获取当前用户信息。 |
| `GetSelfInfoInitNicknameAsync()` | 获取当前用户信息的 `init_nickname` 入口。 |
| `GetSelfInfoMoIndexAsync()` | 获取当前用户信息的 `mo_index` 入口。 |
| `LoginAsync()` | 执行登录态相关入口并返回 `LoginResult`。 |
| `GetBlacklistAsync()` | 获取 `Blacklist` 黑名单列表。 |
| `GetBlacklistOldAsync(int pn = 1, int rn = 20)` | 获取 `BlacklistOld` 黑名单列表。 |
| `SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All)` | 设置 `Blacklist` 权限项。 |
| `AddBlacklistOldAsync(long userId)` | 通过 `_old` 接口把用户加入黑名单。 |
| `RemoveBlacklistOldAsync(long userId)` | 通过 `_old` 接口把用户移出黑名单。 |
| `GetUserForumInfoAsync(ulong fid, string portrait)` / `GetUserForumInfoAsync(string fname, string portrait)` | 获取用户在指定贴吧内的信息。 |
| `GetRankUsersAsync(string fname, int pn = 1)` | 获取吧内等级排行榜用户列表。 |
| `SetNicknameAsync(string nickName)` | 只修改昵称。 |
| `SetProfileAsync(string nickName, string sign, Gender gender)` | 一次修改昵称、签名和性别。 |
| `GetUserByTiebaUidAsync(long tiebaUid)` | 通过 Tieba UID 查询用户信息。 |
| `GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5")` | 获取用户回复列表。 |
| `GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true)` | 获取用户主题帖列表。 |
| `UserContentCmd` | 公开的用户内容命令常量。 |

> 模块边界
> - `Users` 负责资料、主页、社交关系、黑名单和用户内容。
> - `@`、回复、私信、吧群消息、已读状态和推送通知解析都在 `client.Messages`。

## `client.Admins`

适合吧务团队、权限、封禁、日志和申诉处理。

| 方法 | 说明 |
| --- | --- |
| `AddBawuAsync(string fname, string userName, BawuType bawuType)` | 添加吧务。 |
| `DelBawuAsync(string fname, string portrait, BawuType bawuType)` | 删除吧务。 |
| `AddBawuBlacklistAsync(string fname, long userId)` | 添加吧务黑名单。 |
| `DelBawuBlacklistAsync(string fname, long userId)` | 移除吧务黑名单。 |
| `GetBawuBlacklistAsync(string fname, int pn = 1)` | 获取吧务黑名单列表。 |
| `GetBawuInfoAsync(string fname)` | 获取吧务团队信息。 |
| `GetBawuPermAsync(string fname, string portrait)` | 获取目标吧务当前权限。 |
| `SetBawuPermAsync(string fname, string portrait, BawuPermType permissions)` | 设置目标吧务权限。 |
| `GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null)` | 获取吧务删帖日志。 |
| `GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null)` | 获取吧务用户管理日志。 |
| `GetUnblockAppealsAsync(string fname, int pn = 1, int rn = 20)` | 获取解封申诉列表。 |
| `HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false)` | 批量处理解封申诉。 |
| `GetBlocksAsync(string fname, string userName = "", int pn = 1)` | 获取封禁列表。 |
| `BlockAsync(string fname, string portrait, int day = 1, string reason = "")` | 封禁用户。 |
| `UnblockAsync(string fname, long userId)` | 解除封禁。 |

## `client.Messages`

适合 inbox、私信、吧群消息和推送通知解析。

| 方法 | 说明 |
| --- | --- |
| `GetAtsAsync(int pn = 1)` | 获取 `@` 消息列表。 |
| `GetRepliesAsync(int pn = 1)` | 获取回复消息列表。 |
| `GetGroupMessagesAsync(int getType = 1)` | 获取当前已知消息组的消息。 |
| `GetGroupMessagesAsync(IReadOnlyList<long> groupIds, int getType = 1)` | 获取指定消息组的消息。 |
| `SendMessageAsync(long userId, string content)` | 按用户 ID 发送私信。 |
| `SendMessageAsync(string portraitOrUserName, string content)` | 按 portrait 或用户名发送私信。 |
| `SendChatroomMessageAsync(long chatroomId, ulong forumId, string text, IReadOnlyList<long>? atUserIds = null, int robotCode = -1)` | 发送吧群消息。 |
| `SetMessageReadAsync(WsMessage message)` | 将一条私信标记为已读。 |
| `ParsePushNotifications(byte[] payload)` | 解析 `push_notify` 负载。 |

## `client.Client`

适合生命周期初始化和链路预热。

| 方法 | 说明 |
| --- | --- |
| `InitWebSocketAsync()` | 初始化 WebSocket 连接。 |
| `InitZIdAsync()` | 初始化客户端 ZId。 |
| `SyncAsync()` | 同步 `ClientId` 和 `SampleId`。 |

> 模块边界
> - 业务消息 API 在 `client.Messages`。
> - `client.Client` 只保留连接和生命周期辅助能力。

## 日志 helper

`TiebaLogging` 是根级日志 helper。

| 成员 | 说明 |
| --- | --- |
| `Factory` | 当前 `ILoggerFactory`。 |
| `GetLogger(string categoryName)` | 按分类名获取 logger。 |
| `GetLogger<TCategoryName>()` | 按泛型分类获取 logger。 |
| `EnableFileLog(string filePath, LogLevel minimumLevel = LogLevel.Information)` | 启用文件日志并返回新的 `ILoggerFactory`。 |
| `Reset()` | 重置为默认空 logger。 |

## 公开结果类型参考

这一节用于补全方法签名后面出现的返回类型，方便你从“方法名”继续查到“结果对象”。

### Forums

| 类型 | 常见来源 | 说明 |
| --- | --- | --- |
| `Forum` | `GetForumAsync(...)` | 贴吧基础信息。 |
| `ForumDetail` | `GetDetailAsync(...)` | 贴吧详情。 |
| `FollowForums` | `GetFollowForumsAsync(...)` | 指定用户关注吧列表。 |
| `SelfFollowForums` | `GetSelfFollowForumsAsync(...)` | 当前账号关注吧列表，带 `HasMore`。 |
| `SelfFollowForumsV1` | `GetSelfFollowForumsV1Async(...)` | 当前账号关注吧 V1 列表，带 `Page`。 |
| `ExactSearches` | `SearchExactAsync(...)` | 吧内精确搜索结果。 |
| `LastReplyers` | `GetLastReplyersAsync(...)` | 带最后回复人的主题列表。 |
| `MemberUsers` | `GetMemberUsersAsync(...)` | 会员列表。 |
| `RankForums` | `GetRankForumsAsync(...)` | 签到排行。 |
| `RecomStatus` | `GetRecomStatusAsync(...)` | 推荐配额状态。 |
| `SquareForums` | `GetSquareForumsAsync(...)` | 吧广场列表。 |
| `ForumStatistics` | `GetStatisticsAsync(...)` | 贴吧统计数据。 |
| `ForumLevelInfo` | `GetForumLevelAsync(...)` | 当前账号在某吧的等级信息。 |
| `RoomList` | `GetRoomListByFidAsync(...)` | 指定贴吧的房间列表。 |
| `DislikeForums` | `GetDislikeForumsAsync(...)` | 首页推荐屏蔽贴吧列表。 |
| `ForumImage` | `GetImageAsync(...)`、`GetImageByHashAsync(...)`、`GetPortraitAsync(...)` | 图片或头像信息。 |
| `ForumImageBytes` | `GetImageBytesAsync(...)` | 图片原始字节结果。 |

### Threads

| 类型 | 常见来源 | 说明 |
| --- | --- | --- |
| `Threads` | `GetThreadsAsync(...)` | 主题帖列表。 |
| `Posts` | `GetPostsAsync(...)` | 楼层列表。 |
| `Comments` | `GetCommentsAsync(...)` | 楼中楼回复列表。 |
| `Recovers` | `GetRecoversAsync(...)` | 回收站列表。 |
| `RecoverInfo` | `GetRecoverInfoAsync(...)` | 回收站条目的正文详情。 |
| `TabMap` | `GetTabMapAsync(...)` | 分区名与分区 id 映射。 |

### Users

| 类型 | 常见来源 | 说明 |
| --- | --- | --- |
| `UserInfoGuInfoApp` | `GetUserInfoAppAsync(...)` | App `user_info` 返回类型。 |
| `UserInfoGuInfoWeb` | `GetUserInfoWebAsync(...)` | Web `user_info` 返回类型。 |
| `UserInfoPf` | `GetProfileAsync(...)` | 资料页信息。 |
| `Homepage` | `GetHomepageAsync(...)` | 用户主页内容和主页快照。 |
| `UserInfoPanel` | `GetPanelInfoAsync(...)` | 用户面板信息。 |
| `UserInfoJson` | `GetUserInfoJsonAsync(...)` | 用户 JSON 信息。 |
| `UserInfo` | `GetSelfInfoAsync()`、`GetSelfInfoInitNicknameAsync()`、`GetSelfInfoMoIndexAsync()` | 当前用户信息。 |
| `LoginResult` | `LoginAsync()` | 登录相关结果，包含 `User` 和 `Tbs`。 |
| `UserList` | `GetFollowsAsync(...)`、`GetFansAsync(...)` | 关注 / 粉丝列表。 |
| `BlacklistUsers` | `GetBlacklistAsync()` | 权限型黑名单列表。 |
| `BlacklistOldUsers` | `GetBlacklistOldAsync(...)` | `_old` 黑名单列表。 |
| `UserForumInfo` | `GetUserForumInfoAsync(...)` | 用户在指定贴吧内的信息。 |
| `RankUsers` | `GetRankUsersAsync(...)` | 吧内等级排行榜用户列表。 |
| `UserPostGroups` | `GetPostsAsync(...)` | 用户回复分组列表。 |
| `UserThreads` | `GetThreadsAsync(...)` | 用户主题帖列表。 |
| `UserInfoTUid` | `GetUserByTiebaUidAsync(...)` | Tieba UID 查询结果。 |

### Admins

| 类型 | 常见来源 | 说明 |
| --- | --- | --- |
| `BawuInfo` | `GetBawuInfoAsync(...)` | 吧务团队信息。 |
| `BawuPerm` | `GetBawuPermAsync(...)` | 已分配吧务权限。 |
| `BawuBlacklistUsers` | `GetBawuBlacklistAsync(...)` | 吧务黑名单列表。 |
| `BawuPostLogs` | `GetBawuPostLogsAsync(...)` | 删帖日志列表。 |
| `BawuUserLogs` | `GetBawuUserLogsAsync(...)` | 用户管理日志列表。 |
| `Appeals` | `GetUnblockAppealsAsync(...)` | 解封申诉列表。 |
| `Blocks` | `GetBlocksAsync(...)` | 封禁列表。 |

### Messages

| 类型 | 常见来源 | 说明 |
| --- | --- | --- |
| `AtMessages` | `GetAtsAsync(...)` | `@` 消息列表。 |
| `ReplyMessages` | `GetRepliesAsync(...)` | 回复消息列表。 |
| `WsMsgGroups` | `GetGroupMessagesAsync(...)` | WebSocket 私信消息组列表。 |
| `WsNotify` | `ParsePushNotifications(...)` | 解析后的 push 通知。 |

## 完整公开模型命名空间索引

如果你已经知道类型名，可以直接在这一节按命名空间查找。这里补齐了公开模型和辅助类型的完整索引。

### `AioTieba4DotNet.Models`

- `BlacklistType`
- `Gender`
- `PostSortType`
- `PrivLike`
- `PrivReply`
- `ReqUInfo`
- `ThreadSortType`

### `AioTieba4DotNet.Models.Forums`

- `DislikeForum`、`DislikeForums`、`DislikeForumsPage`
- `ExactSearches`
- `FollowForum`、`FollowForums`
- `Forum`、`ForumDetail`
- `ForumImage`、`ForumImageBytes`
- `ForumImageFormat`、`ForumImageSize`
- `ForumLevelInfo`
- `ForumRankType`
- `ForumSearchType`
- `ForumStatistics`
- `LastReplyers`
- `MemberUsers`
- `RankForums`
- `RecomStatus`
- `RoomList`
- `SelfFollowForum`、`SelfFollowForums`
- `SelfFollowForumV1`、`SelfFollowForumsV1`、`SelfFollowForumsV1Page`
- `SquareForums`

### `AioTieba4DotNet.Models.Threads`

- `Comment`、`Comments`
- `ForumT`
- `PageT`
- `Post`、`Posts`
- `Recover`、`RecoverInfo`、`RecoverPage`、`Recovers`、`RecoverUser`
- `ShareThread`
- `TabMap`
- `Thread`、`Threads`
- `UserInfoT`

### `AioTieba4DotNet.Models.Users`

- `AtMessage`、`AtMessages`
- `BlacklistOldUser`、`BlacklistOldUsers`
- `BlacklistUser`、`BlacklistUsers`
- `Homepage`
- `LoginResult`
- `RankUser`、`RankUsers`
- `ReplyMessage`、`ReplyMessages`
- `UserContent`
- `UserForumInfo`
- `UserInfoGuInfoApp`、`UserInfoGuInfoWeb`
- `UserInfoJson`
- `UserInfoPanel`
- `UserInfoPf`
- `UserInfoTUid`
- `UserInfoUf`
- `UserPost`、`UserPostGroups`、`UserPosts`
- `UserThread`、`UserThreads`
- `VirtualImagePf`

### `AioTieba4DotNet.Models.Admins`

- `Appeals`
- `BawuBlacklistUsers`
- `BawuInfo`
- `BawuPerm`
- `BawuPermType`
- `BawuPostLog`、`BawuPostLogMedia`、`BawuPostLogPage`、`BawuPostLogs`、`BawuPostLogQueryOptions`
- `BawuSearchType`
- `BawuType`
- `BawuUserLog`、`BawuUserLogPage`、`BawuUserLogs`、`BawuUserLogQueryOptions`
- `Blocks`

### `AioTieba4DotNet.Models.Messages`

- `GroupType`
- `MsgType`
- `WsMessage`
- `WsMsgGroup`、`WsMsgGroupInfo`、`WsMsgGroups`
- `WsNotify`
- `WsStatus`

### `AioTieba4DotNet.Models.Shared`

- `Containers<T>`
- `UserInfo`
- `UserList`
- `VoteInfo`、`VoteOption`

### `AioTieba4DotNet.Models.Contents`

- `Content`
- `FragAt`
- `FragEmoji`
- `FragImage`
- `FragItem`
- `FragLink`
- `FragText`
- `FragTiebaPlus`
- `FragUnknown`
- `FragVideo`
- `FragVoice`
- `IFrag`

## 公开异常

| 异常 | 说明 |
| --- | --- |
| `TiebaException` | 所有库异常的基类。 |
| `TiebaAuthenticationException` | 鉴权异常。 |
| `TiebaConfigurationException` | 配置异常。 |
| `TieBaServerException` | 服务端业务异常，包含 `Code`。 |
| `TiebaTransportException` | 传输异常。 |
| `TiebaTimeoutException` | 超时异常。 |
| `TiebaProtocolException` | 协议异常。 |
| `TiebaUnsupportedOperationException` | 当前客户端不支持该操作路径。 |
