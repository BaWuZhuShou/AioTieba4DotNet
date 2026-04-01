using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Protocols;

namespace AioTieba4DotNet.Modules;

/// <summary>
///     贴吧模块默认实现
/// </summary>
public sealed class ForumModule : IForumModule
{
    private readonly IForumProtocol _protocol;

    internal ForumModule(IForumProtocol protocol)
    {
        _protocol = protocol;
    }

    /// <inheritdoc/>
    public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetFidAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.GetFnameAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.GetDetailAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetDetailAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.FollowAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.FollowAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.UnfollowAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.UnfollowAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.SignAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> SignForumsAsync(CancellationToken cancellationToken = default) =>
        _protocol.SignForumsAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default) =>
        _protocol.SignGrowthAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetForumAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<FollowForums> GetFollowForumsAsync(long userId, int pn = 1, int rn = 50,
        CancellationToken cancellationToken = default) =>
        _protocol.GetFollowForumsAsync(userId, pn, rn, cancellationToken);

    /// <inheritdoc/>
    public Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn = 1, int rn = 200,
        CancellationToken cancellationToken = default) =>
        _protocol.GetSelfFollowForumsAsync(pn, rn, cancellationToken);

    /// <inheritdoc/>
    public Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default) =>
        _protocol.GetSelfFollowForumsV1Async(pn, rn, cancellationToken);

    /// <inheritdoc/>
    public Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default) =>
        _protocol.GetCidAsync(fname, cname, cancellationToken);

    /// <inheritdoc/>
    public Task<int> GetCidAsync(ulong fid, string cname = "", CancellationToken cancellationToken = default) =>
        _protocol.GetCidAsync(fid, cname, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumImageBytes> GetImageBytesAsync(string imageUrl, CancellationToken cancellationToken = default) =>
        _protocol.GetImageBytesAsync(imageUrl, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumImage> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default) =>
        _protocol.GetImageAsync(imageUrl, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumImage> GetImageByHashAsync(string rawHash, ForumImageSize size = ForumImageSize.Small,
        CancellationToken cancellationToken = default) =>
        _protocol.GetImageByHashAsync(rawHash, size, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumImage> GetPortraitAsync(string portrait, ForumImageSize size = ForumImageSize.Small,
        CancellationToken cancellationToken = default) =>
        _protocol.GetPortraitAsync(portrait, size, cancellationToken);

    /// <inheritdoc/>
    public Task<ExactSearches> SearchExactAsync(string fname, string query, int pn = 1, int rn = 30,
        ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
        CancellationToken cancellationToken = default) =>
        _protocol.SearchExactAsync(fname, query, pn, rn, searchType, onlyThread, cancellationToken);

    /// <inheritdoc/>
    public Task<ExactSearches> SearchExactAsync(ulong fid, string query, int pn = 1, int rn = 30,
        ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
        CancellationToken cancellationToken = default) =>
        _protocol.SearchExactAsync(fid, query, pn, rn, searchType, onlyThread, cancellationToken);

    /// <inheritdoc/>
    public Task<LastReplyers> GetLastReplyersAsync(string fname, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default) =>
        _protocol.GetLastReplyersAsync(fname, pn, rn, sort, isGood, cancellationToken);

    /// <inheritdoc/>
    public Task<LastReplyers> GetLastReplyersAsync(ulong fid, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default) =>
        _protocol.GetLastReplyersAsync(fid, pn, rn, sort, isGood, cancellationToken);

    /// <inheritdoc/>
    public Task<MemberUsers> GetMemberUsersAsync(string fname, int pn = 1,
        CancellationToken cancellationToken = default) =>
        _protocol.GetMemberUsersAsync(fname, pn, cancellationToken);

    /// <inheritdoc/>
    public Task<MemberUsers> GetMemberUsersAsync(ulong fid, int pn = 1,
        CancellationToken cancellationToken = default) =>
        _protocol.GetMemberUsersAsync(fid, pn, cancellationToken);

    /// <inheritdoc/>
    public Task<RankForums> GetRankForumsAsync(string fname, int pn = 1,
        ForumRankType rankType = ForumRankType.Weekly, CancellationToken cancellationToken = default) =>
        _protocol.GetRankForumsAsync(fname, pn, rankType, cancellationToken);

    /// <inheritdoc/>
    public Task<RankForums> GetRankForumsAsync(ulong fid, int pn = 1,
        ForumRankType rankType = ForumRankType.Weekly, CancellationToken cancellationToken = default) =>
        _protocol.GetRankForumsAsync(fid, pn, rankType, cancellationToken);

    /// <inheritdoc/>
    public Task<RecomStatus> GetRecomStatusAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetRecomStatusAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<RecomStatus> GetRecomStatusAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.GetRecomStatusAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<SquareForums> GetSquareForumsAsync(string cname, int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default) =>
        _protocol.GetSquareForumsAsync(cname, pn, rn, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumStatistics> GetStatisticsAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetStatisticsAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumStatistics> GetStatisticsAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.GetStatisticsAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumLevelInfo> GetForumLevelAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetForumLevelAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumLevelInfo> GetForumLevelAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.GetForumLevelAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<RoomList> GetRoomListByFidAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.GetRoomListByFidAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.DislikeAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.DislikeAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.UndislikeAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.UndislikeAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<DislikeForums> GetDislikeForumsAsync(int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default) =>
        _protocol.GetDislikeForumsAsync(pn, rn, cancellationToken);

}
