using AioTieba4DotNet.Api.Block;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Api.GetAts;
using AioTieba4DotNet.Api.GetBlacklist;
using AioTieba4DotNet.Api.GetFans;
using AioTieba4DotNet.Api.FollowUser;
using AioTieba4DotNet.Api.GetFollows;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp;
using AioTieba4DotNet.Api.GetUInfoPanel;
using AioTieba4DotNet.Api.GetReplys;
using AioTieba4DotNet.Api.GetSelfInfoInitNickname;
using AioTieba4DotNet.Api.GetSelfInfoMoIndex;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Api.GetUInfoUserJson;
using AioTieba4DotNet.Api.GetUserContents;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile;
using AioTieba4DotNet.Api.RemoveFan;
using AioTieba4DotNet.Api.SetBlacklist;
using AioTieba4DotNet.Api.UnfollowUser;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Protocols;

internal sealed class LegacyUserProtocol(LegacyTransportContext transport, IForumProtocol forums) : IUserProtocol
{
    public async Task<string> GetTbsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await transport.RequireSession(nameof(GetTbsAsync)).GetTbsAsync(cancellationToken);
    }

    public async Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetUInfoGetUserInfoApp(transport.HttpCore);
        return await api.RequestAsync(userId, cancellationToken);
    }

    public async Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetUInfoProfile<int>(transport.HttpCore);
        return await api.RequestAsync(userId, cancellationToken);
    }

    public async Task<UserInfoPf> GetProfileAsync(string portraitOrUserName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetUInfoProfile<string>(transport.HttpCore);
        return await api.RequestAsync(portraitOrUserName, cancellationToken);
    }

    public async Task<bool> BlockAsync(ulong fid, string portrait, int day, string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(BlockAsync));
        await session.EnsureTbsAsync(nameof(BlockAsync), cancellationToken);
        var api = new Block(transport.HttpCore);
        return await api.RequestAsync(fid, portrait, day, reason, cancellationToken);
    }

    public async Task<bool> BlockAsync(string fname, string portrait, int day, string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(BlockAsync));
        await session.EnsureTbsAsync(nameof(BlockAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await BlockAsync(fid, portrait, day, reason, cancellationToken);
    }

    public async Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(FollowAsync));
        await session.EnsureTbsAsync(nameof(FollowAsync), cancellationToken);
        var api = new FollowUser(transport.HttpCore);
        return await api.RequestAsync(portrait, cancellationToken);
    }

    public async Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(UnfollowAsync));
        await session.EnsureTbsAsync(nameof(UnfollowAsync), cancellationToken);
        var api = new UnfollowUser(transport.HttpCore);
        return await api.RequestAsync(portrait, cancellationToken);
    }

    public async Task<UserList> GetFollowsAsync(long userId, int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        ValidatePageNumber(pn);
        var api = new GetFollows(transport.HttpCore);
        return await api.RequestAsync(userId, pn, cancellationToken);
    }

    public async Task<UserList> GetFansAsync(long userId, int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        ValidatePageNumber(pn);
        transport.RequireSession(nameof(GetFansAsync)).RequireAuthenticatedAccount(nameof(GetFansAsync));
        var api = new GetFans(transport.HttpCore);
        return await api.RequestAsync(userId, pn, cancellationToken);
    }

    public async Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetUInfoPanel(transport.HttpCore);
        return await api.RequestAsync(nameOrPortrait, cancellationToken);
    }

    public async Task<UserInfoJson> GetUserInfoJsonAsync(string username,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetUInfoUserJson(transport.HttpCore);
        return await api.RequestAsync(username, cancellationToken);
    }

    public async Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        transport.RequireSession(nameof(GetSelfInfoAsync)).RequireAuthenticatedAccount(nameof(GetSelfInfoAsync));

        var initNicknameApi = new GetSelfInfoInitNickname(transport.HttpCore);
        var moIndexApi = new GetSelfInfoMoIndex(transport.HttpCore);
        var initNickname = await initNicknameApi.RequestAsync(cancellationToken);
        var moIndex = await moIndexApi.RequestAsync(cancellationToken);

        return MergeSelfInfo(initNickname, moIndex);
    }

    public async Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);
        transport.RequireSession(nameof(GetAtsAsync)).RequireAuthenticatedAccount(nameof(GetAtsAsync));
        var api = new GetAts(transport.HttpCore);
        return await api.RequestAsync(pn, cancellationToken);
    }

    public async Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);
        transport.RequireSession(nameof(GetRepliesAsync)).RequireAuthenticatedAccount(nameof(GetRepliesAsync));
        var api = new GetReplys(transport.HttpCore);
        return await api.RequestAsync(pn, cancellationToken);
    }

    public async Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        transport.RequireSession(nameof(GetBlacklistAsync)).RequireAuthenticatedAccount(nameof(GetBlacklistAsync));
        var api = new GetBlacklist(transport.HttpCore);
        return await api.RequestAsync(cancellationToken);
    }

    public async Task<bool> SetBlacklistAsync(long userId, BlacklistType type,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        transport.RequireSession(nameof(SetBlacklistAsync)).RequireAuthenticatedAccount(nameof(SetBlacklistAsync));
        var api = new SetBlacklist(transport.HttpCore);
        return await api.RequestAsync(userId, type, cancellationToken);
    }

    public async Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        var session = transport.RequireSession(nameof(RemoveFanAsync));
        await session.EnsureTbsAsync(nameof(RemoveFanAsync), cancellationToken);
        var api = new RemoveFan(transport.HttpCore);
        return await api.RequestAsync(userId, cancellationToken);
    }

    public async Task<UserPostss> GetPostsAsync(int userId, uint pn, uint rn, string version,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        transport.RequireSession(nameof(GetPostsAsync)).RequireAuthenticatedAccount(nameof(GetPostsAsync));
        var api = new GetPosts(transport.HttpCore, transport.WsCore);
        return await transport.Dispatcher.DispatchAsync(
            LegacyTransportOperation.GetUserPosts,
            ct => api.RequestHttpAsync(userId, pn, rn, version, ct),
            ct => api.RequestWsAsync(userId, pn, rn, version, ct),
            cancellationToken);
    }

    public async Task<UserThreads> GetThreadsAsync(int userId, uint pn, bool publicOnly,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        transport.RequireSession(nameof(GetThreadsAsync)).RequireAuthenticatedAccount(nameof(GetThreadsAsync));
        var api = new GetUserThreads(transport.HttpCore, transport.WsCore);
        return await transport.Dispatcher.DispatchAsync(
            LegacyTransportOperation.GetUserThreads,
            ct => api.RequestHttpAsync(userId, pn, publicOnly, ct),
            ct => api.RequestWsAsync(userId, pn, publicOnly, ct),
            cancellationToken);
    }

    private static void ValidateUserId(long userId)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "User id must be positive.");
    }

    private static void ValidatePageNumber(int pn)
    {
        if (pn <= 0)
            throw new ArgumentOutOfRangeException(nameof(pn), pn, "Page number must be positive.");
    }

    private static UserInfo MergeSelfInfo(UserInfo initNickname, UserInfo moIndex)
    {
        return new UserInfo
        {
            UserId = moIndex.UserId,
            Portrait = moIndex.Portrait,
            UserName = string.IsNullOrEmpty(initNickname.UserName) ? moIndex.UserName : initNickname.UserName,
            NickNameOld = initNickname.NickNameOld,
            NickNameNew = moIndex.NickNameNew,
            TiebaUid = initNickname.TiebaUid,
            Gender = moIndex.Gender,
            PostNum = moIndex.PostNum,
            FanNum = moIndex.FanNum,
            FollowNum = moIndex.FollowNum,
            ForumNum = moIndex.ForumNum,
            Sign = moIndex.Sign,
            IsVip = moIndex.IsVip
        };
    }
}
