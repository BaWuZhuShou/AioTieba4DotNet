# 贴吧相关

这页按常见论坛任务组织。要查完整方法和签名，请同时对照 [API 参考](/reference/modules)。

本页示例会直接写成“你的吧名”“你的 BDUSS”“目标用户 ID”这类示意值，阅读时按自己的实际参数替换即可。

## 开始前

- 访客可直接使用读取类接口。
- 关注、取关、签到等写操作需要登录态，也就是 `BDUSS` 和 `STOKEN`。
- 下面所有示例都以 `client.Forums` 为入口。

## 读取贴吧基础信息

这组接口适合访客读取。通常先拿到 `fid`，再按需读取吧信息或详情。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var fid = await client.Forums.GetFidAsync("你的吧名");
var forum = await client.Forums.GetForumAsync("你的吧名");
var detail = await client.Forums.GetDetailAsync(fid);

Console.WriteLine($"fid = {fid}");
Console.WriteLine($"名称 = {forum.Fname}");
Console.WriteLine($"成员数 = {detail.MemberNum}");
```

如果你的后续流程要读取主题列表、签到状态或统计信息，这一步通常就是入口。

## 关注或取消关注贴吧

这组接口是写操作，需要登录态。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的 BDUSS", "你的 STOKEN");

await client.Forums.FollowAsync("你的吧名");
await client.Forums.UnfollowAsync("你的吧名");
```

按吧名调用最直接。适合把“用户点关注”或“同步关注状态”这类流程收敛到同一组 API。

## 读取当前账号或其他用户的关注列表

读取其他用户关注列表属于读取路径。读取当前账号自己的关注列表需要登录态，因为它依赖当前账户上下文。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的 BDUSS", "你的 STOKEN");

var selfFollowForums = await client.Forums.GetSelfFollowForumsAsync();
var selfFollowForumsV1 = await client.Forums.GetSelfFollowForumsV1Async();
var anotherUserForums = await client.Forums.GetFollowForumsAsync(123456789L);

Console.WriteLine(selfFollowForums.Count);
Console.WriteLine(selfFollowForumsV1.Count);
Console.WriteLine(anotherUserForums.Count);
```

`GetSelfFollowForumsAsync(...)` 和 `GetSelfFollowForumsV1Async(...)` 是并列支持的两组 self-follow 接口，不是“新接口替代旧接口”的清理关系。

- `GetSelfFollowForumsAsync(...)` 对应 aiotieba 的 `get_self_follow_forums`，返回 `SelfFollowForums`，每个贴吧对象里带有 `IsSigned` 状态，适合直接做签到视图或今日状态判断。
- `GetSelfFollowForumsV1Async(...)` 对应 aiotieba 的 `get_self_follow_forums_v1`，返回 `SelfFollowForumsV1`，保留更显式的分页信息，适合需要分页控制的场景。
- `GetFollowForumsAsync(...)` 用来读取其他用户的关注列表，适合访客可见的资料页或关系页读取。

## 做单吧签到、一键签到和成长任务签到

这三组都是写操作，需要登录态。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的 BDUSS", "你的 STOKEN");

await client.Forums.SignAsync("你的吧名");
await client.Forums.SignForumsAsync();
await client.Forums.SignGrowthAsync();
```

它们分别对应三种不同任务：

- `SignAsync(...)`，给单个贴吧签到。
- `SignForumsAsync()`，给当前账号关注的贴吧做一键签到。
- `SignGrowthAsync()`，完成成长任务签到。

如果你要做自动化签到，请先明确登录前提，再根据任务粒度决定是单吧执行还是整批执行。

## 做精确搜索

这组接口属于读取路径，访客可以直接使用。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var result = await client.Forums.SearchExactAsync(
    "你的吧名",
    query: "关键词",
    searchType: ForumSearchType.All,
    onlyThread: true);

Console.WriteLine(result.Count);
```

如果你手里已经有 `fid`，也可以改用 `SearchExactAsync(ulong fid, ...)`。这类搜索适合做吧内精确检索，而不是替代完整的主题读取流程。

## 读取统计、排行和图片辅助信息

这部分以读取为主。统计或成员相关场景如果依赖账号上下文，建议直接使用登录态客户端，避免后续扩展时重新调整调用方式。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient("你的 BDUSS", "你的 STOKEN");

var statistics = await client.Forums.GetStatisticsAsync("你的吧名");
var rankForums = await client.Forums.GetRankForumsAsync("你的吧名");
var image = await client.Forums.GetImageByHashAsync("图片哈希", ForumImageSize.Large);
var portrait = await client.Forums.GetPortraitAsync("目标用户 portrait", ForumImageSize.Small);

Console.WriteLine($"浏览统计点数 = {statistics.View.Count}");
Console.WriteLine($"排行条目数 = {rankForums.Count}");
Console.WriteLine($"图片尺寸 = {image.Width}x{image.Height}");
Console.WriteLine($"头像尺寸 = {portrait.Width}x{portrait.Height}");
```

常见的补充入口还有：

- `GetMemberUsersAsync(...)`，读取成员列表。
- `GetForumLevelAsync(...)`，读取吧等级相关信息。
- `GetRecomStatusAsync(...)`，读取推荐状态。
- `GetSquareForumsAsync(...)`，读取广场或聚合入口。
- `GetCidAsync(...)`，补充拿到某些后续流程会用到的标识。
- `GetLastReplyersAsync(...)`，读取首页主题时附带最后回复人，适合做列表浏览或轻量看板。

## 下一步

- [快速开始](/guide/getting-started)
- [API 参考](/reference/modules)
- [排障](/guide/troubleshooting)
