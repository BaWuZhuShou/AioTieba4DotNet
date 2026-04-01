# AioTieba4DotNet v2 -> v3 迁移指南

这份文档面向从 v2 升级到 v3 的调用方。重点不是重复所有 API 参考，而是把升级时最容易踩空的模块边界、公开命名规范化和调用替换路径集中讲清楚。

## 先看结论

- v3 只支持 `net10.0`
- 根客户端故事没变，仍然是 `TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)`、`ITiebaClientFactory`
- 公开模块稳定为六个边界: `Forums`、`Threads`、`Users`、`Admins`、`Messages`、`Client`
- 消息业务统一走 `client.Messages`
- 吧务、封禁和 bawu 写操作统一走 `client.Admins`
- `client.Client` 只保留生命周期与初始化职责
- 公开 C# surface 现在使用规范化名字，旧名字只保留在本文的 mapping section 里做搜索和迁移对照

## 1. 支持矩阵变化

### v2

- 同时承载旧版兼容叙事和过渡期文档
- 文档里经常把 v1、v2、v3 的历史层叠在一起

### v3

- **只支持 `net10.0`**
- 仓库语言版本策略固定为 **C# 14**
- 不再为 `net8.0` 或 `net9.0` 保留兼容策略和文档承诺

### 迁移动作

1. 先把消费方项目升级到 .NET 10 SDK 和目标框架
2. 清理任何为了旧目标框架保留的条件编译和兼容分支
3. 再处理模块边界和公开命名替换

## 2. 根入口不需要重学

以下入口在 v3 继续成立:

- `TiebaClient`
- `ITiebaClient`
- `DependencyInjection.AddAioTiebaClient(...)`
- `ITiebaClientFactory`

也就是说，升级时你通常不用把“如何创建客户端”整套推翻，只需要把“创建出来之后访问哪个模块”和“调用哪个规范化名字”改对。

## 3. 公开模块边界

旧文档长期把不少能力压在 `Users` 和 `Client` 两条线上，调用方很难从入口名判断真正的业务边界。v3 把主路径固定为下面这张表。

| v3 模块 | 主职责 |
| --- | --- |
| `Forums` | 贴吧发现、关注、签到、搜索、统计 |
| `Threads` | 读帖、回帖、互动、主题管理 |
| `Users` | 资料、主页、社交关系、黑名单、资料修改 |
| `Admins` | 吧务团队、权限、日志、封禁、申诉 |
| `Messages` | @、回复、私信、吧群消息、push 解析 |
| `Client` | WebSocket、ZId、ClientId / SampleId 初始化 |

这三个 ownership anchor 在迁移时最重要:

- `Messages` owns inbox and message work
- `Admins` owns admin, bawu, and block-management work
- `Client` stays lifecycle-only

## 4. 公开命名规范化 mapping

下面的 mapping 以模块和 family 分组。上游 family identity 不变，变化的是公开 C# surface 名字和部分模块归属。

### 4.1 Forums family

#### Follow and unfollow alias cleanup

| Old public name | Current public name | Category | Upstream family anchor | Caller action |
| --- | --- | --- | --- | --- |
| `IForumModule.LikeAsync(string fname, ...)` | `IForumModule.FollowAsync(string fname, ...)` | `true-alias` | `aiotieba.api.follow_forum` | Replace the old alias with `FollowAsync(...)`. |
| `IForumModule.UnlikeAsync(string fname, ...)` | `IForumModule.UnfollowAsync(string fname, ...)` | `true-alias` | `aiotieba.api.unfollow_forum` | Replace the old alias with `UnfollowAsync(...)`. |

#### Self-follow parallel APIs

这两组 API 现在是并列支持的 peer families，不是一个 family 的过渡别名。

| Old public name | Current public name | Category | Upstream family anchor | Caller action |
| --- | --- | --- | --- | --- |
| `IForumModule.GetSelfFollowForumsPagedAsync(int pn = 1, int rn = 20, ...)` | `IForumModule.GetSelfFollowForumsV1Async(int pn = 1, int rn = 20, ...)` | `peer-family` | `aiotieba.api.get_self_follow_forums_v1` | Keep using the retained upstream version-family peer, but switch to `GetSelfFollowForumsV1Async(...)`. |
| `SelfFollowForumsPaged` | `SelfFollowForumsV1` | `shape-variant` | `aiotieba.api.get_self_follow_forums_v1` | Update the public return type name. |
| `SelfFollowForumV1Item` | `SelfFollowForumV1` | `shape-variant` | `aiotieba.api.get_self_follow_forums_v1` | Update the public item type name to the upstream `SelfFollowForumV1` anchor. |

