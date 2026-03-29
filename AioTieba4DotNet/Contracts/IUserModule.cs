using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet;

public interface IUserModule
{
    Task<string> GetTbsAsync(CancellationToken cancellationToken = default);

    Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default);

    Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "",
        CancellationToken cancellationToken = default);

    Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "",
        CancellationToken cancellationToken = default);

    Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default);

    Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default);

    Task<UserList> GetFollowsAsync(long userId, int pn = 1, CancellationToken cancellationToken = default);

    Task<UserList> GetFansAsync(long userId, int pn = 1, CancellationToken cancellationToken = default);

    Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default);

    Task<UserInfoJson> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default);

    Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default);

    Task<AtMessages> GetAtsAsync(int pn = 1, CancellationToken cancellationToken = default);

    Task<ReplyMessages> GetRepliesAsync(int pn = 1, CancellationToken cancellationToken = default);

    Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default);

    Task<bool> SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default);

    Task<UserPostss> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5",
        CancellationToken cancellationToken = default);

    Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true,
        CancellationToken cancellationToken = default);
}
