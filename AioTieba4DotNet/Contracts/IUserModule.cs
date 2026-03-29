using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet;

/// <summary>
///     用户模块契约
/// </summary>
public interface IUserModule
{
    /// <summary>
    ///     获取当前会话的 TBS
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>TBS 字符串</returns>
    Task<string> GetTbsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户基础信息
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>基础信息 <see cref="UserInfoGuInfoApp"/></returns>
    Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按用户 ID 获取资料页信息
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资料页信息 <see cref="UserInfoPf"/></returns>
    Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按 portrait 或用户名获取资料页信息
    /// </summary>
    /// <param name="portraitOrUserName">portrait 或用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资料页信息 <see cref="UserInfoPf"/></returns>
    Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 封禁用户
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="day">封禁天数</param>
    /// <param name="reason">封禁理由</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧名封禁用户
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="day">封禁天数</param>
    /// <param name="reason">封禁理由</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     关注用户
    /// </summary>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消关注用户
    /// </summary>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取关注列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关注列表 <see cref="UserList"/></returns>
    Task<UserList> GetFollowsAsync(long userId, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取粉丝列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>粉丝列表 <see cref="UserList"/></returns>
    Task<UserList> GetFansAsync(long userId, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户面板信息
    /// </summary>
    /// <param name="nameOrPortrait">用户名或 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>面板信息 <see cref="UserInfoPanel"/></returns>
    Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户 JSON 信息
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户 JSON 信息 <see cref="UserInfoJson"/></returns>
    Task<UserInfoJson> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取当前用户信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户信息 <see cref="UserInfo"/></returns>
    Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取 @ 消息列表
    /// </summary>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>@ 消息列表 <see cref="AtMessages"/></returns>
    Task<AtMessages> GetAtsAsync(int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取回复消息列表
    /// </summary>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回复消息列表 <see cref="ReplyMessages"/></returns>
    Task<ReplyMessages> GetRepliesAsync(int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取黑名单列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>黑名单列表 <see cref="BlacklistUsers"/></returns>
    Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     设置黑名单项
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="type">拉黑类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     移除粉丝
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户回复列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="version">客户端版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户回复列表 <see cref="UserPostss"/></returns>
    Task<UserPostss> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户主题帖列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="publicOnly">是否只看公开内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户主题帖列表 <see cref="UserThreads"/></returns>
    Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true,
        CancellationToken cancellationToken = default);
}
