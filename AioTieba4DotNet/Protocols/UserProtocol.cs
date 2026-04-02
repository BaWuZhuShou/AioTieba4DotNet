using AioTieba4DotNet.Api.AddBlacklistOld;
using AioTieba4DotNet.Api.DelBlacklistOld;
using AioTieba4DotNet.Api.FollowUser;
using AioTieba4DotNet.Api.GetBlacklist;
using AioTieba4DotNet.Api.GetBlacklistOld;
using AioTieba4DotNet.Api.GetFans;
using AioTieba4DotNet.Api.GetFollows;
using AioTieba4DotNet.Api.GetRankUsers;
using AioTieba4DotNet.Api.GetSelfInfoInitNickname;
using AioTieba4DotNet.Api.GetSelfInfoMoIndex;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoWeb;
using AioTieba4DotNet.Api.GetUInfoPanel;
using AioTieba4DotNet.Api.GetUInfoUserJson;
using AioTieba4DotNet.Api.GetUserContents;
using AioTieba4DotNet.Api.GetUserForumInfo;
using AioTieba4DotNet.Api.Login;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile;
using AioTieba4DotNet.Api.RemoveFan;
using AioTieba4DotNet.Api.SetBlacklist;
using AioTieba4DotNet.Api.SetNicknameOld;
using AioTieba4DotNet.Api.SetProfile;
using AioTieba4DotNet.Api.TiebaUid2UserInfo;
using AioTieba4DotNet.Api.UnfollowUser;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using GetHomepageApi = AioTieba4DotNet.Api.Profile.GetHomepage.GetHomepage;

namespace AioTieba4DotNet.Protocols;