### 4.2 Users family

#### Blacklist parallel families

`get_blacklist` and `get_blacklist_old` remain distinct supported peer families. One is permissions-oriented, the other is muted-user oriented.

| Old public name | Current public name | Category | Upstream family anchor | Caller action |
| --- | --- | --- | --- | --- |
| `IUserModule.GetBlacklistPermissionsAsync(...)` | `IUserModule.GetBlacklistAsync(...)` | `peer-family` | `aiotieba.api.get_blacklist` | Switch to the shared `Blacklist` root for the `get_blacklist` family. |
| `IUserModule.SetBlacklistPermissionsAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | `IUserModule.SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | `peer-family` | `aiotieba.api.set_blacklist` | Switch to the shared `Blacklist` root for the `set_blacklist` family. |
| `IUserModule.GetBlacklistMutedAsync(int pn = 1, int rn = 20, ...)` | `IUserModule.GetBlacklistOldAsync(int pn = 1, int rn = 20, ...)` | `peer-family` | `aiotieba.api.get_blacklist_old` | Keep using the retained `_old` sibling family, but switch to the `Old` name. |
| `IUserModule.AddBlacklistMutedAsync(long userId, ...)` | `IUserModule.AddBlacklistOldAsync(long userId, ...)` | `peer-family` | `aiotieba.api.add_blacklist_old` | Switch to the `Old` add-family name. |
| `IUserModule.RemoveBlacklistMutedAsync(long userId, ...)` | `IUserModule.RemoveBlacklistOldAsync(long userId, ...)` | `peer-family` | `aiotieba.api.del_blacklist_old` | Switch to the `Old` remove-family name. |
| `BlacklistPermissions` | `BlacklistUsers` | `shape-variant` | `aiotieba.api.get_blacklist` | Update the list DTO type name. |
| `BlacklistPermission` | `BlacklistUser` | `shape-variant` | `aiotieba.api.get_blacklist` | Update the item DTO type name. |
| `BlacklistMutedUsers` | `BlacklistOldUsers` | `shape-variant` | `aiotieba.api.get_blacklist_old` | Update the `_old` list DTO type name. |
| `BlacklistMutedUser` | `BlacklistOldUser` | `shape-variant` | `aiotieba.api.get_blacklist_old` | Update the `_old` item DTO type name. |

#### Basic-info and nickname families

`GetUserInfoAppAsync(...)` and `GetUserInfoWebAsync(...)` remain parallel supported APIs. `SetNicknameAsync(...)` also remains a distinct write family beside `SetProfileAsync(...)`.

| Old public name | Current public name | Category | Upstream family anchor | Caller action |
| --- | --- | --- | --- | --- |
| `IUserModule.GetBasicInfoAppAsync(int userId, ...)` | `IUserModule.GetUserInfoAppAsync(int userId, ...)` | `peer-family` | `aiotieba.api.get_uinfo_getuserinfo_app` | Switch to the App `user_info` peer-family name. |
| `UserInfoApp` | `UserInfoGuInfoApp` | `shape-variant` | `aiotieba.api.get_uinfo_getuserinfo_app` | Update the App DTO type name to the upstream `UserInfo_guinfo_app` anchor. |
| `UserInfoWeb` | `UserInfoGuInfoWeb` | `shape-variant` | `aiotieba.api.get_uinfo_getUserInfo_web` | Update the Web DTO type name to the upstream `UserInfo_guinfo_web` anchor. |
| `IUserModule.SetNicknameLegacyAsync(string nickName, ...)` | `IUserModule.SetNicknameAsync(string nickName, ...)` | `peer-family` | `aiotieba.api.set_nickname_old` | Switch to `SetNicknameAsync(...)`. |

#### User-content DTO family

| Old public name | Current public name | Category | Upstream family anchor | Caller action |
| --- | --- | --- | --- | --- |
| `UserPostss` | `UserPostGroups` | `shape-variant` | `aiotieba.api.get_user_contents.get_posts` | Update the public DTO name in code and docs. |

### 4.3 Messages ownership moves

消息读取现在固定在 `Messages`。这不是别名替换，而是 old-home bridge removal。

| Old public name | Current public name | Category | Upstream family anchor | Caller action |
| --- | --- | --- | --- | --- |
| `IUserModule.GetAtsAsync(int pn = 1, ...)` | `IMessagesModule.GetAtsAsync(int pn = 1, ...)` | `relocation-bridge` | `aiotieba.api.get_ats` | Move the call from `client.Users` to `client.Messages`. |
| `IUserModule.GetRepliesAsync(int pn = 1, ...)` | `IMessagesModule.GetRepliesAsync(int pn = 1, ...)` | `relocation-bridge` | `aiotieba.api.get_replys` | Move the call from `client.Users` to `client.Messages`. |

