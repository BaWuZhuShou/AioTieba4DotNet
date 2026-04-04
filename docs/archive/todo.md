# docs/archive/todo.md historical archive

本文件仅作 **historical archive**，保留早期粗粒度 backlog 作为历史参考。

- `docs/related/parity.md` 是当前唯一的 **authoritative parity ledger** / 当前范围真源。
- 本页只保留历史快照，**不是** 当前 parity 真值、**不是** release gate 判定依据、**不是**当前完成度清单。
- 下面保留的旧清单可能缺项、命名过粗、状态过时，不能再和 `docs/related/parity.md` 竞争解释权。

---

以下内容仅保留为历史快照，不代表当前范围：

## 1. 吧务管理 (Bawu Management)
- [x] `add_bawu`: 添加吧务 (小吧主/语音小编等)
- [x] `del_bawu`: 移除吧务
- [x] `add_bawu_blacklist`: 添加吧务黑名单 (禁言)
- [x] `del_bawu_blacklist`: 解除吧务黑名单
- [x] `get_bawu_blacklist`: 获取吧务黑名单列表
- [x] `get_bawu_info`: 获取吧务团队信息
- [x] `get_bawu_perm`: 获取当前用户在某吧的吧务权限
- [x] `get_bawu_postlogs`: 获取吧务删帖日志
- [x] `get_bawu_userlogs`: 获取吧务封禁/加精等操作日志
- [x] `set_bawu_perm`: 设置吧务权限
- [x] `get_unblock_appeals`: 获取解封申诉列表
- [x] `handle_unblock_appeals`: 处理解封申诉 (同意/拒绝)
- [x] `get_blocks`: 获取封禁列表
- [x] `block`: 封禁用户
- [x] `unblock`: 解除封禁

## 2. 帖子与评论管理 (Thread & Post Management)
- [x] `get_threads`: 分页获取贴吧主题帖列表
- [x] `get_posts`: 分页获取主题帖内的回复列表
- [x] `get_comments`: 获取回复下的楼中楼列表
- [x] `add_post`: 发布回复/楼中楼
- [x] `del_thread`: 删除主题帖
- [x] `del_post`: 删除回复
- [x] `del_posts`: 批量删除回复
- [x] `del_threads`: 批量删除主题帖
- [x] `good`: 将帖子加精
- [x] `ungood`: 取消帖子精华
- [x] `top`: 置顶帖子
- [x] `untop`: 取消置顶
- [x] `move`: 移动帖子到指定分区
- [x] `recommend`: 推荐帖子 (大推)
- [x] `agree`: 点赞/点踩
- [x] `unagree`: 取消点赞/点踩 (对应 `agree`)
- [x] `recover`: 恢复已删除的帖子/回复
- [x] `get_recovers`: 获取回收站列表
- [x] `get_recover_info`: 获取回收站内单条内容的详情
- [x] `set_thread_privacy`: 设置帖子隐私状态 (公开/私密)
- [x] `get_tab_map`: 获取贴吧的分区(Tab)列表

## 3. 用户与社交 (User & Social)
- [x] `login`: 获取用户信息及 Tbs
- [x] `get_uinfo_getUserInfo_app`: 获取用户基础信息 (App 接口)
- [x] `get_uinfo_getUserInfo_web`: 获取用户基础信息 (Web 兼容接口)
- [x] `get_uinfo_panel`: 获取用户信息面板
- [x] `get_uinfo_user_json`: 通过 JSON 接口获取用户信息
- [x] `profile.get_uinfo_profile`: 获取用户详细资料 (资料页信息)
- [x] `profile.get_homepage`: 获取用户主页帖子列表与资料页快照
- [x] `get_user_contents.get_posts`: 获取用户发表的回复列表
- [x] `get_user_contents.get_threads`: 获取用户发表的主题帖列表
- [x] `get_user_forum_info`: 获取用户在指定贴吧内的信息
- [x] `get_follows`: 获取用户关注的人列表
- [x] `follow_user`: 关注用户
- [x] `unfollow_user`: 取消关注用户
- [x] `get_ats`: 获取 @我的 消息列表
- [x] `get_replys`: 获取 回复我的 消息列表
- [x] `get_fans`: 获取用户的粉丝列表
- [x] `remove_fan`: 移除粉丝
- [x] `get_follow_forums`: 获取他人关注的吧列表
- [x] `get_self_follow_forums`: 获取自己关注的吧列表 (Web 接口)
- [x] `get_self_follow_forums_v1`: 获取自己关注的吧列表 (Web V1 接口)
- [x] `get_dislike_forums`: 获取屏蔽吧列表
- [x] `set_blacklist`: 将用户加入黑名单
- [x] `get_blacklist`: 获取黑名单列表
- [x] `set_profile`: 修改个人资料
- [x] `get_selfinfo_initNickname`: 获取初始昵称信息
- [x] `get_selfinfo_moindex`: 获取主页索引信息
- [x] `tieba_uid2user_info`: 通过 `tieba_uid` 查询用户信息
- [x] `get_rank_users`: 获取用户等级排行榜

> 说明：本清单只保留历史阶段的粗粒度 backlog 视角，不代表当前实现状态；当前支持范围、命名与实现归属请以 `docs/related/parity.md` 为准。

## 4. 搜索与工具 (Search & Tools)
- [x] `get_fid`: 获取贴吧 ID (Fid)
- [x] `get_forum_detail`: 获取贴吧详细信息
- [x] `search_exact`: 精确搜索 (支持按作者、时间范围等)
- [x] `get_cid`: 获取分类 ID
- [x] `get_images`: 下载图片并返回字节流
- [x] `get_last_replyers`: 获取帖子最后回复者的简要信息
- [x] `get_member_users`: 获取吧会员列表
- [x] `get_rank_forums`: 获取吧热度排行榜
- [x] `get_recom_status`: 获取贴吧推荐状态 (大吧主权限)
- [x] `get_square_forums`: 获取吧广场列表
- [x] `get_statistics`: 获取贴吧统计数据

## 5. 消息与即时通讯 (Messaging & WebSocket)
- [x] `get_group_msg`: 获取群聊消息
- [x] `send_msg`: 发送私信
- [x] `send_chatroom_msg`: 发送聊天室(网页/App版)消息
- [x] `set_msg_readed`: 标记消息为已读
- [x] `push_notify`: 解析 App 推送通知
- [x] `init_websocket`: 手动初始化 WebSocket 连接

## 6. 其他功能 (Misc)
- [x] `init_z_id`: 初始化 ZID 设备标识
- [x] `sync`: 同步客户端状态 (ClientId/SampleId)
- [x] `sign_forum`: 贴吧签到
- [x] `follow_forum`: 关注贴吧
- [x] `unfollow_forum`: 取消关注贴吧
- [x] `sign_forums`: 一键签到所有关注的吧
- [x] `sign_growth`: 签到获取成长值
- [x] `get_forum_level`: 获取吧等级详细配置
- [x] `get_roomlist_by_fid`: 获取吧内直播间/聊天室列表
- [x] `dislike_forum`: 屏蔽某个贴吧 (不感兴趣)
- [x] `undislike_forum`: 取消屏蔽

---
*注：以上清单按历史阶段原始整理思路保留；当前能力范围、命名与实现状态请以 `docs/related/parity.md` 为准。*
