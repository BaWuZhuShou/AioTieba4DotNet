using AioTieba4DotNet.Api.GetForumDetail.Entities;
using AioTieba4DotNet.Api.GetForum.Entities;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Api.GetThreadPosts.Entities;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;
using AioTieba4DotNet.Api.GetUInfoPanel.Entities;
using AioTieba4DotNet.Api.GetUInfoUserJson.Entities;
using AioTieba4DotNet.Api.Login.Entities;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;
using AioTieba4DotNet.Api.GetUserContents.Entities;
using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Abstractions;

/// <summary>
/// 贴吧/吧务模块接口
/// </summary>
public interface IForumModule
{
    Task<ulong> GetFidAsync(string fname);
    Task<string> GetFnameAsync(ulong fid);
    Task<ForumDetail> GetDetailAsync(ulong fid);
    Task<ForumDetail> GetDetailAsync(string fname);

    Task<bool> LikeAsync(string fname);
    Task<bool> UnlikeAsync(string fname);
    Task<bool> SignAsync(string fname);
    Task<Forum> GetForumAsync(string fname);
    Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType);
}

/// <summary>
/// 帖子/回复模块接口
/// </summary>
public interface IThreadModule
{
    /// <summary>
    /// 当前模块默认请求模式
    /// </summary>
    TiebaRequestMode RequestMode { get; set; }
    Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply, bool isGood = false, TiebaRequestMode? mode = null);
    Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply, bool isGood = false, TiebaRequestMode? mode = null);

    Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc, bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0, bool commentSortByAgree = false, TiebaRequestMode? mode = null);
    Task<AioTieba4DotNet.Api.GetComments.Entities.Comments> GetCommentsAsync(long tid, long pid, int pn = 1, bool isComment = false, TiebaRequestMode? mode = null);
    
    Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false, bool isUndo = false);
    Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false);
    Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false);
    Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false);

    Task<long> AddThreadAsync(string fname, string title, string content);
    Task<long> AddThreadAsync(string fname, string title, List<IFrag> contents);

    Task<long> AddPostAsync(string fname, long tid, string content, long quoteId = 0, uint floor = 0);
    Task<long> AddPostAsync(string fname, long tid, List<IFrag> contents, long quoteId = 0, uint floor = 0);

    Task<bool> DelThreadAsync(string fname, long tid);
    Task<bool> DelPostAsync(string fname, long tid, long pid);
}

/// <summary>
/// 用户/社交模块接口
/// </summary>
public interface IUserModule
{
    /// <summary>
    /// 当前模块默认请求模式
    /// </summary>
    TiebaRequestMode RequestMode { get; set; }
    Task<string> GetTbsAsync();
    Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId);
    Task<UserInfoPf> GetProfileAsync(int userId);
    Task<UserInfoPf> GetProfileAsync(string portraitOrUserName);
    Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "");
    Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "");
    Task<bool> FollowAsync(string portrait);
    Task<bool> UnfollowAsync(string portrait);

    Task<UserList> GetFollowsAsync(long userId, int pn = 1);
    Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait);
    Task<UserInfoJson> GetUserInfoJsonAsync(string username);
    Task<(UserInfoLogin User, string Tbs)> LoginAsync();

    Task<UserPostss> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5", TiebaRequestMode? mode = null);
    Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true, TiebaRequestMode? mode = null);
}

/// <summary>
/// 客户端底层模块接口
/// </summary>
public interface IClientModule
{
    Task<string> InitZIdAsync();
    Task<(string ClientId, string SampleId)> SyncAsync();
}


/// <summary>
/// 贴吧客户端接口，提供各功能模块的访问入口
/// </summary>
public interface ITiebaClient : IDisposable
{
    /// <summary>
    /// 获取或设置全局请求模式（HTTP 或 WebSocket）
    /// </summary>
    TiebaRequestMode RequestMode { get; set; }

    /// <summary>
    /// HTTP 核心组件
    /// </summary>
    ITiebaHttpCore HttpCore { get; }

    /// <summary>
    /// 贴吧/吧务模块
    /// </summary>
    IForumModule Forums { get; }

    /// <summary>
    /// 帖子/回复模块
    /// </summary>
    IThreadModule Threads { get; }

    /// <summary>
    /// 用户/社交模块
    /// </summary>
    IUserModule Users { get; }

    /// <summary>
    /// 客户端底层模块
    /// </summary>
    IClientModule Client { get; }

    /// <summary>
    /// WebSocket 核心组件
    /// </summary>
    ITiebaWsCore WsCore { get; }
}

/// <summary>
/// 贴吧客户端工厂接口，用于在依赖注入模式下动态创建多账号实例
/// </summary>
public interface ITiebaClientFactory
{
    /// <summary>
    /// 使用配置项创建客户端
    /// </summary>
    /// <param name="options">配置参数</param>
    /// <returns>贴吧客户端实例</returns>
    ITiebaClient CreateClient(TiebaOptions options);

    /// <summary>
    /// 使用账号信息创建客户端
    /// </summary>
    /// <param name="bduss">BDUSS 凭证</param>
    /// <param name="stoken">STOKEN 凭证（可选）</param>
    /// <returns>贴吧客户端实例</returns>
    ITiebaClient CreateClient(string bduss, string stoken = "");
}