旧代码可能长这样:

```csharp
var ats = await client.Users.GetAtsAsync();
var replies = await client.Users.GetRepliesAsync();
```

现在应该改成:

```csharp
var ats = await client.Messages.GetAtsAsync();
var replies = await client.Messages.GetRepliesAsync();
```

私信、吧群消息和 `push_notify` 解析也统一走 `client.Messages`。

### 4.4 Admins ownership and Bawu normalization

admin、block、bawu 的主路径现在固定在 `Admins`。这里既有 old-home bridge removal，也有 canonical public rename。

| Old public name | Current public name | Category | Upstream family anchor | Caller action |
| --- | --- | --- | --- | --- |
| `IUserModule.BlockAsync(ulong fid, string portrait, int day = 1, string reason = "", ...)` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `relocation-bridge` | `aiotieba.api.block` | Move the call to `client.Admins`; update the call shape to the surviving `fname` overload. |
| `IUserModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `relocation-bridge` | `aiotieba.api.block` | Move the call from `client.Users` to `client.Admins`. |
| `IForumModule.DelBaWuAsync(string fname, string portrait, string baWuType, ...)` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)` | `relocation-bridge` | `aiotieba.api.del_bawu` | Move the call to `client.Admins` and switch to `DelBawuAsync(...)`. |
| `IAdminModule.AddBaWuAsync(string fname, string userName, BawuType bawuType, ...)` | `IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType, ...)` | `canonical-operation` | `aiotieba.api.add_bawu` | Replace the old mixed-case method name with `AddBawuAsync(...)`. |
| `IAdminModule.DelBaWuAsync(string fname, string portrait, BawuType bawuType, ...)` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)` | `canonical-operation` | `aiotieba.api.del_bawu` | Replace the old mixed-case spelling with `DelBawuAsync(...)`. |

v3 的 canonical 路径是:

```csharp
var info = await client.Admins.GetBawuInfoAsync("csharp");
await client.Admins.BlockAsync("csharp", "目标 portrait", day: 1, reason: "示例");
await client.Admins.AddBawuAsync("csharp", "目标用户名", BawuType.Manager);
await client.Admins.DelBawuAsync("csharp", "目标 portrait", BawuType.Manager);
```

### 4.5 `Client` 不再承担消息职责

保留下来的只有:

- `InitWebSocketAsync()`
- `InitZIdAsync()`
- `SyncAsync()`

如果你在 v2 的理解里把 `Client` 当成“连接相关能力 + 消息能力”的总入口，v3 需要把消息部分迁走。

## 5. 配置和异常模型

### 保持不变的地方

- 还是用 `TiebaOptions`
- 还是用 `TiebaTransportMode`
- 还是支持直接构造、DI 和 factory

### 更需要注意的地方

- 缺少本地必需凭据时，明确抛出 `TiebaAuthenticationException`
- 配置非法时，明确抛出 `TiebaConfigurationException`
- 服务端业务拒绝时，抛出 `TieBaServerException`

这意味着你在迁移异常处理时，最好按“本地配置 / 本地鉴权 / 服务端业务”三类来分，而不是继续把它们混在一起。

## 6. 推荐升级顺序

1. 升级目标框架到 `net10.0`
2. 保持原有客户端创建方式不变
3. 先修正模块归属, 把消息调用迁到 `client.Messages`，把 admin 和 bawu 调用迁到 `client.Admins`
4. 再按本文的 module-grouped mapping 更新公开方法名和 DTO 名
5. 把依赖 `client.Client` 的代码压缩回生命周期初始化用途
6. 复查 README、how-to 和你自己的调用封装里是否还有旧模块叙事或旧 public names

## 7. 升级后的最小验证

至少做下面这些检查:

1. `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore --nologo`
2. `pwsh ./scripts/verify-local.ps1 -ValidateOnly`
3. 打开新的导航链路，确认 README -> Getting Started -> How-to -> Advanced / Troubleshooting 可以走通
4. 搜索你自己的代码，确认 removed alias、removed bridge 和旧 DTO 名都已经替换成本文列出的 current public names

## 8. 相关阅读

- [README](../README.md)
- [Getting Started](./getting-started.md)
- [Modules Reference](./modules.md)
- [Release Notes v3](./release-notes-v3.md)
- [Parity v3](./parity-v3.md)
