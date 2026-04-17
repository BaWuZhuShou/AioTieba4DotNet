# 对齐台账

这份文档是 AioTieba4DotNet 当前唯一的对齐真源。它同时承担两项职责：

1. 冻结仓库面对上游 `aiotieba` 的公开能力对齐范围。
2. 记录内部 `Api/**` 实现与上游 Python 族系 的对应关系，以及认证需求注记。

它不是上手教程，也不是迁移指南。第一次接入请先看 [README](../../README.md) 和 [快速开始](../guide/getting-started.md)；需要理解公开模块时请看 `docs/how-to/*` 与 `docs/reference/modules.md`；需要核对“这个能力对齐到哪一层、内部实现落在哪个族系”，再回到这份台账。

## 范围与真源来源

这份台账从以下上游真源冻结对齐范围：

- `aiotieba/aiotieba/api/**/__init__.py`
- `aiotieba/aiotieba/__init__.py`
- 冻结 tuple：`lumina37/aiotieba`、`https://github.com/lumina37/aiotieba`、`v4.6.4`、`04f8e431f87507a6228b42061c70d298b34317ff`；可执行记录见 `.sisyphus/evidence/parity-truth-freeze.json`
- 对齐证据的 `comparisonSource` 必须回指同一个 frozen tree：`https://github.com/lumina37/aiotieba/tree/04f8e431f87507a6228b42061c70d298b34317ff`
- 仓库内 `aiotieba/` 只属于 reference material。只有在显式 snapshot metadata 与上面的 repo/tag/SHA 完全一致时，才允许把它当作 comparison reference；缺失、混用或过期 metadata 一律 fail closed

这份台账采用以下解释规则：

- `aiotieba.api` 和 `aiotieba.api._protobuf` 是空包标记，因此只在证据层面体现，不会作为独立台账行列出。
- 上游包路径加导出符号名是权威依据。像 `Replys` 和 `get_uinfo_getUserInfo_web` 这类不规则的上游名称，会继续保留在上游身份中，即使归一化后的 C# 公开表面使用了更整洁的公开名称。
- `docs/archive/todo.md` 仅提供历史背景。`docs/related/parity.md` 是唯一仍在生效的对齐台账，也是当前对齐范围唯一的权威来源。
- 内部 `Api/**` 实现映射和认证注记现在都放在这份文档里，而不是放在 C# 特性或单独的映射文件中。
- 当前 C# 公开基线是 `ITiebaClient` / `TiebaClient`，包含 `Forums`、`Threads`、`Users`、`Admins`、`Messages` 和 `Client`，以及 `TiebaOptions`、DI 注册和工厂入口。
- 上游名称里带有 `old`、`v1` 或其他旧式表述的行，描述的是有意保留的同级族系或同级族系数据形状。它们仍然可以按上游身份被搜索到，而当前生效的 C# 表面保持使用此处列出的归一化公开名称。
- 本地活动验证入口由 `pwsh ./scripts/verify-local.ps1 -ValidateOnly` 和 `pwsh ./scripts/test-lane.ps1 sequence-dry-run` 负责。前者只校验当前保留的 `.sisyphus/evidence/parity-truth-freeze.json`、`.sisyphus/evidence/parity-gap-ledger.json`、`.sisyphus/evidence/local-verification.manifest.json` 与 `.sisyphus/evidence/local-verification.manifest.schema.json`，后者只打印当前有序套件路由，不再承载旧 lane 语义。

## 如何阅读这份文档

- **内部实现映射** 小节面向维护者。它说明内部哪个 `Api/**` 类型映射到哪个上游 Python 族系，以及当前实现是否要求认证上下文。
- **公开命名归一化映射** 记录被移除的别名、所有权迁移，以及形状改名，保证公开表面清理后仍然可检索。
- **族系对齐表** 是公开能力台账。它回答某个族系是否已实现、落在 C# 公开表面的哪里，以及由哪个维护责任域承接。
- 有序套件阶段标签只描述当前的验证分组。它们不是旧时代可运行的 lane 名称。
- `docs/related/public-api-coverage-matrix.md` 负责公开 surface、lane disposition 和首类 `Api:*` discoverability 规则；`.sisyphus/evidence/parity-gap-ledger.json` 冻结 unresolved rows；当前活动 retained artifact model 只包括 `.sisyphus/evidence/parity-truth-freeze.json`、`.sisyphus/evidence/parity-gap-ledger.json`、`.sisyphus/evidence/local-verification.manifest.json` 与 `.sisyphus/evidence/local-verification.manifest.schema.json`。

## Canonical parity audit ledger

当前所有可执行 parity 审计行都必须使用下面这一种 canonical row 结构；后续只允许往这个结构里追加行，不能自定义字段名或自创状态名。

- canonical status 只允许：`match` / `requires-remediation` / `upstream-gap` / `blocked-by-verification` / `intentional-divergence-approved`
- 每一行都必须显式记录：`Audit unit`、`Upstream anchor`、`.NET anchor`、`Transport kind`、`Auth prerequisites`、`Proof artifact`、`Status`、`Verification command`
- `upstream-gap` 与 `intentional-divergence-approved` 必须作为完整 canonical rows 进入这张表；不允许只写在注释、脚注、段落说明或 prose-only backlog 里

