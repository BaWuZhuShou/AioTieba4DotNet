# 对齐台账

这份文档是 AioTieba4DotNet 当前唯一的对齐真源。它同时承担两项职责：

1. 冻结仓库面对上游 `aiotieba` 的公开能力对齐范围。
2. 记录内部 `Api/**` 实现与上游 Python 族系 的对应关系，以及认证需求注记。

它不是上手教程，也不是迁移指南。第一次接入请先看 [README](../../README.md) 和 [快速开始](../guide/getting-started.md)；需要理解公开模块时请看 `docs/how-to/*` 与 `docs/reference/modules.md`；需要核对“这个能力对齐到哪一层、内部实现落在哪个族系”，再回到这份台账。

## 范围与真源来源

这份台账从以下上游真源冻结对齐范围：

- `aiotieba/aiotieba/api/**/__init__.py`
- `aiotieba/aiotieba/__init__.py`

这份台账采用以下解释规则：

- `aiotieba.api` 和 `aiotieba.api._protobuf` 是空包标记，因此只在证据层面体现，不会作为独立台账行列出。
- 上游包路径加导出符号名是权威依据。像 `Replys` 和 `get_uinfo_getUserInfo_web` 这类不规则的上游名称，会继续保留在上游身份中，即使归一化后的 C# 公开表面使用了更整洁的公开名称。
- `docs/archive/todo.md` 仅提供历史背景。`docs/related/parity.md` 是唯一仍在生效的对齐台账，也是当前对齐范围唯一的权威来源。
- 内部 `Api/**` 实现映射和认证注记现在都放在这份文档里，而不是放在 C# 特性或单独的映射文件中。
- 当前 C# 公开基线是 `ITiebaClient` / `TiebaClient`，包含 `Forums`、`Threads`、`Users`、`Admins`、`Messages` 和 `Client`，以及 `TiebaOptions`、DI 注册和工厂入口。
- 上游名称里带有 `old`、`v1` 或其他旧式表述的行，描述的是有意保留的同级族系或同级族系数据形状。它们仍然可以按上游身份被搜索到，而当前生效的 C# 表面保持使用此处列出的归一化公开名称。

## 如何阅读这份文档

