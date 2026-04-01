# How-to: Forums

这页按“论坛相关任务”来组织。你不需要先理解全部模块，只要找到自己的目标，再复制对应模式即可。

## 读取贴吧基础信息

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var fid = await client.Forums.GetFidAsync("csharp");
var forum = await client.Forums.GetForumAsync("csharp");
var detail = await client.Forums.GetDetailAsync(fid);

Console.WriteLine($"fid = {fid}");
Console.WriteLine($"名称 = {forum.Name}");
Console.WriteLine($"成员数 = {detail.MemberNum}");
```

适合先做 forum 名称和 fid 的解析，再把结果传给线程或后台相关流程。

## 关注或取消关注贴吧

按吧名调用最直接。需要登录态。

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Forums.FollowAsync("csharp");
await client.Forums.UnfollowAsync("csharp");
```

论坛公开关注写操作统一使用 `FollowAsync(...)` / `UnfollowAsync(...)` 这组名称。

## 查询某个用户或当前账号关注的贴吧

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var selfFollowForums = await client.Forums.GetSelfFollowForumsAsync();
var anotherUserForums = await client.Forums.GetFollowForumsAsync(123456789);

Console.WriteLine(selfFollowForums.Objs.Count);
Console.WriteLine(anotherUserForums.Objs.Count);
```

`GetSelfFollowForumsAsync(...)` 和 `GetSelfFollowForumsV1Async(...)` 是两组并列支持的接口。前者对应 aiotieba `get_self_follow_forums`，返回 `SelfFollowForums`，包含每个贴吧的 `IsSigned` 状态；后者对应 aiotieba `get_self_follow_forums_v1`，返回 `SelfFollowForumsV1` 并保留显式分页信息。这里的 `V1` 指的是 upstream 的 V1 这一组接口，不是泛指分页别名。

## 单吧签到、批量签到、成长任务签到

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Forums.SignAsync("csharp");
await client.Forums.SignForumsAsync();
await client.Forums.SignGrowthAsync();
```

这三个入口表示三件不同的事:

- `SignAsync(...)`，单个贴吧签到
- `SignForumsAsync()`，对当前账号关注的贴吧做一键签到
- `SignGrowthAsync()`，完成成长任务签到

## 做精确搜索

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var result = await client.Forums.SearchExactAsync(
    "csharp",
    query: "HttpClient",
    searchType: ForumSearchType.All,
    onlyThread: true);

Console.WriteLine(result.Objs.Count);
```

需要按 fid 搜索时，也可以改用 `SearchExactAsync(ulong fid, ...)`。

## 获取首页帖子附带最后回复人

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var threads = await client.Forums.GetLastReplyersAsync(
    "csharp",
    sort: ThreadSortType.Reply,
    isGood: false);

Console.WriteLine(threads.Objs.Count);
```

这个入口适合做首页浏览或看板，而不是替代完整的帖子正文读取。

## 获取统计、排行榜和成员信息

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var statistics = await client.Forums.GetStatisticsAsync("csharp");
var rankForums = await client.Forums.GetRankForumsAsync("csharp");
var memberUsers = await client.Forums.GetMemberUsersAsync("csharp");

Console.WriteLine(statistics.Member.Count);
Console.WriteLine(rankForums.Objs.Count);
Console.WriteLine(memberUsers.Objs.Count);
```

常用补充入口还有:

- `GetForumLevelAsync(...)`
- `GetRecomStatusAsync(...)`
- `GetSquareForumsAsync(...)`
- `GetCidAsync(...)`

## 下载图片或按 hash 获取贴吧图片

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var image = await client.Forums.GetImageByHashAsync("你的图片 hash", ForumImageSize.Original);
var portrait = await client.Forums.GetPortraitAsync("tb.1.someportrait", ForumImageSize.Small);

Console.WriteLine(image.ImageUrl);
Console.WriteLine(portrait.ImageUrl);
```

## 相关阅读

- [Getting Started](./getting-started.md)
- [How-to: Threads](./how-to-threads.md)
- [Modules Reference](./modules.md)
- [Troubleshooting](./troubleshooting.md)
