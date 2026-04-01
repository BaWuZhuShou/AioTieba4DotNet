# AioTieba4DotNet 方法与任务速查

这份速查的目标不是列出全部 API，而是帮助 AI 在回答时快速把“用户目标”映射到正确的公开模块和代表方法。

默认目标是：**直接生成可复制的 C# 代码**，而不是只做 API 讲解。

## 1. 先选初始化方式

### 访客读取

适合只读场景，例如查吧、读主题帖、查用户资料。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();
```

### 已登录任务

适合签到、私信、资料修改、吧务操作等写场景。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");
```

### 显式配置

适合要控制超时、HTTP 强制模式或统一配置的场景。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "你的BDUSS",
    Stoken = "你的STOKEN",
    TransportMode = TiebaTransportMode.Http,
    RequestTimeout = TimeSpan.FromSeconds(15),
    MaxReadRetryAttempts = 1,
});
```

## 2. Forums：查吧、关注、签到、搜索、统计

### 常见任务 -> 代表方法

- 按吧名查 `fid`：`GetFidAsync(string)`
- 读取贴吧基础信息：`GetForumAsync(string)`、`GetDetailAsync(string|ulong)`
- 关注/取关：`FollowAsync(...)`、`UnfollowAsync(...)`
- 单吧签到/一键签到/成长签到：`SignAsync(...)`、`SignForumsAsync()`、`SignGrowthAsync()`
- 获取当前账号关注吧：`GetSelfFollowForumsAsync(...)`
- 获取当前账号关注吧 V1 分页结果：`GetSelfFollowForumsV1Async(...)`
- 做精确搜索：`SearchExactAsync(...)`
- 获取首页帖子附带最后回复人：`GetLastReplyersAsync(...)`
- 读取统计/排行/成员信息：`GetStatisticsAsync(...)`、`GetRankForumsAsync(...)`、`GetMemberUsersAsync(...)`

### 代表用法

```csharp
using var client = new TiebaClient();

var fid = await client.Forums.GetFidAsync("csharp");
var forum = await client.Forums.GetForumAsync("csharp");
var detail = await client.Forums.GetDetailAsync(fid);
```

### 生成代码时优先这样答

- 用户说“查某个吧的 fid / 详情”时，直接生成 `GetFidAsync(...)` + `GetDetailAsync(...)` 示例。
- 用户说“签到”时，默认生成登录态客户端 + `SignAsync(...)` 或 `SignForumsAsync()`。
- 用户说“搜索帖子”时，优先生成 `SearchExactAsync(...)` 示例。

## 3. Threads：读帖、楼层、楼中楼、回复、互动、吧务帖子操作

### 常见任务 -> 代表方法

- 读取主题帖列表：`GetThreadsAsync(...)`
- 读取帖子楼层：`GetPostsAsync(...)`
- 读取楼中楼：`GetCommentsAsync(...)`
- 回复主题帖：`AddPostAsync(...)`
- 点赞/点踩/取消：`AgreeAsync(...)`、`DisagreeAsync(...)`、`UnagreeAsync(...)`、`UndisagreeAsync(...)`
- 删除、加精、置顶、移动、推荐、恢复：`DelThreadAsync(...)`、`DelPostAsync(...)`、`GoodAsync(...)`、`TopAsync(...)`、`MoveAsync(...)`、`RecommendAsync(...)`、`RecoverAsync(...)`
- 读取回收站和分区映射：`GetRecoversAsync(...)`、`GetRecoverInfoAsync(...)`、`GetTabMapAsync(...)`
- 设置回复隐私：`SetThreadPrivacyAsync(...)`

### 代表用法

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var threads = await client.Threads.GetThreadsAsync(
    "csharp",
    pn: 1,
    rn: 30,
    sort: ThreadSortType.Reply,
    isGood: false);
```

### 生成代码时优先这样答

- 用户说“读取帖子列表”时，优先生成 `GetThreadsAsync(...)` 示例。
- 用户说“读某个帖子的楼层”时，优先生成 `GetPostsAsync(...)` 示例。
- 用户说“回复帖子”时，默认生成登录态客户端 + `AddPostAsync(...)` 示例。
- 用户说“置顶/加精/删帖/移动”时，直接生成对应写方法，并提醒通常需要吧务权限。

## 4. Users：资料、主页、社交关系、黑名单、资料修改

### 常见任务 -> 代表方法

- 查用户基础资料：`GetUserInfoAppAsync(...)`、`GetUserInfoWebAsync(...)`
- 查资料页：`GetProfileAsync(...)`
- 查主页内容：`GetHomepageAsync(...)`
- 查自己/TBS/面板信息：`GetSelfInfoAsync()`、`GetTbsAsync()`、`GetPanelInfoAsync(...)`
- 关注/取关用户：`FollowAsync(...)`、`UnfollowAsync(...)`
- 获取关注和粉丝：`GetFollowsAsync(...)`、`GetFansAsync(...)`
- 维护黑名单权限组：`GetBlacklistAsync()`、`SetBlacklistAsync(...)`
- 维护 `_old` 黑名单接口组：`GetBlacklistOldAsync(...)`、`AddBlacklistOldAsync(...)`、`RemoveBlacklistOldAsync(...)`
- 修改资料或昵称：`SetProfileAsync(...)`、`SetNicknameAsync(...)`
- 读取吧内用户信息和用户发帖历史：`GetUserForumInfoAsync(...)`、`GetThreadsAsync(...)`、`GetPostsAsync(...)`

