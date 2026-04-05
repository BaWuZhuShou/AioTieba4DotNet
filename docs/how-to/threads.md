# 帖子相关

这页按常见帖子任务组织。要查完整方法和签名，请同时对照 [API 参考](/reference/modules)。

本页示例里的 `FORUM_NAME_PLACEHOLDER`、`THREAD_ID_PLACEHOLDER` 等值统一遵循[示例占位符词汇表](/guide/getting-started#example-placeholder-glossary)。

## 开始前

- 主题列表、楼层和楼中楼读取属于访客可读路径。
- 回复、互动、隐私设置等写操作需要登录态，也就是 `BDUSS` 和 `STOKEN`。
- 加精、置顶、移动、恢复、删帖这类帖子管理操作通常还要求吧务权限。
- 下面所有示例都以 `client.Threads` 为入口。涉及吧务团队、权限、封禁或日志时，请切换到 `client.Admins`，不要把后台管理任务混进 Threads 流程。

## 读取主题列表

这是最常见的起点。你可以按吧名直接拿首页主题，也可以在已经知道 `fid` 时走 `ulong` 重载。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var threads = await client.Threads.GetThreadsAsync(
    "FORUM_NAME_PLACEHOLDER",
    pn: 1,
    rn: 30,
    sort: ThreadSortType.Reply,
    isGood: false);

Console.WriteLine(threads.Objs.Count);
```

如果你已经有 `fid`，可以改用 `GetThreadsAsync(ulong fid, ...)`。`sort` 和 `isGood` 适合把“首页浏览”和“只看精华”这两类读取需求收敛到同一个入口。

## 读取主题帖内楼层

进入具体帖子后，下一步通常是读取楼层。`withComments` 可以让你在读楼层时顺带拿到楼中楼摘要。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var posts = await client.Threads.GetPostsAsync(
    tid: long.Parse("THREAD_ID_PLACEHOLDER"),
    pn: 1,
    rn: 30,
    sort: PostSortType.Asc,
    onlyThreadAuthor: false,
    withComments: true,
    commentRn: 3);

Console.WriteLine(posts.Objs.Count);
```

这几个参数最常用。

- `onlyThreadAuthor: true`，只看楼主发言。
- `withComments: true`，顺带取回每层下的楼中楼摘要。
- `commentRn`，控制每层预取多少条楼中楼。

## 单独读取楼中楼

如果你已经定位到某一层，或者需要继续翻楼中楼分页，直接调用 `GetCommentsAsync(...)`。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var comments = await client.Threads.GetCommentsAsync(
    tid: long.Parse("THREAD_ID_PLACEHOLDER"),
    pid: long.Parse("POST_ID_PLACEHOLDER"),
    pn: 1);

Console.WriteLine(comments.Objs.Count);
```

当传入的 `pid` 本身就是楼中楼回复 id 时，把 `isComment` 设成 `true`。这样可以避免把“楼层 id”与“楼中楼回复 id”混用。

## 回复主题帖

这组接口是写操作，需要登录态。最直接的入口是 `AddPostAsync(...)`。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

await client.Threads.AddPostAsync(
    fname: "FORUM_NAME_PLACEHOLDER",
    tid: long.Parse("THREAD_ID_PLACEHOLDER"),
    content: "MESSAGE_TEXT_PLACEHOLDER");
```

需要显式传展示名时，再使用 `showName` 参数。这里仍然属于帖子回复路径，不需要切到 `Users` 或 `Admins`。

## 做互动操作

点赞、点踩和撤销都属于已登录写操作。默认对主题帖本身生效，操作楼层或楼中楼时再补充 `pid` 和 `isComment`。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

await client.Threads.AgreeAsync(tid: long.Parse("THREAD_ID_PLACEHOLDER"));
await client.Threads.DisagreeAsync(tid: long.Parse("THREAD_ID_PLACEHOLDER"));
await client.Threads.UnagreeAsync(tid: long.Parse("THREAD_ID_PLACEHOLDER"));
await client.Threads.UndisagreeAsync(tid: long.Parse("THREAD_ID_PLACEHOLDER"));
```

如果你是对某一层或楼中楼操作，可以这样补充目标信息。

```csharp
await client.Threads.AgreeAsync(
    tid: long.Parse("THREAD_ID_PLACEHOLDER"),
    pid: long.Parse("POST_ID_PLACEHOLDER"),
    isComment: false);
```

## 做吧务帖子操作

这一节覆盖的都是帖子管理路径。请先确认两件事：你已经登录，并且当前账号具备目标贴吧的吧务权限。`GoodAsync(...)`、`TopAsync(...)`、`MoveAsync(...)`、`RecommendAsync(...)`、`DelThreadAsync(...)`、`DelPostAsync(...)`、`SetThreadPrivacyAsync(...)` 都不适合在访客或普通账号上下文里盲目调用。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

await client.Threads.GoodAsync("FORUM_NAME_PLACEHOLDER", long.Parse("THREAD_ID_PLACEHOLDER"), cname: "THREAD_CATEGORY_NAME_PLACEHOLDER");
await client.Threads.TopAsync("FORUM_NAME_PLACEHOLDER", long.Parse("THREAD_ID_PLACEHOLDER"), isVip: false);
await client.Threads.MoveAsync("FORUM_NAME_PLACEHOLDER", long.Parse("THREAD_ID_PLACEHOLDER"), toTabId: 2);
await client.Threads.RecommendAsync("FORUM_NAME_PLACEHOLDER", long.Parse("THREAD_ID_PLACEHOLDER"));

await client.Threads.DelThreadAsync("FORUM_NAME_PLACEHOLDER", long.Parse("THREAD_ID_PLACEHOLDER"));
await client.Threads.DelPostAsync("FORUM_NAME_PLACEHOLDER", long.Parse("THREAD_ID_PLACEHOLDER"), long.Parse("POST_ID_PLACEHOLDER"));

await client.Threads.SetThreadPrivacyAsync(
    fname: "FORUM_NAME_PLACEHOLDER",
    tid: long.Parse("THREAD_ID_PLACEHOLDER"),
    pid: long.Parse("POST_ID_PLACEHOLDER"),
    isPrivate: true);
```

如果你需要批量删帖或批量删楼，可以继续使用同一模块下的 `DelThreadsAsync(...)` 和 `DelPostsAsync(...)`。这仍然是帖子管理范围，不是 `Admins` 的吧务团队或日志管理范围。

做管理动作时，最好同时准备回滚入口。常见配对如下。

- `GoodAsync(...)` ↔ `UngoodAsync(...)`
- `TopAsync(...)` ↔ `UntopAsync(...)`
- 删除或隐藏后的恢复 ↔ `RecoverAsync(...)`

例如，在执行加精或置顶后，你可以保留对应的撤销路径。

```csharp
await client.Threads.UngoodAsync("FORUM_NAME_PLACEHOLDER", long.Parse("THREAD_ID_PLACEHOLDER"));
await client.Threads.UntopAsync("FORUM_NAME_PLACEHOLDER", long.Parse("THREAD_ID_PLACEHOLDER"), isVip: false);
await client.Threads.RecoverAsync("FORUM_NAME_PLACEHOLDER", tid: long.Parse("THREAD_ID_PLACEHOLDER"));
```

`RecoverAsync(...)` 也支持按 `pid` 恢复单层内容，必要时还可以结合 `isHide` 区分恢复场景。做这类操作前，建议先把目标帖子、楼层和权限条件固定好，避免把回滚动作打到错误对象上。

## 读取回收站和分区映射

这部分通常用于吧务辅助流程。读取回收站、查看恢复信息、或者在移动帖子前先拿到分区映射，都可以留在 `client.Threads`。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var recovers = await client.Threads.GetRecoversAsync("FORUM_NAME_PLACEHOLDER");
var recoverInfo = await client.Threads.GetRecoverInfoAsync("FORUM_NAME_PLACEHOLDER", tid: long.Parse("THREAD_ID_PLACEHOLDER"));
var tabMap = await client.Threads.GetTabMapAsync("FORUM_NAME_PLACEHOLDER");

Console.WriteLine(recovers.Objs.Count);
Console.WriteLine(recoverInfo.Content);
Console.WriteLine(tabMap.Count);
```

这三个入口分别适合不同任务。

- `GetRecoversAsync(...)`，按页读取回收站内容。
- `GetRecoverInfoAsync(...)`，查看某个帖子或楼层的恢复详情。
- `GetTabMapAsync(...)`，先拿到分区名和分区 id 的映射，再决定 `MoveAsync(...)` 的 `toTabId`。

## 下一步

- 想查根客户端、六个模块和完整方法索引，继续看 [API 参考](/reference/modules)
- 想理解传输、生命周期和模块边界，继续看 [进阶](/guide/advanced)
- 想排查凭据、权限或服务端拒绝问题，继续看 [排障](/guide/troubleshooting)
