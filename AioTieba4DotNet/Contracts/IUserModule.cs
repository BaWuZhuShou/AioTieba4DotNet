using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Models;

namespace AioTieba4DotNet.Contracts;

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
    ///     获取推荐的用户基础信息。
    /// </summary>
    /// <remarks>
    ///     这是常规默认读取入口。若需要对齐 upstream Web 兼容形状，请改用 <see cref="GetBasicInfoWebAsync"/>。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>基础信息 <see cref="UserInfoGuInfoApp"/></returns>
    Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按用户 ID 获取推荐的资料页元数据。
    /// </summary>
    /// <remarks>
    ///     该方法读取的是资料页信息本身；若需要用户主页帖子列表与主页快照，请改用 <see cref="GetHomepageAsync"/>。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资料页信息 <see cref="UserInfoPf"/></returns>
    Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按 portrait 或用户名获取推荐的资料页元数据。
    /// </summary>
    /// <remarks>
    ///     该方法读取的是资料页信息本身；若需要用户主页帖子列表与主页快照，请改用 <see cref="GetHomepageAsync"/>。
    /// </remarks>
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

    Task<UserInfo> GetSelfInfoInitNicknameAsync(CancellationToken cancellationToken = default);

    Task<UserInfo> GetSelfInfoMoIndexAsync(CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default);

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
    ///     通过旧版兼容接口获取黑名单列表。
    /// </summary>
    /// <remarks>
    ///     这是 legacy compatibility 路径。常规黑名单读取请优先使用 <see cref="GetBlacklistAsync"/>。
    /// </remarks>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧版黑名单列表 <see cref="BlacklistOldUsers"/></returns>
    Task<BlacklistOldUsers> GetBlacklistLegacyAsync(int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     设置当前黑名单项。
    /// </summary>
    /// <remarks>
    ///     这是当前推荐的黑名单写入入口；旧版兼容写路径请参见 <see cref="AddBlacklistLegacyAsync"/> 和 <see cref="RemoveBlacklistLegacyAsync"/>。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="type">拉黑类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过旧版兼容接口将用户加入黑名单。
    /// </summary>
    /// <remarks>
    ///     这是 legacy compatibility 路径。新的黑名单写入请优先使用 <see cref="SetBlacklistAsync"/>。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> AddBlacklistLegacyAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过旧版兼容接口将用户移出黑名单。
    /// </summary>
    /// <remarks>
    ///     这是 legacy compatibility 路径。新的黑名单写入请优先使用 <see cref="SetBlacklistAsync"/>。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveBlacklistLegacyAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     移除粉丝
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过 Web 兼容接口获取用户基础信息。
    /// </summary>
    /// <remarks>
    ///     这是与 upstream Web 形状对齐的兼容读取入口；常规默认读取请优先使用 <see cref="GetBasicInfoAsync"/>。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Web 用户基础信息 <see cref="UserInfoGuInfoWeb"/></returns>
    Task<UserInfoGuInfoWeb> GetBasicInfoWebAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户在指定贴吧内的信息
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧内信息 <see cref="UserForumInfo"/></returns>
    Task<UserForumInfo> GetUserForumInfoAsync(ulong fid, string portrait, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户在指定贴吧内的信息
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧内信息 <see cref="UserForumInfo"/></returns>
    Task<UserForumInfo> GetUserForumInfoAsync(string fname, string portrait,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧内等级排行榜用户列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>排行榜用户列表 <see cref="RankUsers"/></returns>
    Task<RankUsers> GetRankUsersAsync(string fname, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户主页帖子列表与主页快照。
    /// </summary>
    /// <remarks>
    ///     该方法与 <see cref="GetProfileAsync(int, CancellationToken)"/> / <see cref="GetProfileAsync(string, CancellationToken)"/> 表示不同的 upstream 子能力：资料页元数据查询与主页内容读取不会被折叠为同一个入口。
    /// </remarks>
    /// <param name="userId">用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户主页快照 <see cref="Homepage"/></returns>
    Task<Homepage> GetHomepageAsync(int userId, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过旧版兼容接口设置昵称。
    /// </summary>
    /// <remarks>
    ///     这是 legacy compatibility 路径。若需要当前资料修改入口，请优先使用 <see cref="SetProfileAsync"/>。
    /// </remarks>
    /// <param name="nickName">昵称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetNicknameLegacyAsync(string nickName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     设置当前个人资料。
    /// </summary>
    /// <remarks>
    ///     这是当前推荐的资料修改入口，可一次性更新昵称、签名和性别；旧版单昵称修改兼容路径请参见 <see cref="SetNicknameLegacyAsync"/>。
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
    /// <returns>用户信息 <see cref="UserInfoTUid"/></returns>
    Task<UserInfoTUid> GetUserByTiebaUidAsync(long tiebaUid, CancellationToken cancellationToken = default);

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

    int UserContentCmd => UserContent.Cmd;
}
