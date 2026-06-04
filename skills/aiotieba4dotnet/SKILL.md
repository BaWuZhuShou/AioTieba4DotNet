---
name: aiotieba4dotnet
description: "正确使用已发布的 AioTieba4DotNet 库完成贴吧相关开发，包括安装包、创建 TiebaClient、选择正确模块、调用公开方法、处理常见配置和异常边界。适用于这类请求：怎么用 AioTieba4DotNet、怎么登录后签到/发私信/查帖子、Forums/Threads/Users/Admins/Messages/Client 该用哪个、TiebaClient 怎么初始化、AioTieba4DotNet 某个方法怎么调用、帮我生成用这个库的示例代码。"
---

# AioTieba4DotNet 库使用指南

这个 skill 用于**使用已发布的 `AioTieba4DotNet` 库**，不用于维护它的源码仓库。

如果你在示例里看到 `FORUM_NAME_PLACEHOLDER`、`BDUSS_PLACEHOLDER`、`USER_ID_PLACEHOLDER` 这类名字，可以把它们理解成“这里要换成你自己的真实值”。需要对照时，直接看下面这份[示例占位符说明](#example-placeholders)。

## 示例占位符说明 {#example-placeholders}

| 占位符 | 你通常要换成什么 |
| --- | --- |
| `BDUSS_PLACEHOLDER` / `STOKEN_PLACEHOLDER` | 当前账号的登录凭据 |
| `BDUSS_ACCOUNT_A_PLACEHOLDER` / `STOKEN_ACCOUNT_A_PLACEHOLDER` | 多账号示例里的账号 A 凭据 |
| `BDUSS_ACCOUNT_B_PLACEHOLDER` / `STOKEN_ACCOUNT_B_PLACEHOLDER` | 多账号示例里的账号 B 凭据 |
| `FORUM_NAME_PLACEHOLDER` / `FORUM_ID_PLACEHOLDER` | 贴吧名称或 fid |
| `THREAD_ID_PLACEHOLDER` / `POST_ID_PLACEHOLDER` | 主题帖 id，或楼层 / 楼中楼 id |
| `USER_ID_PLACEHOLDER` / `USER_NAME_PLACEHOLDER` / `PORTRAIT_PLACEHOLDER` | 用户 id、用户名或 portrait |
| `USER_NAME_OR_PORTRAIT_PLACEHOLDER` | 同时接受用户名或 portrait 的公开入口值 |
| `GROUP_ID_PLACEHOLDER` / `CHATROOM_ID_PLACEHOLDER` | 私信分组 id 或吧群聊天室 id |
| `SEARCH_QUERY_PLACEHOLDER` / `MESSAGE_TEXT_PLACEHOLDER` | 搜索词或消息正文 |
| `NICKNAME_PLACEHOLDER` / `SIGNATURE_PLACEHOLDER` | 昵称或个性签名 |
| `IMAGE_HASH_PLACEHOLDER` / `THREAD_CATEGORY_NAME_PLACEHOLDER` | 图片 hash 或帖子分区名 |
| `REASON_PLACEHOLDER` / `APPEAL_ID_PLACEHOLDER` | 吧务理由或解封申诉 id |
| `PROXY_URL_PLACEHOLDER` | 自定义代理地址 |

当参数类型不是 `string` 时，示例会继续用 `int.Parse(...)`、`long.Parse(...)` 或 `ulong.Parse(...)` 包装这些占位符，目的是把目标参数类型写清楚。

## 先看哪里

1. 先读 `references/quickstart.md`，了解安装、初始化、模块入口、常见边界和异常。
2. 如果用户明确要代码、示例、snippet 或模板，优先读 `references/scenario-templates.md`。
3. 再读 `references/method-recipes.md`，按任务选择模块和代表方法。
4. 如果要按固定结构输出答案，再读 `references/output-format.md`。
5. 优先使用稳定公开入口：`TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)`、`ITiebaClientFactory`、`TiebaClientFactory`。
6. 只围绕公开根命名空间以及公开的 `Contracts`、`Models` 概念来回答。

## 这个 skill 应该解决什么问题

- 安装 NuGet 包
- 选择访客、登录态、显式配置、DI 或 factory 的初始化方式
- 按任务挑对公开模块
- 解释常见读写流程
- 把用户目标映射到代表性的公开方法
- 默认产出尽量短、可直接复制的示例代码
- 说明配置、权限、传输和异常边界

## 公开 API 规则

- 当前活动产品线是 v3，只支持 `net10.0`。
- `Messages` 负责 inbox、私信、吧群消息、已读状态和 push 解析。
- `Client` 只负责生命周期初始化，例如 WebSocket 初始化、ZId 初始化和同步状态。
- `GetProfileAsync(...)` 和 `GetHomepageAsync(...)` 是两类独立的用户读取接口。
- `GetUserInfoAppAsync(...)` 和 `GetUserInfoWebAsync(...)` 是并列支持的接口，不是别名。
- `GetBlacklistAsync(...)` 这一组和 `BlacklistOld` 这一组也是并列支持的接口。

## 方法选择规则

- 先按任务选模块，再按模块选方法；不要一上来猜内部 API。
- 读吧、关注吧、签到、搜索、统计，用 `Forums`。
- 读主题帖、楼层、楼中楼，或做回复/点赞/删帖/置顶/加精/移动/推荐，用 `Threads`。
- 查资料、主页、关注关系、黑名单、资料修改，用 `Users`。
- 读 @、回复、私信、吧群消息、push 解析，用 `Messages`。
- 做吧务、封禁、权限、申诉、Bawu 相关操作，用 `Admins`。
- 只有在需要预热链路、初始化 WebSocket、初始化 ZId、同步客户端状态时，才进入 `Client`。

## 代码生成规则

- 默认输出 **最小可运行 C# 示例**，不要只给概念说明。
- 用户明确说“给我代码 / 示例 / 模板 / snippet”时，先匹配 `references/scenario-templates.md` 里的最近场景。
- 用户命中了明显中文任务词时，也优先走模板，不要先自由发挥。
- 先判断是否需要登录态；需要写操作时，默认使用 `new TiebaClient("BDUSS_PLACEHOLDER", "STOKEN_PLACEHOLDER")`。
- 只读场景默认使用访客客户端 `new TiebaClient()`。
- 需要配置传输或超时时，再切换到 `new TiebaClient(new TiebaOptions { ... })`。
- 示例优先直接展示公开模块调用，例如 `client.Forums.*`、`client.Threads.*`、`client.Messages.*`。
- 没有被要求时，不要生成 DI、factory、日志、异常包装等额外样板。
- 除非用户明确要完整项目模板，否则优先输出单文件片段，而不是整套工程。
- 当目标存在多组并列接口时，要明确选用哪一组，并在代码里直接使用对应公开方法。
- 模板优先级默认按这 5 类场景走：访客读取吧与帖子、登录态签到或关注、读楼层或回复、用户资料或主页、消息读取或私信。
- 扩展模板也应参与匹配：搜索、我的关注吧列表、用户关注/粉丝列表、私信会话与已读。
- 默认按 `references/output-format.md` 的结构输出；只有用户明确要求纯代码时，才压缩说明部分。

## 中文触发映射规则

- 命中“查吧 / 读吧 / fid / 吧详情 / 帖子列表 / 主题帖列表”时，优先使用“访客读取吧信息和帖子列表”模板。
- 命中“签到 / 一键签到 / 关注吧 / 取关吧”时，优先使用“登录态签到或关注贴吧”模板。
- 命中“楼层 / 楼中楼 / 回复帖子 / 回帖 / 删帖 / 置顶 / 加精 / 移动 / 推荐”时，优先使用“读取帖子楼层或回复帖子”模板。
- 命中“资料 / 主页 / 用户信息 / 黑名单 / 关注用户 / 改昵称 / 改签名”时，优先使用“查询用户资料或主页”模板。
- 命中“私信 / @ / 回复消息 / inbox / push / 吧群消息”时，优先使用“读取消息或发送私信”模板。
- 命中“搜索 / 搜帖子 / 搜吧 / 关键词查帖 / 精确搜索”时，优先使用“搜索吧内帖子或内容”模板。
- 命中“我的关注吧 / 关注吧列表 / 我关注了哪些吧 / 关注贴吧列表”时，优先使用“读取当前账号关注吧列表”模板。
- 命中“关注列表 / 粉丝列表 / 我关注了谁 / 谁关注了我 / 用户关注关系”时，优先使用“读取用户关注和粉丝列表”模板。
- 命中“私信会话 / 聊天记录 / 私信已读 / 标记已读”时，优先使用“读取私信会话或标记已读”模板。
- 如果同时命中多个模板，优先选择和用户动词最接近的那个；例如“回复消息”归 `Messages`，“回复帖子”归 `Threads`。
- 如果用户明确点名方法名、模块名或返回结构，就以用户指定目标为准，再从最近模板改写。
- `Users` 场景里如果命中“改 / 设置 / 拉黑 / 取消拉黑 / 关注 / 取关”这类写动词，仍然走 Users 模板，但必须切到登录态并改写成对应写方法，不能继续复用 `GetProfileAsync(...)` 读模板。
- `Threads` 场景里如果命中“楼中楼”，优先改写成 `GetCommentsAsync(...)`；如果命中“删帖 / 置顶 / 加精 / 移动 / 推荐”，优先改写成对应的 `Threads` 写方法，而不是停留在 `AddPostAsync(...)`。
- `Messages` 场景里如果命中“吧群消息”，优先改写成 `SendChatroomMessageAsync(...)`；如果命中“解析 push”，优先改写成 `ParsePushNotifications(byte[])`，只有明确要求连接初始化时才补 `client.Client.InitWebSocketAsync()`。

## 不要这样做

- 不要教调用方直接使用内部的 `Api/*`、`Transport/*`、`Session/*`、`Protocols/*` 或生成的 protobuf 类型。
- 不要把消息能力重新归到 `client.Client`。
- 不要声称 v3 支持 `net8.0`、`net9.0` 或多目标框架。
- 不要虚构包里并不存在的公开 API、传输模式或行为。
- 不要只给方法名列表却不给可落地的调用示例，除非用户明确只要索引。

## 输出要求

使用这个 skill 回答时，尽量包含：

1. 一句中文说明应该选哪个公开模块；
2. 1-3 个代表性的公开方法；
3. 一个最小可运行的 C# 示例；
4. 必要时补充登录态、权限、传输或异常注意点。

如果用户没有指定格式，优先使用 `references/output-format.md` 里的标准骨架。
