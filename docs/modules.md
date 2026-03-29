# 功能模块说明

`AioTieba4DotNet` v2 保留四个核心模块：`Forums`、`Threads`、`Users`、`Client`。所有业务 API 都通过 `ITiebaClient` 暴露；调用方只需表达业务意图，无需手动决定 HTTP / WebSocket。

## 使用规则

- 传输选择统一由 `TiebaOptions.TransportMode` 控制：默认 `Auto`，唯一公开覆盖为 `Http`。
- 所有公开 I/O API 都带有最后一个可选参数 `CancellationToken cancellationToken = default`。
- `client.Client` 负责客户端元数据与连接相关能力。

## 1. 贴吧模块 (`client.Forums`)

负责贴吧元数据、关注状态与吧务操作。

| 方法 | 说明 | 主要参数 |
| --- | --- | --- |
| `GetFidAsync` | 获取贴吧 ID (`fid`) | `fname` |
| `GetFnameAsync` | 获取贴吧名 | `fid` |
| `GetDetailAsync` | 获取贴吧详细信息 | `fid` 或 `fname` |
| `LikeAsync` | 关注贴吧 | `fname` |
| `UnlikeAsync` | 取消关注贴吧 | `fname` |
| `SignAsync` | 贴吧签到 | `fname` |
| `GetForumAsync` | 获取贴吧基础信息 (`forumInfo`) | `fname` |
| `DelBaWuAsync` | 移除吧务 | `fname`, `portrait`, `baWuType` |

## 2. 帖子模块 (`client.Threads`)

负责主题帖、回帖、楼中楼，以及线程相关的互动与管理能力。

### 读取 API

| 方法 | 说明 | 主要参数 |
| --- | --- | --- |
| `GetThreadsAsync` | 分页获取贴吧主题帖列表 | `fname` 或 `fid`, `pn`, `rn`, `sort`, `isGood` |
| `GetPostsAsync` | 分页获取主题帖内回复 | `tid`, `pn`, `rn`, `sort`, `onlyThreadAuthor`, `withComments`, `commentRn`, `commentSortByAgree` |
| `GetCommentsAsync` | 获取回复下的楼中楼列表 | `tid`, `pid`, `pn`, `isComment` |

### 互动 API

| 方法 | 说明 | 主要参数 |
| --- | --- | --- |
| `AgreeAsync` | 点赞 | `tid`, `pid`, `isComment`, `isDisagree`, `isUndo` |
| `DisagreeAsync` | 点踩 | `tid`, `pid`, `isComment`, `isUndo` |
| `UnagreeAsync` | 取消点赞 | `tid`, `pid`, `isComment` |
| `UndisagreeAsync` | 取消点踩 | `tid`, `pid`, `isComment` |

### 发布与管理 API

| 方法 | 说明 | 主要参数 |
| --- | --- | --- |
| `AddPostAsync` | 回复帖子或楼中楼 | `fname`, `tid`, `content`, `showName` |
| `DelThreadAsync` | 删除主题帖 | `fname`, `tid` |
| `DelPostAsync` | 删除回复 | `fname`, `tid`, `pid` |
| `DelThreadsAsync` | 批量删除主题帖 | `fname`, `tids`, `block` |
| `DelPostsAsync` | 批量删除回复 | `fname`, `tid`, `pids`, `block` |
| `GoodAsync` | 加精 | `fname`, `tid`, `cname` |
| `UngoodAsync` | 取消加精 | `fname`, `tid` |
| `TopAsync` | 置顶 | `fname`, `tid`, `isVip` |
| `UntopAsync` | 取消置顶 | `fname`, `tid`, `isVip` |
| `MoveAsync` | 移动帖子分区 | `fname`, `tid`, `toTabId`, `fromTabId` |
| `RecommendAsync` | 推荐帖子 | `fname`, `tid` |
| `RecoverAsync` | 恢复帖子或回复 | `fname`, `tid`, `pid`, `isHide` |
| `SetThreadPrivacyAsync` | 设置回复隐私 | `fname`, `tid`, `pid`, `isPrivate` |

## 3. 用户模块 (`client.Users`)

负责用户资料、自身信息、社交关系、收件箱与黑名单能力。

| 方法 | 说明 | 主要参数 |
| --- | --- | --- |
| `GetTbsAsync` | 获取当前会话的 TBS | - |
| `GetBasicInfoAsync` | 获取用户基础信息 | `userId` |
| `GetProfileAsync` | 获取用户资料页 | `userId` 或 `portraitOrUserName` |
| `GetSelfInfoAsync` | 获取当前登录用户的自身信息 | - |
| `GetFollowsAsync` | 获取用户关注列表 | `userId`, `pn` |
| `GetFansAsync` | 获取用户粉丝列表 | `userId`, `pn` |
| `GetAtsAsync` | 获取 @ 消息 | `pn` |
| `GetRepliesAsync` | 获取回复消息 | `pn` |
| `GetBlacklistAsync` | 获取黑名单 | - |
| `GetPanelInfoAsync` | 获取用户信息面板 | `nameOrPortrait` |
| `GetUserInfoJsonAsync` | 通过 JSON API 获取用户信息 | `username` |
| `GetPostsAsync` | 获取用户发表的回复列表 | `userId`, `pn`, `rn`, `version` |
| `GetThreadsAsync` | 获取用户发表的主题帖列表 | `userId`, `pn`, `publicOnly` |
| `FollowAsync` | 关注用户 | `portrait` |
| `UnfollowAsync` | 取消关注用户 | `portrait` |
| `BlockAsync` | 封禁用户 | `fid`/`fname`, `portrait`, `day`, `reason` |
| `SetBlacklistAsync` | 拉黑用户 | `userId`, `type` |
| `RemoveFanAsync` | 移除粉丝 | `userId` |

## 4. 客户端模块 (`client.Client`)

负责客户端级别的元数据初始化与连接预热。

| 方法 | 说明 | 主要参数 |
| --- | --- | --- |
| `InitWebSocketAsync` | 预热 WebSocket 链路，适合连接敏感场景 | - |
| `InitZIdAsync` | 初始化 ZID | - |
| `SyncAsync` | 同步客户端标识 (`ClientId`, `SampleId`) | - |

如果你正在从 v1 升级，请直接查看 [migration-v1-to-v2.md](./migration-v1-to-v2.md)。