- **内部实现映射** 小节面向维护者。它说明内部哪个 `Api/**` 类型映射到哪个上游 Python 族系，以及当前实现是否要求认证上下文。
- **公开命名归一化映射** 记录被移除的别名、所有权迁移，以及形状改名，保证公开表面清理后仍然可检索。
- **族系对齐表** 是公开能力台账。它回答某个族系是否已实现、落在 C# 公开表面的哪里，以及由哪个任务/阶段负责。
- 有序套件阶段标签只描述当前的验证分组。它们不是旧时代可运行的 lane 名称。

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

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `follow_forum` | `aiotieba.api.follow_forum` | `IForumModule.FollowAsync(ulong)` / `FollowAsync(string)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `2. 贴吧关注/反感/搜索/统计` | 上游 `follow_forum` 族系 仍然在对齐台账中被完整表示，而归一化后的 C# 公开表面在 v4 true-alias 清理移除 `LikeAsync(...)` 后，只保留规范的 `FollowAsync(...)` 名称。 |
| `unfollow_forum` | `aiotieba.api.unfollow_forum` | `IForumModule.UnfollowAsync(ulong)` / `UnfollowAsync(string)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `2. 贴吧关注/反感/搜索/统计` | 上游 `unfollow_forum` 族系 仍然在对齐台账中被完整表示，而归一化后的 C# 公开表面在 v4 true-alias 清理移除 `UnlikeAsync(...)` 后，只保留规范的 `UnfollowAsync(...)` 名称。 |
| `sign_forum` | `aiotieba.api.sign_forum` | `IForumModule.SignAsync(string)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `1. 贴吧基础/读取` | `docs/todo.md` 保留过这行，但新的台账才是权威，因为它锚定在上游导出树上，而不是锚定在积压说明文字上。 |
| `get_fid` | `aiotieba.api.get_fid` | `IForumModule.GetFidAsync(string)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `1. 贴吧基础/读取` | `docs/todo.md` 在这里方向上是对的，但它仍然是次要来源，因为它不能定义完整范围。 |
| `get_forum` (`Forum`) | `aiotieba.api.get_forum` | `IForumModule.GetForumAsync(string)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `1. 贴吧基础/读取` | `docs/todo.md` 完全没有跟踪这个 族系，所以即使是现有 v2 贴吧覆盖，旧积压也并不完整。 |
| `get_forum_detail` (`Forum_detail`) | `aiotieba.api.get_forum_detail` | `IForumModule.GetDetailAsync(ulong)` 与 `IForumModule.GetDetailAsync(string)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `1. 贴吧基础/读取` | `docs/todo.md` 只表达了大致意思，而台账现在记录了精确的上游 族系 以及当前两个 C# 重载。 |
| `dislike_forum` | `aiotieba.api.dislike_forum` | `IForumModule.DislikeAsync(ulong)` / `DislikeAsync(string)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `2. 贴吧关注/反感/搜索/统计` | `AioTieba4DotNet/Api/DislikeForum` 加上 forum protocol/module，现在把这个反感变更直接暴露在 v3 公开表面上，不再通过仅限 TBS 的关注路径转发。 |
| `undislike_forum` | `aiotieba.api.undislike_forum` | `IForumModule.UndislikeAsync(ulong)` / `UndislikeAsync(string)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `2. 贴吧关注/反感/搜索/统计` | `AioTieba4DotNet/Api/UndislikeForum` 加上 forum protocol/module，现在把这个反感回滚 族系 直接暴露在 v3 表面上。 |
| `get_follow_forums` (`FollowForum` / `FollowForums`) | `aiotieba.api.get_follow_forums` | `IForumModule.GetFollowForumsAsync(long userId, ...)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `2. 贴吧关注/反感/搜索/统计` | `AioTieba4DotNet/Api/GetFollowForums`、`Models/Forums/FollowForum(s)` 以及 forum protocol/module 连接层，现在覆盖了 v3 表面上的他人关注列表 族系。 |
| `get_self_follow_forums` (`SelfFollowForum` / `SelfFollowForums`) | `aiotieba.api.get_self_follow_forums` | `IForumModule.GetSelfFollowForumsAsync(int pn = 1, int rn = 200, ...)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `2. 贴吧关注/反感/搜索/统计` | `AioTieba4DotNet/Api/GetSelfFollowForums` 保持当前账号形状独立，包括 `is_signed`，并由 forum 协议层与模块层直接暴露。 |
| `get_self_follow_forums_v1` (`SelfFollowForumsV1` / `SelfFollowForumV1`) | `aiotieba.api.get_self_follow_forums_v1` | `IForumModule.GetSelfFollowForumsV1Async(int pn = 1, int rn = 20, ...)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `2. 贴吧关注/反感/搜索/统计` | V1 同级 族系 形状通过 `Api/GetSelfFollowForumsV1` 以及专用公开模型 `SelfFollowForumV1` / `SelfFollowForumsV1` 保持独立，而不是被折叠进更新的已签到列表模型。 |
| `get_dislike_forums` (`DislikeForum` / `DislikeForums`) | `aiotieba.api.get_dislike_forums` | `IForumModule.GetDislikeForumsAsync(int pn = 1, int rn = 20, ...)` | 已实现 | `11` | `任务 11 贴吧核心/关注负责人` | `2. 贴吧关注/反感/搜索/统计` | `Api/GetDislikeForums` 加上专用 `DislikeForum(s)` 模型，现在覆盖了反感列表 族系，包括它自己的分页形状和贴吧统计字段。 |

## 贴吧发现 / 搜索 / 统计族系

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `get_cid` | `aiotieba.api.get_cid` | `IForumModule.GetCidAsync(string, string)` 与 `GetCidAsync(ulong, string)`；内部 `Api/GetCid/GetCid.cs` 会被 forum protocol 和 `IThreadModule.GoodAsync(...)` 复用。 | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 把现有内部移植提升到公开的贴吧发现表面上，而不重复实现 API。 |
| `get_images` (`Image` / `ImageBytes`) | `aiotieba.api.get_images` | `IForumModule.GetImageBytesAsync(string)`、`GetImageAsync(string)`、`GetImageByHashAsync(string, ForumImageSize)` 与 `GetPortraitAsync(string, ForumImageSize)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 把图片辅助能力保留在贴吧公开表面上，因为上游调用方会把它们和贴吧/主题发现流程一起使用；确定性覆盖现在固定了 `content-type` 校验、图片尺寸解析，以及无效负载时的空图片语义。 |
| `search_exact` (`ExactSearches`) | `aiotieba.api.search_exact` | `IForumModule.SearchExactAsync(string, ...)` 与 `SearchExactAsync(ulong, ...)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 现在通过专用公开模型暴露精确搜索分页、枚举校验、空结果容器，以及基于 `pid` 的相等性。 |
| `get_last_replyers` (`Thread_lp` / `Threads_lp` / `UserInfo_lp`) | `aiotieba.api.get_last_replyers` | `IForumModule.GetLastReplyersAsync(string, ...)` 与 `GetLastReplyersAsync(ulong, ...)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 保留了上游优先 WS、回退 HTTP 的行为，并在页面大小非零时把 `current_page=0` 归一化为 `1`。 |
| `get_member_users` (`MemberUser` / `MemberUsers`) | `aiotieba.api.get_member_users` | `IForumModule.GetMemberUsersAsync(string, ...)` 与 `GetMemberUsersAsync(ulong, ...)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | HTML 解析继续对齐上游成员列表形状，包括分页默认值和 forum protocol 中 STOKEN 的快速失败行为。 |
| `get_rank_forums` (`RankForum` / `RankForums`) | `aiotieba.api.get_rank_forums` | `IForumModule.GetRankForumsAsync(string, ...)` 与 `GetRankForumsAsync(ulong, ...)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 保留了上游 HTML 排名页形状，包括根据渲染出来的 CSS/内容标记推断 `HasBaWu` 语义。 |
| `get_recom_status` (`RecomStatus`) | `aiotieba.api.get_recom_status` | `IForumModule.GetRecomStatusAsync(string)` 与 `GetRecomStatusAsync(ulong)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 把上游 JSON 键 `total_recommend_num` 和 `used_recommend_num` 映射到专用公开模型上。 |
| `get_square_forums` (`SquareForum` / `SquareForums`) | `aiotieba.api.get_square_forums` | `IForumModule.GetSquareForumsAsync(string cname, ...)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 保留了优先 WS、回退 HTTP 的传输形状，并把上游 `is_like` 映射到公开属性 `IsFollowed`。 |
| `get_statistics` (`Statistics`) | `aiotieba.api.get_statistics` | `IForumModule.GetStatisticsAsync(string)` 与 `GetStatisticsAsync(ulong)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 保留了上游按顺序排列的时间序列负载，以从旧到新的公开序列呈现，而不是把它压平成有损的聚合形状。 |
| `get_forum_level` (`LevelInfo`) | `aiotieba.api.get_forum_level` | `IForumModule.GetForumLevelAsync(string)` 与 `GetForumLevelAsync(ulong)` | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | 任务 12 保留了上游引导流程，在发起 level-info 请求之前先加载 self-info 前置数据。 |
| `get_roomlist_by_fid` (`RoomList`) | `aiotieba.api.get_roomlist_by_fid` | `IForumModule.GetRoomListByFidAsync(ulong fid, ...)`，通过 `ForumProtocol.GetRoomListByFidAsync(...)`、`Api/GetRoomListByFid/GetRoomListByFid.cs` 与 `Models/Forums/RoomList.cs` 暴露 | 已实现 | `12` | `任务 12 贴吧发现/统计负责人` | `2. 贴吧关注/反感/搜索/统计` | v3 贴吧表面现在直接暴露这个需要认证的 room-list 族系，而聚焦的确定性覆盖冻结了认证表单打包逻辑，以及把 `data.list[*].room_list[*]` 扁平化进公开 `RoomList` 容器的行为，而不是臆造更丰富的 chatroom 模型。 |

## 吧务 / 管理 / 申诉 / 封禁管理族系

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `add_bawu` | `aiotieba.api.add_bawu` | `IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType, ...)`，通过 `AdminProtocol.AddBawuAsync(...)` 与 `Api/AddBaWu/AddBaWu.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 规范的管理表面让公开 `Bawu` 根直接对齐上游 `add_bawu`，同时保持内部可按上游搜索的 `Api/AddBaWu` 族系 不变。 |
| `add_bawu_blacklist` | `aiotieba.api.add_bawu_blacklist` | `IAdminModule.AddBawuBlacklistAsync(string fname, long userId, ...)`，通过 `AdminProtocol.AddBawuBlacklistAsync(...)` 与 `Api/AddBawuBlacklist/AddBawuBlacklist.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露 bawu 黑名单添加操作，并保留上游 `errno` / `errmsg` 响应形状与 HTTP 表单端点。 |
| `del_bawu` | `aiotieba.api.del_bawu` | `IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, ...)`，通过 `AdminProtocol.DelBawuAsync(...)` 与 `Api/DelBawu/DelBaWu.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 规范的管理表面现在独占 bawu 移除能力，而公开 `Bawu` 根仍然可以直接追溯到上游 `del_bawu`，旧的 forum 侧桥接也继续保持已移除状态。 |
| `del_bawu_blacklist` | `aiotieba.api.del_bawu_blacklist` | `IAdminModule.DelBawuBlacklistAsync(string fname, long userId, ...)`，通过 `AdminProtocol.DelBawuBlacklistAsync(...)` 与 `Api/DelBawuBlacklist/DelBawuBlacklist.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露 bawu 黑名单移除操作，并保留上游 `list[]` 表单打包和 `errno` / `errmsg` 响应形状。 |
| `get_bawu_blacklist` (`BawuBlacklistUser` / `BawuBlacklistUsers`) | `aiotieba.api.get_bawu_blacklist` | `IAdminModule.GetBawuBlacklistAsync(string fname, int pn = 1, ...)`，通过 `AdminProtocol.GetBawuBlacklistAsync(...)` 与 `Api/GetBawuBlacklist/GetBawuBlacklist.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在专用的 `Admins` 公开模块上暴露 bawu 黑名单读取，包括 HTML 列表解析和页面元数据映射。 |
| `get_bawu_info` (`BawuInfo` / `UserInfo_bawu`) | `aiotieba.api.get_bawu_info` | `IAdminModule.GetBawuInfoAsync(string fname, ...)`，通过 `AdminProtocol.GetBawuInfoAsync(...)` 与 `Api/GetBawuInfo/GetBawuInfo.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在专用 `Admins` 公开模块上暴露 bawu 团队信息，并保留上游优先 WebSocket、回退 HTTP 的传输形状。 |
| `get_bawu_perm` (`BawuPerm`) | `aiotieba.api.get_bawu_perm` | `IAdminModule.GetBawuPermAsync(string fname, string portrait, ...)`，通过 `AdminProtocol.GetBawuPermAsync(...)` 与 `Api/GetBawuPerm/GetBawuPerm.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露 bawu 权限读取，并保留上游 `no` / `error` JSON 形状和标志映射。 |
| `get_bawu_postlogs` (`Postlog` / `Postlogs`) | `aiotieba.api.get_bawu_postlogs` | `IAdminModule.GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null, ...)`，通过 `AdminProtocol.GetBawuPostLogsAsync(...)` 与 `Api/GetBawuPostlogs/GetBawuPostlogs.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露 bawu 帖子日志读取，包括与上游行为一致的过滤项打包（`op_type`、`svalue`、`stype`、`begin`、`end`）以及 HTML/媒体映射。 |
| `get_bawu_userlogs` (`Userlog` / `Userlogs`) | `aiotieba.api.get_bawu_userlogs` | `IAdminModule.GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null, ...)`，通过 `AdminProtocol.GetBawuUserLogsAsync(...)` 与 `Api/GetBawuUserlogs/GetBawuUserlogs.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露 bawu 用户日志读取，包括从上游 HTML 形状中进行搜索类型归一化以及时长/页码解析。 |
| `set_bawu_perm` | `aiotieba.api.set_bawu_perm` | `IAdminModule.SetBawuPermAsync(string fname, string portrait, BawuPermType permissions, ...)`，通过 `AdminProtocol.SetBawuPermAsync(...)` 与 `Api/SetBawuPerm/SetBawuPerm.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露 bawu 权限写入，并保留上游权限顺序 JSON 负载（`4,5,3,2`）以及当前仅认证可用的传输门槛。 |
| `get_unblock_appeals` (`Appeal` / `Appeals`) | `aiotieba.api.get_unblock_appeals` | `IAdminModule.GetUnblockAppealsAsync(string fname, int pn = 1, int rn = 20, ...)`，通过 `AdminProtocol.GetUnblockAppealsAsync(...)` 与 `Api/GetUnblockAppeals/GetUnblockAppeals.cs` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露解封申诉读取，并保留上游表单负载和 JSON 申诉列表映射。 |
| `handle_unblock_appeals` | `aiotieba.api.handle_unblock_appeals` | `IAdminModule.HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false, ...)`，通过 `AdminProtocol.HandleUnblockAppealsAsync(...)` 与 `Api/HandleUnblockAppeals/HandleUnblockAppeals.cs` | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露解封申诉处理，并保留带索引的 `appeal_list[i]` 表单打包和上游 `status` 映射（`1` 接受 / `2` 拒绝）。 |
| `get_blocks` (`Block` / `Blocks`) | `aiotieba.api.get_blocks` | `IAdminModule.GetBlocksAsync(string fname, string userName = "", int pn = 1, ...)`，通过 `AdminProtocol.GetBlocksAsync(...)` 与 `Api/GetBlocks/GetBlocks.cs` | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露封禁列表读取，并保留上游 JSON 加内嵌 HTML 的页面/内容形状。 |
| `block` | `aiotieba.api.block` | `IAdminModule.BlockAsync(string fname, string portrait, int day = 1, string reason = "", ...)`，通过 `AdminProtocol.BlockAsync(...)` 暴露 | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 封禁管理现在仅保留在 `Admins` 上；旧的 `Users.BlockAsync(...)` 桥接已经从公开表面移除。 |
| `unblock` | `aiotieba.api.unblock` | `IAdminModule.UnblockAsync(string fname, long userId, ...)`，通过 `AdminProtocol.UnblockAsync(...)` 与 `Api/Unblock/Unblock.cs` | 已实现 | `13` | `任务 13 吧务/管理负责人` | `7. 破坏性清理/回滚验证` | 任务 13 现在在 `Admins` 上暴露解封操作，并保留上游 `block_un=-` / `block_uid` 表单负载以及认证与 TBS 门槛。 |

## 主题读取 / 恢复 / tab-map / 媒体族系

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `get_threads` (`ShareThread` / `Thread` / `Threads` / `UserInfo_t`) | `aiotieba.api.get_threads` | `IThreadModule.GetThreadsAsync(string, ...)` 与 `IThreadModule.GetThreadsAsync(ulong, ...)` | 已实现 | `14` | `任务 14 主题读取/恢复负责人` | `3. 主题读取/评论/tab-map/恢复检查` | `docs/todo.md` 标成了已完成，但台账记录的是精确的上游 族系 和当前重载覆盖。 |
| `get_posts` (`Comment_p` / `Post` / `Posts` / `Thread_p` / `UserInfo_p` / `UserInfo_pt`) | `aiotieba.api.get_posts` | `IThreadModule.GetPostsAsync(...)` | 已实现 | `14` | `任务 14 主题读取/恢复负责人` | `3. 主题读取/评论/tab-map/恢复检查` | `docs/todo.md` 标成了已完成；台账保留了精确的上游导出 族系 和与评论相关的负载类型。 |
| `get_comments` (`Comment` / `Comments` / `Post_c` / `Thread_c` / `UserInfo_c` / `UserInfo_cp` / `UserInfo_ct`) | `aiotieba.api.get_comments` | `IThreadModule.GetCommentsAsync(...)` | 已实现 | `14` | `任务 14 主题读取/恢复负责人` | `3. 主题读取/评论/tab-map/恢复检查` | `docs/todo.md` 标成了已完成；台账把它锚定到上游 comments 族系，而不是旧的说明性分类。 |
| `get_recovers` (`Recover` / `Recovers`) | `aiotieba.api.get_recovers` | `IThreadModule.GetRecoversAsync(string, ...)` 与 `GetRecoversAsync(ulong, ...)` | 已实现 | `14` | `任务 14 主题读取/恢复负责人` | `3. 主题读取/评论/tab-map/恢复检查` | 任务 14 现在把这个需要认证的恢复列表读取 族系 直接暴露在 `Threads` 上，包括上游分页以及按需的已删作者 user-id 过滤。 |
| `get_recover_info` (`RecoverInfo`) | `aiotieba.api.get_recover_info` | `IThreadModule.GetRecoverInfoAsync(string, long tid, long pid = 0, ...)` 与 `GetRecoverInfoAsync(ulong, long tid, long pid = 0, ...)` | 已实现 | `14` | `任务 14 主题读取/恢复负责人` | `3. 主题读取/评论/tab-map/恢复检查` | 虽然当前上游 client facade 没有在公开层明确推广这个 helper，但 v3 任务 14 表面现在仍然在 C# thread module 上暴露了相同的需要认证的恢复详情查询，并让主题/楼层语义与底层 API 保持一致。 |
| `recover` | `aiotieba.api.recover` | `IThreadModule.RecoverAsync(string fname, long tid = 0, long pid = 0, bool isHide = false, ...)` | 已实现 | `14` | `任务 14 主题读取/恢复负责人` | `3. 主题读取/评论/tab-map/恢复检查` | `docs/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `RecoverAsync`，所以旧积压已经过时。 |
| `get_tab_map` (`TabMap`) | `aiotieba.api.get_tab_map` | `IThreadModule.GetTabMapAsync(string, ...)` 与 `GetTabMapAsync(ulong, ...)` | 已实现 | `14` | `任务 14 主题读取/恢复负责人` | `3. 主题读取/评论/tab-map/恢复检查` | 任务 14 为了兼容，在线程列表读取里保留 `Threads.TabDictionary`，同时又为这个独立的上游 族系 增加了单独的需要认证的 `GetTabMapAsync(...)` 移植。 |

