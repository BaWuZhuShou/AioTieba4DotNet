# AioTieba4DotNet v2 -> v3 迁移指南

这份文档面向从 v2 升级到 v3 的调用方。重点不是重复完整 API，而是告诉你升级时最容易改错的地方：目标框架、模块入口、公开名称和异常处理。

这页示例会直接写成“你的吧名”“目标用户 portrait”“违规理由”这类示意值，阅读时按自己的实际参数替换即可。

## 先看结论

- v3 只支持 `net10.0`
- 客户端创建方式没有重学成本，还是 `TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)`、`ITiebaClientFactory`
- 公开模块稳定为六个边界：`Forums`、`Threads`、`Users`、`Admins`、`Messages`、`Client`
- 消息业务统一走 `client.Messages`
- 吧务、封禁和 bawu 操作统一走 `client.Admins`
- `client.Client` 只保留生命周期与初始化职责
- 公开名称做了统一整理，旧名称只在这份迁移文档里保留作对照

## 1. 先升级运行环境

### v3 的要求

- 目标框架固定为 `net10.0`
- 仓库语言版本固定为 `C# 14`
- 不再为 `net8.0` 或 `net9.0` 保留兼容承诺

### 你要做的事

1. 先把消费方项目升级到 .NET 10 SDK 和目标框架。
2. 清理为了旧目标框架保留的条件编译和兼容分支。
3. 再处理模块入口和公开名称替换。

## 2. 根入口基本不变

下面这些入口在 v3 继续成立：

- `TiebaClient`
- `ITiebaClient`
- `DependencyInjection.AddAioTiebaClient(...)`
- `ITiebaClientFactory`

也就是说，升级时通常不用重写“怎么创建客户端”，主要是把“创建后去哪个模块调用”和“调用哪个公开名称”改对。

## 3. 先把模块入口改对

v3 把公开模块边界固定成下面这张表。

| v3 模块 | 主职责 |
| --- | --- |
| `Forums` | 贴吧发现、关注、签到、搜索、统计 |
| `Threads` | 读帖、回帖、互动、主题管理 |
| `Users` | 资料、主页、社交关系、黑名单、资料修改 |
| `Admins` | 吧务团队、权限、日志、封禁、申诉 |
| `Messages` | `@`、回复、私信、吧群消息、推送通知解析 |
| `Client` | WebSocket、ZId、ClientId / SampleId 初始化 |

迁移时最重要的是这三条：

- 消息相关调用都迁到 `client.Messages`
- 吧务、封禁、bawu 相关调用都迁到 `client.Admins`
- `client.Client` 只保留连接和生命周期辅助能力

## 4. 常见公开名称替换

下面只列最容易搜到旧名、也最容易改错的替换项。

### Forums

#### 关注 / 取关名称统一

| 旧名称 | 新名称 | 你要怎么改 |
| --- | --- | --- |
| `IForumModule.LikeAsync(string fname, ...)` | `IForumModule.FollowAsync(string fname, ...)` | 直接改成 `FollowAsync(...)`。 |
| `IForumModule.UnlikeAsync(string fname, ...)` | `IForumModule.UnfollowAsync(string fname, ...)` | 直接改成 `UnfollowAsync(...)`。 |

#### 当前账号关注吧列表的两组接口并列保留

这两组接口都还支持，不是“旧接口暂时过渡到新接口”的关系。

| 旧名称 | 新名称 | 你要怎么改 |
| --- | --- | --- |
| `IForumModule.GetSelfFollowForumsPagedAsync(int pn = 1, int rn = 20, ...)` | `IForumModule.GetSelfFollowForumsV1Async(int pn = 1, int rn = 20, ...)` | 如果你要用 V1 这组接口，改成 `GetSelfFollowForumsV1Async(...)`。 |
| `SelfFollowForumsPaged` | `SelfFollowForumsV1` | 更新返回类型名称。 |
| `SelfFollowForumV1Item` | `SelfFollowForumV1` | 更新条目类型名称。 |

### Users

#### 黑名单有两组接口，继续并列存在

`get_blacklist` 和 `get_blacklist_old` 不是同一个接口的前后版本，而是两组不同用途的公开接口。

