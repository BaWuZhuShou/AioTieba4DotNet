using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.GetComments.Entities;
using AioTieba4DotNet.Api.GetForum.Entities;
using AioTieba4DotNet.Api.GetForumDetail.Entities;
using AioTieba4DotNet.Api.GetThreadPosts.Entities;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;
using AioTieba4DotNet.Api.GetUInfoPanel.Entities;
using AioTieba4DotNet.Api.GetUInfoUserJson.Entities;
using AioTieba4DotNet.Api.GetUserContents.Entities;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Abstractions;

/// <summary>
///     贴吧/吧务模块接口
/// </summary>
public interface IForumModule
{
    /// <summary>
    ///     获取贴吧 ID (fid)
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <returns>贴吧 ID (ulong)</returns>
    Task<ulong> GetFidAsync(string fname);

    /// <summary>
    ///     获取贴吧名
    /// </summary>
    /// <param name="fid">贴吧 ID</param>
    /// <returns>贴吧名 (string)</returns>
    Task<string> GetFnameAsync(ulong fid);

    /// <summary>
    ///     获取贴吧详细资料
    /// </summary>
    /// <param name="fid">贴吧 ID</param>
    /// <returns>包含贴吧详细资料的 <see cref="ForumDetail"/> 实体</returns>
    Task<ForumDetail> GetDetailAsync(ulong fid);

    /// <summary>
    ///     获取贴吧详细资料
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <returns>包含贴吧详细资料的 <see cref="ForumDetail"/> 实体</returns>
    Task<ForumDetail> GetDetailAsync(string fname);

    /// <summary>
    ///     关注贴吧
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <returns>是否成功</returns>
    Task<bool> LikeAsync(string fname);

    /// <summary>
    ///     取消关注贴吧
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <returns>是否成功</returns>
    Task<bool> UnlikeAsync(string fname);

    /// <summary>
    ///     贴吧签到
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <returns>是否成功</returns>
    Task<bool> SignAsync(string fname);

    /// <summary>
    ///     获取贴吧基本信息（底层 Frs API）
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <returns>包含贴吧基本信息的 <see cref="Forum"/> 实体</returns>
    Task<Forum> GetForumAsync(string fname);

    /// <summary>
    ///     移除吧务（辞职或撤职）
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="baWuType">吧务类型</param>
    /// <returns>是否成功</returns>
    Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType);
}

/// <summary>
///     帖子/回复模块接口
/// </summary>
public interface IThreadModule
{
    /// <summary>
    ///     当前模块默认请求模式 <see cref="TiebaRequestMode"/>
    /// </summary>
    TiebaRequestMode RequestMode { get; set; }

