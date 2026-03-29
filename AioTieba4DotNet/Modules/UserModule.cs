using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Enums;

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
    public Task<string> GetTbsAsync(CancellationToken cancellationToken = default) =>
        _protocol.GetTbsAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId, CancellationToken cancellationToken = default) =>
        _protocol.GetBasicInfoAsync(userId, cancellationToken);

    /// <inheritdoc/>
    public Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default) =>
        _protocol.GetProfileAsync(userId, cancellationToken);

    /// <inheritdoc/>
    public Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default) =>
        _protocol.GetProfileAsync(portraitOrUserName, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "",
        CancellationToken cancellationToken = default) =>
        _protocol.BlockAsync(fid, portrait, day, reason, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "",
        CancellationToken cancellationToken = default) =>
        _protocol.BlockAsync(fname, portrait, day, reason, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default) =>
        _protocol.FollowAsync(portrait, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default) =>
        _protocol.UnfollowAsync(portrait, cancellationToken);

    /// <inheritdoc/>
    public Task<UserList> GetFollowsAsync(long userId, int pn = 1, CancellationToken cancellationToken = default) =>
        _protocol.GetFollowsAsync(userId, pn, cancellationToken);

    /// <inheritdoc/>
    public Task<UserList> GetFansAsync(long userId, int pn = 1, CancellationToken cancellationToken = default) =>
        _protocol.GetFansAsync(userId, pn, cancellationToken);

    /// <inheritdoc/>
    public Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default) =>
        _protocol.GetPanelInfoAsync(nameOrPortrait, cancellationToken);

    /// <inheritdoc/>
    public Task<UserInfoJson> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default) =>
        _protocol.GetUserInfoJsonAsync(username, cancellationToken);

    /// <inheritdoc/>
    public Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default) =>
        _protocol.GetSelfInfoAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<AtMessages> GetAtsAsync(int pn = 1, CancellationToken cancellationToken = default) =>
        _protocol.GetAtsAsync(pn, cancellationToken);

    /// <inheritdoc/>
    public Task<ReplyMessages> GetRepliesAsync(int pn = 1, CancellationToken cancellationToken = default) =>
        _protocol.GetRepliesAsync(pn, cancellationToken);

    /// <inheritdoc/>
    public Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default) =>
        _protocol.GetBlacklistAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<bool> SetBlacklistAsync(long userId, BlacklistType type = BlacklistType.All,
        CancellationToken cancellationToken = default) =>
        _protocol.SetBlacklistAsync(userId, type, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default) =>
        _protocol.RemoveFanAsync(userId, cancellationToken);

    /// <inheritdoc/>
    public Task<UserPostss> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5",
        CancellationToken cancellationToken = default) =>
        _protocol.GetPostsAsync(userId, pn, rn, version, cancellationToken);

    /// <inheritdoc/>
    public Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true,
        CancellationToken cancellationToken = default) =>
        _protocol.GetThreadsAsync(userId, pn, publicOnly, cancellationToken);
}