| Audit unit | Upstream anchor | .NET anchor | Transport kind | Auth prerequisites | Proof artifact | Status | Verification command | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `truth-source-policy` | `lumina37/aiotieba@04f8e431f87507a6228b42061c70d298b34317ff` via `aiotieba/aiotieba/__init__.py` export freeze | `ParityTruthFreezeContract` | `n/a-contract` | `none` | `.sisyphus/evidence/parity-truth-freeze.json` | match | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:TruthFreeze"` | Frozen upstream repo/tag/SHA/tree policy is enforced before downstream parity evidence is trusted. |
| `parity-evidence-schema` | `lumina37/aiotieba@04f8e431f87507a6228b42061c70d298b34317ff` parity baseline reused by all downstream audit rows | `ParityEvidenceSchemaContract` | `n/a-contract` | `none` | `schema contract in code` | match | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:EvidenceSchema"` | This row keeps the durable schema contract reference without promoting retired evidence files into the active retained artifact model. |
| `public-api.forums.sign-forum` | `aiotieba.api.sign_forum` | `IForumModule.SignAsync(string fname)` | `http-app-form` | `safe auth + signable forum` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Single-forum sign remains implemented, but the current safe lane has no truthful undo path for a daily action. |
| `public-api.forums.sign-forums` | `aiotieba.api.sign_forums` | `IForumModule.SignForumsAsync()` | `http-web-form` | `safe auth + follow set` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Batch sign remains implemented, but the current safe lane has no truthful compensation model for the daily action. |
| `public-api.forums.sign-growth` | `aiotieba.api.sign_growth` | `IForumModule.SignGrowthAsync()` | `http-web-form` | `safe auth` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Growth sign remains implemented, but the current safe lane has no truthful compensation model for the daily action. |
| `public-api.threads.move` | `aiotieba.api.move` | `IThreadModule.MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0)` | `http-app-form` | `restricted moderation forum + thread + alternate tab ids` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Move remains implemented, but the current moderation forum exposes no alternate tab and the dedicated thread already starts in tab `0`, so bounded direct coverage has no truthful reversible target. |
| `public-api.users.remove-fan` | `aiotieba.api.remove_fan` | `IUserModule.RemoveFanAsync(long userId)` | `http-app-form` | `safe auth + dedicated reciprocal fan fixture` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Fan removal remains implemented, but the current safe lane has no public re-add path or dedicated reciprocal fixture model. |
| `public-api.users.set-nickname` | `aiotieba.api.set_nickname_old` | `IUserModule.SetNicknameAsync(string nickName)` | `http-web-form` | `safe auth + disposable profile fixture` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Nickname writes remain implemented, but the current safe lane has no approved restore contract for uniqueness or cooldown-sensitive profile state. |
| `public-api.users.set-profile` | `aiotieba.api.set_profile` | `IUserModule.SetProfileAsync(string nickName, string sign, Gender gender)` | `http-web-form` | `safe auth + disposable profile fixture` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Profile writes remain implemented, but the current safe lane has no approved restore contract for cooldown or review-sensitive profile state. |
| `public-api.messages.send-chatroom-message` | `aiotieba.api.send_chatroom_msg` | `IMessagesModule.SendChatroomMessageAsync(long chatroomId, ulong forumId, string text, IReadOnlyList<long>? atUserIds = null, int robotCode = -1)` | `blcp-chatroom-send` | `messaging capability + dedicated chatroom id + forum id` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Chatroom send remains implemented and parity-frozen offline, but the current safe lane has no approved compensation or acknowledgement model for outward group-message mutations. |
| `public-api.messages.set-message-read` | `aiotieba.api.set_msg_readed` | `IMessagesModule.SetMessageReadAsync(WsMessage message)` | `websocket-private-group-state-mutation` | `messaging capability + dedicated unread message fixture` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Read-state mutation remains implemented and parity-frozen offline, but the current safe lane has no public unread reset path for reversible inbox-state proof. |
| `public-api.admins.add-bawu` | `aiotieba.api.add_bawu` | `IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType)` | `http-web-form` | `restricted admin capability + forum + target user` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Bawu assignment remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family. |
| `public-api.admins.del-bawu` | `aiotieba.api.del_bawu` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType)` | `http-web-form` | `restricted admin capability + forum + target portrait` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Bawu removal remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family. |
| `public-api.admins.add-bawu-blacklist` | `aiotieba.api.add_bawu_blacklist` | `IAdminModule.AddBawuBlacklistAsync(string fname, long userId)` | `http-web-form` | `restricted admin capability + forum + target user id` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Bawu-blacklist add remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family. |
| `public-api.admins.del-bawu-blacklist` | `aiotieba.api.del_bawu_blacklist` | `IAdminModule.DelBawuBlacklistAsync(string fname, long userId)` | `http-web-form` | `restricted admin capability + forum + target user id` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Bawu-blacklist remove remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family. |
| `public-api.admins.set-bawu-perm` | `aiotieba.api.set_bawu_perm` | `IAdminModule.SetBawuPermAsync(string fname, string portrait, BawuPermType permissions)` | `http-web-form` | `restricted admin capability + forum + target portrait` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Bawu permission writes remain implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family. |
| `public-api.admins.handle-unblock-appeals` | `aiotieba.api.handle_unblock_appeals` | `IAdminModule.HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false)` | `http-web-form` | `restricted admin capability + forum + appeal fixtures` | `.sisyphus/evidence/parity-gap-ledger.json` | blocked-by-verification | `dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter "TestCategory=Parity:Gaps"` | Appeal handling remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family. |
## 有序套件阶段图例

这份台账中的阶段标签只描述当前的有序套件分组。它们不是旧时代可运行的 lane 名称。

- `1. 贴吧基础/读取`
- `2. 贴吧关注/反感/搜索/统计`
- `3. 主题读取/评论/tab-map/恢复检查`
- `4. 主题写入/管理/签到族系`
- `5. 用户/社交/资料`
- `6. 消息/推送/客户端生命周期`
- `7. 破坏性清理/回滚验证`
- `不适用 - 仅文档对齐`

## 内部实现映射与认证注记

这一节取代了旧的独立 Python API 映射文件。只要你新增、删除、重命名或移动内部 `Api/**` 实现，就要同步更新这里。

- `内部 C# 类型` 是当前承载上游映射的具体内部请求族系或辅助类型。
- `实现文件` 是对齐工作发生变化时需要更新的规范代码锚点。
- `需要认证` 取代了旧的 `RequireBduss` 标记约定：`是` 表示内部 API 需要认证上下文；`否` 表示它不依赖那个旧标记。

| 上游 Python 族系 | 内部 C# 类型 | 实现文件 | 需要认证 |
| --- | --- | --- | --- |
| `aiotieba.api.add_bawu` | `AddBaWu` | `AioTieba4DotNet/Api/AddBaWu/AddBaWu.cs` | 是 |
| `aiotieba.api.add_bawu_blacklist` | `AddBawuBlacklist` | `AioTieba4DotNet/Api/AddBawuBlacklist/AddBawuBlacklist.cs` | 是 |
| `aiotieba.api.add_blacklist_old` | `AddBlacklistOld` | `AioTieba4DotNet/Api/AddBlacklistOld/AddBlacklistOld.cs` | 是 |
| `aiotieba.api.add_post` | `AddPost` | `AioTieba4DotNet/Api/AddPost/AddPost.cs` | 是 |
| `aiotieba.api.agree` | `Agree` | `AioTieba4DotNet/Api/Agree/Agree.cs` | 是 |
| `aiotieba.api.block` | `Block` | `AioTieba4DotNet/Api/Block/Block.cs` | 是 |
| `aiotieba.api.del_bawu` | `DelBaWu` | `AioTieba4DotNet/Api/DelBaWu/DelBaWu.cs` | 是 |
| `aiotieba.api.del_bawu_blacklist` | `DelBawuBlacklist` | `AioTieba4DotNet/Api/DelBawuBlacklist/DelBawuBlacklist.cs` | 是 |
| `aiotieba.api.del_blacklist_old` | `DelBlacklistOld` | `AioTieba4DotNet/Api/DelBlacklistOld/DelBlacklistOld.cs` | 是 |
| `aiotieba.api.del_post` | `DelPost` | `AioTieba4DotNet/Api/DelPost/DelPost.cs` | 是 |
| `aiotieba.api.del_posts` | `DelPosts` | `AioTieba4DotNet/Api/DelPosts/DelPosts.cs` | 是 |
| `aiotieba.api.del_thread` | `DelThread` | `AioTieba4DotNet/Api/DelThread/DelThread.cs` | 是 |
| `aiotieba.api.del_threads` | `DelThreads` | `AioTieba4DotNet/Api/DelThreads/DelThreads.cs` | 是 |
| `aiotieba.api.dislike_forum` | `DislikeForum` | `AioTieba4DotNet/Api/DislikeForum/DislikeForum.cs` | 是 |
| `aiotieba.api.follow_forum` | `LikeForum` | `AioTieba4DotNet/Api/LikeForum/LikeForum.cs` | 是 |
| `aiotieba.api.follow_user` | `FollowUser` | `AioTieba4DotNet/Api/FollowUser/FollowUser.cs` | 是 |
| `aiotieba.api.get_ats` | `GetAts` | `AioTieba4DotNet/Api/GetAts/GetAts.cs` | 是 |
| `aiotieba.api.get_bawu_blacklist` | `GetBawuBlacklist` | `AioTieba4DotNet/Api/GetBawuBlacklist/GetBawuBlacklist.cs` | 是 |
| `aiotieba.api.get_bawu_info` | `GetBawuInfo` | `AioTieba4DotNet/Api/GetBawuInfo/GetBawuInfo.cs` | 否 |
| `aiotieba.api.get_bawu_perm` | `GetBawuPerm` | `AioTieba4DotNet/Api/GetBawuPerm/GetBawuPerm.cs` | 是 |
| `aiotieba.api.get_bawu_postlogs` | `GetBawuPostlogs` | `AioTieba4DotNet/Api/GetBawuPostlogs/GetBawuPostlogs.cs` | 是 |
| `aiotieba.api.get_bawu_userlogs` | `GetBawuUserlogs` | `AioTieba4DotNet/Api/GetBawuUserlogs/GetBawuUserlogs.cs` | 是 |
| `aiotieba.api.get_blacklist` | `GetBlacklist` | `AioTieba4DotNet/Api/GetBlacklist/GetBlacklist.cs` | 是 |
| `aiotieba.api.get_blacklist_old` | `GetBlacklistOld` | `AioTieba4DotNet/Api/GetBlacklistOld/GetBlacklistOld.cs` | 是 |
| `aiotieba.api.get_blocks` | `GetBlocks` | `AioTieba4DotNet/Api/GetBlocks/GetBlocks.cs` | 是 |
| `aiotieba.api.get_cid` | `GetCid` | `AioTieba4DotNet/Api/GetCid/GetCid.cs` | 是 |
| `aiotieba.api.get_comments` | `GetComments` | `AioTieba4DotNet/Api/GetComments/GetComments.cs` | 否 |
| `aiotieba.api.get_dislike_forums` | `GetDislikeForums` | `AioTieba4DotNet/Api/GetDislikeForums/GetDislikeForums.cs` | 是 |
| `aiotieba.api.get_fans` | `GetFans` | `AioTieba4DotNet/Api/GetFans/GetFans.cs` | 是 |
| `aiotieba.api.get_fid` | `GetFid` | `AioTieba4DotNet/Api/GetFid/GetFid.cs` | 否 |
| `aiotieba.api.get_follow_forums` | `GetFollowForums` | `AioTieba4DotNet/Api/GetFollowForums/GetFollowForums.cs` | 否 |
| `aiotieba.api.get_follows` | `GetFollows` | `AioTieba4DotNet/Api/GetFollows/GetFollows.cs` | 否 |
| `aiotieba.api.get_forum` | `GetForum` | `AioTieba4DotNet/Api/GetForum/GetForum.cs` | 否 |
| `aiotieba.api.get_forum_detail` | `GetForumDetail` | `AioTieba4DotNet/Api/GetForumDetail/GetForumDetail.cs` | 否 |
| `aiotieba.api.get_forum_level` | `GetForumLevel` | `AioTieba4DotNet/Api/GetForumLevel/GetForumLevel.cs` | 是 |
| `aiotieba.api.get_group_msg` | `GetGroupMsg` | `AioTieba4DotNet/Api/GetGroupMsg/GetGroupMsg.cs` | 是 |
| `aiotieba.api.get_images` | `GetImages` | `AioTieba4DotNet/Api/GetImages/GetImages.cs` | 否 |
| `aiotieba.api.get_last_replyers` | `GetLastReplyers` | `AioTieba4DotNet/Api/GetLastReplyers/GetLastReplyers.cs` | 否 |
| `aiotieba.api.get_member_users` | `GetMemberUsers` | `AioTieba4DotNet/Api/GetMemberUsers/GetMemberUsers.cs` | 是 |
| `aiotieba.api.get_rank_forums` | `GetRankForums` | `AioTieba4DotNet/Api/GetRankForums/GetRankForums.cs` | 否 |
| `aiotieba.api.get_rank_users` | `GetRankUsers` | `AioTieba4DotNet/Api/GetRankUsers/GetRankUsers.cs` | 否 |
| `aiotieba.api.get_recom_status` | `GetRecomStatus` | `AioTieba4DotNet/Api/GetRecomStatus/GetRecomStatus.cs` | 是 |
| `aiotieba.api.get_recover_info` | `GetRecoverInfo` | `AioTieba4DotNet/Api/GetRecoverInfo/GetRecoverInfo.cs` | 是 |
| `aiotieba.api.get_recovers` | `GetRecovers` | `AioTieba4DotNet/Api/GetRecovers/GetRecovers.cs` | 是 |
| `aiotieba.api.get_replys` | `GetReplys` | `AioTieba4DotNet/Api/GetReplys/GetReplys.cs` | 是 |
| `aiotieba.api.get_roomlist_by_fid` | `GetRoomListByFid` | `AioTieba4DotNet/Api/GetRoomListByFid/GetRoomListByFid.cs` | 是 |
| `aiotieba.api.get_self_follow_forums` | `GetSelfFollowForums` | `AioTieba4DotNet/Api/GetSelfFollowForums/GetSelfFollowForums.cs` | 是 |
| `aiotieba.api.get_self_follow_forums_v1` | `GetSelfFollowForumsV1` | `AioTieba4DotNet/Api/GetSelfFollowForumsV1/GetSelfFollowForumsV1.cs` | 是 |
| `aiotieba.api.get_selfinfo_initNickname` | `GetSelfInfoInitNickname` | `AioTieba4DotNet/Api/GetSelfInfoInitNickname/GetSelfInfoInitNickname.cs` | 是 |
| `aiotieba.api.get_selfinfo_moindex` | `GetSelfInfoMoIndex` | `AioTieba4DotNet/Api/GetSelfInfoMoIndex/GetSelfInfoMoIndex.cs` | 否 |
| `aiotieba.api.get_square_forums` | `GetSquareForums` | `AioTieba4DotNet/Api/GetSquareForums/GetSquareForums.cs` | 是 |
| `aiotieba.api.get_statistics` | `GetStatistics` | `AioTieba4DotNet/Api/GetStatistics/GetStatistics.cs` | 是 |
| `aiotieba.api.get_tab_map` | `GetTabMap` | `AioTieba4DotNet/Api/GetTabMap/GetTabMap.cs` | 是 |
| `aiotieba.api.get_posts` | `GetThreadPosts` | `AioTieba4DotNet/Api/GetThreadPosts/GetThreadPosts.cs` | 否 |
| `aiotieba.api.get_threads` | `GetThreads` | `AioTieba4DotNet/Api/GetThreads/GetThreads.cs` | 否 |
| `aiotieba.api.get_uinfo_getuserinfo_app` | `GetUInfoGetUserInfoApp` | `AioTieba4DotNet/Api/GetUInfoGetUserInfoApp/GetUInfoGetUserInfoApp.cs` | 否 |
| `aiotieba.api.get_uinfo_getUserInfo_web` | `GetUInfoGetUserInfoWeb` | `AioTieba4DotNet/Api/GetUInfoGetUserInfoWeb/GetUInfoGetUserInfoWeb.cs` | 否 |
| `aiotieba.api.get_uinfo_panel` | `GetUInfoPanel` | `AioTieba4DotNet/Api/GetUInfoPanel/GetUInfoPanel.cs` | 否 |
| `aiotieba.api.get_uinfo_user_json` | `GetUInfoUserJson` | `AioTieba4DotNet/Api/GetUInfoUserJson/GetUInfoUserJson.cs` | 否 |
| `aiotieba.api.get_unblock_appeals` | `GetUnblockAppeals` | `AioTieba4DotNet/Api/GetUnblockAppeals/GetUnblockAppeals.cs` | 是 |
| `aiotieba.api.get_user_contents.get_posts` | `GetPosts` | `AioTieba4DotNet/Api/GetUserContents/GetPosts.cs` | 是 |
| `aiotieba.api.get_user_contents.get_threads` | `GetUserThreads` | `AioTieba4DotNet/Api/GetUserContents/GetUserThreads.cs` | 是 |
| `aiotieba.api.get_user_forum_info` | `GetUserForumInfo` | `AioTieba4DotNet/Api/GetUserForumInfo/GetUserForumInfo.cs` | 是 |
| `aiotieba.api.good` | `Good` | `AioTieba4DotNet/Api/Good/Good.cs` | 是 |
| `aiotieba.api.handle_unblock_appeals` | `HandleUnblockAppeals` | `AioTieba4DotNet/Api/HandleUnblockAppeals/HandleUnblockAppeals.cs` | 是 |
| `aiotieba.api.init_websocket` | `InitWebSocket` | `AioTieba4DotNet/Api/InitWebSocket/InitWebSocket.cs` | 是 |
| `aiotieba.api.init_z_id` | `InitZId` | `AioTieba4DotNet/Api/InitZId/InitZId.cs` | 否 |
| `aiotieba.api.login` | `Login` | `AioTieba4DotNet/Api/Login/Login.cs` | 是 |
| `aiotieba.api.move` | `Move` | `AioTieba4DotNet/Api/Move/Move.cs` | 是 |
| `aiotieba.api.profile` | `GetUInfoProfile` | `AioTieba4DotNet/Api/Profile/GetUInfoProfile/GetUInfoProfile.cs` | 否 |
| `aiotieba.api.profile.get_homepage` | `GetHomepage` | `AioTieba4DotNet/Api/Profile/GetHomepage/GetHomepage.cs` | 否 |
| `aiotieba.api.push_notify` | `PushNotify` | `AioTieba4DotNet/Api/PushNotify/PushNotify.cs` | 否 |
| `aiotieba.api.recommend` | `Recommend` | `AioTieba4DotNet/Api/Recommend/Recommend.cs` | 是 |
| `aiotieba.api.recover` | `Recover` | `AioTieba4DotNet/Api/Recover/Recover.cs` | 是 |
| `aiotieba.api.remove_fan` | `RemoveFan` | `AioTieba4DotNet/Api/RemoveFan/RemoveFan.cs` | 是 |
| `aiotieba.api.search_exact` | `SearchExact` | `AioTieba4DotNet/Api/SearchExact/SearchExact.cs` | 否 |
| `aiotieba.api.send_msg` | `SendMsg` | `AioTieba4DotNet/Api/SendMsg/SendMsg.cs` | 是 |
| `aiotieba.api.set_bawu_perm` | `SetBawuPerm` | `AioTieba4DotNet/Api/SetBawuPerm/SetBawuPerm.cs` | 是 |
| `aiotieba.api.set_blacklist` | `SetBlacklist` | `AioTieba4DotNet/Api/SetBlacklist/SetBlacklist.cs` | 是 |
| `aiotieba.api.set_msg_readed` | `SetMsgReaded` | `AioTieba4DotNet/Api/SetMsgReaded/SetMsgReaded.cs` | 是 |
| `aiotieba.api.set_nickname_old` | `SetNicknameOld` | `AioTieba4DotNet/Api/SetNicknameOld/SetNicknameOld.cs` | 是 |
| `aiotieba.api.set_profile` | `SetProfile` | `AioTieba4DotNet/Api/SetProfile/SetProfile.cs` | 是 |
| `aiotieba.api.set_thread_privacy` | `SetThreadPrivacy` | `AioTieba4DotNet/Api/SetThreadPrivacy/SetThreadPrivacy.cs` | 是 |
| `aiotieba.api.sign_forum` | `Sign` | `AioTieba4DotNet/Api/Sign/Sign.cs` | 是 |
| `aiotieba.api.sign_forums` | `SignForums` | `AioTieba4DotNet/Api/SignForums/SignForums.cs` | 是 |
| `aiotieba.api.sign_growth` | `SignGrowth` | `AioTieba4DotNet/Api/SignGrowth/SignGrowth.cs` | 是 |
| `aiotieba.api.sync` | `Sync` | `AioTieba4DotNet/Api/Sync/Sync.cs` | 是 |
| `aiotieba.api.tieba_uid2user_info` | `TiebaUid2UserInfo` | `AioTieba4DotNet/Api/TiebaUid2UserInfo/TiebaUid2UserInfo.cs` | 否 |
| `aiotieba.api.top` | `Top` | `AioTieba4DotNet/Api/Top/Top.cs` | 是 |
| `aiotieba.api.unblock` | `Unblock` | `AioTieba4DotNet/Api/Unblock/Unblock.cs` | 是 |
| `aiotieba.api.undislike_forum` | `UndislikeForum` | `AioTieba4DotNet/Api/UndislikeForum/UndislikeForum.cs` | 是 |
| `aiotieba.api.unfollow_forum` | `UnlikeForum` | `AioTieba4DotNet/Api/UnlikeForum/UnlikeForum.cs` | 是 |
| `aiotieba.api.unfollow_user` | `UnfollowUser` | `AioTieba4DotNet/Api/UnfollowUser/UnfollowUser.cs` | 是 |
| `aiotieba.api.ungood` | `Ungood` | `AioTieba4DotNet/Api/Ungood/Ungood.cs` | 是 |

## 公开命名归一化映射

对齐行始终锚定在上游族系标识符上。`C# 表面` 列只使用归一化后的当前公开名称。旧公开名称仍然保留在下面的映射表中，这样调用方仍然可以搜索到被移除的别名、被移除的桥接层，以及改名后的同级族系符号。

### 已移除的别名与已移除的所有权桥接

| 所属模块 | 上游族系锚点 | 已移除的公开名称 | 归一化后的当前 C# 名称 | 类别 | 映射说明 |
| --- | --- | --- | --- | --- | --- |
| `Forums` | `aiotieba.api.follow_forum` | `IForumModule.LikeAsync(string fname, ...)` | `IForumModule.FollowAsync(string fname, ...)` | `等价别名` | 同一个上游族系，旧别名已移除。 |
| `Forums` | `aiotieba.api.unfollow_forum` | `IForumModule.UnlikeAsync(string fname, ...)` | `IForumModule.UnfollowAsync(string fname, ...)` | `等价别名` | 同一个上游族系，旧别名已移除。 |
| `Messages` | `aiotieba.api.get_ats` | `IUserModule.GetAtsAsync(int pn = 1, ...)` | `IMessagesModule.GetAtsAsync(int pn = 1, ...)` | `迁移桥接` | 收件箱 `@` 读取属于 `Messages`。 |
| `Messages` | `aiotieba.api.get_replys` | `IUserModule.GetRepliesAsync(int pn = 1, ...)` | `IMessagesModule.GetRepliesAsync(int pn = 1, ...)` | `迁移桥接` | 回复收件箱读取属于 `Messages`。 |
| `Admins` | `aiotieba.api.block` | `IUserModule.BlockAsync(ulong fid, string portrait, int day = 1, string reason = "", ...)` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `迁移桥接` | 吧务和封禁工作属于 `Admins`；已不存在保留的 `fid` 重载。 |
| `Admins` | `aiotieba.api.block` | `IUserModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)` | `迁移桥接` | 吧务和封禁工作属于 `Admins`。 |
| `Admins` | `aiotieba.api.del_bawu` | `IForumModule.DelBaWuAsync(string fname, string portrait, string baWuType, ...)` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)` | `迁移桥接` | 吧务成员移除属于 `Admins`。 |

### 已改名的规范项、同级族系项与形状项

| 所属模块 | 上游族系锚点 | 旧公开名称 | 归一化后的当前 C# 名称 | 类别 | 映射说明 |
| --- | --- | --- | --- | --- | --- |
| `Forums` | `aiotieba.api.get_self_follow_forums_v1` | `IForumModule.GetSelfFollowForumsPagedAsync(int pn = 1, int rn = 20, ...)` | `IForumModule.GetSelfFollowForumsV1Async(int pn = 1, int rn = 20, ...)` | `同级族系` | 这是与 `GetSelfFollowForumsAsync(...)` 并行支持的上游版本同级族系 API。 |
| `Forums` | `aiotieba.api.get_self_follow_forums_v1` | `SelfFollowForumsPaged` | `SelfFollowForumsV1` | `形状变体` | 受支持的 V1 同级族系容器。 |
| `Forums` | `aiotieba.api.get_self_follow_forums_v1` | `SelfFollowForumV1Item` | `SelfFollowForumV1` | `形状变体` | 受支持的 V1 同级族系项形状，与上游 `SelfFollowForumV1` 对齐。 |
| `Users` | `aiotieba.api.get_blacklist` | `IUserModule.GetBlacklistPermissionsAsync(...)` | `IUserModule.GetBlacklistAsync(...)` | `同级族系` | 在共享的 `Blacklist` 根下支持 `get_blacklist` 族系。 |
| `Users` | `aiotieba.api.set_blacklist` | `IUserModule.SetBlacklistPermissionsAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | `IUserModule.SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | `同级族系` | 在共享的 `Blacklist` 根下支持 `set_blacklist` 写入族系。 |
| `Users` | `aiotieba.api.get_blacklist_old` | `IUserModule.GetBlacklistMutedAsync(int pn = 1, int rn = 20, ...)` | `IUserModule.GetBlacklistOldAsync(int pn = 1, int rn = 20, ...)` | `同级族系` | 在共享的 `Blacklist` 根下支持 `_old` 同级族系。 |
| `Users` | `aiotieba.api.add_blacklist_old` | `IUserModule.AddBlacklistMutedAsync(long userId, ...)` | `IUserModule.AddBlacklistOldAsync(long userId, ...)` | `同级族系` | 受支持的 `_old` 同级变更族系。 |
| `Users` | `aiotieba.api.del_blacklist_old` | `IUserModule.RemoveBlacklistMutedAsync(long userId, ...)` | `IUserModule.RemoveBlacklistOldAsync(long userId, ...)` | `同级族系` | 受支持的 `_old` 同级变更族系。 |
| `Users` | `aiotieba.api.get_blacklist` | `BlacklistPermissions` | `BlacklistUsers` | `形状变体` | 当前族系的列表形状。 |
| `Users` | `aiotieba.api.get_blacklist` | `BlacklistPermission` | `BlacklistUser` | `形状变体` | 当前族系的单项形状。 |
| `Users` | `aiotieba.api.get_blacklist_old` | `BlacklistMutedUsers` | `BlacklistOldUsers` | `形状变体` | `_old` 列表形状。 |
| `Users` | `aiotieba.api.get_blacklist_old` | `BlacklistMutedUser` | `BlacklistOldUser` | `形状变体` | `_old` 单项形状。 |
| `Users` | `aiotieba.api.get_uinfo_getuserinfo_app` | `IUserModule.GetBasicInfoAppAsync(int userId, ...)` | `IUserModule.GetUserInfoAppAsync(int userId, ...)` | `同级族系` | 这是与 `GetUserInfoWebAsync(...)` 并行支持的 App `user_info` 同级族系。 |
| `Users` | `aiotieba.api.get_uinfo_getuserinfo_app` | `UserInfoApp` | `UserInfo` | `形状变体` | App `user_info` 同级项现在返回共享的公开 `UserInfo` 契约，而不是端点专属 DTO。 |
| `Users` | `aiotieba.api.get_uinfo_getUserInfo_web` | `UserInfoWeb` | `UserInfo` | `形状变体` | Web `user_info` 同级项同样返回共享的公开 `UserInfo` 契约，而不是单独的端点专属 DTO。 |
| `Users` | `aiotieba.api.set_nickname_old` | `IUserModule.SetNicknameLegacyAsync(string nickName, ...)` | `IUserModule.SetNicknameAsync(string nickName, ...)` | `同级族系` | 这是与 `SetProfileAsync(...)` 并列支持的单字段昵称写入族系。 |
| `Admins` | `aiotieba.api.add_bawu` | `IAdminModule.AddBaWuAsync(string fname, string userName, BawuType bawuType, ...)` | `IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType, ...)` | `规范操作` | 规范公开大小写已统一为 `Bawu`。 |
| `Admins` | `aiotieba.api.del_bawu` | `IAdminModule.DelBaWuAsync(string fname, string portrait, BawuType bawuType, ...)` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)` | `规范操作` | 规范公开拼写保留了上游 `del` 动词，同时继续保持 `Bawu` 根名称。 |
| `Users` | `aiotieba.api.get_user_contents.get_posts` | `UserPostss` | `UserPostGroups` | `形状变体` | 公开 DTO 中的异常命名已归一化，但不改变上游族系锚点。 |

## 贴吧核心 / 关注 / 反感族系

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `follow_forum` | `aiotieba.api.follow_forum` | `IForumModule.FollowAsync(ulong)` / `FollowAsync(string)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结并验证了关注写入族系的 app-form 路径、签名输入顺序与公开表面命名；`LikeAsync(...)` 旧别名仍然保持移除。 |
| `unfollow_forum` | `aiotieba.api.unfollow_forum` | `IForumModule.UnfollowAsync(ulong)` / `UnfollowAsync(string)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了 legacy `/c/c/forum/unfavolike` 路径，并验证未额外引入 `_client_version` 等漂移字段。 |
| `sign_forum` | `aiotieba.api.sign_forum` | `IForumModule.SignAsync(string)` | 已实现 | `论坛能力对齐` | `Forums` | `1. 贴吧基础/读取` | 当前对齐冻结了按吧名打包的单吧签到请求，并验证它继续保持上游 HTTP app-form 形状。 |
| `get_fid` | `aiotieba.api.get_fid` | `IForumModule.GetFidAsync(string)` | 已实现 | `论坛能力对齐` | `Forums` | `1. 贴吧基础/读取` | 当前对齐验证了贴吧标识读取仍然锚定在冻结的 web GET 端点与 `fname`/`ie` 查询形状上。 |
| `get_forum` (`Forum`) | `aiotieba.api.get_forum` | `IForumModule.GetForumAsync(string)` | 已实现 | `论坛能力对齐` | `Forums` | `1. 贴吧基础/读取` | 当前对齐冻结了 `frsBottom` 的 app host 与 HTTP scheme，防止漂移回 web host 或 HTTPS 变体。 |
| `get_forum_detail` (`Forum_detail`) | `aiotieba.api.get_forum_detail` | `IForumModule.GetDetailAsync(ulong)` 与 `IForumModule.GetDetailAsync(string)` | 已实现 | `论坛能力对齐` | `Forums` | `1. 贴吧基础/读取` | 当前对齐冻结了 forum-detail app-proto 路径、HTTP scheme，以及 `CommonReq` 中不携带额外 `client_type` 的上游语义。 |
| `dislike_forum` | `aiotieba.api.dislike_forum` | `IForumModule.DislikeAsync(ulong)` / `DislikeAsync(string)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了首页反感写入族系的 JSON 负载键、来源标志与签名输入顺序。 |
| `undislike_forum` | `aiotieba.api.undislike_forum` | `IForumModule.UndislikeAsync(ulong)` / `UndislikeAsync(string)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了反感回滚族系的 `BDUSS` / `cuid` / `forum_id` 打包顺序。 |
| `get_follow_forums` (`FollowForum` / `FollowForums`) | `aiotieba.api.get_follow_forums` | `IForumModule.GetFollowForumsAsync(long userId, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐验证了他人关注列表族系的认证 app-form 打包顺序与分页字段。 |
| `get_self_follow_forums` (`SelfFollowForum` / `SelfFollowForums`) | `aiotieba.api.get_self_follow_forums` | `IForumModule.GetSelfFollowForumsAsync(int pn = 1, int rn = 200, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了当前账号关注列表的 web form 字段、cookie 与 `Subapp-Type: hybrid` 头语义。 |
| `get_self_follow_forums_v1` (`SelfFollowForumsV1` / `SelfFollowForumV1`) | `aiotieba.api.get_self_follow_forums_v1` | `IForumModule.GetSelfFollowForumsV1Async(int pn = 1, int rn = 20, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了 V1 同级族系的 `/mg/o/getForumHome` web GET 形状，避免它被折叠进较新的 `_signed` 家族。 |
| `get_dislike_forums` (`DislikeForum` / `DislikeForums`) | `aiotieba.api.get_dislike_forums` | `IForumModule.GetDislikeForumsAsync(int pn = 1, int rn = 20, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了反感列表的 websocket-preferred→HTTP fallback 家族与 protobuf 分页负载。 |

## 贴吧发现 / 搜索 / 统计族系

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `get_cid` | `aiotieba.api.get_cid` | `IForumModule.GetCidAsync(string, string)` 与 `GetCidAsync(ulong, string)`；内部 `Api/GetCid/GetCid.cs` 会被 forum protocol 和 `IThreadModule.GoodAsync(...)` 复用。 | 已实现 | `论坛发现能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前公开表面把现有内部移植提升到贴吧发现能力上，而不重复实现 API。 |
| `get_images` (`Image` / `ImageBytes`) | `aiotieba.api.get_images` | `IForumModule.GetImageBytesAsync(string)`、`GetImageAsync(string)`、`GetImageByHashAsync(string, ForumImageSize)` 与 `GetPortraitAsync(string, ForumImageSize)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了图片族系的 `Referer: tieba.baidu.com`、HTTP host/scheme 与 hash/portrait URL 家族，确保不偏离冻结上游。 |
| `search_exact` (`ExactSearches`) | `aiotieba.api.search_exact` | `IForumModule.SearchExactAsync(string, ...)` 与 `SearchExactAsync(ulong, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了精确搜索的 app-form 字段顺序、排序枚举与分页打包语义。 |
| `get_last_replyers` (`Thread_lp` / `Threads_lp` / `UserInfo_lp`) | `aiotieba.api.get_last_replyers` | `IForumModule.GetLastReplyersAsync(string, ...)` 与 `GetLastReplyersAsync(ulong, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了该族系优先 WS、回退 HTTP 的传输顺序，以及 app-proto `frs/page` 负载形状。 |
| `get_member_users` (`MemberUser` / `MemberUsers`) | `aiotieba.api.get_member_users` | `IForumModule.GetMemberUsersAsync(string, ...)` 与 `GetMemberUsersAsync(ulong, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了成员列表读取的 bawu web GET 查询、header 与 cookie 门槛。 |
| `get_rank_forums` (`RankForum` / `RankForums`) | `aiotieba.api.get_rank_forums` | `IForumModule.GetRankForumsAsync(string, ...)` 与 `GetRankForumsAsync(ulong, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了签到排行页的 `/sign/index` 路径、查询参数与 web header 语义。 |
| `get_recom_status` (`RecomStatus`) | `aiotieba.api.get_recom_status` | `IForumModule.GetRecomStatusAsync(string)` 与 `GetRecomStatusAsync(ulong)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了推荐状态读取的 app-form 路径和分页哨兵值。 |
| `get_square_forums` (`SquareForum` / `SquareForums`) | `aiotieba.api.get_square_forums` | `IForumModule.GetSquareForumsAsync(string cname, ...)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了该发现族系的 websocket-preferred→HTTP fallback 顺序与 class-name protobuf 负载。 |
| `get_statistics` (`Statistics`) | `aiotieba.api.get_statistics` | `IForumModule.GetStatisticsAsync(string)` 与 `GetStatisticsAsync(ulong)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了统计读取的 app-form 路径，以及从旧到新的时间序列顺序映射。 |
| `get_forum_level` (`LevelInfo`) | `aiotieba.api.get_forum_level` | `IForumModule.GetForumLevelAsync(string)` 与 `GetForumLevelAsync(ulong)` | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了 level-info app-proto 请求与发起前的 self-info bootstrap 前置链。 |
| `get_roomlist_by_fid` (`RoomList`) | `aiotieba.api.get_roomlist_by_fid` | `IForumModule.GetRoomListByFidAsync(ulong fid, ...)`，通过 `ForumProtocol.GetRoomListByFidAsync(...)`、`Api/GetRoomListByFid/GetRoomListByFid.cs` 与 `Models/Forums/RoomList.cs` 暴露 | 已实现 | `论坛能力对齐` | `Forums` | `2. 贴吧关注/反感/搜索/统计` | 当前对齐冻结了 chat `12.68.1.0` 客户端版本与 `data.list[*].room_list[*]` 的扁平化公开语义。 |

## 吧务 / 管理 / 申诉 / 封禁管理族系

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `add_bawu` | `aiotieba.api.add_bawu` | `IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType, ...)`，通过 `AdminProtocol.AddBawuAsync(...)` 与 `Api/AddBaWu/AddBaWu.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认公开 `Bawu` 根继续直接对齐上游 `add_bawu`，同时保持内部可按上游搜索的 `Api/AddBaWu` 族系不变。 |
| `add_bawu_blacklist` | `aiotieba.api.add_bawu_blacklist` | `IAdminModule.AddBawuBlacklistAsync(string fname, long userId, ...)`，通过 `AdminProtocol.AddBawuBlacklistAsync(...)` 与 `Api/AddBawuBlacklist/AddBawuBlacklist.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认 `Admins` 上的 bawu 黑名单添加继续保留上游 `errno` / `errmsg` 形状与 HTTP 表单端点。 |
| `del_bawu` | `aiotieba.api.del_bawu` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)`，通过 `AdminProtocol.DelBawuAsync(...)` 与 `Api/DelBawu/DelBaWu.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认 bawu 移除能力继续只留在 `Admins` 上，而公开 `Bawu` 根仍可直接追溯到上游 `del_bawu`。 |
| `del_bawu_blacklist` | `aiotieba.api.del_bawu_blacklist` | `IAdminModule.DelBawuBlacklistAsync(string fname, long userId, ...)`，通过 `AdminProtocol.DelBawuBlacklistAsync(...)` 与 `Api/DelBawuBlacklist/DelBawuBlacklist.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认 bawu 黑名单移除继续保留上游 `list[]` 表单打包和 `errno` / `errmsg` 响应形状。 |
| `get_bawu_blacklist` (`BawuBlacklistUser` / `BawuBlacklistUsers`) | `aiotieba.api.get_bawu_blacklist` | `IAdminModule.GetBawuBlacklistAsync(string fname, int pn = 1, ...)`，通过 `AdminProtocol.GetBawuBlacklistAsync(...)` 与 `Api/GetBawuBlacklist/GetBawuBlacklist.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认专用 `Admins` 公开模块继续承载 bawu 黑名单读取，包括 HTML 列表解析和页面元数据映射。 |
| `get_bawu_info` (`BawuInfo` / `UserInfo_bawu`) | `aiotieba.api.get_bawu_info` | `IAdminModule.GetBawuInfoAsync(string fname, ...)`，通过 `AdminProtocol.GetBawuInfoAsync(...)` 与 `Api/GetBawuInfo/GetBawuInfo.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认 bawu 团队信息继续保留上游优先 WebSocket、回退 HTTP 的传输形状。 |
| `get_bawu_perm` (`BawuPerm`) | `aiotieba.api.get_bawu_perm` | `IAdminModule.GetBawuPermAsync(string fname, string portrait, ...)`，通过 `AdminProtocol.GetBawuPermAsync(...)` 与 `Api/GetBawuPerm/GetBawuPerm.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认 bawu 权限读取继续保留上游 `no` / `error` JSON 形状和标志映射。 |
| `get_bawu_postlogs` (`Postlog` / `Postlogs`) | `aiotieba.api.get_bawu_postlogs` | `IAdminModule.GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null, ...)`，通过 `AdminProtocol.GetBawuPostLogsAsync(...)` 与 `Api/GetBawuPostlogs/GetBawuPostlogs.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认 bawu 帖子日志读取继续保持与上游一致的过滤项打包（`op_type`、`svalue`、`stype`、`begin`、`end`）以及 HTML/媒体映射。 |
| `get_bawu_userlogs` (`Userlog` / `Userlogs`) | `aiotieba.api.get_bawu_userlogs` | `IAdminModule.GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null, ...)`，通过 `AdminProtocol.GetBawuUserLogsAsync(...)` 与 `Api/GetBawuUserlogs/GetBawuUserlogs.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认 bawu 用户日志读取继续保持从上游 HTML 形状中进行搜索类型归一化以及时长/页码解析。 |
| `set_bawu_perm` | `aiotieba.api.set_bawu_perm` | `IAdminModule.SetBawuPermAsync(string fname, string portrait, BawuPermType permissions, ...)`，通过 `AdminProtocol.SetBawuPermAsync(...)` 与 `Api/SetBawuPerm/SetBawuPerm.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认 bawu 权限写入继续保留上游权限顺序 JSON 负载（`4,5,3,2`）以及当前仅认证可用的传输门槛。 |
| `get_unblock_appeals` (`Appeal` / `Appeals`) | `aiotieba.api.get_unblock_appeals` | `IAdminModule.GetUnblockAppealsAsync(string fname, int pn = 1, int rn = 20, ...)`，通过 `AdminProtocol.GetUnblockAppealsAsync(...)` 与 `Api/GetUnblockAppeals/GetUnblockAppeals.cs` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认解封申诉读取继续保留上游表单负载和 JSON 申诉列表映射。 |
| `handle_unblock_appeals` | `aiotieba.api.handle_unblock_appeals` | `IAdminModule.HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false, ...)`，通过 `AdminProtocol.HandleUnblockAppealsAsync(...)` 与 `Api/HandleUnblockAppeals/HandleUnblockAppeals.cs` | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认解封申诉处理继续保留带索引的 `appeal_list[i]` 表单打包和上游 `status` 映射（`1` 接受 / `2` 拒绝）。 |
| `get_blocks` (`Block` / `Blocks`) | `aiotieba.api.get_blocks` | `IAdminModule.GetBlocksAsync(string fname, string userName = "", int pn = 1, ...)`，通过 `AdminProtocol.GetBlocksAsync(...)` 与 `Api/GetBlocks/GetBlocks.cs` | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认封禁列表读取继续保留上游 JSON 加内嵌 HTML 的页面/内容形状。 |
| `block` | `aiotieba.api.block` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)`，通过 `AdminProtocol.BlockAsync(...)` 暴露 | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认封禁管理继续仅保留在 `Admins` 上，并把 `/c/c/bawu/commitprison` 的 app-form scheme 固定回上游 `http`。 |
| `unblock` | `aiotieba.api.unblock` | `IAdminModule.UnblockAsync(string fname, long userId, ...)`，通过 `AdminProtocol.UnblockAsync(...)` 与 `Api/Unblock/Unblock.cs` | 已实现 | `管理能力对齐` | `Admins` | `7. 破坏性清理/回滚验证` | 当前对齐确认解封操作继续保留上游 `block_un=-` / `block_uid` 表单负载以及认证与 TBS 门槛。 |

## 主题读取 / 恢复 / tab-map / 媒体族系

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `get_threads` (`ShareThread` / `Thread` / `Threads` / `UserInfo_t`) | `aiotieba.api.get_threads` | `IThreadModule.GetThreadsAsync(string, ...)` 与 `GetThreadsAsync(ulong, ...)` | 已实现 | `主题能力对齐` | `Threads` | `3. 主题读取/评论/tab-map/恢复检查` | 当前对齐冻结了这个主题读取族系的匿名 WS bootstrap 与 `FrsPageReqIdl` 上游打包语义：首页保持 `pn=0`、`rn_need=rn+5`；当前治理 proof 还显式用 `TiebaTransportMode.WebSocketOnly` 封死 `/c/f/frs/page?cmd=301001`，验证真实线上不会把 HTTP fallback 当作正确性故事。 |
| `get_posts` (`Comment_p` / `Post` / `Posts` / `Thread_p` / `UserInfo_p` / `UserInfo_pt`) | `aiotieba.api.get_posts` | `IThreadModule.GetPostsAsync(...)` | 已实现 | `主题能力对齐` | `Threads` | `3. 主题读取/评论/tab-map/恢复检查` | 当前对齐冻结了楼层读取的 WS 优先、HTTP 回退顺序，以及 `rn>=2`、`lz`、`with_floor`/`floor_rn`/`floor_sort_type` 与仅在楼中楼预览时携带 `BDUSS` 的上游 protobuf 语义；当前治理 proof 还显式用 `TiebaTransportMode.WebSocketOnly` 封死 `/c/f/pb/page?cmd=302001`，验证 `get_posts` 能在线上真实走 websocket。 |
| `get_comments` (`Comment` / `Comments` / `Post_c` / `Thread_c` / `UserInfo_c` / `UserInfo_cp` / `UserInfo_ct`) | `aiotieba.api.get_comments` | `IThreadModule.GetCommentsAsync(...)` | 已实现 | `主题能力对齐` | `Threads` | `3. 主题读取/评论/tab-map/恢复检查` | 当前对齐冻结了楼中楼读取的 WS 优先、HTTP 回退顺序，以及仅通过 `is_comment` 在 `pid` / `spid` 之间切换的上游分支语义。 |
| `get_recovers` (`Recover` / `Recovers`) | `aiotieba.api.get_recovers` | `IThreadModule.GetRecoversAsync(string, ...)` 与 `GetRecoversAsync(ulong, ...)` | 已实现 | `主题能力对齐` | `Threads` | `3. 主题读取/评论/tab-map/恢复检查` | 当前对齐冻结了恢复列表读取属于 restricted moderation lane，而不是 safe 近似路径；它继续保持上游分页和可选 `uid` 过滤语义。 |
| `get_recover_info` (`RecoverInfo`) | `aiotieba.api.get_recover_info` | `IThreadModule.GetRecoverInfoAsync(string, long tid, long pid = 0, ...)` 与 `GetRecoverInfoAsync(ulong, long tid, long pid = 0, ...)` | 已实现 | `主题能力对齐` | `Threads` | `3. 主题读取/评论/tab-map/恢复检查` | 当前对齐冻结了恢复详情读取属于 restricted moderation lane，并验证 `sub_type=1/2` 仍然只由 `pid` 是否为零决定。 |
| `recover` | `aiotieba.api.recover` | `IThreadModule.RecoverAsync(string fname, long tid = 0, long pid = 0, bool isHide = false, ...)` | 已实现 | `主题能力对齐` | `Threads` | `3. 主题读取/评论/tab-map/恢复检查` | 当前对齐冻结了恢复写入属于 restricted moderation lane，且继续保持 `type_list[]`、`is_frs_mask_list[]` 与线程/回复分支的 pid-first 顺序语义。 |
| `get_tab_map` (`TabMap`) | `aiotieba.api.get_tab_map` | `IThreadModule.GetTabMapAsync(string, ...)` 与 `GetTabMapAsync(ulong, ...)` | 已实现 | `主题能力对齐` | `Threads` | `3. 主题读取/评论/tab-map/恢复检查` | 当前对齐冻结了 tab-map 读取的 WS 优先、HTTP 回退顺序，以及仅依赖 `BDUSS` / `client_version` 而不引入额外 `TBS` 门槛的上游请求语义。 |

## 主题写入 / 管理 / 签到族系

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `add_post` | `aiotieba.api.add_post` | `IThreadModule.AddPostAsync(string fname, long tid, string content, string? showName = null, ...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 标成了已完成；台账保留了精确的上游写入族系和当前可选 `showName` 映射。 |
| `agree` | `aiotieba.api.agree` | `IThreadModule.AgreeAsync(...)`、`DisagreeAsync(...)`、`UnagreeAsync(...)` 与 `UndisagreeAsync(...)` 都经由同一个上游族系路由。 | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了统一 `agree` 旗标族系：`agree_type` 负责赞/踩，`obj_type` 负责主题/楼层/楼中楼，`op_type` 负责撤销，不再把这些入口误记成几个互不相关的实现。 |
| `del_post` | `aiotieba.api.del_post` | `IThreadModule.DelPostAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 标成了已完成；台账保留了精确的上游族系。 |
| `del_posts` | `aiotieba.api.del_posts` | `IThreadModule.DelPostsAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了批量删楼层的 restricted moderation lane、逗号拼接 id 载荷，以及仅在 `block=true` 时把 `type` 切到 `2` 的上游语义。 |
| `del_thread` | `aiotieba.api.del_thread` | `IThreadModule.DelThreadAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐确认单删主题帖当前只有 restricted moderation lane 拥有完整的 recover-proof 路径，不再把它描述成 safe 可替代验证。 |
| `del_threads` | `aiotieba.api.del_threads` | `IThreadModule.DelThreadsAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了批量删主题帖的 restricted moderation lane、逗号拼接 id 载荷，以及仅在 `block=true` 时把 `type` 切到 `2` 的上游语义。 |
| `good` | `aiotieba.api.good` | `IThreadModule.GoodAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了加精写入的 restricted moderation lane，并验证它继续通过 `cid` 承载可选 `cname` 解析结果。 |
| `ungood` | `aiotieba.api.ungood` | `IThreadModule.UngoodAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了取消加精写入的 restricted moderation lane，并验证它继续复用上游 `commitgood` 端点而不携带 `cid` / `ntn=set`。 |
| `top` | `aiotieba.api.top` | `IThreadModule.TopAsync(...)` 与 `UntopAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了置顶/撤销置顶的 restricted moderation lane，并验证 `ntn=set` 仍然只属于正向置顶路径。 |
| `move` | `aiotieba.api.move` | `IThreadModule.MoveAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了移动分区写入的 restricted moderation lane，并验证它继续把 `from_tab_id` / `to_tab_id` 保存在同一个 JSON `threads` 字段里。 |
| `recommend` | `aiotieba.api.recommend` | `IThreadModule.RecommendAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了推荐写入的 restricted moderation lane，并验证它继续保持 BDUSS-only 语义而不额外引入 `TBS`。 |
| `set_thread_privacy` | `aiotieba.api.set_thread_privacy` | `IThreadModule.SetThreadPrivacyAsync(...)` | 已实现 | `主题写入能力对齐` | `Threads` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了楼层隐私写入的 restricted moderation lane，并验证它继续通过 `is_hide` 表达隐私开关而不额外引入 `TBS`。 |
| `sign_forums` | `aiotieba.api.sign_forums` | `IForumModule.SignForumsAsync(...)`，通过 `ForumProtocol.SignForumsAsync(...)` 与 `Api/SignForums/SignForums.cs` | 已实现 | `论坛能力对齐` | `Forums` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了批量签到族系的 web-form 字段顺序、cookie 形状与 `Subapp-Type: hybrid` 请求头。 |
| `sign_growth` | `aiotieba.api.sign_growth` | `IForumModule.SignGrowthAsync(...)`，通过 `ForumProtocol.SignGrowthAsync(...)` 与 `Api/SignGrowth/SignGrowth.cs` | 已实现 | `论坛能力对齐` | `Forums` | `4. 主题写入/管理/签到族系` | 当前对齐冻结了成长签到族系的 HTTPS web-form 路径与 `act_type=page_sign` 打包语义。 |

## 用户 / 社交 / 资料 / 旧式族系

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `add_blacklist_old` | `aiotieba.api.add_blacklist_old` | `IUserModule.AddBlacklistOldAsync(long userId, ...)`，通过 `UserProtocol.AddBlacklistOldAsync(...)` 与 `Api/AddBlacklistOld/AddBlacklistOld.cs` 暴露 | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `_old` 黑名单添加变更作为受支持的同级族系继续公开存在，而不是并入 `SetBlacklistAsync(...)` 表面。 |
| `del_blacklist_old` | `aiotieba.api.del_blacklist_old` | `IUserModule.RemoveBlacklistOldAsync(long userId, ...)`，通过 `UserProtocol.RemoveBlacklistOldAsync(...)` 与 `Api/DelBlacklistOld/DelBlacklistOld.cs` 暴露 | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `_old` 黑名单流程中的删除半段仍然是一级同级族系，这样调用方就能把它与权限写入语义区分开来。 |
| `follow_user` | `aiotieba.api.follow_user` | `IUserModule.FollowAsync(string portrait, ...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `docs/archive/todo.md` 只跟踪了友好的动词 `follow`，所以那份历史积压过于粗糙，不能作为权威对齐台账。 |
| `unfollow_user` | `aiotieba.api.unfollow_user` | `IUserModule.UnfollowAsync(string portrait, ...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `docs/archive/todo.md` 只跟踪了友好的动词 `unfollow`，所以那份历史积压名称没有保留精确的上游包身份。 |
| `get_ats` (`At` / `Ats`) | `aiotieba.api.get_ats` | `IMessagesModule.GetAtsAsync(int pn = 1, ...)`，通过 `MessagesProtocol.GetAtsAsync(...)` 与 `Api/GetAts/GetAts.cs` 暴露 | 已实现 | `消息归属对齐` | `Messages` | `5. 用户/社交/资料` | 收件箱 `@` 读取现在只保留在 `Messages` 上；旧的 `Users.GetAtsAsync(...)` 桥接已从公开表面移除。 |
| `get_blacklist` (`BlacklistUser` / `BlacklistUsers`) | `aiotieba.api.get_blacklist` | `IUserModule.GetBlacklistAsync(...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `get_blacklist` 族系保持独立，并使用共享的 `Blacklist` 根，这样在概念上不会和 `_old` 同级族系混在一起。 |
| `get_blacklist_old` (`BlacklistOldUser` / `BlacklistOldUsers`) | `aiotieba.api.get_blacklist_old` | `IUserModule.GetBlacklistOldAsync(int pn = 1, int rn = 20, ...)`，通过 `UserProtocol.GetBlacklistOldAsync(...)` 与 `Api/GetBlacklistOld/GetBlacklistOld.cs` 暴露 | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `_old` 黑名单读取族系保留了自己的分页/模型形状，以及优先 WS、回退 HTTP 的传输行为，而不是被折叠进 `GetBlacklistAsync(...)`。 |
| `get_fans` (`Fan` / `Fans`) | `aiotieba.api.get_fans` | `IUserModule.GetFansAsync(long userId, int pn = 1, ...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `docs/archive/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `GetFansAsync`，所以那份历史积压已经过时。 |
| `get_follows` (`Follow` / `Follows`) | `aiotieba.api.get_follows` | `IUserModule.GetFollowsAsync(long userId, int pn = 1, ...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `docs/archive/todo.md` 标成了已完成，但台账现在保留了精确的上游包身份。 |
| `get_rank_users` (`RankUser` / `RankUsers`) | `aiotieba.api.get_rank_users` | `IUserModule.GetRankUsersAsync(string fname, int pn = 1, ...)`，通过 `UserProtocol.GetRankUsersAsync(...)` 与 `Api/GetRankUsers/GetRankUsers.cs` 暴露 | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | v3 user module 现在直接暴露上游 HTML rank-users 族系，并提供专用公开模型 `RankUser` / `RankUsers` 与针对行/页解析的确定性覆盖。 |
| `get_replys` (`Reply` / `Replys`) | `aiotieba.api.get_replys` | `IMessagesModule.GetRepliesAsync(int pn = 1, ...)`，通过 `MessagesProtocol.GetRepliesAsync(...)` 与 `Api/GetReplys/GetReplys.cs` 暴露 | 已实现 | `消息归属对齐` | `Messages` | `5. 用户/社交/资料` | 回复收件箱读取现在只保留在 `Messages` 上；旧的 `Users.GetRepliesAsync(...)` 桥接已经移除，即使上游族系仍然保留了不规则的 `Replys` 拼写。 |
| `get_selfinfo_initNickname` | `aiotieba.api.get_selfinfo_initNickname` | `IUserModule.GetSelfInfoInitNicknameAsync(...)`，通过 `UserProtocol.GetSelfInfoInitNicknameAsync(...)` 与 `Api/GetSelfInfoInitNickname/GetSelfInfoInitNickname.cs` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | v3 用户表面现在让这个直接兼容族系与合并后的便捷入口 `GetSelfInfoAsync()` 并存可用。 |
| `get_selfinfo_moindex` (`UserInfo_moindex`) | `aiotieba.api.get_selfinfo_moindex` | `IUserModule.GetSelfInfoMoIndexAsync(...)`，通过 `UserProtocol.GetSelfInfoMoIndexAsync(...)` 与 `Api/GetSelfInfoMoIndex/GetSelfInfoMoIndex.cs` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | 这个直接的 moindex 族系现在被单独暴露出来，而不再只作为内部合并输入存在。 |
| `get_uinfo_getuserinfo_app` (`UserInfo_guinfo_app`) | `aiotieba.api.get_uinfo_getuserinfo_app` | `IUserModule.GetUserInfoAppAsync(int userId, ...)`，通过 `UserProtocol.GetUserInfoAppAsync(...)`、`Api/GetUInfoGetUserInfoApp/GetUInfoGetUserInfoApp.cs` 与公开 `UserInfo` 暴露 | 已实现 | `用户能力对齐` | `Users` | `5. 用户/社交/资料` | App `user_info` 族系在共享的 `GetUserInfo` 根下继续保持公开，而当前公开契约有意复用 `UserInfo`；当前对齐冻结了它优先 WS、回退 HTTP 的传输行为。 |
| `get_uinfo_getUserInfo_web` (`UserInfo_guinfo_web`) | `aiotieba.api.get_uinfo_getUserInfo_web` | `IUserModule.GetUserInfoWebAsync(int userId, ...)`，通过 `UserProtocol.GetUserInfoWebAsync(...)`、`Api/GetUInfoGetUserInfoWeb/GetUInfoGetUserInfoWeb.cs` 与公开 `UserInfo` 暴露 | 已实现 | `用户能力对齐` | `Users` | `5. 用户/社交/资料` | Web `user_info` 同级项同样保留在共享公开 `UserInfo` 契约上，而不是单独的端点专属 DTO；当前对齐冻结了它必须带 BDUSS 的认证前置条件。 |
| `get_uinfo_panel` (`UserInfo_panel`) | `aiotieba.api.get_uinfo_panel` | `IUserModule.GetPanelInfoAsync(string nameOrPortrait, ...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `docs/archive/todo.md` 标成了已完成；台账保留了精确的上游族系。 |
| `get_uinfo_user_json` (`UserInfo_json`) | `aiotieba.api.get_uinfo_user_json` | `IUserModule.GetUserInfoJsonAsync(string username, ...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `docs/archive/todo.md` 标成了已完成；台账保留了精确的上游族系。 |
| `get_user_contents` (`get_posts` / `get_threads` / `UserPost` / `UserPosts` / `UserPostss` / `UserThread` / `UserThreads` / `CMD`) | `aiotieba.api.get_user_contents` | `IUserModule.GetPostsAsync(...)`、`IUserModule.GetThreadsAsync(...)`、公开模型 `UserPost` / `UserPosts` / `UserPostGroups` / `UserThread` / `UserThreads`，以及根辅助项 `UserContent.Cmd` 与 `IUserModule.UserContentCmd` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | 公开 C# DTO 中的异常命名已归一化为 `UserPostGroups`，而底层上游族系和内部请求命名仍然可以追溯到 `UserPostss`。 |
| `get_user_contents.get_posts` | `aiotieba.api.get_user_contents.get_posts` | `IUserModule.GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5", ...)` | 已实现 | `用户能力对齐` | `Users` | `5. 用户/社交/资料` | 当前对齐冻结了显式 user `posts` 分支的独立语义：继续使用上游 `8.9.8.5` 版本字符串，并直接走 HTTP，而不是借用 self-session 的 WS 分支。 |
| `get_user_contents.get_threads` | `aiotieba.api.get_user_contents.get_threads` | `IUserModule.GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true, ...)` | 已实现 | `用户能力对齐` | `Users` | `5. 用户/社交/资料` | 当前对齐冻结了 `threads` 分支与 `posts` 分离的公开语义，以及它优先 WS、回退 HTTP 的传输行为。 |
| `get_user_forum_info` (`UserForumInfo` / `UserInfo_uf`) | `aiotieba.api.get_user_forum_info` | `IUserModule.GetUserForumInfoAsync(ulong fid, string portrait, ...)` 与 `GetUserForumInfoAsync(string fname, string portrait, ...)`，通过 `UserProtocol.GetUserForumInfoAsync(...)` 与 `Api/GetUserForumInfo/GetUserForumInfo.cs` 暴露 | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | user module 现在暴露这个需要认证的吧内用户信息族系，并同时提供 fid 和贴吧名两个重载；确定性覆盖冻结了认证预检、fid 解析和 JSON 映射。 |
| `login` | `aiotieba.api.login` | `IUserModule.LoginAsync(...)`，通过 `UserProtocol.LoginAsync(...)`、`Api/Login/Login.cs` 与公开 `Models/Users/LoginResult.cs` 暴露 | 已实现 | `用户能力对齐` | `Users` | `5. 用户/社交/资料` | 这个兼容登录族系现在在公开表面同时返回用户信息和 TBS；当前对齐冻结了 HTTP `/c/s/login` 请求和“仅成功后更新会话 TBS”的规则。 |
| `profile` (`get_homepage` / `get_uinfo_profile` / `Homepage` / `Thread_pf` / `UserInfo_pf` / `CMD`) | `aiotieba.api.profile` | `IUserModule.GetProfileAsync(int)` / `GetProfileAsync(string)` 覆盖 `profile.get_uinfo_profile`，而 `IUserModule.GetHomepageAsync(int userId, int pn = 1, ...)` 覆盖 `profile.get_homepage`。 | 已实现 | `用户能力对齐` | `Users` | `5. 用户/社交/资料` | 这些嵌套 profile 族系在公开表面上明确保持分离：`GetProfileAsync(...)` 读取资料元数据，`GetHomepageAsync(...)` 读取主页内容和主页拥有者快照。 |
| `profile.get_homepage` | `aiotieba.api.profile.get_homepage` | `IUserModule.GetHomepageAsync(int userId, int pn = 1, ...)`，通过 `UserProtocol.GetHomepageAsync(...)` 与 `Api/Profile/GetHomepage/GetHomepage.cs` | 已实现 | `用户能力对齐` | `Users` | `5. 用户/社交/资料` | 主页族系现在保留了上游优先 WS、回退 HTTP 的行为，并使用主页专属 `pn` 分页字段，把拥有者快照与返回的主题列表分开映射。 |
| `profile.get_uinfo_profile` | `aiotieba.api.profile.get_uinfo_profile` | `IUserModule.GetProfileAsync(int)` 与 `IUserModule.GetProfileAsync(string)`，通过 `AioTieba4DotNet.Api.Profile.GetUInfoProfile.GetUInfoProfile<T>` | 已实现 | `用户能力对齐` | `Users` | `5. 用户/社交/资料` | `profile` 资料元数据分支继续使用它自己的 `page=1` 请求语义，并保留上游优先 WS、回退 HTTP 的行为，而不是被 `homepage` 分页合并。 |
| `remove_fan` | `aiotieba.api.remove_fan` | `IUserModule.RemoveFanAsync(long userId, ...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `docs/archive/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `RemoveFanAsync`，所以那份历史积压已经过时。 |
| `set_blacklist` | `aiotieba.api.set_blacklist` | `IUserModule.SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | `set_blacklist` 写入族系继续与 `_old` 添加/删除同级族系分离，这样调用方就能显式选择正确的传输方式和语义。 |
| `set_nickname_old` | `aiotieba.api.set_nickname_old` | `IUserModule.SetNicknameAsync(string nickName, ...)`，通过 `UserProtocol.SetNicknameAsync(...)` 与 `Api/SetNicknameOld/SetNicknameOld.cs` | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | 这个单字段昵称变更仍然作为独立写入族系公开存在，使用归一化后的 `SetNicknameAsync(...)` 名称，而 `SetProfileAsync(...)` 继续负责更广泛的资料元数据更新路径。 |
| `set_profile` | `aiotieba.api.set_profile` | `IUserModule.SetProfileAsync(string nickName, string sign, Gender gender, ...)`，通过 `UserProtocol.SetProfileAsync(...)` 与 `Api/SetProfile/SetProfile.cs` 暴露 | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | 当前资料变更族系与 `set_nickname_old` 分开实现，保留推荐路径和旧式路径在资料写入上的区分，而不是把两者压平成一个方法。 |
| `tieba_uid2user_info` (`UserInfo_TUid`) | `aiotieba.api.tieba_uid2user_info` | `IUserModule.GetUserByTiebaUidAsync(long tiebaUid, ...)`，通过 `UserProtocol.GetUserByTiebaUidAsync(...)`、`Api/TiebaUid2UserInfo/TiebaUid2UserInfo.cs` 与公开 `UserInfo` 暴露 | 已实现 | `用户与社交能力对齐` | `Users` | `5. 用户/社交/资料` | 这个 tieba-uid 查询族系保留了上游传输形状，同时复用共享的公开 `UserInfo` 契约，而不是使用端点专属 DTO。 |

## 消息 / 推送 / 客户端生命周期族系

Messages 族系继续保持当前活动语义：`get_replys` 走 websocket-preferred / HTTP fallback，group cursor bootstrap 保留初始化顺序，`send_chatroom_msg` 维持 richer mention 形状，`set_msg_readed` 只复用 websocket bootstrap 得到的 private group id，`push_notify` 继续保持纯 parser 语义。

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `get_group_msg` (`UserInfo_ws` / `WsMessage` / `WsMsgGroup` / `WsMsgGroups`) | `aiotieba.api.get_group_msg` | `IMessagesModule.GetGroupMessagesAsync(...)`，通过 `MessagesProtocol`、`Api/GetGroupMsg/GetGroupMsg.cs` 与 `Models/Messages/*` 暴露 | 已实现 | `消息与推送能力对齐` | `Messages` | `6. 消息/推送/客户端生命周期` | 当前对齐补充冻结了 websocket bootstrap cursor 顺序语义：当前实现继续复用初始化出的 group 游标，并避免人为排序导致的上游顺序漂移。 |
| `init_websocket` (`WsMsgGroupInfo`) | `aiotieba.api.init_websocket` | 生命周期 `IClientModule.InitWebSocketAsync(...)`，加上供 `MessagesProtocol` 游标引导消费的内部 `Api/InitWebSocket/InitWebSocket.cs` / `Models/Messages/WsMsgGroupInfo` | 已实现 | `客户端生命周期对齐` | `Client` | `6. 消息/推送/客户端生命周期` | 当前对齐让 `init_websocket` 在 `Client` 上继续保持纯生命周期职责，同时仍然移植了上游响应族系，这样新的 `Messages` 模块就能初始化消息组游标，而不会模糊公开边界。 |
| `init_z_id` | `aiotieba.api.init_z_id` | `IClientModule.InitZIdAsync(...)` | 已实现 | `客户端生命周期对齐` | `Client` | `6. 消息/推送/客户端生命周期` | `docs/todo.md` 标成了已完成；台账保留了精确的上游生命周期族系。 |
| `push_notify` (`WsNotify`) | `aiotieba.api.push_notify` | `IMessagesModule.ParsePushNotifications(byte[] payload)`，通过 `Api/PushNotify/PushNotify.cs` 与 `Models/Messages/WsNotify.cs` 暴露 | 已实现 | `消息与推送能力对齐` | `Messages` | `6. 消息/推送/客户端生命周期` | 当前对齐让上游语义保持清晰可见：`push_notify` 在 `Messages` 上被暴露为纯解析入口，而不是推测性的事件总线或后台订阅框架。 |
| `send_chatroom_msg` | `aiotieba.api.send_chatroom_msg` | `IMessagesModule.SendChatroomMessageAsync(...)`，通过 `MessagesProtocol` 与内部 `Transport/Chatrooms/BlcpChatroomSender.cs` 暴露 | 已实现 | `消息与推送能力对齐` | `Messages` | `6. 消息/推送/客户端生命周期` | 当前对齐补充冻结了 chatroom mention 解析与 BLCP `at_data` 形状：公开表面仍只接收 `atUserIds`，但内部现在会解析昵称/portrait/position 并生成 richer upstream payload。 |
| `send_msg` | `aiotieba.api.send_msg` | `IMessagesModule.SendMessageAsync(long, ...)` / `SendMessageAsync(string, ...)`，通过 `MessagesProtocol` 与 `Api/SendMsg/SendMsg.cs` 暴露 | 已实现 | `消息与推送能力对齐` | `Messages` | `6. 消息/推送/客户端生命周期` | 当前对齐补充冻结了两个发送入口的 parity：numeric send 继续沿用 private-group `record_id`，name/portrait overload 先解析 user id 再复用同一 websocket 发送路径。 |
| `set_msg_readed` | `aiotieba.api.set_msg_readed` | `IMessagesModule.SetMessageReadAsync(WsMessage, ...)`，通过 `MessagesProtocol` 与 `Api/SetMsgReaded/SetMsgReaded.cs` 暴露 | 已实现 | `消息与推送能力对齐` | `Messages` | `6. 消息/推送/客户端生命周期` | 当前对齐明确收紧为“只允许 private-message websocket item，并且只复用 bootstrap 得到的 private group id”，不再退回消息对象自带的 `GroupId`。 |
| `sync` | `aiotieba.api.sync` | `IClientModule.SyncAsync(...)` | 已实现 | `客户端生命周期对齐` | `Client` | `6. 消息/推送/客户端生命周期` | `docs/todo.md` 标成了已完成；台账保留了精确的上游生命周期族系。 |

## 来自上游 API 树的支撑 / 导出族系

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 集成阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `_classdef` (`Account`, `TypeMessage`, `Containers`, `Frag*`, `TypeFrag*`, `UserInfo`, `VoteInfo`) | `aiotieba.api._classdef` | 公开支撑模型分布在 `AioTieba4DotNet.Models.*`、`AioTieba4DotNet.Models.Shared`、`AioTieba4DotNet.Models.Messages.MessageEnums.cs` 与 `AioTieba4DotNet.Contracts.Account` 中 | 已实现 | `文档与公开表面对齐` | `Docs` | `不适用 - 仅文档对齐` | v3 把这些支撑导出保留为公开根/契约/模型类型，而不是复刻上游包布局；目前剩余的缺口已经由 `FragUnknown`、公开 `Account` 和 websocket message/group 枚举补齐。 |

## 影响对齐范围的顶层 `aiotieba` 包导出

| 功能 | 上游模块 | C# 表面 | 状态 | 对齐阶段 | 维护责任域 | 集成阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 顶层 `Client` 门面 | `aiotieba.__init__ -> Client` | `TiebaClient`、`ITiebaClient`、`DependencyInjection.AddAioTiebaClient(...)`、`ITiebaClientFactory` 与 `TiebaClientFactory` | 已实现 | `文档与公开表面对齐` | `Docs` | `不适用 - 仅文档对齐` | `docs/modules.md` 现在把直接入口、DI 入口和工厂入口一起写清楚，因此根客户端契约已经与真实的 v3 表面对齐。 |
| 顶层超时配置 | `aiotieba.__init__ -> TimeoutConfig` | 公开 `TimeoutConfig`，以及 `TiebaOptions.Timeout`、`TiebaOptions.RequestTimeout` 与 `TiebaOptions.MaxReadRetryAttempts` | 已实现 | `文档与公开表面对齐` | `Docs` | `不适用 - 仅文档对齐` | 独立的 timeout 导出现在已经真实存在，并连接到现有 v3 执行策略上，而不是只存在于文档里的别名。 |
| 顶层 `Account` 导出 | `aiotieba.__init__ -> Account` | 公开 `AioTieba4DotNet.Contracts.Account`，以及 `TiebaClient(Account)` 与 `ITiebaClientFactory.CreateClient(Account)` | 已实现 | `文档与公开表面对齐` | `Docs` | `不适用 - 仅文档对齐` | 顶层 account 导出现在可以干净地映射到现有 client/factory 创建路径上，而不是暴露内部会话 account 类型。 |
| 顶层异常族系 | `aiotieba.__init__ -> exception` | 公开根级 `AioTieba4DotNet.*` 异常（`TiebaException`、`TiebaAuthenticationException`、`TieBaServerException` 等） | 已实现 | `文档与公开表面对齐` | `Docs` | `不适用 - 仅文档对齐` | `docs/todo.md` 从未跟踪过异常表面对齐，所以旧积压并不能作为产品契约参考。 |
| 顶层通配符枚举导出 | `aiotieba.__init__ -> from .enums import *` | `AioTieba4DotNet.Models*` 下的公开枚举表面，包括 `Gender`、隐私枚举、搜索/排名/管理枚举、`BlacklistType`，以及 websocket `WsStatus` / `GroupType` / `MsgType` | 已实现 | `文档与公开表面对齐` | `Docs` | `不适用 - 仅文档对齐` | 之前缺失的 websocket/message 枚举现在已经公开，这就补齐了剩余的顶层枚举导出对齐缺口，同时没有发明额外的通配符命名空间。 |
| 顶层日志辅助项 | `aiotieba.__init__ -> enable_filelog, get_logger, logging` | 根级 `TiebaLogging.EnableFileLog(...)`、`TiebaLogging.GetLogger(...)` 与 `TiebaLogging.Factory` | 已实现 | `文档与公开表面对齐` | `Docs` | `不适用 - 仅文档对齐` | v3 现在保留了一个最小化的根日志辅助层来满足对齐，同时仍然兼容正常的 Microsoft.Extensions.Logging 宿主集成。 |
| 顶层版本导出 | `aiotieba.__init__ -> __version__` | 根级 `VersionInfo.Version` | 已实现 | `文档与公开表面对齐` | `Docs` | `不适用 - 仅文档对齐` | 运行时版本现在从现有程序集信息版本值公开导出，而不是只存在于包元数据和发布文档中。 |
