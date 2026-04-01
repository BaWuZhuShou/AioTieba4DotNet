using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Models;

namespace AioTieba4DotNet.Modules;

/// <summary>
///     用户模块默认实现
/// </summary>
public class UserModule : IUserModule
{
    private readonly IUserProtocol _protocol;

    internal UserModule(IUserProtocol protocol)
    {
        _protocol = protocol;
    }

    /// <inheritdoc/>
    public Task<string> GetTbsAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.GetTbsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfoGuInfoApp> GetUserInfoAppAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _protocol.GetUserInfoAppAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _protocol.GetProfileAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default)
    {
        return _protocol.GetProfileAsync(portraitOrUserName, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default)
    {
        return _protocol.FollowAsync(portrait, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default)
    {
        return _protocol.UnfollowAsync(portrait, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserList> GetFollowsAsync(long userId, int pn = 1, CancellationToken cancellationToken = default)
    {
        return _protocol.GetFollowsAsync(userId, pn, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserList> GetFansAsync(long userId, int pn = 1, CancellationToken cancellationToken = default)
    {
        return _protocol.GetFansAsync(userId, pn, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default)
    {
        return _protocol.GetPanelInfoAsync(nameOrPortrait, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfoJson> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default)
    {
        return _protocol.GetUserInfoJsonAsync(username, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.GetSelfInfoAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfo> GetSelfInfoInitNicknameAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.GetSelfInfoInitNicknameAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfo> GetSelfInfoMoIndexAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.GetSelfInfoMoIndexAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.LoginAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.GetBlacklistAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BlacklistOldUsers> GetBlacklistOldAsync(int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetBlacklistOldAsync(pn, rn, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All,
        CancellationToken cancellationToken = default)
    {
        return _protocol.SetBlacklistAsync(userId, type, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> AddBlacklistOldAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _protocol.AddBlacklistOldAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> RemoveBlacklistOldAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _protocol.RemoveBlacklistOldAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _protocol.RemoveFanAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfoGuInfoWeb> GetUserInfoWebAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _protocol.GetUserInfoWebAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserForumInfo> GetUserForumInfoAsync(ulong fid, string portrait,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetUserForumInfoAsync(fid, portrait, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserForumInfo> GetUserForumInfoAsync(string fname, string portrait,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetUserForumInfoAsync(fname, portrait, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<RankUsers> GetRankUsersAsync(string fname, int pn = 1, CancellationToken cancellationToken = default)
    {
        return _protocol.GetRankUsersAsync(fname, pn, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Homepage> GetHomepageAsync(int userId, int pn = 1, CancellationToken cancellationToken = default)
    {
        return _protocol.GetHomepageAsync(userId, pn, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> SetNicknameAsync(string nickName, CancellationToken cancellationToken = default)
    {
        return _protocol.SetNicknameAsync(nickName, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> SetProfileAsync(string nickName, string sign, Gender gender,
        CancellationToken cancellationToken = default)
    {
        return _protocol.SetProfileAsync(nickName, sign, gender, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserInfoTUid> GetUserByTiebaUidAsync(long tiebaUid, CancellationToken cancellationToken = default)
    {
        return _protocol.GetUserByTiebaUidAsync(tiebaUid, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserPostGroups> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5",
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetPostsAsync(userId, pn, rn, version, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetThreadsAsync(userId, pn, publicOnly, cancellationToken);
    }
}
