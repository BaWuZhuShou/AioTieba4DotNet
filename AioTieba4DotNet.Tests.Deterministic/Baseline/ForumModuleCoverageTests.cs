#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public sealed class ForumModuleCoverageTests
{
    [TestMethod]
    public async Task ForumModule_DelegatesRemainingMembersToInternalProtocol()
    {
        var protocol = new RecordingForumProtocol();
        var module = new ForumModule(protocol);

        Assert.AreEqual(123UL, await module.GetFidAsync("csharp"));
        Assert.AreEqual("csharp", await module.GetFnameAsync(123));
        Assert.IsNull(await module.GetDetailAsync(123));
        Assert.IsNull(await module.GetDetailAsync("csharp"));
        Assert.IsTrue(await module.LikeAsync("csharp"));
        Assert.IsTrue(await module.FollowAsync("csharp"));
        Assert.IsTrue(await module.UnlikeAsync("csharp"));
        Assert.IsTrue(await module.UnfollowAsync(123));
        Assert.IsTrue(await module.UnfollowAsync("csharp"));
        Assert.IsTrue(await module.SignAsync("csharp"));
        Assert.IsNull(await module.GetFollowForumsAsync(42, 2, 10));
        Assert.IsNull(await module.GetSelfFollowForumsAsync(3, 4));
        Assert.IsNull(await module.GetSelfFollowForumsV1Async(5, 6));
        Assert.AreEqual(77, await module.GetCidAsync(123, "目录"));
        Assert.IsNull(await module.GetImageBytesAsync("https://example.com/image.png"));
        Assert.IsNull(await module.GetImageAsync("https://example.com/image.png"));
        Assert.IsNull(await module.GetImageByHashAsync("abcdef", ForumImageSize.Large));
        Assert.IsNull(await module.GetPortraitAsync("tb.1.safe", ForumImageSize.Large));
        Assert.IsNull(await module.SearchExactAsync(123, "query", 7, 8, ForumSearchType.Time, true));
        Assert.IsNull(await module.GetLastReplyersAsync("csharp", 9, 10, ThreadSortType.Create, true));
        Assert.IsNull(await module.GetMemberUsersAsync(123, 11));
        Assert.IsNull(await module.GetRankForumsAsync(123, 12, ForumRankType.Monthly));
        Assert.IsNull(await module.GetRecomStatusAsync("csharp"));
        Assert.IsNull(await module.GetStatisticsAsync(123));
        Assert.IsNull(await module.GetForumLevelAsync("csharp"));
        Assert.IsTrue(await module.DislikeAsync(123));
        Assert.IsTrue(await module.DislikeAsync("csharp"));
        Assert.IsTrue(await module.UndislikeAsync(123));
        Assert.IsTrue(await module.UndislikeAsync("csharp"));
        Assert.IsTrue(await module.DelBaWuAsync("csharp", "tb.1.safe", "assist"));

        Assert.AreEqual("csharp", protocol.LastFname);
        Assert.AreEqual(123UL, protocol.LastFid);
        Assert.AreEqual("目录", protocol.LastCategoryName);
        Assert.AreEqual("https://example.com/image.png", protocol.LastImageUrl);
        Assert.AreEqual("abcdef", protocol.LastImageHash);
        Assert.AreEqual(ForumImageSize.Large, protocol.LastImageSize);
        Assert.AreEqual("tb.1.safe", protocol.LastPortrait);
        Assert.AreEqual("query", protocol.LastSearchQuery);
        Assert.AreEqual(11, protocol.LastPageNumber);
        Assert.AreEqual(10, protocol.LastPageSize);
        Assert.AreEqual(ForumSearchType.Time, protocol.LastSearchType);
        Assert.IsTrue(protocol.LastOnlyThread);
        Assert.AreEqual(ThreadSortType.Create, protocol.LastThreadSortType);
        Assert.IsTrue(protocol.LastIsGood);
        Assert.AreEqual(42L, protocol.LastUserId);
        Assert.AreEqual(12, protocol.LastRankPageNumber);
        Assert.AreEqual(ForumRankType.Monthly, protocol.LastRankType);
        Assert.AreEqual("assist", protocol.LastBawuType);
    }

    [TestMethod]
    public async Task IForumProtocol_DefaultOptionalMembers_ThrowNotSupportedException()
    {
        IForumProtocol protocol = new DefaultForumProtocolProbe();

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetCidAsync("csharp", "目录"));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetCidAsync(123, "目录"));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetImageBytesAsync("https://example.com/image.png"));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetImageAsync("https://example.com/image.png"));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetImageByHashAsync("abcdef", ForumImageSize.Large));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetPortraitAsync("tb.1.safe", ForumImageSize.Large));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.SearchExactAsync("csharp", "query", 1, 2));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.SearchExactAsync(123, "query", 1, 2));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetLastReplyersAsync("csharp", 1, 2, ThreadSortType.Reply, false));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetLastReplyersAsync(123, 1, 2, ThreadSortType.Reply, false));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetMemberUsersAsync("csharp", 1));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetMemberUsersAsync(123, 1));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetRankForumsAsync("csharp", 1, ForumRankType.Weekly));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetRankForumsAsync(123, 1, ForumRankType.Weekly));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetRoomListByFidAsync(123));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetRecomStatusAsync("csharp"));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetRecomStatusAsync(123));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetSquareForumsAsync("目录", 1, 2));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetStatisticsAsync("csharp"));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetStatisticsAsync(123));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetForumLevelAsync("csharp"));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => protocol.GetForumLevelAsync(123));
    }

    private sealed class RecordingForumProtocol : IForumProtocol
    {
        public string? LastFname { get; private set; }
        public ulong LastFid { get; private set; }
        public long LastUserId { get; private set; }
        public string? LastCategoryName { get; private set; }
        public string? LastImageUrl { get; private set; }
        public string? LastImageHash { get; private set; }
        public ForumImageSize LastImageSize { get; private set; }
        public string? LastPortrait { get; private set; }
        public string? LastSearchQuery { get; private set; }
        public int LastPageNumber { get; private set; }
        public int LastPageSize { get; private set; }
        public ForumSearchType LastSearchType { get; private set; }
        public bool LastOnlyThread { get; private set; }
        public ThreadSortType LastThreadSortType { get; private set; }
        public bool LastIsGood { get; private set; }
        public int LastRankPageNumber { get; private set; }
        public ForumRankType LastRankType { get; private set; }
        public string? LastBawuType { get; private set; }

        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(123UL);
        }

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult("csharp");
        }

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult<ForumDetail>(null!);
        }

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult<ForumDetail>(null!);
        }

        public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(true);
        }

        public Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult(true);
        }

        public Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(true);
        }

        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(true);
        }

        public Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult(true);
        }

        public Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(true);
        }

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(true);
        }

        public Task<bool> SignForumsAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult<Forum>(null!);
        }

        public Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastPageNumber = pn;
            LastPageSize = rn;
            return Task.FromResult<FollowForums>(null!);
        }

        public Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            LastPageNumber = pn;
            LastPageSize = rn;
            return Task.FromResult<SelfFollowForums>(null!);
        }

        public Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            LastPageNumber = pn;
            LastPageSize = rn;
            return Task.FromResult<SelfFollowForumsV1>(null!);
        }

        public Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            LastCategoryName = cname;
            return Task.FromResult(77);
        }

        public Task<int> GetCidAsync(ulong fid, string cname = "", CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            LastCategoryName = cname;
            return Task.FromResult(77);
        }

        public Task<ForumImageBytes> GetImageBytesAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            LastImageUrl = imageUrl;
            return Task.FromResult<ForumImageBytes>(null!);
        }

        public Task<ForumImage> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            LastImageUrl = imageUrl;
            return Task.FromResult<ForumImage>(null!);
        }

        public Task<ForumImage> GetImageByHashAsync(string rawHash, ForumImageSize size = ForumImageSize.Small,
            CancellationToken cancellationToken = default)
        {
            LastImageHash = rawHash;
            LastImageSize = size;
            return Task.FromResult<ForumImage>(null!);
        }

        public Task<ForumImage> GetPortraitAsync(string portrait, ForumImageSize size = ForumImageSize.Small,
            CancellationToken cancellationToken = default)
        {
            LastPortrait = portrait;
            LastImageSize = size;
            return Task.FromResult<ForumImage>(null!);
        }

        public Task<ExactSearches> SearchExactAsync(string fname, string query, int pn, int rn,
            ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
            CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            LastSearchQuery = query;
            LastPageNumber = pn;
            LastPageSize = rn;
            LastSearchType = searchType;
            LastOnlyThread = onlyThread;
            return Task.FromResult<ExactSearches>(null!);
        }

        public Task<ExactSearches> SearchExactAsync(ulong fid, string query, int pn, int rn,
            ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
            CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            LastSearchQuery = query;
            LastPageNumber = pn;
            LastPageSize = rn;
            LastSearchType = searchType;
            LastOnlyThread = onlyThread;
            return Task.FromResult<ExactSearches>(null!);
        }

        public Task<LastReplyers> GetLastReplyersAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            LastPageNumber = pn;
            LastPageSize = rn;
            LastThreadSortType = sort;
            LastIsGood = isGood;
            return Task.FromResult<LastReplyers>(null!);
        }

        public Task<LastReplyers> GetLastReplyersAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            LastPageNumber = pn;
            LastPageSize = rn;
            LastThreadSortType = sort;
            LastIsGood = isGood;
            return Task.FromResult<LastReplyers>(null!);
        }

        public Task<MemberUsers> GetMemberUsersAsync(string fname, int pn, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            LastPageNumber = pn;
            return Task.FromResult<MemberUsers>(null!);
        }

        public Task<MemberUsers> GetMemberUsersAsync(ulong fid, int pn, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            LastPageNumber = pn;
            return Task.FromResult<MemberUsers>(null!);
        }

        public Task<RankForums> GetRankForumsAsync(string fname, int pn, ForumRankType rankType,
            CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            LastRankPageNumber = pn;
            LastRankType = rankType;
            return Task.FromResult<RankForums>(null!);
        }

        public Task<RankForums> GetRankForumsAsync(ulong fid, int pn, ForumRankType rankType,
            CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            LastRankPageNumber = pn;
            LastRankType = rankType;
            return Task.FromResult<RankForums>(null!);
        }

        public Task<RecomStatus> GetRecomStatusAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult<RecomStatus>(null!);
        }

        public Task<RecomStatus> GetRecomStatusAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult<RecomStatus>(null!);
        }

        public Task<SquareForums> GetSquareForumsAsync(string cname, int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            LastCategoryName = cname;
            LastPageNumber = pn;
            LastPageSize = rn;
            return Task.FromResult<SquareForums>(null!);
        }

        public Task<ForumStatistics> GetStatisticsAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult<ForumStatistics>(null!);
        }

        public Task<ForumStatistics> GetStatisticsAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult<ForumStatistics>(null!);
        }

        public Task<ForumLevelInfo> GetForumLevelAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult<ForumLevelInfo>(null!);
        }

        public Task<ForumLevelInfo> GetForumLevelAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult<ForumLevelInfo>(null!);
        }

        public Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult(true);
        }

        public Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(true);
        }

        public Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastFid = fid;
            return Task.FromResult(true);
        }

        public Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(true);
        }

        public Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn, CancellationToken cancellationToken = default)
        {
            LastPageNumber = pn;
            LastPageSize = rn;
            return Task.FromResult<DislikeForums>(null!);
        }

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            LastPortrait = portrait;
            LastBawuType = baWuType;
            return Task.FromResult(true);
        }
    }

    private sealed class DefaultForumProtocolProbe : IForumProtocol
    {
        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult(0UL);
        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default) => Task.FromResult<ForumDetail>(null!);
        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult<ForumDetail>(null!);
        public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> SignForumsAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult<Forum>(null!);
        public Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn, CancellationToken cancellationToken = default) => Task.FromResult<FollowForums>(null!);
        public Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn, CancellationToken cancellationToken = default) => Task.FromResult<SelfFollowForums>(null!);
        public Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn, CancellationToken cancellationToken = default) => Task.FromResult<SelfFollowForumsV1>(null!);
        public Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn, CancellationToken cancellationToken = default) => Task.FromResult<DislikeForums>(null!);
        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
