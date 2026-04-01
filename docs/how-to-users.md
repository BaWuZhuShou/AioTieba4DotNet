# How-to: Users

这页覆盖“用户资料、社交关系和黑名单”相关的任务。`Users` 模块里既有推荐入口，也有为了 upstream 兼容保留的 legacy 入口，文档会把这两条路径分开说清楚。

## 读取推荐的用户基础信息和资料页

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var basicInfo = await client.Users.GetBasicInfoAsync(123456789);
var profile = await client.Users.GetProfileAsync("某个 portrait 或用户名");

Console.WriteLine(basicInfo.ShowName);
Console.WriteLine(profile.ShowName);
```

推荐默认路径是:

- `GetBasicInfoAsync(...)`
- `GetProfileAsync(int)` 或 `GetProfileAsync(string)`
- `GetSelfInfoAsync()`

## 读取主页内容而不是资料页元数据

`GetHomepageAsync(...)` 和 `GetProfileAsync(...)` 不是一回事。前者是主页帖子列表和主页快照，后者是资料页元数据。

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

## 维护当前黑名单

推荐新代码直接使用当前入口:

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var blacklist = await client.Users.GetBlacklistAsync();
await client.Users.SetBlacklistAsync(123456789, BlacklistType.All);

Console.WriteLine(blacklist.Objs.Count);
```

如果你在迁移旧调用，也可以继续使用:

- `GetBlacklistLegacyAsync(...)`
- `AddBlacklistLegacyAsync(...)`
- `RemoveBlacklistLegacyAsync(...)`

## 修改资料

推荐新代码使用 `SetProfileAsync(...)`，一次更新昵称、签名和性别。

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Users.SetProfileAsync(
    nickName: "新的昵称",
    sign: "新的个性签名",
    gender: Gender.Male);
```

如果你只是在迁移老接口，v3 仍保留 `SetNicknameLegacyAsync(...)`。

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

这些入口主要给兼容或数据对齐场景使用，不是默认第一选择。

```csharp
using var client = new TiebaClient();

var webInfo = await client.Users.GetBasicInfoWebAsync(123456789);
var mappedUser = await client.Users.GetUserByTiebaUidAsync(9876543210);

Console.WriteLine(webInfo.ShowName);
Console.WriteLine(mappedUser.ShowName);
```

## 相关阅读

- [How-to: Messages](./how-to-messages.md)
- [How-to: Forums](./how-to-forums.md)
- [Advanced](./advanced.md)
- [Troubleshooting](./troubleshooting.md)