    /// <summary>
    ///     分页获取贴吧主题帖列表
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="sort">排序方式 <see cref="ThreadSortType"/></param>
    /// <param name="isGood">是否只看精华</param>
    /// <param name="mode">请求模式 <see cref="TiebaRequestMode"/>（覆盖默认值）</param>
    /// <returns>包含主题帖列表的 <see cref="Threads"/> 实体</returns>
    Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply,
        bool isGood = false, TiebaRequestMode? mode = null);

    /// <summary>
    ///     分页获取贴吧主题帖列表
    /// </summary>
    /// <param name="fid">贴吧 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="sort">排序方式 <see cref="ThreadSortType"/></param>
    /// <param name="isGood">是否只看精华</param>
    /// <param name="mode">请求模式 <see cref="TiebaRequestMode"/>（覆盖默认值）</param>
    /// <returns>包含主题帖列表的 <see cref="Threads"/> 实体</returns>
    Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply,
        bool isGood = false, TiebaRequestMode? mode = null);

    /// <summary>
    ///     分页获取主题帖内的回复列表
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="sort">排序方式 <see cref="PostSortType"/></param>
    /// <param name="onlyThreadAuthor">是否只看楼主</param>
    /// <param name="withComments">是否包含楼中楼预览</param>
    /// <param name="commentRn">楼中楼显示数量</param>
    /// <param name="commentSortByAgree">楼中楼是否按点赞数排序</param>
    /// <param name="mode">请求模式 <see cref="TiebaRequestMode"/>（覆盖默认值）</param>
    /// <returns>包含回复列表的 <see cref="Posts"/> 实体</returns>
    Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc,
        bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0, bool commentSortByAgree = false,
        TiebaRequestMode? mode = null);

    /// <summary>
    ///     分页获取楼中楼列表
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID (floor)</param>
    /// <param name="pn">页码</param>
    /// <param name="isComment">是否为子楼中楼</param>
    /// <param name="mode">请求模式 <see cref="TiebaRequestMode"/>（覆盖默认值）</param>
    /// <returns>包含楼中楼列表的 <see cref="Comments"/> 实体</returns>
    Task<Comments> GetCommentsAsync(long tid, long pid, int pn = 1,
        bool isComment = false, TiebaRequestMode? mode = null);

    /// <summary>
    ///     对帖子/回复进行点赞/表态
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID（若为 0 则对主题帖操作）</param>
    /// <param name="isComment">是否为楼中楼</param>
    /// <param name="isDisagree">是否点踩（反对）</param>
    /// <param name="isUndo">是否撤销之前的操作</param>
    /// <returns>是否成功</returns>
    Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false, bool isUndo = false);

    /// <summary>
    ///     点踩
    /// </summary>
    Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false);

    /// <summary>
    ///     取消点赞
    /// </summary>
    Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false);

    /// <summary>
    ///     取消点踩
    /// </summary>
    Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false);


    /// <summary>
    ///     回复帖子
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="content">回复内容</param>
    /// <param name="showName">展示名称 (昵称)</param>
    /// <param name="mode">请求模式</param>
    /// <returns>是否成功</returns>
    Task<bool> AddPostAsync(string fname, long tid, string content, string? showName = null,
        TiebaRequestMode? mode = null);

    /// <summary>
    ///     删除主题帖
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DelThreadAsync(string fname, long tid);

    /// <summary>
    ///     删除回复或楼中楼
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DelPostAsync(string fname, long tid, long pid);
}

/// <summary>
///     用户/社交模块接口
/// </summary>
public interface IUserModule
{
    /// <summary>
    ///     当前模块默认请求模式 <see cref="TiebaRequestMode"/>
    /// </summary>
    TiebaRequestMode RequestMode { get; set; }

    /// <summary>
    ///     获取用户 tbs（用于某些写操作的安全令牌）
    /// </summary>
    /// <returns>tbs 字符串 (string)</returns>
    Task<string> GetTbsAsync();

    /// <summary>
    ///     获取用户基础信息（通过 userId）
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <returns>包含基础信息的 <see cref="UserInfoGuInfoApp"/> 实体</returns>
    Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId);

    /// <summary>
    ///     获取用户详细资料（个人主页）
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <returns>包含详细资料的 <see cref="UserInfoPf"/> 实体</returns>
    Task<UserInfoPf> GetProfileAsync(int userId);

    /// <summary>
    ///     获取用户详细资料（个人主页）
    /// </summary>
    /// <param name="portraitOrUserName">用户 portrait 或用户名</param>
    /// <returns>包含详细资料的 <see cref="UserInfoPf"/> 实体</returns>
    Task<UserInfoPf> GetProfileAsync(string portraitOrUserName);

    /// <summary>
    ///     封禁用户（吧务功能）
    /// </summary>
    /// <param name="fid">贴吧 ID</param>
    /// <param name="portrait">被封禁用户的 portrait</param>
    /// <param name="day">封禁时长（天）</param>
    /// <param name="reason">封禁原因</param>
    /// <returns>是否成功</returns>
    Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "");

    /// <summary>
    ///     封禁用户（吧务功能）
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <param name="portrait">被封禁用户的 portrait</param>
    /// <param name="day">封禁时长（天）</param>
    /// <param name="reason">封禁原因</param>
    /// <returns>是否成功</returns>
    Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "");

    /// <summary>
    ///     关注用户
    /// </summary>
    /// <param name="portrait">目标用户 portrait</param>
    /// <returns>是否成功</returns>
    Task<bool> FollowAsync(string portrait);

    /// <summary>
    ///     取消关注用户
    /// </summary>
    /// <param name="portrait">目标用户 portrait</param>
    /// <returns>是否成功</returns>
    Task<bool> UnfollowAsync(string portrait);

    /// <summary>
    ///     分页获取用户关注列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <returns>包含用户关注列表的 <see cref="UserList"/> 实体</returns>
    Task<UserList> GetFollowsAsync(long userId, int pn = 1);

    /// <summary>
    ///     获取用户信息面板（浮窗信息）
    /// </summary>
    /// <param name="nameOrPortrait">用户名或 portrait</param>
    /// <returns>包含面板信息的 <see cref="UserInfoPanel"/> 实体</returns>
    Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait);

    /// <summary>
    ///     通过 JSON API 获取用户信息
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>包含用户信息的 <see cref="UserInfoJson"/> 实体</returns>
    Task<UserInfoJson> GetUserInfoJsonAsync(string username);


    /// <summary>
    ///     分页获取用户发表的回复列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页条数</param>
    /// <param name="version">模拟的客户端版本</param>
    /// <param name="mode">请求模式 <see cref="TiebaRequestMode"/></param>
    /// <returns>包含用户回复列表的 <see cref="UserPostss"/> 实体</returns>
    Task<UserPostss> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5",
        TiebaRequestMode? mode = null);

    /// <summary>
    ///     分页获取用户发表的主题帖列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="publicOnly">是否只获取公开贴子</param>
    /// <param name="mode">请求模式 <see cref="TiebaRequestMode"/></param>
    /// <returns>包含用户主题帖列表的 <see cref="UserThreads"/> 实体</returns>
    Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true, TiebaRequestMode? mode = null);
}

