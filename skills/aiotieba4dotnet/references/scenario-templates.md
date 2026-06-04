# AioTieba4DotNet 场景代码模板

如果你在示例里看到 `BDUSS_PLACEHOLDER`、`FORUM_NAME_PLACEHOLDER`、`USER_ID_PLACEHOLDER` 这类名字，把它们换成你自己的真实值即可；需要对照时，看 [SKILL 里的示例占位符说明](../SKILL.md#example-placeholders)。

这份模板集专门给“直接生成代码”用。优先挑最接近用户目标的模板，再按需要微调参数或方法。

## 触发词到模板映射

| 用户常见说法 | 默认模板 |
| --- | --- |
| “怎么查吧”“怎么拿 fid”“怎么读某个吧的帖子” | 模板 1：访客读取吧信息和帖子列表 |
| “怎么签到”“怎么一键签到”“怎么关注吧/取关吧” | 模板 2：登录态签到或关注贴吧 |
| “怎么读楼层”“怎么读楼中楼”“怎么回复帖子”“怎么删帖/置顶/加精” | 模板 3：读取帖子楼层或回复帖子 |
| “怎么查资料”“怎么查主页”“怎么拉黑”“怎么改昵称/签名” | 模板 4：查询用户资料或主页 |
| “怎么收 @”“怎么收回复消息”“怎么发私信”“怎么解析 push” | 模板 5：读取消息或发送私信 |
| “怎么搜索帖子”“怎么按关键词查帖”“怎么精确搜索” | 模板 6：搜索吧内帖子或内容 |
| “怎么读我关注的吧”“怎么拿关注吧列表” | 模板 7：读取当前账号关注吧列表 |
| “怎么查关注列表/粉丝列表”“我关注了谁”“谁关注了我” | 模板 8：读取用户关注和粉丝列表 |
| “怎么读私信会话”“怎么拉聊天记录”“怎么标记私信已读” | 模板 9：读取私信会话或标记已读 |

## 歧义优先级

- “回复消息”默认理解为读取回复通知，走模板 5。
- “回复帖子”或“回帖”默认理解为发帖回复，走模板 3。
- “用户信息”默认先走模板 4；如果用户明确要 App/Web 形状，再在模板 4 基础上切到 `GetUserInfoAppAsync(...)` 或 `GetUserInfoWebAsync(...)`。
- “黑名单”未补充说明时，默认先按 `GetBlacklistAsync()` / `SetBlacklistAsync(...)` 这一组生成；只有明确提到 `_old`、旧版、分页增删时，再切到 `BlacklistOld` 这一组。
- “消息”默认归 `Messages`，除非用户明确说 websocket 初始化或同步状态，这时再改走 `Client`。
- “关注列表”如果宾语是“吧”，走模板 7；如果宾语是“用户 / 粉丝 / 我关注了谁 / 谁关注了我”，走模板 8。
- “搜索”默认走模板 6，不要回退到模板 1 的读吧模板。

## 模板 1：访客读取吧信息和帖子列表

适用问题：

- 怎么查某个吧的 `fid`
- 怎么读取贴吧详情
- 怎么读取某个吧的主题帖列表

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var forumName = "FORUM_NAME_PLACEHOLDER";

var fid = await client.Forums.GetFidAsync(forumName);
var detail = await client.Forums.GetDetailAsync(fid);
var threads = await client.Threads.GetThreadsAsync(
    forumName,
    pn: 1,
    rn: 30,
    sort: ThreadSortType.Reply,
    isGood: false);

Console.WriteLine($"fid = {fid}");
Console.WriteLine($"成员数 = {detail.MemberNum}");
Console.WriteLine($"帖子数 = {threads.Count}");
```

默认理由：这是最常见的只读入门路径，不需要登录态。

触发提示：适合“查吧”“查 fid”“读某吧帖子”“看吧详情”。

## 模板 2：登录态签到或关注贴吧

适用问题：

- 怎么签到
- 怎么一键签到
- 怎么关注或取关贴吧

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var forumName = "FORUM_NAME_PLACEHOLDER";

await client.Forums.SignAsync(forumName);
// await client.Forums.SignForumsAsync();
// await client.Forums.FollowAsync(forumName);
// await client.Forums.UnfollowAsync(forumName);

Console.WriteLine($"已完成对 {forumName} 的签到示例调用");
```

默认理由：这类操作需要登录态，优先用最短的认证客户端模板。

触发提示：适合“签到”“一键签到”“关注吧”“取关吧”。

动词改写规则：如果用户目标是“关注吧/取关吧”，把主调用改成 `FollowAsync(...)` / `UnfollowAsync(...)`，不要保留 `SignAsync(...)` 作为默认主调用。

## 模板 3：读取帖子楼层或回复帖子

适用问题：

- 怎么读取某个帖子的楼层
- 怎么读取楼中楼
- 怎么回复帖子

### 3A. 读取楼层

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var posts = await client.Threads.GetPostsAsync(
    tid: long.Parse("THREAD_ID_PLACEHOLDER"),
    pn: 1,
    rn: 30,
    sort: PostSortType.Asc,
    withComments: true,
    commentRn: 3);

Console.WriteLine($"楼层数 = {posts.Count}");
```

### 3B. 回复帖子

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

await client.Threads.AddPostAsync(
    fname: "FORUM_NAME_PLACEHOLDER",
    tid: long.Parse("THREAD_ID_PLACEHOLDER"),
    content: "MESSAGE_TEXT_PLACEHOLDER");

Console.WriteLine("回复已发送");
```

默认理由：同属 `Threads`，但一个是只读，一个是写操作，按是否需要登录态切换即可。

触发提示：适合“读楼层”“读楼中楼”“回帖”“删帖”“加精”“置顶”“移动帖子”。

动词改写规则：

- 命中“楼中楼”时，优先改成 `GetCommentsAsync(...)`，不要停留在 `GetPostsAsync(..., withComments: true)`。
- 命中“删帖 / 置顶 / 加精 / 移动 / 推荐”时，直接把主调用改成对应的 `DelThreadAsync(...)`、`TopAsync(...)`、`GoodAsync(...)`、`MoveAsync(...)`、`RecommendAsync(...)`。

## 模板 4：查询用户资料或主页

适用问题：

- 怎么查用户资料
- 怎么查主页内容
- 怎么查用户基础信息

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var profile = await client.Users.GetProfileAsync("USER_NAME_OR_PORTRAIT_PLACEHOLDER");
Console.WriteLine($"用户名 = {profile.ShowName}");

// 如果你要的是主页帖子和主页快照，而不是资料页信息：
// var homepage = await client.Users.GetHomepageAsync(int.Parse("USER_ID_PLACEHOLDER"), pn: 1);
// Console.WriteLine(homepage.Threads.Count);
```

默认理由：用户最常问“查资料”，而 `GetProfileAsync(...)` 与 `GetHomepageAsync(...)` 容易混淆，所以模板里直接给出切换提示。

触发提示：适合“查用户资料”“查主页”“查用户信息”“改昵称”“黑名单”。

动词改写规则：

- 命中“改昵称 / 改签名 / 改资料”时，切到登录态，并把主调用改成 `SetNicknameAsync(...)` 或 `SetProfileAsync(...)`。
- 命中“关注用户 / 取关用户”时，切到登录态，并把主调用改成 `FollowAsync(...)` / `UnfollowAsync(...)`。
- 命中“黑名单”时，先判断是权限型黑名单还是 `_old` 接口组，再分别改成 `SetBlacklistAsync(...)` 或 `AddBlacklistOldAsync(...)` / `RemoveBlacklistOldAsync(...)` / `GetBlacklistOldAsync(...)`。

## 模板 5：读取消息或发送私信

适用问题：

- 怎么读取 @ 消息
- 怎么读取回复消息
- 怎么发私信

### 5A. 读取 @ 和回复

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var ats = await client.Messages.GetAtsAsync();
var replies = await client.Messages.GetRepliesAsync();

Console.WriteLine($"@ 消息数 = {ats.Count}");
Console.WriteLine($"回复消息数 = {replies.Count}");
```

### 5B. 发送私信

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var messageId = await client.Messages.SendMessageAsync(
    long.Parse("USER_ID_PLACEHOLDER"),
    "MESSAGE_TEXT_PLACEHOLDER");

Console.WriteLine($"消息 ID = {messageId}");
```

默认理由：消息能力统一归属 `client.Messages`，这个模板能直接防止错用 `client.Client`。

触发提示：适合“收 @”“收回复消息”“发私信”“吧群消息”“解析 push”。

动词改写规则：

- 命中“吧群消息”时，把主调用改成 `SendChatroomMessageAsync(...)`，并提示需要 `chatroomId` 和 `forumId`。
- 命中“解析 push”时，把主调用改成 `ParsePushNotifications(byte[])`。
- 只有用户明确要求初始化连接、预热 websocket 或同步状态时，才额外补 `client.Client.InitWebSocketAsync()`。

## 模板 6：搜索吧内帖子或内容

适用问题：

- 怎么按关键词搜索帖子
- 怎么做精确搜索
- 怎么查某个吧里包含某个词的帖子

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var result = await client.Forums.SearchExactAsync(
    "FORUM_NAME_PLACEHOLDER",
    query: "SEARCH_QUERY_PLACEHOLDER",
    searchType: ForumSearchType.All,
    onlyThread: true);

Console.WriteLine($"命中数 = {result.Count}");
if (result.Count > 0)
{
    Console.WriteLine($"第一条标题 = {result[0].Title}");
}
```

默认理由：搜索是高频需求，但它和普通“读吧详情”不是一回事，单独做模板更稳。

触发提示：适合“搜索”“搜帖子”“关键词查帖”“精确搜索”。

## 模板 7：读取当前账号关注吧列表

适用问题：

- 怎么获取我关注的吧
- 怎么读取关注吧列表
- 怎么分页读取关注吧

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var selfFollowForums = await client.Forums.GetSelfFollowForumsAsync();
Console.WriteLine($"关注吧数量 = {selfFollowForums.Count}");

// 如果你需要保留更明确的分页形状，可以改用：
// var selfFollowForumsV1 = await client.Forums.GetSelfFollowForumsV1Async();
```

默认理由：这是登录态高频读取，不应误路由到“关注吧/取关吧”的写操作模板。

触发提示：适合“我的关注吧”“关注吧列表”“我关注了哪些吧”“分页读关注吧”。

## 模板 8：读取用户关注和粉丝列表

适用问题：

- 怎么查某个用户关注了谁
- 怎么查某个用户的粉丝
- 谁关注了我 / 我关注了谁

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var userId = long.Parse("USER_ID_PLACEHOLDER");

var follows = await client.Users.GetFollowsAsync(userId);
var fans = await client.Users.GetFansAsync(userId);

Console.WriteLine($"关注数 = {follows.Count}");
Console.WriteLine($"粉丝数 = {fans.Count}");
```

默认理由：这属于 `Users` 的关系读取，不应和“关注用户/取关用户”的写操作混在同一个模板里。

触发提示：适合“关注列表”“粉丝列表”“我关注了谁”“谁关注了我”。

动词改写规则：如果用户目标是“关注用户/取关用户”，仍走 `Users`，但把主调用改成 `FollowAsync(...)` / `UnfollowAsync(...)` 并切到登录态。

## 模板 9：读取私信会话或标记已读

适用问题：

- 怎么读取私信会话
- 怎么拉聊天记录
- 怎么标记某条私信为已读

```csharp
using AioTieba4DotNet;
using System.Linq;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var groups = await client.Messages.GetGroupMessagesAsync();
Console.WriteLine($"消息组数 = {groups.Count}");

var firstMessage = groups
    .SelectMany(group => group.Messages)
    .FirstOrDefault();

if (firstMessage is not null)
{
    await client.Messages.SetMessageReadAsync(firstMessage);
    Console.WriteLine("已把首条私信标记为已读");
}
```

默认理由：私信会话与已读处理是高频消息场景，不该每次都从发私信模板临时改写。

触发提示：适合“私信会话”“聊天记录”“私信已读”“标记已读”。

动词改写规则：如果用户只想读取会话，不要附带 `SetMessageReadAsync(...)`；如果用户重点是“标记已读”，保留 `GetGroupMessagesAsync(...)` 只作为找到目标消息的最小前置步骤。

## 不作为默认模板的场景

- `Admins`：因为通常需要更明确的权限上下文，但用户明确问到封禁、申诉、Bawu、权限时，仍然应该切到 `Admins` 生成代码。
- `Client` 生命周期：只有明确要求预热 WebSocket、初始化 ZId 或同步状态时，才生成 `InitWebSocketAsync()`、`InitZIdAsync()`、`SyncAsync()` 代码。
- DI / factory：只有用户明确说要宿主集成、多账号或按任务建客户端时，才生成对应样板。

## 模板使用规则

1. 用户明确说“给我代码/示例/模板/snippet”时，先从当前模板集中选最近的一个；现阶段默认模板池包含模板 1 到模板 9。
2. 如果需求落在模板边缘，只替换方法和少量参数，不要整段重写。
3. 默认给单文件、最小可运行示例。
4. 除非用户明确要求，否则不要顺手加 DI、日志、异常包装、完整项目结构。
5. 遇到“回复”这种歧义词时，先看宾语：帖子/楼层 -> 模板 3；消息/@/私信 -> 模板 5。