| 旧名称 | 新名称 | 你要怎么改 |
| --- | --- | --- |
| `IUserModule.GetBlacklistPermissionsAsync(...)` | `IUserModule.GetBlacklistAsync(...)` | 需要权限型黑名单时改成 `GetBlacklistAsync(...)`。 |
| `IUserModule.SetBlacklistPermissionsAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | `IUserModule.SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | 设置权限型黑名单时改成 `SetBlacklistAsync(...)`。 |
| `IUserModule.GetBlacklistMutedAsync(int pn = 1, int rn = 20, ...)` | `IUserModule.GetBlacklistOldAsync(int pn = 1, int rn = 20, ...)` | 需要 `_old` 那组接口时改成 `GetBlacklistOldAsync(...)`。 |
| `IUserModule.AddBlacklistMutedAsync(long userId, ...)` | `IUserModule.AddBlacklistOldAsync(long userId, ...)` | 加入 `_old` 黑名单时改成 `AddBlacklistOldAsync(...)`。 |
| `IUserModule.RemoveBlacklistMutedAsync(long userId, ...)` | `IUserModule.RemoveBlacklistOldAsync(long userId, ...)` | 移出 `_old` 黑名单时改成 `RemoveBlacklistOldAsync(...)`。 |
| `BlacklistPermissions` | `BlacklistUsers` | 更新列表 DTO 名称。 |
| `BlacklistPermission` | `BlacklistUser` | 更新条目 DTO 名称。 |
| `BlacklistMutedUsers` | `BlacklistOldUsers` | 更新 `_old` 列表 DTO 名称。 |
| `BlacklistMutedUser` | `BlacklistOldUser` | 更新 `_old` 条目 DTO 名称。 |

#### 用户信息与昵称相关接口

| 旧名称 | 新名称 | 你要怎么改 |
| --- | --- | --- |
| `IUserModule.GetBasicInfoAppAsync(int userId, ...)` | `IUserModule.GetUserInfoAppAsync(int userId, ...)` | 读取 App `user_info` 时改成 `GetUserInfoAppAsync(...)`。 |
| `IUserModule.GetUserInfoWebAsync(int userId, ...) : UserInfoGuInfoWeb` | `IUserModule.GetUserInfoWebAsync(int userId, ...) : UserInfo` | Web `user_info` 读取结果也统一收敛到共享 `UserInfo`。 |
| `IUserModule.GetPanelInfoAsync(string nameOrPortrait, ...) : UserInfoPanel` | `IUserModule.GetPanelInfoAsync(string nameOrPortrait, ...) : UserInfo` | panel 读取结果改为共享 `UserInfo`。 |
| `IUserModule.GetUserInfoJsonAsync(string username, ...) : UserInfoJson` | `IUserModule.GetUserInfoJsonAsync(string username, ...) : UserInfo` | JSON 用户信息读取结果改为共享 `UserInfo`。 |
| `IUserModule.GetUserByTiebaUidAsync(long tiebaUid, ...) : UserInfoTUid` | `IUserModule.GetUserByTiebaUidAsync(long tiebaUid, ...) : UserInfo` | Tieba UID 反查用户结果改为共享 `UserInfo`。 |
| `UserInfoGuInfoApp` | `UserInfo` | App `user_info` 读取结果统一收敛到共享 `UserInfo`。 |
| `UserInfoGuInfoWeb` | `UserInfo` | Web `user_info` 读取结果统一收敛到共享 `UserInfo`。 |
| `UserInfoJson` | `UserInfo` | JSON 用户信息读取结果统一收敛到共享 `UserInfo`。 |
| `UserInfoPanel` | `UserInfo` | panel 用户信息读取结果统一收敛到共享 `UserInfo`。 |
| `UserInfoTUid` | `UserInfo` | Tieba UID 用户信息读取结果统一收敛到共享 `UserInfo`。 |
| `UserInfoApp` | `UserInfo` | App `user_info` 读取结果统一收敛到共享 `UserInfo`。 |
| `UserInfoWeb` | `UserInfo` | Web `user_info` 读取结果统一收敛到共享 `UserInfo`。 |
| `IUserModule.SetNicknameLegacyAsync(string nickName, ...)` | `IUserModule.SetNicknameAsync(string nickName, ...)` | 改成 `SetNicknameAsync(...)`。 |
| `UserPostss` | `UserPostGroups` | 更新用户回复分组 DTO 名称。 |

这里还有一个行为层面的 breaking change：如果你之前通过 `result.GetType()`、`is UserInfoPanel`、`is UserInfoJson` 之类的方式区分数据来自哪个入口，这种 endpoint-specific 运行时类型标签现在已经删除，改为统一返回 `UserInfo`。调用方如果还需要区分来源，需要改成根据调用的方法本身来区分，而不是依赖返回 DTO 的运行时类型。

另外，少量历史上保留下来的公开字段现在也统一改成了公开属性：`Thread.TabId`、`VirtualImagePf.Enabled`、`VirtualImagePf.State` 都从 public field 改成了可读写 property。如果你的代码依赖字段反射（例如 `GetField(...)`）或明确要求字段元数据，需要切换到属性访问或 `GetProperty(...)`。

