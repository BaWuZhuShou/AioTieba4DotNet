# 用户相关

这页按常见用户任务组织。要查完整方法和签名，请同时对照 [API 参考](/reference/modules)。

本页示例里的 `USER_ID_PLACEHOLDER`、`PORTRAIT_PLACEHOLDER` 等值统一遵循[示例占位符词汇表](/guide/getting-started#example-placeholder-glossary)。

## 开始前

- 访客可直接使用大部分读取类接口。
- 关注、黑名单、资料修改等写操作需要登录态，也就是 `BDUSS` 和 `STOKEN`。
- 下面所有示例都以 `client.Users` 为入口。
- 消息读取、私信、吧群消息、已读状态和推送通知解析属于 `client.Messages`，不属于 `client.Users`。

## 读取 App 或 Web `user_info`

`GetUserInfoAppAsync(...)` 和 `GetUserInfoWebAsync(...)` 是并列支持的两组 `user_info` 接口。它们都读取用户基础信息，并统一返回 `UserInfo`；区别主要在于它们对应的上游入口不同，而不是公开 DTO 类型不同。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var appInfo = await client.Users.GetUserInfoAppAsync(int.Parse("USER_ID_PLACEHOLDER"));
var webInfo = await client.Users.GetUserInfoWebAsync(int.Parse("USER_ID_PLACEHOLDER"));

Console.WriteLine(appInfo.ShowName);
Console.WriteLine(webInfo.ShowName);
```

可以这样选用：

- 需要走 App `user_info` 入口时，用 `GetUserInfoAppAsync(...)`
- 需要走 Web `user_info` 入口时，用 `GetUserInfoWebAsync(...)`
- 如果你要的是资料页信息或主页帖子列表，不要在这里二选一，改看下面两节

## 区分资料页和主页

`GetProfileAsync(...)` 和 `GetHomepageAsync(...)` 不是同一件事。

- `GetProfileAsync(...)` 读取资料页元数据，适合展示昵称、签名、头像相关资料页信息
- `GetHomepageAsync(...)` 读取主页内容和主页快照，适合展示用户主页上的帖子列表

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var profile = await client.Users.GetProfileAsync("USER_NAME_OR_PORTRAIT_PLACEHOLDER");
var homepage = await client.Users.GetHomepageAsync(int.Parse("USER_ID_PLACEHOLDER"), pn: 1);

Console.WriteLine(profile.ShowName);
Console.WriteLine(homepage.Count);
```

如果你正在做用户资料卡片或资料页详情，优先从 `GetProfileAsync(...)` 开始。如果你要做主页动态、主页帖子翻页或主页快照读取，应该调用 `GetHomepageAsync(...)`。

## 获取自己、TBS 和面板信息

这一组适合“当前账号上下文”或“轻量面板信息”场景。它们都属于 `Users` 模块，但用途并不相同。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var tbs = await client.Users.GetTbsAsync();
var selfInfo = await client.Users.GetSelfInfoAsync();
var panel = await client.Users.GetPanelInfoAsync("USER_NAME_OR_PORTRAIT_PLACEHOLDER");

Console.WriteLine(tbs);
Console.WriteLine(selfInfo.ShowName);
Console.WriteLine(panel.ShowName);
```

补充入口还有：

- `GetSelfInfoInitNicknameAsync()`
- `GetSelfInfoMoIndexAsync()`
- `LoginAsync()`

如果你要处理 `@`、回复、私信或消息已读状态，请切到 `client.Messages`，不要继续在 `Users` 里找消息能力。

## 关注、取关、查看关注和粉丝

关注关系这组接口覆盖写操作和读取操作。写操作要求登录态，读取其他用户的关注或粉丝列表则可以按场景决定是否使用已登录客户端。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

await client.Users.FollowAsync("PORTRAIT_PLACEHOLDER");
await client.Users.UnfollowAsync("PORTRAIT_PLACEHOLDER");

var follows = await client.Users.GetFollowsAsync(long.Parse("USER_ID_PLACEHOLDER"));
var fans = await client.Users.GetFansAsync(long.Parse("USER_ID_PLACEHOLDER"));

Console.WriteLine(follows.Objs.Count);
Console.WriteLine(fans.Objs.Count);
```

如果你要移除粉丝，还可以使用 `RemoveFanAsync(...)`。这仍然是社交关系管理，不是黑名单接口。

## 使用 `Blacklist` 接口组管理权限型黑名单

`GetBlacklistAsync(...)` 和 `SetBlacklistAsync(...)` 对应 aiotieba 的 `get_blacklist` / `set_blacklist`。这组接口返回 `BlacklistUsers`，适合按权限位管理黑名单能力，比如 follow、interact、chat 等限制项。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var blacklist = await client.Users.GetBlacklistAsync();
await client.Users.SetBlacklistAsync(long.Parse("USER_ID_PLACEHOLDER"), BlacklistType.All);

Console.WriteLine(blacklist.Objs.Count);
```

如果你的业务需要的是“设置一组权限项”，优先使用这组 `Blacklist` 接口，而不是 `_old` 接口组。

## 使用 `BlacklistOld` 接口组管理 `_old` 黑名单列表

`GetBlacklistOldAsync(...)`、`AddBlacklistOldAsync(...)` 和 `RemoveBlacklistOldAsync(...)` 对应 aiotieba 的 `get_blacklist_old`、`add_blacklist_old`、`del_blacklist_old`。这组接口返回 `BlacklistOldUsers`，保留了 `_old` 这一组自己的分页和 add/remove 语义。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var oldBlacklist = await client.Users.GetBlacklistOldAsync(1, 20);
await client.Users.AddBlacklistOldAsync(long.Parse("USER_ID_PLACEHOLDER"));
await client.Users.RemoveBlacklistOldAsync(long.Parse("USER_ID_PLACEHOLDER"));

Console.WriteLine(oldBlacklist.Page.CurrentPage);
```

这里的 `Old` 直接对应 upstream `_old` 这一组接口名，不表示 deprecated，也不表示它会被 `SetBlacklistAsync(...)` 自动替代。两组黑名单接口并列存在，按你的返回结构和写入语义选择即可。

## 修改资料

资料写入有两条并列路径，不应该混成一个入口：

- `SetProfileAsync(...)`，一次更新昵称、签名和性别
- `SetNicknameAsync(...)`，只更新昵称

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

await client.Users.SetProfileAsync(
    nickName: "NICKNAME_PLACEHOLDER",
    sign: "SIGNATURE_PLACEHOLDER",
    gender: Gender.Male);

await client.Users.SetNicknameAsync("NICKNAME_PLACEHOLDER");
```

如果你只改一个昵称字段，用 `SetNicknameAsync(...)` 更直接。如果你要同步更新资料页上的多个字段，使用 `SetProfileAsync(...)`。

## 读取吧内用户信息、等级榜和发帖历史

这部分适合做吧内名片、等级榜或用户内容页。它们仍属于 `Users` 模块，因为关注点是“某个用户在贴吧里的信息和内容”。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var forumInfo = await client.Users.GetUserForumInfoAsync("FORUM_NAME_PLACEHOLDER", "PORTRAIT_PLACEHOLDER");
var rankUsers = await client.Users.GetRankUsersAsync("FORUM_NAME_PLACEHOLDER");
var userThreads = await client.Users.GetThreadsAsync(int.Parse("USER_ID_PLACEHOLDER"));
var userPosts = await client.Users.GetPostsAsync(int.Parse("USER_ID_PLACEHOLDER"));

Console.WriteLine(forumInfo.User.ShowName);
Console.WriteLine(rankUsers.Objs.Count);
Console.WriteLine(userThreads.Objs.Count);
Console.WriteLine(userPosts.Objs.Count);
```

如果你还需要从 Tieba UID 反查用户，可以补充使用 `GetUserByTiebaUidAsync(...)`。这属于用户身份映射，不是资料页或主页读取。

## 下一步

- 想确认六个模块的完整边界，继续看 [API 参考](/reference/modules)
- 想理解传输策略、生命周期和模块拆分原因，继续看 [进阶](/guide/advanced)
- 想排查凭据、配置或链路问题，继续看 [排障](/guide/troubleshooting)
