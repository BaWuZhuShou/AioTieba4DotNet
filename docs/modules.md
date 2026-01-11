# 功能模块说明

`AioTieba4DotNet` 按照贴吧业务逻辑划分为四个核心模块，均可以通过 `ITiebaClient` 访问。

> **提示**: 大多数查询与发布 API 都支持可选的 `TiebaRequestMode? mode` 参数，用于指定请求模式（`Http` 或 `Ws`）。如果未指定，将使用客户端默认配置。

## 1. 贴吧模块 (`client.Forums`)

负责获取贴吧元数据、操作贴吧状态（如关注、签到）。

| 方法 | 说明 | 参数 |
| --- | --- | --- |
| `GetFidAsync` | 获取贴吧 ID (fid) | `fname` (吧名) |
| `GetFnameAsync` | 获取贴吧名 | `fid` (ID) |
| `GetDetailAsync` | 获取贴吧详细资料 | `fid` 或 `fname` |
| `LikeAsync` | 关注贴吧 | `fname` |
| `UnlikeAsync` | 取消关注贴吧 | `fname` |
| `SignAsync` | 贴吧签到 | `fname` |
| `GetForumAsync` | 获取贴吧基本信息 (frsBottom) | `fname` |
| `DelBaWuAsync` | 移除吧务 | `fname`, `portrait`, `baWuType` |

## 2. 帖子模块 (`client.Threads`)

负责主题帖、回复贴、楼中楼的操作。

### 浏览 API
| 方法 | 说明 | 主要参数 |
| --- | --- | --- |
| `GetThreadsAsync` | 分页获取贴吧主题帖列表 | `fname`/`fid`, `pn` (页码), `rn` (每页数量), `sort` (排序方式), `isGood` (精华), `mode` |
| `GetPostsAsync` | 分页获取主题帖内的回复列表 | `tid` (帖子ID), `pn`, `rn`, `sort`, `onlyThreadAuthor` (只看楼主), `withComments` (带楼中楼), `commentRn`, `commentSortByAgree`, `mode` |
| `GetCommentsAsync` | 获取回复下的楼中楼列表 | `tid`, `pid` (回复ID), `pn`, `isComment`, `mode` |

### 互动 API
| 方法 | 说明 | 参数 |
| --- | --- | --- |
| `AgreeAsync` | 点赞 | `tid`, `pid`, `isComment`, `isDisagree`, `isUndo` |
| `DisagreeAsync` | 点踩 | `tid`, `pid`, `isComment`, `isUndo` |
| `UnagreeAsync` | 取消点赞 | `tid`, `pid`, `isComment` |
| `UndisagreeAsync` | 取消点踩 | `tid`, `pid`, `isComment` |

### 发布与管理 API
| 方法 | 说明 | 参数 |
| --- | --- | --- |
| `AddPostAsync` | 回复帖子/楼中楼 | `fname`, `tid`, `content` (纯文本), `showName` (小号名), `mode` |
| `DelThreadAsync` | 删除主题帖 | `fname`, `tid` |
| `DelPostAsync` | 删除回复 | `fname`, `tid`, `pid` |

## 3. 用户模块 (`client.Users`)

负责用户信息查询、用户管理、关注管理。

| 方法 | 说明 | 参数 |
| --- | --- | --- |
| `GetTbsAsync` | 获取用户 tbs (用于某些写操作) | - |
| `GetBasicInfoAsync` | 获取用户基础信息 | `userId` |
| `GetProfileAsync` | 获取用户详细资料 (个人主页) | `userId` 或 `portrait`/`userName` |
| `GetPostsAsync` | 获取用户发表的回复列表 | `userId`, `pn`, `rn`, `mode` |
| `GetThreadsAsync` | 获取用户发表的主题帖列表 | `userId`, `pn`, `publicOnly`, `mode` |
| `FollowAsync` | 关注用户 | `portrait` |
| `UnfollowAsync` | 取消关注用户 | `portrait` |
| `BlockAsync` | 封禁用户 (吧务功能) | `fid`/`fname`, `portrait`, `day` (天数), `reason` (原因) |
| `GetFollowsAsync` | 获取用户关注列表 | `userId`, `pn` |
| `GetPanelInfoAsync` | 获取用户信息面板 | `nameOrPortrait` |
| `GetUserInfoJsonAsync` | 通过 JSON API 获取用户信息 | `username` |
| `LoginAsync` | 执行登录操作 | - |

## 4. 客户端模块 (`client.Client`)

负责客户端级别的基础设置与初始化。

| 方法 | 说明 | 参数 |
| --- | --- | --- |
| `InitZIdAsync` | 初始化 ZID 令牌 | - |
| `SyncAsync` | 同步客户端配置 | - |