### Messages

消息读取现在固定在 `Messages` 模块，不再挂在 `Users` 上。

| 旧名称 | 新名称 | 你要怎么改 |
| --- | --- | --- |
| `IUserModule.GetAtsAsync(int pn = 1, ...)` | `IMessagesModule.GetAtsAsync(int pn = 1, ...)` | 把调用从 `client.Users` 移到 `client.Messages`。 |
| `IUserModule.GetRepliesAsync(int pn = 1, ...)` | `IMessagesModule.GetRepliesAsync(int pn = 1, ...)` | 把调用从 `client.Users` 移到 `client.Messages`。 |

旧代码可能是：

```csharp
var ats = await client.Users.GetAtsAsync();
var replies = await client.Users.GetRepliesAsync();
```

现在应改成：

```csharp
var ats = await client.Messages.GetAtsAsync();
var replies = await client.Messages.GetRepliesAsync();
```

私信、吧群消息和 `push_notify` 解析也统一走 `client.Messages`。

### Admins

吧务、封禁和 bawu 相关调用现在固定在 `Admins`。

| 旧名称 | 新名称 | 你要怎么改 |
| --- | --- | --- |
| `IUserModule.BlockAsync(ulong fid, string portrait, int day = 1, string reason = "", ...)` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | 改到 `client.Admins`，并使用保留下来的 `fname` 调用形状。 |
| `IUserModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | 把调用从 `client.Users` 移到 `client.Admins`。 |
| `IForumModule.DelBaWuAsync(string fname, string portrait, string baWuType, ...)` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)` | 改到 `client.Admins`，并使用 `DelBawuAsync(...)`。 |
| `IAdminModule.AddBaWuAsync(string fname, string userName, BawuType bawuType, ...)` | `IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType, ...)` | 把旧的大小写写法改成 `AddBawuAsync(...)`。 |
| `IAdminModule.DelBaWuAsync(string fname, string portrait, BawuType bawuType, ...)` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)` | 把旧的大小写写法改成 `DelBawuAsync(...)`。 |

v3 的标准调用路径是：

```csharp
var info = await client.Admins.GetBawuInfoAsync("你的吧名");
await client.Admins.BlockAsync("你的吧名", "目标用户 portrait", day: 1, reason: "违规理由");
await client.Admins.AddBawuAsync("你的吧名", "目标用户名", BawuType.Manager);
await client.Admins.DelBawuAsync("你的吧名", "目标用户 portrait", BawuType.Manager);
```

### Client

`client.Client` 只保留这三类能力：

- `InitWebSocketAsync()`
- `InitZIdAsync()`
- `SyncAsync()`

如果你在 v2 里把 `Client` 当成“连接 + 消息”的总入口，v3 需要把消息相关调用迁到 `Messages`。

## 5. 配置和异常模型

### 保持不变的地方

- 继续使用 `TiebaOptions`
- 继续使用 `TiebaTransportMode`
- 继续支持直接构造、DI 和 factory

### 迁移时要重点确认的地方

- 缺少本地必需凭据时，明确抛出 `TiebaAuthenticationException`
- 配置非法时，明确抛出 `TiebaConfigurationException`
- 服务端业务拒绝时，抛出 `TieBaServerException`

迁移异常处理时，最好按“本地配置 / 本地鉴权 / 服务端业务”三类整理，而不是继续混在一起。

## 6. 推荐升级顺序

1. 升级目标框架到 `net10.0`。
2. 保持原有客户端创建方式不变。
3. 先修正模块归属：把消息调用迁到 `client.Messages`，把 admin 和 bawu 调用迁到 `client.Admins`。
4. 再按本文表格替换公开方法名和 DTO 名。
5. 把依赖 `client.Client` 的代码收回到生命周期初始化用途。
6. 复查 README、how-to 和你自己的调用封装里是否还有旧模块叙事或旧名称。

## 7. 升级后的最小验证

至少做下面这些检查：

1. `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore --nologo`
2. `pwsh ./scripts/verify-local.ps1 -ValidateOnly`
3. 打开新的导航链路，确认 README -> Getting Started -> How-to -> 进阶 / 排障 可以走通。
4. 搜索你自己的代码，确认旧别名、旧模块入口和旧 DTO 名都已经替换成当前公开名称。

## 8. 相关阅读

- [README](../../README.md)
- [Getting Started](../guide/getting-started.md)
- [API 参考](../reference/modules.md)
- [Release Notes v3](./release-notes-v3.md)
- [Parity](./parity.md)
