# AioTieba4DotNet v3 发布说明

v3 是一次“公开边界更清楚，文档入口更清楚，发布治理更清楚”的发布线。它没有把客户端根故事推倒重来，但把长期混杂的模块职责和用户文档路径重新整理成了稳定形态。公开 C# surface 现在也统一使用规范化名字，同时保留 upstream family identity 的可检索性。

## 用户可见变化

### 1. 支持矩阵冻结到 .NET 10

v3 只支持 `net10.0`。这让文档、示例和发布契约终于可以围绕同一条技术线来写，而不用继续在 README 里混放多个过渡时代的叙事。

### 2. 六个公开模块成为主叙事

v3 的根客户端稳定为:

- `Forums`
- `Threads`
- `Users`
- `Admins`
- `Messages`
- `Client`

其中最值得升级方注意的是:

- `Messages` 成为消息业务 canonical 入口
- `Admins` 成为吧务和后台管理 canonical 入口
- `Client` 只保留生命周期和初始化职责

### 3. 公开命名规范化按模块落地

发布后的公开 surface 只保留当前规范化名字。旧 alias、旧 bridge、旧 DTO 名只会出现在 migration 和 parity 的 mapping section 里，用来帮助搜索和升级。

#### Forums family

`follow_forum` / `unfollow_forum` 现在只保留 `FollowAsync(...)` / `UnfollowAsync(...)`。`get_self_follow_forums` 和 `get_self_follow_forums_v1` 继续作为两个并列支持的 peer families，对外名字分别是默认 family 和 paged family。

| Old public name | Current public name | Category |
| --- | --- | --- |
| `IForumModule.LikeAsync(string fname, ...)` | `IForumModule.FollowAsync(string fname, ...)` | `true-alias` |
| `IForumModule.UnlikeAsync(string fname, ...)` | `IForumModule.UnfollowAsync(string fname, ...)` | `true-alias` |
| `IForumModule.GetSelfFollowForumsPagedAsync(int pn = 1, int rn = 20, ...)` | `IForumModule.GetSelfFollowForumsV1Async(int pn = 1, int rn = 20, ...)` | `peer-family` |
| `SelfFollowForumsPaged` | `SelfFollowForumsV1` | `shape-variant` |
| `SelfFollowForumV1Item` | `SelfFollowForumV1` | `shape-variant` |

#### Users family

用户侧最明显的变化是 blacklist family、`user_info` family、nickname family 和 user-content DTO family 都用了更接近 upstream root 的公开名字。需要注意的是，`get_blacklist` family 与 `_old` sibling family 仍然是 parallel supported APIs，不是兼容残留。

| Old public name | Current public name | Category |
| --- | --- | --- |
| `IUserModule.GetBlacklistPermissionsAsync(...)` | `IUserModule.GetBlacklistAsync(...)` | `peer-family` |
| `IUserModule.SetBlacklistPermissionsAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | `IUserModule.SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | `peer-family` |
| `IUserModule.GetBlacklistMutedAsync(int pn = 1, int rn = 20, ...)` | `IUserModule.GetBlacklistOldAsync(int pn = 1, int rn = 20, ...)` | `peer-family` |
| `IUserModule.AddBlacklistMutedAsync(long userId, ...)` | `IUserModule.AddBlacklistOldAsync(long userId, ...)` | `peer-family` |
| `IUserModule.RemoveBlacklistMutedAsync(long userId, ...)` | `IUserModule.RemoveBlacklistOldAsync(long userId, ...)` | `peer-family` |
| `BlacklistPermissions` | `BlacklistUsers` | `shape-variant` |
| `BlacklistPermission` | `BlacklistUser` | `shape-variant` |
| `BlacklistMutedUsers` | `BlacklistOldUsers` | `shape-variant` |
| `BlacklistMutedUser` | `BlacklistOldUser` | `shape-variant` |
| `IUserModule.GetBasicInfoAppAsync(int userId, ...)` | `IUserModule.GetUserInfoAppAsync(int userId, ...)` | `peer-family` |
| `UserInfoApp` | `UserInfoGuInfoApp` | `shape-variant` |
| `UserInfoWeb` | `UserInfoGuInfoWeb` | `shape-variant` |
| `IUserModule.SetNicknameLegacyAsync(string nickName, ...)` | `IUserModule.SetNicknameAsync(string nickName, ...)` | `peer-family` |
| `UserPostss` | `UserPostGroups` | `shape-variant` |

