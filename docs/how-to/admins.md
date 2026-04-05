# 吧务相关

这页按常见吧务任务组织。要查完整方法和签名，请同时对照 [API 参考](/reference/modules)。

本页示例里的 `FORUM_NAME_PLACEHOLDER`、`USER_ID_PLACEHOLDER` 等值统一遵循[示例占位符词汇表](/guide/getting-started#example-placeholder-glossary)。

## 开始前

- 下面所有示例都以 `client.Admins` 为入口。
- 这类操作基本都需要登录态，也就是 `BDUSS` 和 `STOKEN`。
- 大多数写操作还要求当前账号具备目标贴吧的吧务权限。
- 贴吧帖子本身的加精、置顶、移动、删帖、恢复等管理动作仍然在 `client.Threads`；这里聚焦吧务团队、权限、封禁、日志和申诉处理。

## 读取吧务团队和权限信息

这组接口适合先确认当前贴吧的吧务成员和权限分配，再决定后续写操作。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var bawuInfo = await client.Admins.GetBawuInfoAsync("FORUM_NAME_PLACEHOLDER");
var bawuPerm = await client.Admins.GetBawuPermAsync("FORUM_NAME_PLACEHOLDER", "PORTRAIT_PLACEHOLDER");

Console.WriteLine(bawuInfo.Managers.Count);
Console.WriteLine(bawuPerm.Value);
```

如果你要做权限变更、封禁或解封，建议先走这一步，避免在权限条件不满足时盲目执行写操作。

## 增加、删除吧务并设置权限

这组接口属于高权限写操作。执行前最好先确认目标用户和目标贴吧都没有弄错。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

await client.Admins.AddBawuAsync("FORUM_NAME_PLACEHOLDER", "USER_NAME_PLACEHOLDER", BawuType.Manager);
await client.Admins.SetBawuPermAsync("FORUM_NAME_PLACEHOLDER", "PORTRAIT_PLACEHOLDER", BawuPermType.All);
await client.Admins.DelBawuAsync("FORUM_NAME_PLACEHOLDER", "PORTRAIT_PLACEHOLDER", BawuType.Manager);
```

适合吧务团队维护或权限同步场景。`AddBawuAsync(...)` / `DelBawuAsync(...)` 管的是角色本身，`SetBawuPermAsync(...)` 管的是已分配吧务的权限集合。

## 管理吧务黑名单

这组接口适合做吧务黑名单的读取和增删。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var blacklist = await client.Admins.GetBawuBlacklistAsync("FORUM_NAME_PLACEHOLDER");
await client.Admins.AddBawuBlacklistAsync("FORUM_NAME_PLACEHOLDER", long.Parse("USER_ID_PLACEHOLDER"));
await client.Admins.DelBawuBlacklistAsync("FORUM_NAME_PLACEHOLDER", long.Parse("USER_ID_PLACEHOLDER"));

Console.WriteLine(blacklist.Objs.Count);
```

如果你的目标只是普通用户黑名单，请回到 `client.Users`；这里的黑名单属于吧务管理范围。

## 查询删帖日志和用户管理日志

这组接口适合做后台审计、排查误操作，或者回看最近的吧务动作。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Models;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var postLogs = await client.Admins.GetBawuPostLogsAsync(
    "FORUM_NAME_PLACEHOLDER",
    new BawuPostLogQueryOptions
    {
        PageNumber = 1
    });

var userLogs = await client.Admins.GetBawuUserLogsAsync(
    "FORUM_NAME_PLACEHOLDER",
    new BawuUserLogQueryOptions
    {
        PageNumber = 1
    });

Console.WriteLine(postLogs.Objs.Count);
Console.WriteLine(userLogs.Objs.Count);
```

如果你已经知道筛选范围，可以再补充 `SearchValue`、`StartTime`、`EndTime` 或其他查询字段，把日志读取收窄到更具体的管理动作。

## 查询封禁列表、封禁和解封用户

这组接口适合做封禁维护、人工复核和解封回滚。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var blocks = await client.Admins.GetBlocksAsync("FORUM_NAME_PLACEHOLDER");
await client.Admins.BlockAsync("FORUM_NAME_PLACEHOLDER", "PORTRAIT_PLACEHOLDER", day: 1, reason: "REASON_PLACEHOLDER");
await client.Admins.UnblockAsync("FORUM_NAME_PLACEHOLDER", long.Parse("USER_ID_PLACEHOLDER"));

Console.WriteLine(blocks.Objs.Count);
```

执行 `BlockAsync(...)` 前，最好先把理由和时长固定好。解封前也建议先确认目标用户和封禁记录对应的是同一个对象。

## 处理解封申诉

这组接口适合做申诉审核流程。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER");

var appeals = await client.Admins.GetUnblockAppealsAsync("FORUM_NAME_PLACEHOLDER");
await client.Admins.HandleUnblockAppealsAsync("FORUM_NAME_PLACEHOLDER", new long[] { long.Parse("APPEAL_ID_PLACEHOLDER") }, refuse: false);

Console.WriteLine(appeals.Objs.Count);
```

`refuse: false` 表示按通过处理；如果你要拒绝申诉，再显式传 `refuse: true`。

## 下一步

- [快速开始](/guide/getting-started)
- [API 参考](/reference/modules)
- [排障](/guide/troubleshooting)
