using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Protocols;

internal interface IForumProtocol
{
    Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default);

    Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> SignForumsAsync(CancellationToken cancellationToken = default);

    Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default);

    Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default);

    Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn, CancellationToken cancellationToken = default);

    Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn, CancellationToken cancellationToken = default);

    Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn,
        CancellationToken cancellationToken = default);

    Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<int> GetCidAsync(ulong fid, string cname = "", CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<ForumImageBytes> GetImageBytesAsync(string imageUrl, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<ForumImage> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<ForumImage> GetImageByHashAsync(string rawHash, ForumImageSize size = ForumImageSize.Small,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<ForumImage> GetPortraitAsync(string portrait, ForumImageSize size = ForumImageSize.Small,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<ExactSearches> SearchExactAsync(string fname, string query, int pn, int rn,
        ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<ExactSearches> SearchExactAsync(ulong fid, string query, int pn, int rn,
        ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<LastReplyers> GetLastReplyersAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<LastReplyers> GetLastReplyersAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<MemberUsers> GetMemberUsersAsync(string fname, int pn, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<MemberUsers> GetMemberUsersAsync(ulong fid, int pn, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<RankForums> GetRankForumsAsync(string fname, int pn, ForumRankType rankType,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<RankForums> GetRankForumsAsync(ulong fid, int pn, ForumRankType rankType,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<RoomList> GetRoomListByFidAsync(ulong fid, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<RecomStatus> GetRecomStatusAsync(string fname, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<RecomStatus> GetRecomStatusAsync(ulong fid, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<SquareForums> GetSquareForumsAsync(string cname, int pn, int rn,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    Task<ForumStatistics> GetStatisticsAsync(string fname, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<ForumStatistics> GetStatisticsAsync(ulong fid, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<ForumLevelInfo> GetForumLevelAsync(string fname, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<ForumLevelInfo> GetForumLevelAsync(ulong fid, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default);

    Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn, CancellationToken cancellationToken = default);

    Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
        CancellationToken cancellationToken = default);
}