#### Messages ownership

消息读取不再挂在 `Users` 下。`Messages` 是 inbox、private-message、chatroom-message 和 push parsing 的唯一公开 home。

| Old public name | Current public name | Category |
| --- | --- | --- |
| `IUserModule.GetAtsAsync(int pn = 1, ...)` | `IMessagesModule.GetAtsAsync(int pn = 1, ...)` | `relocation-bridge` |
| `IUserModule.GetRepliesAsync(int pn = 1, ...)` | `IMessagesModule.GetRepliesAsync(int pn = 1, ...)` | `relocation-bridge` |

#### Admins ownership and Bawu normalization

后台管理写操作也不再分散。`Admins` 现在拥有 block、bawu 和相关后台 family。公开 casing 统一为 `Bawu`，删除动词直接跟 upstream `del_bawu` 对齐。

| Old public name | Current public name | Category |
| --- | --- | --- |
| `IUserModule.BlockAsync(ulong fid, string portrait, int day = 1, string reason = "", ...)` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `relocation-bridge` |
| `IUserModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `relocation-bridge` |
| `IForumModule.DelBaWuAsync(string fname, string portrait, string baWuType, ...)` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)` | `relocation-bridge` |
| `IAdminModule.AddBaWuAsync(string fname, string userName, BawuType bawuType, ...)` | `IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType, ...)` | `canonical-operation` |
| `IAdminModule.DelBaWuAsync(string fname, string portrait, BawuType bawuType, ...)` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)` | `canonical-operation` |

### 4. 用户文档 IA 改成“从旅程出发”

主文档链路现在是:

1. `README.md`
2. `docs/getting-started.md`
3. 四份按任务组织的 how-to 页面
4. `docs/modules.md` 参考索引
5. `docs/advanced.md` / `docs/troubleshooting.md`
6. `docs/migration-v2-to-v3.md` / `docs/parity-v3.md`

## 发布治理

v3 发布线继续采用 **build/codegen/packaging only** 的 GitHub Actions 治理模型。

- GitHub Actions **不运行 `dotnet test`**
- deterministic、integration、live 验证都通过本地或 agent 环境执行
- GitHub Actions release gate 在 pack / publish 之前检查 restore、build、codegen 和 packaging

## 本地验证契约

发版前需要保证以下文件齐备并且非空:

- `README.md`
- `docs/getting-started.md`
- `docs/how-to-forums.md`
- `docs/how-to-threads.md`
- `docs/how-to-users.md`
- `docs/how-to-messages.md`
- `docs/modules.md`
- `docs/advanced.md`
- `docs/troubleshooting.md`
- `docs/migration-v2-to-v3.md`
- `docs/release-notes-v3.md`
- `docs/parity-v3.md`

此外还要保留本地验证 manifest 与三份 lane evidence:

- `.sisyphus/evidence/local-verification.manifest.json`
- `.sisyphus/evidence/local-deterministic-verification.md`
- `.sisyphus/evidence/local-integration-verification.md`
- `.sisyphus/evidence/local-live-verification.md`

## 发布前最小检查清单

1. `dotnet restore --nologo`
2. `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore --nologo`
3. `dotnet run --project ProtoGenerator/ProtoGenerator.csproj`
4. `pwsh ./scripts/verify-local.ps1 -ValidateOnly`
5. 完整检查 README 和 docs 导航链路

## 相关文档

- [Getting Started](./getting-started.md)
- [Migration v2 to v3](./migration-v2-to-v3.md)
- [Parity v3](./parity-v3.md)
