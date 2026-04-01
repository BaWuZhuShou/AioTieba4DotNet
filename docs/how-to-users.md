# How-to: Users

这页覆盖“用户资料、社交关系和黑名单”相关的任务。`Users` 模块同时支持几组不同来源的用户接口，分别对应 aiotieba 的 app/web `user_info`、资料页信息、主页内容，以及 `get_blacklist` / `get_blacklist_old` 这两组黑名单接口。下面会把它们各自适合的场景分开说明。

## 读取用户基础信息和资料页

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var userInfo = await client.Users.GetUserInfoAppAsync(123456789);
var profile = await client.Users.GetProfileAsync("某个 portrait 或用户名");

Console.WriteLine(userInfo.ShowName);
Console.WriteLine(profile.ShowName);
```

常用组合是:

- `GetUserInfoAppAsync(...)`
- `GetUserInfoWebAsync(...)`
- `GetProfileAsync(int)` 或 `GetProfileAsync(string)`
- `GetSelfInfoAsync()`

`GetUserInfoAppAsync(...)` 和 `GetUserInfoWebAsync(...)` 是两组并列支持的 `user_info` 接口，分别对应 aiotieba `get_uinfo_getuserinfo_app` / `UserInfo_guinfo_app` 和 `get_uinfo_getuserinfo_web` / `UserInfo_guinfo_web`，并返回 `UserInfoGuInfoApp` 与 `UserInfoGuInfoWeb`。如果你要对齐 Web 返回结果，就直接选择 `GetUserInfoWebAsync(...)`。

## 读取主页内容而不是资料页元数据

`GetHomepageAsync(...)` 和 `GetProfileAsync(...)` 不是一回事。前者读取主页帖子列表和主页快照，后者读取资料页信息；两者对应 aiotieba 里的不同接口，不会合并成同一个入口。

```csharp
using var client = new TiebaClient();

var homepage = await client.Users.GetHomepageAsync(123456789, pn: 1);
Console.WriteLine(homepage.Threads.Count);
```

## 获取自己、TBS 和面板信息

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var tbs = await client.Users.GetTbsAsync();
var selfInfo = await client.Users.GetSelfInfoAsync();
var panel = await client.Users.GetPanelInfoAsync("某个用户名或 portrait");

Console.WriteLine(tbs);
Console.WriteLine(selfInfo.NameShow);
Console.WriteLine(panel.NameShow);
```

## 关注、取关、查看关注和粉丝

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Users.FollowAsync("目标用户 portrait");
await client.Users.UnfollowAsync("目标用户 portrait");

var follows = await client.Users.GetFollowsAsync(123456789);
var fans = await client.Users.GetFansAsync(123456789);

Console.WriteLine(follows.Objs.Count);
Console.WriteLine(fans.Objs.Count);
```

## 维护 `get_blacklist` 这组接口

`GetBlacklistAsync(...)` / `SetBlacklistAsync(...)` 这组入口对应 aiotieba `get_blacklist` / `set_blacklist`，返回 `BlacklistUsers`，用来读取或设置 follow / interact / chat 这些权限位。

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var blacklist = await client.Users.GetBlacklistAsync();
await client.Users.SetBlacklistAsync(123456789, BlacklistType.All);

Console.WriteLine(blacklist.Objs.Count);
```

## 维护 `get_blacklist_old` 这一组接口

`GetBlacklistOldAsync(...)` / `AddBlacklistOldAsync(...)` / `RemoveBlacklistOldAsync(...)` 这组入口对应 aiotieba `get_blacklist_old` / `add_blacklist_old` / `del_blacklist_old`，返回 `BlacklistOldUsers`。这里的 `Old` 直接对应 upstream `_old` 这一组接口，并不表示 deprecated 或 unsupported。它们和 `GetBlacklistAsync(...)` / `SetBlacklistAsync(...)` 同属黑名单能力，但保留自己的分页与 add/remove 语义，不会被折叠进权限入口。

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var oldBlacklist = await client.Users.GetBlacklistOldAsync(1, 20);
await client.Users.AddBlacklistOldAsync(123456789);
await client.Users.RemoveBlacklistOldAsync(123456789);

Console.WriteLine(oldBlacklist.Page.CurrentPage);
```

如果你需要这条 `_old` 接口组，请使用下面这三个并列入口:

- `GetBlacklistOldAsync(...)`
- `AddBlacklistOldAsync(...)`
- `RemoveBlacklistOldAsync(...)`

## 修改资料

`SetProfileAsync(...)` 和 `SetNicknameAsync(...)` 是分开的两组写入 API。前者一次更新昵称、签名和性别，后者只更新昵称。

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Users.SetProfileAsync(
    nickName: "新的昵称",
    sign: "新的个性签名",
    gender: Gender.Male);
```

如果你只想修改昵称，请使用 `SetNicknameAsync(...)`。如果要一次更新昵称、签名和性别，请使用 `SetProfileAsync(...)`。

## 读取吧内用户信息、等级榜和用户发帖历史

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var forumInfo = await client.Users.GetUserForumInfoAsync("csharp", "目标用户 portrait");
var rankUsers = await client.Users.GetRankUsersAsync("csharp");
var userThreads = await client.Users.GetThreadsAsync(123456789);
var userPosts = await client.Users.GetPostsAsync(123456789);

Console.WriteLine(forumInfo.UserName);
Console.WriteLine(rankUsers.Objs.Count);
Console.WriteLine(userThreads.Objs.Count);
Console.WriteLine(userPosts.Objs.Count);
```

## 读取 Web 兼容形状或 Tieba UID 映射

这些入口要么对应另一组并列支持的接口，要么保留了特定的返回结果。它们都是当前支持的公开 API，适合需要对齐 Web 返回结果或明确依赖返回结构的场景。

```csharp
using var client = new TiebaClient();

var webInfo = await client.Users.GetUserInfoWebAsync(123456789);
var mappedUser = await client.Users.GetUserByTiebaUidAsync(9876543210);

Console.WriteLine(webInfo.ShowName);
Console.WriteLine(mappedUser.ShowName);
```

## 相关阅读

- [How-to: Messages](./how-to-messages.md)
- [How-to: Forums](./how-to-forums.md)
- [Advanced](./advanced.md)
- [Troubleshooting](./troubleshooting.md)