internal sealed class UserProtocol(TiebaOperationDispatcher dispatcher, IForumProtocol forums) : IUserProtocol
{
    public async Task<string> GetTbsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                nameof(GetTbsAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => session.GetTbsAsync(ct)),
            cancellationToken);
    }

    public async Task<UserInfo> GetUserInfoAppAsync(int userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfo>(
                nameof(GetUserInfoAppAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetUInfoGetUserInfoApp(session.HttpCore).RequestAsync(userId, ct)),
            cancellationToken);
    }

    public async Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfoPf>(
                nameof(GetProfileAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetUInfoProfile<int>(session.HttpCore).RequestAsync(userId, ct)),
            cancellationToken);
    }

    public async Task<UserInfoPf> GetProfileAsync(string portraitOrUserName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfoPf>(
                nameof(GetProfileAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetUInfoProfile<string>(session.HttpCore).RequestAsync(portraitOrUserName, ct)),
            cancellationToken);
    }

    public async Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(FollowAsync),
                TiebaOperationCapabilities.HttpOnly(true, true),
                (session, ct) => new FollowUser(session.HttpCore).RequestAsync(portrait, ct)),
            cancellationToken);
    }

    public async Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(UnfollowAsync),
                TiebaOperationCapabilities.HttpOnly(true, true),
                (session, ct) => new UnfollowUser(session.HttpCore).RequestAsync(portrait, ct)),
            cancellationToken);
    }

    public async Task<UserList> GetFollowsAsync(long userId, int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        ValidatePageNumber(pn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserList>(
                nameof(GetFollowsAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetFollows(session.HttpCore).RequestAsync(userId, pn, ct)),
            cancellationToken);
    }

    public async Task<UserList> GetFansAsync(long userId, int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        ValidatePageNumber(pn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserList>(
                nameof(GetFansAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new GetFans(session.HttpCore).RequestAsync(userId, pn, ct)),
            cancellationToken);
    }

    public async Task<UserInfo> GetPanelInfoAsync(string nameOrPortrait,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfo>(
                nameof(GetPanelInfoAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetUInfoPanel(session.HttpCore).RequestAsync(nameOrPortrait, ct)),
            cancellationToken);
    }

    public async Task<UserInfo> GetUserInfoJsonAsync(string username,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfo>(
                nameof(GetUserInfoJsonAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetUInfoUserJson(session.HttpCore).RequestAsync(username, ct)),
            cancellationToken);
    }

    public async Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var initNickname = await GetSelfInfoInitNicknameAsync(cancellationToken);
        var moIndex = await GetSelfInfoMoIndexAsync(cancellationToken);

        return MergeSelfInfo(initNickname, moIndex);
    }

    public async Task<UserInfo> GetSelfInfoInitNicknameAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfo>(
                nameof(GetSelfInfoInitNicknameAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new GetSelfInfoInitNickname(session.HttpCore).RequestAsync(ct)),
            cancellationToken);
    }

    public async Task<UserInfo> GetSelfInfoMoIndexAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfo>(
                nameof(GetSelfInfoMoIndexAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new GetSelfInfoMoIndex(session.HttpCore).RequestAsync(ct)),
            cancellationToken);
    }

    public async Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<LoginResult>(
                nameof(LoginAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                async (session, ct) =>
                {
                    var (user, tbs) = await new Login(session.HttpCore).RequestAsync(ct);
                    return new LoginResult { User = user, Tbs = tbs };
                },
                ApplySessionMutation: static (session, result) => session.UpdateTbs(result.Tbs)),
            cancellationToken);
    }

    public async Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<BlacklistUsers>(
                nameof(GetBlacklistAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new GetBlacklist(session.HttpCore).RequestAsync(ct)),
            cancellationToken);
    }

    public async Task<BlacklistOldUsers> GetBlacklistOldAsync(int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);
        ValidatePageSize(rn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<BlacklistOldUsers>(
                nameof(GetBlacklistOldAsync),
                TiebaOperationCapabilities.WebSocketPreferred(true),
                (session, ct) => new GetBlacklistOld(session.HttpCore, session.WsCore).RequestHttpAsync(pn, rn, ct),
                (session, ct) => new GetBlacklistOld(session.HttpCore, session.WsCore).RequestWsAsync(pn, rn, ct)),
            cancellationToken);
    }

    public async Task<bool> SetBlacklistAsync(long userId, BlacklistType type,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SetBlacklistAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new SetBlacklist(session.HttpCore).RequestAsync(userId, type, ct)),
            cancellationToken);
    }

    public async Task<bool> AddBlacklistOldAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(AddBlacklistOldAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new AddBlacklistOld(session.HttpCore).RequestAsync(userId, ct)),
            cancellationToken);
    }

    public async Task<bool> RemoveBlacklistOldAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(RemoveBlacklistOldAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new DelBlacklistOld(session.HttpCore).RequestAsync(userId, ct)),
            cancellationToken);
    }

    public async Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(RemoveFanAsync),
                TiebaOperationCapabilities.HttpOnly(true, true),
                (session, ct) => new RemoveFan(session.HttpCore).RequestAsync(userId, ct)),
            cancellationToken);
    }

    public async Task<UserInfo> GetUserInfoWebAsync(int userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfo>(
                nameof(GetUserInfoWebAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetUInfoGetUserInfoWeb(session.HttpCore).RequestAsync(userId, ct)),
            cancellationToken);
    }

    public async Task<UserForumInfo> GetUserForumInfoAsync(ulong fid, string portrait,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);
        ValidateRequiredText(nameof(portrait), portrait, "Portrait must not be blank.");

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserForumInfo>(
                nameof(GetUserForumInfoAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new GetUserForumInfo(session.HttpCore).RequestAsync(fid, portrait, ct)),
            cancellationToken);
    }

    public async Task<UserForumInfo> GetUserForumInfoAsync(string fname, string portrait,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(nameof(fname), fname, "Forum name must not be blank.");
        ValidateRequiredText(nameof(portrait), portrait, "Portrait must not be blank.");

        await dispatcher.EnsureCanExecuteAsync(nameof(GetUserForumInfoAsync),
            TiebaOperationCapabilities.HttpOnly(true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await GetUserForumInfoAsync(fid, portrait, cancellationToken);
    }

    public async Task<RankUsers> GetRankUsersAsync(string fname, int pn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(nameof(fname), fname, "Forum name must not be blank.");
        ValidatePageNumber(pn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<RankUsers>(
                nameof(GetRankUsersAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetRankUsers(session.HttpCore).RequestAsync(fname, pn, ct)),
            cancellationToken);
    }

    public async Task<Homepage> GetHomepageAsync(int userId, int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        ValidatePageNumber(pn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<Homepage>(
                nameof(GetHomepageAsync),
                TiebaOperationCapabilities.WebSocketPreferred(),
                (session, ct) => new GetHomepageApi(session.HttpCore, session.WsCore).RequestHttpAsync(userId, pn, ct),
                (session, ct) => new GetHomepageApi(session.HttpCore, session.WsCore).RequestWsAsync(userId, pn, ct)),
            cancellationToken);
    }

    public async Task<bool> SetNicknameAsync(string nickName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(nameof(nickName), nickName, "Nickname must not be blank.");

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SetNicknameAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new SetNicknameOld(session.HttpCore).RequestAsync(nickName, ct)),
            cancellationToken);
    }

    public async Task<bool> SetProfileAsync(string nickName, string sign, Gender gender,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(nameof(nickName), nickName, "Nickname must not be blank.");
        ValidateGender(gender);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SetProfileAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => new SetProfile(session.HttpCore).RequestAsync(nickName, sign, gender, ct)),
            cancellationToken);
    }

    public async Task<UserInfo> GetUserByTiebaUidAsync(long tiebaUid,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateTiebaUid(tiebaUid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserInfo>(
                nameof(GetUserByTiebaUidAsync),
                TiebaOperationCapabilities.WebSocketPreferred(),
                (session, ct) => new TiebaUid2UserInfo(session.HttpCore, session.WsCore).RequestHttpAsync(tiebaUid, ct),
                (session, ct) => new TiebaUid2UserInfo(session.HttpCore, session.WsCore).RequestWsAsync(tiebaUid, ct)),
            cancellationToken);
    }

    public async Task<UserPostGroups> GetPostsAsync(int userId, uint pn, uint rn, string version,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserPostGroups>(
                nameof(GetPostsAsync),
                TiebaOperationCapabilities.WebSocketPreferred(true),
                (session, ct) => new GetPosts(session.HttpCore, session.WsCore).RequestHttpAsync(userId, pn, rn,
                    version, ct),
                (session, ct) => new GetPosts(session.HttpCore, session.WsCore).RequestWsAsync(userId, pn, rn,
                    version, ct)),
            cancellationToken);
    }

    public async Task<UserThreads> GetThreadsAsync(int userId, uint pn, bool publicOnly,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<UserThreads>(
                nameof(GetThreadsAsync),
                TiebaOperationCapabilities.WebSocketPreferred(true),
                (session, ct) => new GetUserThreads(session.HttpCore, session.WsCore).RequestHttpAsync(userId, pn,
                    publicOnly, ct),
                (session, ct) => new GetUserThreads(session.HttpCore, session.WsCore).RequestWsAsync(userId, pn,
                    publicOnly, ct)),
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

    private static void ValidatePageSize(int rn)
    {
        if (rn <= 0)
            throw new ArgumentOutOfRangeException(nameof(rn), rn, "Page size must be positive.");
    }

    private static void ValidateForumId(ulong fid)
    {
        if (fid == 0)
            throw new ArgumentOutOfRangeException(nameof(fid), fid, "Forum id must be positive.");
    }

    private static void ValidateTiebaUid(long tiebaUid)
    {
        if (tiebaUid <= 0)
            throw new ArgumentOutOfRangeException(nameof(tiebaUid), tiebaUid, "Tieba uid must be positive.");
    }

    private static void ValidateGender(Gender gender)
    {
        if (gender is not (Gender.Male or Gender.Female))
            throw new ArgumentOutOfRangeException(nameof(gender), gender,
                "Profile updates require either Male or Female.");
    }

    private static void ValidateRequiredText(string paramName, string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message, paramName);
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
