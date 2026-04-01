using AioTieba4DotNet.Api.DelBawu;
using AioTieba4DotNet.Api.GetDislikeForums;
using AioTieba4DotNet.Api.GetFid;
using AioTieba4DotNet.Api.GetFollowForums;
using AioTieba4DotNet.Api.GetForumLevel;
using AioTieba4DotNet.Api.GetForum;
using AioTieba4DotNet.Api.GetImages;
using AioTieba4DotNet.Api.GetLastReplyers;
using AioTieba4DotNet.Api.GetMemberUsers;
using AioTieba4DotNet.Api.GetRankForums;
using AioTieba4DotNet.Api.GetRecomStatus;
using AioTieba4DotNet.Api.GetForumDetail;
using AioTieba4DotNet.Api.GetRoomListByFid;
using AioTieba4DotNet.Api.GetSquareForums;
using AioTieba4DotNet.Api.GetStatistics;
using AioTieba4DotNet.Api.GetSelfFollowForums;
using AioTieba4DotNet.Api.GetSelfFollowForumsV1;
using AioTieba4DotNet.Api.LikeForum;
using AioTieba4DotNet.Api.SearchExact;
using AioTieba4DotNet.Api.Sign;
using AioTieba4DotNet.Api.SignForums;
using AioTieba4DotNet.Api.SignGrowth;
using AioTieba4DotNet.Api.GetSelfInfoInitNickname;
using AioTieba4DotNet.Api.GetSelfInfoMoIndex;
using AioTieba4DotNet.Api.UndislikeForum;
using AioTieba4DotNet.Api.UnlikeForum;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;
using DislikeForumApi = AioTieba4DotNet.Api.DislikeForum.DislikeForum;

namespace AioTieba4DotNet.Protocols;