/// <summary>
///     客户端底层模块接口
/// </summary>
public interface IClientModule
{
    /// <summary>
    ///     初始化 ZID 令牌（用于身份识别）
    /// </summary>
    /// <returns>ZID 字符串 (string)</returns>
    Task<string> InitZIdAsync();

    /// <summary>
    ///     同步客户端配置（SampleId 等参数）
    /// </summary>
    /// <returns>包含 ClientId 和 SampleId 的元组</returns>
    Task<(string ClientId, string SampleId)> SyncAsync();
}

/// <summary>
///     贴吧客户端接口，提供各功能模块的访问入口
/// </summary>
public interface ITiebaClient : IDisposable
{
    /// <summary>
    ///     获取或设置全局请求模式（HTTP 或 WebSocket）
    /// </summary>
    TiebaRequestMode RequestMode { get; set; }

    /// <summary>
    ///     HTTP 核心组件
    /// </summary>
    ITiebaHttpCore HttpCore { get; }

    /// <summary>
    ///     贴吧/吧务模块 <see cref="IForumModule"/>
    /// </summary>
    IForumModule Forums { get; }

    /// <summary>
    ///     帖子/回复模块 <see cref="IThreadModule"/>
    /// </summary>
    IThreadModule Threads { get; }

    /// <summary>
    ///     用户/社交模块 <see cref="IUserModule"/>
    /// </summary>
    IUserModule Users { get; }

    /// <summary>
    ///     客户端底层模块 <see cref="IClientModule"/>
    /// </summary>
    IClientModule Client { get; }

    /// <summary>
    ///     WebSocket 核心组件 <see cref="ITiebaWsCore"/>
    /// </summary>
    ITiebaWsCore WsCore { get; }
}

/// <summary>
///     贴吧客户端工厂接口，用于在依赖注入模式下动态创建多账号实例
/// </summary>
public interface ITiebaClientFactory
{
    /// <summary>
    ///     使用配置项创建客户端
    /// </summary>
    /// <param name="options">配置参数</param>
    /// <returns>贴吧客户端实例</returns>
    ITiebaClient CreateClient(TiebaOptions options);

    /// <summary>
    ///     使用账号信息创建客户端
    /// </summary>
    /// <param name="bduss">BDUSS 凭证</param>
    /// <param name="stoken">STOKEN 凭证（可选）</param>
    /// <returns>贴吧客户端实例</returns>
    ITiebaClient CreateClient(string bduss, string stoken = "");
}