## 主题写入 / 管理 / 签到族系

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `add_post` | `aiotieba.api.add_post` | `IThreadModule.AddPostAsync(string fname, long tid, string content, string? showName = null, ...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 标成了已完成；台账保留了精确的上游写入 族系 和当前可选 `showName` 映射。 |
| `agree` | `aiotieba.api.agree` | `IThreadModule.AgreeAsync(...)`、`DisagreeAsync(...)`、`UnagreeAsync(...)` 与 `UndisagreeAsync(...)` 都经由同一个上游族系路由。 | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 在概念上把 `agree` 和 `unagree` 分开了，但上游真相是它们都属于一个带标志位的 `agree` 族系，因此旧积压过于粗糙。 |
| `del_post` | `aiotieba.api.del_post` | `IThreadModule.DelPostAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 标成了已完成；台账保留了精确的上游 族系。 |
| `del_posts` | `aiotieba.api.del_posts` | `IThreadModule.DelPostsAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `DelPostsAsync`，所以旧积压已经过时。 |
| `del_thread` | `aiotieba.api.del_thread` | `IThreadModule.DelThreadAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 标成了已完成；台账保留了精确的上游 族系。 |
| `del_threads` | `aiotieba.api.del_threads` | `IThreadModule.DelThreadsAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `DelThreadsAsync`，所以旧积压已经过时。 |
| `good` | `aiotieba.api.good` | `IThreadModule.GoodAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `GoodAsync`，所以旧积压已经过时。 |
| `ungood` | `aiotieba.api.ungood` | `IThreadModule.UngoodAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `UngoodAsync`，所以旧积压已经过时。 |
| `top` | `aiotieba.api.top` | `IThreadModule.TopAsync(...)` 与 `UntopAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 仍把 `top` 显示为未勾选，而且没有表达 v2 中已经存在的配套撤销行为。 |
| `move` | `aiotieba.api.move` | `IThreadModule.MoveAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `MoveAsync`，所以旧积压已经过时。 |
| `recommend` | `aiotieba.api.recommend` | `IThreadModule.RecommendAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `RecommendAsync`，所以旧积压已经过时。 |
| `set_thread_privacy` | `aiotieba.api.set_thread_privacy` | `IThreadModule.SetThreadPrivacyAsync(...)` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | `docs/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `SetThreadPrivacyAsync`，所以旧积压已经过时。 |
| `sign_forums` | `aiotieba.api.sign_forums` | `IForumModule.SignForumsAsync(...)`，通过 `ForumProtocol.SignForumsAsync(...)` 与 `Api/SignForums/SignForums.cs` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | 任务 15 现在在贴吧模块上暴露这个批量签到族系，并保留上游 `/c/c/forum/msign` 的混合 Web 表单请求形状，包括 `Subapp-Type: hybrid` 请求头。 |
| `sign_growth` | `aiotieba.api.sign_growth` | `IForumModule.SignGrowthAsync(...)`，通过 `ForumProtocol.SignGrowthAsync(...)` 与 `Api/SignGrowth/SignGrowth.cs` | 已实现 | `15` | `任务 15 主题写入/管理负责人` | `4. 主题写入/管理/签到族系` | 任务 15 现在把成长签到族系与 `sign_forum` / `sign_forums` 分开暴露，并保留上游 Web 任务打包路径 `/mo/q/usergrowth/commitUGTaskInfo` 及 `act_type=page_sign`。 |

## 用户 / 社交 / 资料 / 旧式族系

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `add_blacklist_old` | `aiotieba.api.add_blacklist_old` | `IUserModule.AddBlacklistOldAsync(long userId, ...)`，通过 `UserProtocol.AddBlacklistOldAsync(...)` 与 `Api/AddBlacklistOld/AddBlacklistOld.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `_old` 黑名单添加变更作为受支持的同级 族系 继续公开存在，而不是并入 `SetBlacklistAsync(...)` 表面。 |
| `del_blacklist_old` | `aiotieba.api.del_blacklist_old` | `IUserModule.RemoveBlacklistOldAsync(long userId, ...)`，通过 `UserProtocol.RemoveBlacklistOldAsync(...)` 与 `Api/DelBlacklistOld/DelBlacklistOld.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `_old` 黑名单流程中的删除半段仍然是一级同级 族系，这样调用方就能把它与权限写入语义区分开来。 |
| `follow_user` | `aiotieba.api.follow_user` | `IUserModule.FollowAsync(string portrait, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 只跟踪了友好的动词 `follow`，所以那份历史积压过于粗糙，不能作为权威对齐台账。 |
| `unfollow_user` | `aiotieba.api.unfollow_user` | `IUserModule.UnfollowAsync(string portrait, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 只跟踪了友好的动词 `unfollow`，所以那份历史积压名称没有保留精确的上游包身份。 |
| `get_ats` (`At` / `Ats`) | `aiotieba.api.get_ats` | `IMessagesModule.GetAtsAsync(int pn = 1, ...)`，通过 `MessagesProtocol.GetAtsAsync(...)` 与 `Api/GetAts/GetAts.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 收件箱 `@` 读取现在只保留在 `Messages` 上；旧的 `Users.GetAtsAsync(...)` 桥接已从公开表面移除。 |
| `get_blacklist` (`BlacklistUser` / `BlacklistUsers`) | `aiotieba.api.get_blacklist` | `IUserModule.GetBlacklistAsync(...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `get_blacklist` 族系保持独立，并使用共享的 `Blacklist` 根，这样在概念上不会和 `_old` 同级族系混在一起。 |
| `get_blacklist_old` (`BlacklistOldUser` / `BlacklistOldUsers`) | `aiotieba.api.get_blacklist_old` | `IUserModule.GetBlacklistOldAsync(int pn = 1, int rn = 20, ...)`，通过 `UserProtocol.GetBlacklistOldAsync(...)` 与 `Api/GetBlacklistOld/GetBlacklistOld.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `_old` 黑名单读取族系保留了自己的分页/模型形状，以及优先 WS、回退 HTTP 的传输行为，而不是被折叠进 `GetBlacklistAsync(...)`。 |
| `get_fans` (`Fan` / `Fans`) | `aiotieba.api.get_fans` | `IUserModule.GetFansAsync(long userId, int pn = 1, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `GetFansAsync`，所以那份历史积压已经过时。 |
| `get_follows` (`Follow` / `Follows`) | `aiotieba.api.get_follows` | `IUserModule.GetFollowsAsync(long userId, int pn = 1, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 标成了已完成，但台账现在保留了精确的上游包身份。 |
| `get_rank_users` (`RankUser` / `RankUsers`) | `aiotieba.api.get_rank_users` | `IUserModule.GetRankUsersAsync(string fname, int pn = 1, ...)`，通过 `UserProtocol.GetRankUsersAsync(...)` 与 `Api/GetRankUsers/GetRankUsers.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | v3 user module 现在直接暴露上游 HTML rank-users 族系，并提供专用公开模型 `RankUser` / `RankUsers` 与针对行/页解析的确定性覆盖。 |
| `get_replys` (`Reply` / `Replys`) | `aiotieba.api.get_replys` | `IMessagesModule.GetRepliesAsync(int pn = 1, ...)`，通过 `MessagesProtocol.GetRepliesAsync(...)` 与 `Api/GetReplys/GetReplys.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 回复收件箱读取现在只保留在 `Messages` 上；旧的 `Users.GetRepliesAsync(...)` 桥接已经移除，即使上游族系仍然保留了不规则的 `Replys` 拼写。 |
| `get_selfinfo_initNickname` | `aiotieba.api.get_selfinfo_initNickname` | `IUserModule.GetSelfInfoInitNicknameAsync(...)`，通过 `UserProtocol.GetSelfInfoInitNicknameAsync(...)` 与 `Api/GetSelfInfoInitNickname/GetSelfInfoInitNickname.cs` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | v3 用户表面现在让这个直接兼容 族系 与合并后的便捷入口 `GetSelfInfoAsync()` 并存可用。 |
| `get_selfinfo_moindex` (`UserInfo_moindex`) | `aiotieba.api.get_selfinfo_moindex` | `IUserModule.GetSelfInfoMoIndexAsync(...)`，通过 `UserProtocol.GetSelfInfoMoIndexAsync(...)` 与 `Api/GetSelfInfoMoIndex/GetSelfInfoMoIndex.cs` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 这个直接的 moindex 族系 现在被单独暴露出来，而不再只作为内部合并输入存在。 |
| `get_uinfo_getuserinfo_app` (`UserInfo_guinfo_app`) | `aiotieba.api.get_uinfo_getuserinfo_app` | `IUserModule.GetUserInfoAppAsync(int userId, ...)`，通过 `UserProtocol.GetUserInfoAppAsync(...)`、`Api/GetUInfoGetUserInfoApp/GetUInfoGetUserInfoApp.cs` 与公开 `UserInfo` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | App `user_info` 族系 在共享的 `GetUserInfo` 根下继续保持公开，而当前公开契约有意复用 `UserInfo`，不再暴露端点专属 DTO 名称。 |
| `get_uinfo_getUserInfo_web` (`UserInfo_guinfo_web`) | `aiotieba.api.get_uinfo_getUserInfo_web` | `IUserModule.GetUserInfoWebAsync(int userId, ...)`，通过 `UserProtocol.GetUserInfoWebAsync(...)`、`Api/GetUInfoGetUserInfoWeb/GetUInfoGetUserInfoWeb.cs` 与公开 `UserInfo` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | Web `user_info` 同级项同样保留在共享公开 `UserInfo` 契约上，而不是单独的端点专属 DTO。 |
| `get_uinfo_panel` (`UserInfo_panel`) | `aiotieba.api.get_uinfo_panel` | `IUserModule.GetPanelInfoAsync(string nameOrPortrait, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 标成了已完成；台账保留了精确的上游 族系。 |
| `get_uinfo_user_json` (`UserInfo_json`) | `aiotieba.api.get_uinfo_user_json` | `IUserModule.GetUserInfoJsonAsync(string username, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 标成了已完成；台账保留了精确的上游 族系。 |
| `get_user_contents` (`get_posts` / `get_threads` / `UserPost` / `UserPosts` / `UserPostss` / `UserThread` / `UserThreads` / `CMD`) | `aiotieba.api.get_user_contents` | `IUserModule.GetPostsAsync(...)`、`IUserModule.GetThreadsAsync(...)`、公开模型 `UserPost` / `UserPosts` / `UserPostGroups` / `UserThread` / `UserThreads`，以及根辅助项 `UserContent.Cmd` 与 `IUserModule.UserContentCmd` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 公开 C# DTO 中的异常命名已归一化为 `UserPostGroups`，而底层上游 族系 和内部请求命名仍然可以追溯到 `UserPostss`。 |
| `get_user_contents.get_posts` | `aiotieba.api.get_user_contents.get_posts` | `IUserModule.GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5", ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 把它记作 `get_user_posts`，有帮助，但不是权威命名。 |
| `get_user_contents.get_threads` | `aiotieba.api.get_user_contents.get_threads` | `IUserModule.GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 把它记作 `get_user_threads`，有帮助，但不是权威命名。 |
| `get_user_forum_info` (`UserForumInfo` / `UserInfo_uf`) | `aiotieba.api.get_user_forum_info` | `IUserModule.GetUserForumInfoAsync(ulong fid, string portrait, ...)` 与 `GetUserForumInfoAsync(string fname, string portrait, ...)`，通过 `UserProtocol.GetUserForumInfoAsync(...)` 与 `Api/GetUserForumInfo/GetUserForumInfo.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | user module 现在暴露这个需要认证的吧内用户信息 族系，并同时提供 fid 和贴吧名两个重载；确定性覆盖冻结了认证预检、fid 解析和 JSON 映射。 |
| `login` | `aiotieba.api.login` | `IUserModule.LoginAsync(...)`，通过 `UserProtocol.LoginAsync(...)`、`Api/Login/Login.cs` 与公开 `Models/Users/LoginResult.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 这个兼容登录 族系 现在在公开表面同时返回用户信息和 TBS，并且只有在成功后才更新会话 TBS，这与现有 v3 会话生命周期规则一致。 |
| `profile` (`get_homepage` / `get_uinfo_profile` / `Homepage` / `Thread_pf` / `UserInfo_pf` / `CMD`) | `aiotieba.api.profile` | `IUserModule.GetProfileAsync(int)` / `GetProfileAsync(string)` 覆盖 `profile.get_uinfo_profile`，而 `IUserModule.GetHomepageAsync(int userId, int pn = 1, ...)` 覆盖 `profile.get_homepage`。 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 这些嵌套 profile 族系 在公开表面上明确保持分离：`GetProfileAsync(...)` 读取资料元数据，`GetHomepageAsync(...)` 读取主页内容和主页拥有者快照。 |
| `profile.get_homepage` | `aiotieba.api.profile.get_homepage` | `IUserModule.GetHomepageAsync(int userId, int pn = 1, ...)`，通过 `UserProtocol.GetHomepageAsync(...)` 与 `Api/Profile/GetHomepage/GetHomepage.cs` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 主页 族系 现在保留了上游优先 WS、回退 HTTP 的行为，并把拥有者快照与返回的主题列表分开映射。 |
| `profile.get_uinfo_profile` | `aiotieba.api.profile.get_uinfo_profile` | `IUserModule.GetProfileAsync(int)` 与 `IUserModule.GetProfileAsync(string)`，通过 `AioTieba4DotNet.Api.Profile.GetUInfoProfile.GetUInfoProfile<T>` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 只用粗粒度的 `get_profile` 项跟踪它，而不是精确的上游嵌套 族系。 |
| `remove_fan` | `aiotieba.api.remove_fan` | `IUserModule.RemoveFanAsync(long userId, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `docs/archive/todo.md` 仍把它显示为未勾选，尽管 v2 早就暴露了 `RemoveFanAsync`，所以那份历史积压已经过时。 |
| `set_blacklist` | `aiotieba.api.set_blacklist` | `IUserModule.SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All, ...)` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | `set_blacklist` 写入 族系 继续与 `_old` 添加/删除同级 族系 分离，这样调用方就能显式选择正确的传输方式和语义。 |
| `set_nickname_old` | `aiotieba.api.set_nickname_old` | `IUserModule.SetNicknameAsync(string nickName, ...)`，通过 `UserProtocol.SetNicknameAsync(...)` 与 `Api/SetNicknameOld/SetNicknameOld.cs` | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 这个单字段昵称变更仍然作为独立写入 族系 公开存在，使用归一化后的 `SetNicknameAsync(...)` 名称，而 `SetProfileAsync(...)` 继续负责更广泛的资料元数据更新路径。 |
| `set_profile` | `aiotieba.api.set_profile` | `IUserModule.SetProfileAsync(string nickName, string sign, Gender gender, ...)`，通过 `UserProtocol.SetProfileAsync(...)` 与 `Api/SetProfile/SetProfile.cs` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 当前资料变更族系与 `set_nickname_old` 分开实现，保留推荐路径和旧式路径在资料写入上的区分，而不是把两者压平成一个方法。 |
| `tieba_uid2user_info` (`UserInfo_TUid`) | `aiotieba.api.tieba_uid2user_info` | `IUserModule.GetUserByTiebaUidAsync(long tiebaUid, ...)`，通过 `UserProtocol.GetUserByTiebaUidAsync(...)`、`Api/TiebaUid2UserInfo/TiebaUid2UserInfo.cs` 与公开 `UserInfo` 暴露 | 已实现 | `16` | `任务 16 用户/社交/资料负责人` | `5. 用户/社交/资料` | 这个 tieba-uid 查询族系保留了上游传输形状，同时复用共享的公开 `UserInfo` 契约，而不是使用端点专属 DTO。 |

## 消息 / 推送 / 客户端生命周期族系

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 有序套件阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `get_group_msg` (`UserInfo_ws` / `WsMessage` / `WsMsgGroup` / `WsMsgGroups`) | `aiotieba.api.get_group_msg` | `IMessagesModule.GetGroupMessagesAsync(...)`，通过 `MessagesProtocol`、`Api/GetGroupMsg/GetGroupMsg.cs` 与 `Models/Messages/*` 暴露 | 已实现 | `17` | `任务 17 消息/客户端负责人` | `6. 消息/推送/客户端生命周期` | 任务 17 增加了专用 `Messages` 模块、来自 websocket bootstrap 的游标初始化，以及明确的公开模型 `WsMessage` / `WsMsgGroup`，而不是继续把 websocket 消息隐藏在生命周期模块后面。 |
| `init_websocket` (`WsMsgGroupInfo`) | `aiotieba.api.init_websocket` | 生命周期 `IClientModule.InitWebSocketAsync(...)`，加上供 `MessagesProtocol` 游标引导消费的内部 `Api/InitWebSocket/InitWebSocket.cs` / `Models/Messages/WsMsgGroupInfo` | 已实现 | `17` | `任务 17 消息/客户端负责人` | `6. 消息/推送/客户端生命周期` | 任务 17 让 `init_websocket` 在 `Client` 上继续保持纯生命周期职责，同时仍然移植了上游响应族系，这样新的 `Messages` 模块就能初始化消息组游标，而不会模糊公开边界。 |
| `init_z_id` | `aiotieba.api.init_z_id` | `IClientModule.InitZIdAsync(...)` | 已实现 | `17` | `任务 17 消息/客户端负责人` | `6. 消息/推送/客户端生命周期` | `docs/todo.md` 标成了已完成；台账保留了精确的上游生命周期族系。 |
| `push_notify` (`WsNotify`) | `aiotieba.api.push_notify` | `IMessagesModule.ParsePushNotifications(byte[] payload)`，通过 `Api/PushNotify/PushNotify.cs` 与 `Models/Messages/WsNotify.cs` 暴露 | 已实现 | `17` | `任务 17 消息/客户端负责人` | `6. 消息/推送/客户端生命周期` | 任务 17 让上游语义保持清晰可见：`push_notify` 在 `Messages` 上被暴露为纯解析入口，而不是推测性的事件总线或后台订阅框架。 |
| `send_chatroom_msg` | `aiotieba.api.send_chatroom_msg` | `IMessagesModule.SendChatroomMessageAsync(...)`，通过 `MessagesProtocol` 与内部 `Transport/Chatrooms/BlcpChatroomSender.cs` 暴露 | 已实现 | `17` | `任务 17 消息/客户端负责人` | `6. 消息/推送/客户端生命周期` | 任务 17 把基于 BLCP 的 chatroom 发送路径移植为最小化的内部传输同级项，同时把公开业务表面保留在 `Messages` 上，并继续保持用于验证的安全 live-fixture 门控。 |
| `send_msg` | `aiotieba.api.send_msg` | `IMessagesModule.SendMessageAsync(long, ...)` / `SendMessageAsync(string, ...)`，通过 `MessagesProtocol` 与 `Api/SendMsg/SendMsg.cs` 暴露 | 已实现 | `17` | `任务 17 消息/客户端负责人` | `6. 消息/推送/客户端生命周期` | 任务 17 在 `Messages` 上新增了 websocket 私信发送族系，包括通过现有用户 panel 查询路径完成 portrait 或用户名解析。 |
| `set_msg_readed` | `aiotieba.api.set_msg_readed` | `IMessagesModule.SetMessageReadAsync(WsMessage, ...)`，通过 `MessagesProtocol` 与 `Api/SetMsgReaded/SetMsgReaded.cs` 暴露 | 已实现 | `17` | `任务 17 消息/客户端负责人` | `6. 消息/推送/客户端生命周期` | 任务 17 移植了 websocket 已读状态 族系，并复用了已经初始化的私信组游标状态，而不是另造第二套已读跟踪机制。 |
| `sync` | `aiotieba.api.sync` | `IClientModule.SyncAsync(...)` | 已实现 | `17` | `任务 17 消息/客户端负责人` | `6. 消息/推送/客户端生命周期` | `docs/todo.md` 标成了已完成；台账保留了精确的上游生命周期族系。 |

## 来自上游 API 树的支撑 / 导出族系

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 集成阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `_classdef` (`Account`, `TypeMessage`, `Containers`, `Frag*`, `TypeFrag*`, `UserInfo`, `VoteInfo`) | `aiotieba.api._classdef` | 公开支撑模型分布在 `AioTieba4DotNet.Models.*`、`AioTieba4DotNet.Models.Shared`、`AioTieba4DotNet.Models.Messages.MessageEnums.cs` 与 `AioTieba4DotNet.Contracts.Account` 中 | 已实现 | `20` | `任务 20 对齐/文档负责人` | `不适用 - 仅文档对齐` | v3 把这些支撑导出保留为公开根/契约/模型类型，而不是复刻上游包布局；目前剩余的缺口已经由 `FragUnknown`、公开 `Account` 和 websocket message/group 枚举补齐。 |

## 影响对齐范围的顶层 `aiotieba` 包导出

| 功能 | 上游模块 | C# 表面 | 状态 | 目标任务 | 覆盖责任人 | 集成阶段 | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 顶层 `Client` 门面 | `aiotieba.__init__ -> Client` | `TiebaClient`、`ITiebaClient`、`DependencyInjection.AddAioTiebaClient(...)`、`ITiebaClientFactory` 与 `TiebaClientFactory` | 已实现 | `20` | `任务 20 对齐/文档负责人` | `不适用 - 仅文档对齐` | `docs/modules.md` 现在把直接入口、DI 入口和工厂入口一起写清楚，因此根客户端契约已经与真实的 v3 表面对齐。 |
| 顶层超时配置 | `aiotieba.__init__ -> TimeoutConfig` | 公开 `TimeoutConfig`，以及 `TiebaOptions.Timeout`、`TiebaOptions.RequestTimeout` 与 `TiebaOptions.MaxReadRetryAttempts` | 已实现 | `20` | `任务 20 对齐/文档负责人` | `不适用 - 仅文档对齐` | 独立的 timeout 导出现在已经真实存在，并连接到现有 v3 执行策略上，而不是只存在于文档里的别名。 |
| 顶层 `Account` 导出 | `aiotieba.__init__ -> Account` | 公开 `AioTieba4DotNet.Contracts.Account`，以及 `TiebaClient(Account)` 与 `ITiebaClientFactory.CreateClient(Account)` | 已实现 | `20` | `任务 20 对齐/文档负责人` | `不适用 - 仅文档对齐` | 顶层 account 导出现在可以干净地映射到现有 client/factory 创建路径上，而不是暴露内部会话 account 类型。 |
| 顶层异常族系 | `aiotieba.__init__ -> exception` | 公开根级 `AioTieba4DotNet.*` 异常（`TiebaException`、`TiebaAuthenticationException`、`TieBaServerException` 等） | 已实现 | `20` | `任务 20 对齐/文档负责人` | `不适用 - 仅文档对齐` | `docs/todo.md` 从未跟踪过异常表面对齐，所以旧积压并不能作为产品契约参考。 |
| 顶层通配符枚举导出 | `aiotieba.__init__ -> from .enums import *` | `AioTieba4DotNet.Models*` 下的公开枚举表面，包括 `Gender`、隐私枚举、搜索/排名/管理枚举、`BlacklistType`，以及 websocket `WsStatus` / `GroupType` / `MsgType` | 已实现 | `20` | `任务 20 对齐/文档负责人` | `不适用 - 仅文档对齐` | 之前缺失的 websocket/message 枚举现在已经公开，这就补齐了剩余的顶层枚举导出对齐缺口，同时没有发明额外的通配符命名空间。 |
| 顶层日志辅助项 | `aiotieba.__init__ -> enable_filelog, get_logger, logging` | 根级 `TiebaLogging.EnableFileLog(...)`、`TiebaLogging.GetLogger(...)` 与 `TiebaLogging.Factory` | 已实现 | `20` | `任务 20 对齐/文档负责人` | `不适用 - 仅文档对齐` | v3 现在保留了一个最小化的根日志辅助层来满足对齐，同时仍然兼容正常的 Microsoft.Extensions.Logging 宿主集成。 |
| 顶层版本导出 | `aiotieba.__init__ -> __version__` | 根级 `VersionInfo.Version` | 已实现 | `20` | `任务 20 对齐/文档负责人` | `不适用 - 仅文档对齐` | 运行时版本现在从现有程序集信息版本值公开导出，而不是只存在于包元数据和发布文档中。 |