internal sealed class ForumProtocol(TiebaOperationDispatcher dispatcher, ForumInfoCache cache,
    IAdminProtocol? adminProtocol = null) : IForumProtocol
{
    private readonly ForumInfoCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var forumId = _cache.GetForumId(fname);
        if (forumId != 0)
            return forumId;

        forumId = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ulong>(
                nameof(GetFidAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetFid(session.HttpCore).RequestAsync(fname, ct)),
            cancellationToken);

        CacheForum(forumId, fname);
        return forumId;
    }

    public async Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var forumName = _cache.GetForumName(fid);
        if (!string.IsNullOrEmpty(forumName))
            return forumName;

        var detail = await GetDetailAsync(fid, cancellationToken);
        _cache.SetForumName(fid, detail.Fname);
        return detail.Fname;
    }

    public async Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var detail = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ForumDetail>(
                nameof(GetDetailAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetForumDetail(session.HttpCore).RequestAsync((long)fid, ct)),
            cancellationToken);

        CacheForum(detail.Fid, detail.Fname);
        return detail;
    }

    public async Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fid = await GetFidAsync(fname, cancellationToken);
        return await GetDetailAsync(fid, cancellationToken);
    }

    public async Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default)
        => await FollowAsync(fname, cancellationToken);

    public async Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(FollowAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new LikeForum(session.HttpCore).RequestAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(FollowAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await FollowAsync(fid, cancellationToken);
    }

    public async Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default)
        => await UnfollowAsync(fname, cancellationToken);

    public async Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(UnfollowAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new UnlikeForum(session.HttpCore).RequestAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(UnfollowAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await UnfollowAsync(fid, cancellationToken);
    }

    public async Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(SignAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SignAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new Sign(session.HttpCore).RequestAsync(fname, fid, ct)),
            cancellationToken);
    }

    public async Task<bool> SignForumsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SignForumsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new SignForums(session.HttpCore).RequestAsync(ct)),
            cancellationToken);
    }

    public async Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SignGrowthAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new SignGrowth(session.HttpCore).RequestAsync("page_sign", ct)),
            cancellationToken);
    }

    public async Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var forum = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<Forum>(
                nameof(GetForumAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetForum(session.HttpCore).RequestAsync(fname, ct)),
            cancellationToken);

        CacheForum((ulong)forum.Fid, forum.Fname);
        return forum;
    }

    public async Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        ValidatePageNumber(pn);
        ValidatePageSize(rn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<FollowForums>(
                nameof(GetFollowForumsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetFollowForums(session.HttpCore).RequestAsync(userId, pn, rn, ct)),
            cancellationToken);
    }

    public async Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);
        ValidatePageSize(rn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<SelfFollowForums>(
                nameof(GetSelfFollowForumsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new GetSelfFollowForums(session.HttpCore).RequestAsync(pn, rn, ct)),
            cancellationToken);
    }

    public async Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);
        ValidatePageSize(rn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<SelfFollowForumsV1>(
                nameof(GetSelfFollowForumsV1Async),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetSelfFollowForumsV1(session.HttpCore).RequestAsync(pn, rn, ct)),
            cancellationToken);
    }

    public async Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(cname))
            return 0;

        ValidateForumName(fname);
        await dispatcher.EnsureCanExecuteAsync(nameof(GetCidAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<int>(
                nameof(GetCidAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new Api.GetCid.GetCid(session.HttpCore).RequestAsync(fname, cname, ct)),
            cancellationToken);
    }

    public async Task<int> GetCidAsync(ulong fid, string cname = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(cname))
            return 0;

        await dispatcher.EnsureCanExecuteAsync(nameof(GetCidAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fname = await GetFnameAsync(fid, cancellationToken);
        return await GetCidAsync(fname, cname, cancellationToken);
    }

    public async Task<ForumImageBytes> GetImageBytesAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var imageUri = ValidateImageUri(imageUrl);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ForumImageBytes>(
                nameof(GetImageBytesAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetImages(session.HttpCore).RequestBytesAsync(imageUri, ct)),
            cancellationToken);
    }

    public async Task<ForumImage> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var imageUri = ValidateImageUri(imageUrl);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ForumImage>(
                nameof(GetImageAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetImages(session.HttpCore).RequestAsync(imageUri, ct)),
            cancellationToken);
    }

    public async Task<ForumImage> GetImageByHashAsync(string rawHash, ForumImageSize size = ForumImageSize.Small,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(rawHash, nameof(rawHash));
        if (!Enum.IsDefined(size))
            return new ForumImage();

        var imageUri = new Uri(size switch
        {
            ForumImageSize.Small => $"http://imgsrc.baidu.com/forum/w=720;q=60;g=0/sign=__/{rawHash}.jpg",
            ForumImageSize.Medium => $"http://imgsrc.baidu.com/forum/w=960;q=60;g=0/sign=__/{rawHash}.jpg",
            ForumImageSize.Large => $"http://imgsrc.baidu.com/forum/pic/item/{rawHash}.jpg",
            _ => throw new InvalidOperationException()
        });

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ForumImage>(
                nameof(GetImageByHashAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetImages(session.HttpCore).RequestAsync(imageUri, ct)),
            cancellationToken);
    }

    public async Task<ForumImage> GetPortraitAsync(string portrait, ForumImageSize size = ForumImageSize.Small,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(portrait, nameof(portrait));
        if (!Enum.IsDefined(size))
            return new ForumImage();

        var path = size switch
        {
            ForumImageSize.Small => "n",
            ForumImageSize.Medium => string.Empty,
            ForumImageSize.Large => "h",
            _ => throw new InvalidOperationException()
        };

        var imageUri = new Uri($"http://tb.himg.baidu.com/sys/portrait{path}/item/{portrait}");
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ForumImage>(
                nameof(GetPortraitAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetImages(session.HttpCore).RequestAsync(imageUri, ct)),
            cancellationToken);
    }

    public async Task<ExactSearches> SearchExactAsync(string fname, string query, int pn, int rn,
        ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidateRequiredText(query, nameof(query));
        ValidatePageNumber(pn);
        ValidatePageSize(rn);
        ValidateEnum(searchType, nameof(searchType));

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ExactSearches>(
                nameof(SearchExactAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new SearchExact(session.HttpCore).RequestAsync(fname, query, pn, rn, searchType,
                    onlyThread, ct)),
            cancellationToken);
    }

    public async Task<ExactSearches> SearchExactAsync(ulong fid, string query, int pn, int rn,
        ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fname = await GetFnameAsync(fid, cancellationToken);
        return await SearchExactAsync(fname, query, pn, rn, searchType, onlyThread, cancellationToken);
    }

    public async Task<LastReplyers> GetLastReplyersAsync(string fname, int pn, int rn, ThreadSortType sort,
        bool isGood, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidatePageNumber(pn);
        ValidatePageSize(rn, 100);
        ValidateEnum(sort, nameof(sort));

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<LastReplyers>(
                nameof(GetLastReplyersAsync),
                TiebaOperationCapabilities.WebSocketPreferred(),
                (session, ct) => new GetLastReplyers(session.HttpCore, session.WsCore)
                    .RequestHttpAsync(fname, pn, rn, sort, isGood, ct),
                (session, ct) => new GetLastReplyers(session.HttpCore, session.WsCore)
                    .RequestWsAsync(fname, pn, rn, sort, isGood, ct)),
            cancellationToken);
    }

    public async Task<LastReplyers> GetLastReplyersAsync(ulong fid, int pn, int rn, ThreadSortType sort,
        bool isGood, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fname = await GetFnameAsync(fid, cancellationToken);
        return await GetLastReplyersAsync(fname, pn, rn, sort, isGood, cancellationToken);
    }

    public async Task<MemberUsers> GetMemberUsersAsync(string fname, int pn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidatePageNumber(pn);
        await dispatcher.EnsureCanExecuteAsync(nameof(GetMemberUsersAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);
        EnsureStokenAvailable(nameof(GetMemberUsersAsync));

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<MemberUsers>(
                nameof(GetMemberUsersAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetMemberUsers(session.HttpCore).RequestAsync(fname, pn, ct)),
            cancellationToken);
    }

    public async Task<MemberUsers> GetMemberUsersAsync(ulong fid, int pn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);
        await dispatcher.EnsureCanExecuteAsync(nameof(GetMemberUsersAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);
        EnsureStokenAvailable(nameof(GetMemberUsersAsync));

        var fname = await GetFnameAsync(fid, cancellationToken);
        return await GetMemberUsersAsync(fname, pn, cancellationToken);
    }

    public async Task<RankForums> GetRankForumsAsync(string fname, int pn, ForumRankType rankType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        ValidatePageNumber(pn);
        ValidateEnum(rankType, nameof(rankType));

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<RankForums>(
                nameof(GetRankForumsAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetRankForums(session.HttpCore).RequestAsync(fname, pn, rankType, ct)),
            cancellationToken);
    }

    public async Task<RankForums> GetRankForumsAsync(ulong fid, int pn, ForumRankType rankType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fname = await GetFnameAsync(fid, cancellationToken);
        return await GetRankForumsAsync(fname, pn, rankType, cancellationToken);
    }

    public async Task<RecomStatus> GetRecomStatusAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        await dispatcher.EnsureCanExecuteAsync(nameof(GetRecomStatusAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await GetRecomStatusAsync(fid, cancellationToken);
    }

    public async Task<RecomStatus> GetRecomStatusAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<RecomStatus>(
                nameof(GetRecomStatusAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetRecomStatus(session.HttpCore).RequestAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<SquareForums> GetSquareForumsAsync(string cname, int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(cname, nameof(cname));
        ValidatePageNumber(pn);
        ValidatePageSize(rn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<SquareForums>(
                nameof(GetSquareForumsAsync),
                TiebaOperationCapabilities.WebSocketPreferred(requiresAuthentication: true),
                (session, ct) => new GetSquareForums(session.HttpCore, session.WsCore).RequestHttpAsync(cname, pn, rn,
                    ct),
                (session, ct) => new GetSquareForums(session.HttpCore, session.WsCore).RequestWsAsync(cname, pn, rn,
                    ct)),
            cancellationToken);
    }

    public async Task<ForumStatistics> GetStatisticsAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        await dispatcher.EnsureCanExecuteAsync(nameof(GetStatisticsAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await GetStatisticsAsync(fid, cancellationToken);
    }

    public async Task<ForumStatistics> GetStatisticsAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ForumStatistics>(
                nameof(GetStatisticsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetStatistics(session.HttpCore).RequestAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<ForumLevelInfo> GetForumLevelAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumName(fname);
        await dispatcher.EnsureCanExecuteAsync(nameof(GetForumLevelAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await GetForumLevelAsync(fid, cancellationToken);
    }

    public async Task<ForumLevelInfo> GetForumLevelAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);
        await EnsureSelfInfoBootstrapAsync(cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ForumLevelInfo>(
                nameof(GetForumLevelAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetForumLevel(session.HttpCore).RequestAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<RoomList> GetRoomListByFidAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<RoomList>(
                nameof(GetRoomListByFidAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetRoomListByFid(session.HttpCore).RequestAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(DislikeAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new DislikeForumApi(session.HttpCore).RequestAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(DislikeAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await DislikeAsync(fid, cancellationToken);
    }

    public async Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(UndislikeAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new UndislikeForum(session.HttpCore).RequestAsync(fid, ct)),
            cancellationToken);
    }

    public async Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(UndislikeAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await UndislikeAsync(fid, cancellationToken);
    }

    public async Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);
        ValidatePageSize(rn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<DislikeForums>(
                nameof(GetDislikeForumsAsync),
                TiebaOperationCapabilities.WebSocketPreferred(requiresAuthentication: true),
                (session, ct) => new GetDislikeForums(session.HttpCore, session.WsCore).RequestHttpAsync(pn, rn, ct),
                (session, ct) => new GetDislikeForums(session.HttpCore, session.WsCore).RequestWsAsync(pn, rn, ct)),
            cancellationToken);
    }

    public async Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (adminProtocol is not null && BawuTypeWireMapper.TryFromWireValue(baWuType, out var mappedType))
            return await adminProtocol.DelBaWuAsync(fname, portrait, mappedType, cancellationToken);

        await dispatcher.EnsureCanExecuteAsync(nameof(DelBaWuAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(DelBaWuAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new DelBaWu(session.HttpCore).RequestAsync((long)fid, portrait, baWuType, ct)),
            cancellationToken);
    }

    private void CacheForum(ulong forumId, string forumName)
    {
        if (forumId == 0 || string.IsNullOrWhiteSpace(forumName))
            return;

        _cache.SetForumName(forumId, forumName);
    }

    private static void ValidateForumId(ulong fid)
    {
        if (fid == 0)
            throw new ArgumentOutOfRangeException(nameof(fid), fid, "Forum id must be positive.");
    }

    private static void ValidateForumName(string fname)
    {
        if (string.IsNullOrWhiteSpace(fname))
            throw new ArgumentException("Forum name must not be empty.", nameof(fname));
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

    private static void ValidatePageSize(int rn, int max)
    {
        if (rn <= 0 || rn > max)
            throw new ArgumentOutOfRangeException(nameof(rn), rn, $"Page size must be between 1 and {max}.");
    }

    private static void ValidateRequiredText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", paramName);
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(paramName, value, "Enum value is not supported.");
    }

    private static Uri ValidateImageUri(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri) ||
            (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Image url must be an absolute http/https URL.", nameof(imageUrl));

        return imageUri;
    }

    private void EnsureStokenAvailable(string operationName)
    {
        var account = dispatcher.RequireAuthenticatedAccount(operationName);
        if (string.IsNullOrWhiteSpace(account.Stoken))
            throw new TiebaAuthenticationException(
                $"Operation '{operationName}' requires an authenticated session with STOKEN.");
    }

    private async Task EnsureSelfInfoBootstrapAsync(CancellationToken cancellationToken)
    {
        await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                $"{nameof(GetForumLevelAsync)}BootstrapInitNickname",
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                async (session, ct) =>
                {
                    _ = await new GetSelfInfoInitNickname(session.HttpCore).RequestAsync(ct);
                    return true;
                }),
            cancellationToken);

        await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                $"{nameof(GetForumLevelAsync)}BootstrapMoIndex",
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                async (session, ct) =>
                {
                    _ = await new GetSelfInfoMoIndex(session.HttpCore).RequestAsync(ct);
                    return true;
                }),
            cancellationToken);
    }
}
