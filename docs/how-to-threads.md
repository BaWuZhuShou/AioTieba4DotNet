# How-to: Threads

这页覆盖“帖子和回帖”相关的常见工作流，包括读取、回复、互动和吧务操作。

## 读取主题帖列表

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var threads = await client.Threads.GetThreadsAsync(
    "csharp",
    pn: 1,
    rn: 30,
    sort: ThreadSortType.Reply,
    isGood: false);

Console.WriteLine(threads.Objs.Count);
```

如果你已经有 `fid`，也可以用 `GetThreadsAsync(ulong fid, ...)`。

## 读取主题帖内楼层

```csharp
using AioTieba4DotNet.Models;

using var client = new TiebaClient();

var posts = await client.Threads.GetPostsAsync(
    tid: 1234567890,
    pn: 1,
    rn: 30,
    sort: PostSortType.Asc,
    withComments: true,
    commentRn: 3);

Console.WriteLine(posts.Objs.Count);
```

这里有两个常用开关:

- `onlyThreadAuthor: true`，只看楼主
- `withComments: true`，顺带拿到楼中楼

## 单独读取楼中楼

```csharp
using var client = new TiebaClient();

var comments = await client.Threads.GetCommentsAsync(
    tid: 1234567890,
    pid: 9876543210,
    pn: 1);

Console.WriteLine(comments.Objs.Count);
```

当传入的 `pid` 本身就是楼中楼回复 id 时，把 `isComment` 设成 `true`。

## 回复主题帖

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Threads.AddPostAsync(
    fname: "csharp",
    tid: 1234567890,
    content: "这是一条来自 v3 文档示例的回复");
```

需要显式传展示名时，再用 `showName` 参数。

## 点赞、点踩、取消操作

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Threads.AgreeAsync(tid: 1234567890);
await client.Threads.DisagreeAsync(tid: 1234567890);
await client.Threads.UnagreeAsync(tid: 1234567890);
await client.Threads.UndisagreeAsync(tid: 1234567890);
```

需要对回复或楼中楼操作时，传 `pid`，并按需要设置 `isComment`。

## 删除、加精、置顶、移动、推荐

这些都是已登录且通常要求吧务权限的操作。

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Threads.GoodAsync("csharp", 1234567890, cname: "精华区分类名");
await client.Threads.TopAsync("csharp", 1234567890, isVip: false);
await client.Threads.MoveAsync("csharp", 1234567890, toTabId: 2);
await client.Threads.RecommendAsync("csharp", 1234567890);
```

配套回滚入口也都在同一模块里，例如:

- `UngoodAsync(...)`
- `UntopAsync(...)`
- `RecoverAsync(...)`

## 读取回收站和分区映射

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

var recovers = await client.Threads.GetRecoversAsync("csharp");
var recoverInfo = await client.Threads.GetRecoverInfoAsync("csharp", tid: 1234567890);
var tabMap = await client.Threads.GetTabMapAsync("csharp");

Console.WriteLine(recovers.Objs.Count);
Console.WriteLine(recoverInfo.Content);
Console.WriteLine(tabMap.Dictionary.Count);
```

`GetTabMapAsync(...)` 适合在不依赖首页帖子列表时单独取分区名到分区 id 的映射。

## 设置回复隐私

```csharp
using var client = new TiebaClient("你的BDUSS", "你的STOKEN");

await client.Threads.SetThreadPrivacyAsync(
    fname: "csharp",
    tid: 1234567890,
    pid: 9876543210,
    isPrivate: true);
```

## 相关阅读

- [How-to: Forums](./how-to-forums.md)
- [How-to: Users](./how-to-users.md)
- [Modules Reference](./modules.md)
- [Troubleshooting](./troubleshooting.md)
