using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Contracts;

/// <summary>
///     用户模块契约
/// </summary>
public interface IUserModule
{
    int UserContentCmd => UserContent.Cmd;

    /// <summary>
    ///     获取当前会话的 TBS
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>TBS 字符串</returns>
    Task<string> GetTbsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过 App `user_info` 接口获取用户信息。
    /// </summary>
    /// <remarks>
    ///     该方法对应 aiotieba `get_uinfo_getuserinfo_app`；若需要读取并列支持的 Web `user_info` 接口，请改用 <see cref="GetUserInfoWebAsync" />。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>App 用户信息 <see cref="UserInfo" /></returns>
    Task<UserInfo> GetUserInfoAppAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按用户 ID 获取推荐的资料页元数据。
    /// </summary>
    /// <remarks>
    ///     该方法读取的是资料页信息本身；若需要用户主页帖子列表与主页快照，请改用 <see cref="GetHomepageAsync" />。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资料页信息 <see cref="UserInfoPf" /></returns>
    Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按 portrait 或用户名获取推荐的资料页元数据。
    /// </summary>
    /// <remarks>
    ///     该方法读取的是资料页信息本身；若需要用户主页帖子列表与主页快照，请改用 <see cref="GetHomepageAsync" />。
    /// </remarks>
    /// <param name="portraitOrUserName">portrait 或用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资料页信息 <see cref="UserInfoPf" /></returns>
    Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default);

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
    /// <returns>关注列表 <see cref="UserList" /></returns>
    Task<UserList> GetFollowsAsync(long userId, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取粉丝列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>粉丝列表 <see cref="UserList" /></returns>
    Task<UserList> GetFansAsync(long userId, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户面板信息
    /// </summary>
    /// <param name="nameOrPortrait">用户名或 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>面板信息 <see cref="UserInfo" /></returns>
    Task<UserInfo> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户 JSON 信息
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户 JSON 信息 <see cref="UserInfo" /></returns>
    Task<UserInfo> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取当前用户信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户信息 <see cref="UserInfo" /></returns>
    Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default);

    Task<UserInfo> GetSelfInfoInitNicknameAsync(CancellationToken cancellationToken = default);

    Task<UserInfo> GetSelfInfoMoIndexAsync(CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取黑名单列表。
    /// </summary>
    /// <remarks>
    ///     该方法对应 aiotieba `get_blacklist`；若需要 `get_blacklist_old` 这一组并列支持的接口，请改用 <see cref="GetBlacklistOldAsync" />。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>黑名单用户列表 <see cref="BlacklistUsers" /></returns>
    Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取黑名单 `_old` 列表。
    /// </summary>
    /// <remarks>
    ///     该方法对应 aiotieba `get_blacklist_old`，并保留 upstream `_old` 这一组接口自己的分页与返回形状；若需要 `get_blacklist`，请改用
    ///     <see cref="GetBlacklistAsync" />。
    /// </remarks>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>黑名单 `_old` 用户列表 <see cref="BlacklistOldUsers" /></returns>
    Task<BlacklistOldUsers> GetBlacklistOldAsync(int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     设置当前黑名单权限项。
    /// </summary>
    /// <remarks>
    ///     该方法对应 aiotieba `set_blacklist`；若需要 `_old` 这一组接口里的 add/remove 写入路径，请参见 <see cref="AddBlacklistOldAsync" /> 和
    ///     <see cref="RemoveBlacklistOldAsync" />。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="type">拉黑类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过 `_old` 这一组接口将用户加入黑名单。
    /// </summary>
    /// <remarks>
    ///     该入口对应 aiotieba `add_blacklist_old`，保留的是 upstream `_old` 写入语义；若需要 `set_blacklist`，请改用
    ///     <see cref="SetBlacklistAsync" />。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> AddBlacklistOldAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过 `_old` 这一组接口将用户移出黑名单。
    /// </summary>
    /// <remarks>
    ///     该入口对应 aiotieba `del_blacklist_old`，保留的是 upstream `_old` 删除语义；若需要 `set_blacklist`，请改用
    ///     <see cref="SetBlacklistAsync" />。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveBlacklistOldAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     移除粉丝
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过 Web `user_info` 接口获取用户信息。
    /// </summary>
    /// <remarks>
    ///     该方法对应 aiotieba `get_uinfo_getuserinfo_web`；它和 <see cref="GetUserInfoAppAsync" /> 是并列支持的 `user_info`
    ///     接口，而不是资料页信息或主页内容。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Web 用户信息 <see cref="UserInfo" /></returns>
    Task<UserInfo> GetUserInfoWebAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户在指定贴吧内的信息
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧内信息 <see cref="UserForumInfo" /></returns>
    Task<UserForumInfo> GetUserForumInfoAsync(ulong fid, string portrait,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户在指定贴吧内的信息
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧内信息 <see cref="UserForumInfo" /></returns>
    Task<UserForumInfo> GetUserForumInfoAsync(string fname, string portrait,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧内等级排行榜用户列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>排行榜用户列表 <see cref="RankUsers" /></returns>
    Task<RankUsers> GetRankUsersAsync(string fname, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户主页帖子列表与主页快照。
    /// </summary>
    /// <remarks>
    ///     该方法与 <see cref="GetProfileAsync(int, CancellationToken)" /> /
    ///     <see cref="GetProfileAsync(string, CancellationToken)" /> 对应不同的 upstream 接口：
    ///     <see cref="GetProfileAsync(int, CancellationToken)" /> 和 <see cref="GetProfileAsync(string, CancellationToken)" />
    ///     读取资料页信息，而此方法读取主页内容与主页快照。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户主页快照 <see cref="Homepage" /></returns>
    Task<Homepage> GetHomepageAsync(int userId, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过单字段昵称接口设置当前昵称。
    /// </summary>
    /// <remarks>
    ///     该方法对应 aiotieba `set_nickname_old` 这一组独立写入接口；若需要同时更新昵称、签名和性别，请改用 <see cref="SetProfileAsync" />。
    /// </remarks>
    /// <param name="nickName">昵称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetNicknameAsync(string nickName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     设置当前个人资料。
    /// </summary>
    /// <remarks>
    ///     这是资料页信息的写入入口，可一次性更新昵称、签名和性别；若只需要单字段昵称修改，请参见 <see cref="SetNicknameAsync" />。
    /// </remarks>
    /// <param name="nickName">昵称</param>
    /// <param name="sign">个性签名</param>
    /// <param name="gender">性别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetProfileAsync(string nickName, string sign, Gender gender,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过 Tieba UID 查询用户信息
    /// </summary>
    /// <param name="tiebaUid">Tieba UID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息 <see cref="UserInfo" /></returns>
    Task<UserInfo> GetUserByTiebaUidAsync(long tiebaUid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户回复列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="version">客户端版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户回复分组列表 <see cref="UserPostGroups" /></returns>
    Task<UserPostGroups> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户主题帖列表
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="publicOnly">是否只看公开内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户主题帖列表 <see cref="UserThreads" /></returns>
    Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true,
        CancellationToken cancellationToken = default);
}