### 重要边界

- `GetProfileAsync(...)` 和 `GetHomepageAsync(...)` 是两类不同读取。
- `GetUserInfoAppAsync(...)` 和 `GetUserInfoWebAsync(...)` 是并列支持的两组接口。
- `GetBlacklistAsync(...)` 这一组和 `BlacklistOld` 这一组也是并列支持的接口，不要混成一个入口。

### 生成代码时优先这样答

- 用户说“查用户资料”但没提主页时，优先用 `GetProfileAsync(...)` 或 `GetUserInfoAppAsync(...)`。
- 用户明确说“主页帖子/主页内容”时，用 `GetHomepageAsync(...)`。
- 用户说“黑名单”时，先判断是权限型黑名单还是 `_old` 接口组，再生成对应示例。
- 用户说“改昵称/改签名/改性别”时，默认生成登录态客户端 + `SetProfileAsync(...)` 或 `SetNicknameAsync(...)`。

## 5. Messages：@、回复、私信、吧群消息、push 解析

### 常见任务 -> 代表方法

- 读取 @ 消息：`GetAtsAsync(...)`
- 读取回复消息：`GetRepliesAsync(...)`
- 读取私信消息组：`GetGroupMessagesAsync(...)`
- 发送私信：`SendMessageAsync(long, ...)` / `SendMessageAsync(string, ...)`
- 发送吧群消息：`SendChatroomMessageAsync(...)`
- 标记私信已读：`SetMessageReadAsync(...)`
- 解析 push 负载：`ParsePushNotifications(byte[])`

### 代表用法

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var ats = await client.Messages.GetAtsAsync();
var replies = await client.Messages.GetRepliesAsync();
```

### 重要边界

- 消息能力都在 `client.Messages`。
- `client.Client` 只负责生命周期，不负责消息 API。

### 生成代码时优先这样答

- 用户说“收 @ / 收回复”时，优先生成 `GetAtsAsync()` / `GetRepliesAsync()`。
- 用户说“发私信”时，优先生成 `SendMessageAsync(...)` 示例。
- 用户说“吧群消息”时，生成 `SendChatroomMessageAsync(...)`，并提醒需要已知 `chatroomId` 和 `forumId`。
- 用户说“解析 push”时，生成 `ParsePushNotifications(byte[])` 示例；只有需要提前连 WebSocket 时，再补 `client.Client.InitWebSocketAsync()`。

## 6. Admins：吧务、权限、日志、申诉、封禁管理

### 常见任务 -> 代表方法

- 管理吧务团队：`AddBawuAsync(...)`、`DelBawuAsync(...)`、`GetBawuInfoAsync(...)`
- 读写吧务权限：`GetBawuPermAsync(...)`、`SetBawuPermAsync(...)`
- 管理吧务黑名单：`AddBawuBlacklistAsync(...)`、`DelBawuBlacklistAsync(...)`、`GetBawuBlacklistAsync(...)`
- 读取吧务日志：`GetBawuPostLogsAsync(...)`、`GetBawuUserLogsAsync(...)`
- 封禁与申诉：`GetUnblockAppealsAsync(...)`、`HandleUnblockAppealsAsync(...)`、`GetBlocksAsync(...)`、`BlockAsync(...)`、`UnblockAsync(...)`

### 注意

这类操作通常要求登录态、吧务权限，以及明确的安全目标。普通资料或消息读取不要误用这个模块。

### 生成代码时优先这样答

- 用户说“封禁/解封/处理申诉/Bawu 权限”时，直接切到 `Admins`，不要误放到 `Forums` 或 `Users`。
- 生成示例时要附一句权限提醒，但代码仍然应该直接展示目标公开方法。

## 7. Client：生命周期初始化，不承载业务模块能力

### 代表方法

- `InitWebSocketAsync()`
- `InitZIdAsync()`
- `SyncAsync()`

### 什么时候用

- 你想提前建立 WebSocket 链路
- 你准备马上读取消息组、发送私信或处理连接敏感场景
- 你需要初始化客户端生命周期状态

### 生成代码时优先这样答

- 默认不要主动生成 `Client` 相关代码。
- 只有当用户明确要求预热连接、初始化 WebSocket、初始化 ZId 或同步状态时，才生成 `InitWebSocketAsync()`、`InitZIdAsync()`、`SyncAsync()` 示例。

## 8. 常见异常和选择建议

- `TiebaAuthenticationException`：缺少登录凭据却调用了写接口
- `TiebaConfigurationException`：配置本身非法，例如只传了 `Stoken` 没传 `Bduss`
- `TieBaServerException`：请求到服务端了，但业务条件不满足，例如无权限、目标不存在

## 9. 回答时的推荐模式

当用户问“怎么做某件事”时，优先这样组织回答：

1. 先判断是否需要登录态
2. 再指出应该用哪个模块
3. 再列 1-3 个代表方法
4. 最后给一个最小可运行示例

如果用户明确说“直接给我代码”，就把说明压缩到一两句，其余篇幅都给代码。
