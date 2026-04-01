using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Models;

namespace AioTieba4DotNet.Protocols;

internal interface IUserProtocol
{
    Task<string> GetTbsAsync(CancellationToken cancellationToken = default);

    Task<UserInfoGuInfoApp> GetUserInfoAppAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default);

    Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default);

    Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default);

    Task<UserList> GetFollowsAsync(long userId, int pn, CancellationToken cancellationToken = default);

    Task<UserList> GetFansAsync(long userId, int pn, CancellationToken cancellationToken = default);

    Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default);

    Task<UserInfoJson> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default);

    Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default);

    Task<UserInfo> GetSelfInfoInitNicknameAsync(CancellationToken cancellationToken = default);

    Task<UserInfo> GetSelfInfoMoIndexAsync(CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default);

    Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default);

    Task<BlacklistOldUsers> GetBlacklistOldAsync(int pn, int rn, CancellationToken cancellationToken = default);

    Task<bool> SetBlacklistAsync(long userId, BlacklistType type, CancellationToken cancellationToken = default);

    Task<bool> AddBlacklistOldAsync(long userId, CancellationToken cancellationToken = default);

    Task<bool> RemoveBlacklistOldAsync(long userId, CancellationToken cancellationToken = default);

    Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default);

    Task<UserInfoGuInfoWeb> GetUserInfoWebAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserForumInfo> GetUserForumInfoAsync(ulong fid, string portrait, CancellationToken cancellationToken = default);

    Task<UserForumInfo> GetUserForumInfoAsync(string fname, string portrait,
        CancellationToken cancellationToken = default);

    Task<RankUsers> GetRankUsersAsync(string fname, int pn, CancellationToken cancellationToken = default);

    Task<Homepage> GetHomepageAsync(int userId, int pn, CancellationToken cancellationToken = default);

    Task<bool> SetNicknameAsync(string nickName, CancellationToken cancellationToken = default);

    Task<bool> SetProfileAsync(string nickName, string sign, Gender gender,
        CancellationToken cancellationToken = default);

    Task<UserInfoTUid> GetUserByTiebaUidAsync(long tiebaUid, CancellationToken cancellationToken = default);

    Task<UserPostGroups> GetPostsAsync(int userId, uint pn, uint rn, string version,
        CancellationToken cancellationToken = default);

    Task<UserThreads> GetThreadsAsync(int userId, uint pn, bool publicOnly,
        CancellationToken cancellationToken = default);
}
