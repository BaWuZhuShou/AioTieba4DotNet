#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public sealed class ForumDiscoveryModuleBehaviorTests
{
    [TestMethod]
    public async Task ForumModule_GetImageAsync_DelegatesToProtocol()
    {
        var expected = new ForumImage { Width = 2, Height = 3, Format = ForumImageFormat.Png };
        var protocol = new RecordingForumDiscoveryProtocol { ImageResult = expected };
        var module = new ForumModule(protocol);

        var actual = await module.GetImageAsync("https://imgsrc.baidu.com/forum/test.png");

        Assert.AreSame(expected, actual);
        Assert.AreEqual("https://imgsrc.baidu.com/forum/test.png", protocol.LastImageUrl);
    }

    [TestMethod]
    public async Task ForumModule_GetLastReplyersAsync_DelegatesToProtocol()
    {
        var expected = new LastReplyers([], new LastReplyersPage { CurrentPage = 1 }, new Forum());
        var protocol = new RecordingForumDiscoveryProtocol { LastReplyersResult = expected };
        var module = new ForumModule(protocol);

        var actual = await module.GetLastReplyersAsync(7356044, 2, 30, ThreadSortType.Create, true);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(7356044UL, protocol.LastLastReplyersFid);
        Assert.AreEqual(2, protocol.LastLastReplyersPn);
        Assert.AreEqual(30, protocol.LastLastReplyersRn);
        Assert.AreEqual(ThreadSortType.Create, protocol.LastLastReplyersSort);
        Assert.IsTrue(protocol.LastLastReplyersIsGood);
    }

    [TestMethod]
    public async Task ForumModule_GetMemberUsersAsync_DelegatesToProtocol()
    {
        var expected = new MemberUsers([], new MemberUsersPage { CurrentPage = 1 });
        var protocol = new RecordingForumDiscoveryProtocol { MemberUsersResult = expected };
        var module = new ForumModule(protocol);

        var actual = await module.GetMemberUsersAsync("safe", 3);

        Assert.AreSame(expected, actual);
        Assert.AreEqual("safe", protocol.LastMemberUsersFname);
        Assert.AreEqual(3, protocol.LastMemberUsersPn);
    }

    [TestMethod]
    public async Task ForumModule_GetRankForumsAsync_DelegatesToProtocol()
    {
        var expected = new RankForums([], new RankForumsPage { CurrentPage = 1 });
        var protocol = new RecordingForumDiscoveryProtocol { RankForumsResult = expected };
        var module = new ForumModule(protocol);

        var actual = await module.GetRankForumsAsync("safe", 4, ForumRankType.Monthly);

        Assert.AreSame(expected, actual);
        Assert.AreEqual("safe", protocol.LastRankForumsFname);
        Assert.AreEqual(4, protocol.LastRankForumsPn);
        Assert.AreEqual(ForumRankType.Monthly, protocol.LastRankForumsType);
    }

    [TestMethod]
    public async Task ForumModule_GetRecomStatusAsync_DelegatesToProtocol()
    {
        var expected = new RecomStatus { TotalRecommendNum = 12 };
        var protocol = new RecordingForumDiscoveryProtocol { RecomStatusResult = expected };
        var module = new ForumModule(protocol);

        var actual = await module.GetRecomStatusAsync(7356044);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(7356044UL, protocol.LastRecomStatusFid);
    }

    [TestMethod]
    public async Task ForumModule_GetSquareForumsAsync_DelegatesToProtocol()
    {
        var expected = new SquareForums([], new SquareForumsPage { CurrentPage = 1 });
        var protocol = new RecordingForumDiscoveryProtocol { SquareForumsResult = expected };
        var module = new ForumModule(protocol);

        var actual = await module.GetSquareForumsAsync("游戏", 2, 10);

        Assert.AreSame(expected, actual);
        Assert.AreEqual("游戏", protocol.LastSquareForumsClassName);
        Assert.AreEqual(2, protocol.LastSquareForumsPn);
        Assert.AreEqual(10, protocol.LastSquareForumsRn);
    }

    [TestMethod]
    public async Task ForumModule_GetStatisticsAsync_DelegatesToProtocol()
    {
        var expected = new ForumStatistics();
        var protocol = new RecordingForumDiscoveryProtocol { StatisticsResult = expected };
        var module = new ForumModule(protocol);

        var actual = await module.GetStatisticsAsync("safe");

        Assert.AreSame(expected, actual);
        Assert.AreEqual("safe", protocol.LastStatisticsFname);
    }

    [TestMethod]
    public async Task ForumModule_GetForumLevelAsync_DelegatesToProtocol()
    {
        var expected = new ForumLevelInfo { UserLevel = 9 };
        var protocol = new RecordingForumDiscoveryProtocol { ForumLevelResult = expected };
        var module = new ForumModule(protocol);

        var actual = await module.GetForumLevelAsync(7356044);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(7356044UL, protocol.LastForumLevelFid);
    }

    private sealed class RecordingForumDiscoveryProtocol : IForumProtocol
    {
        public string? LastImageUrl { get; private set; }
        public ulong LastLastReplyersFid { get; private set; }
        public int LastLastReplyersPn { get; private set; }
        public int LastLastReplyersRn { get; private set; }
        public ThreadSortType LastLastReplyersSort { get; private set; }
        public bool LastLastReplyersIsGood { get; private set; }
        public string? LastMemberUsersFname { get; private set; }
        public int LastMemberUsersPn { get; private set; }
        public string? LastRankForumsFname { get; private set; }
        public int LastRankForumsPn { get; private set; }
        public ForumRankType LastRankForumsType { get; private set; }
        public ulong LastRecomStatusFid { get; private set; }
        public string? LastSquareForumsClassName { get; private set; }
        public int LastSquareForumsPn { get; private set; }
        public int LastSquareForumsRn { get; private set; }
        public string? LastStatisticsFname { get; private set; }
        public ulong LastForumLevelFid { get; private set; }

        public ForumImage? ImageResult { get; init; }
        public LastReplyers? LastReplyersResult { get; init; }
        public MemberUsers? MemberUsersResult { get; init; }
        public RankForums? RankForumsResult { get; init; }
        public RecomStatus? RecomStatusResult { get; init; }
        public SquareForums? SquareForumsResult { get; init; }
        public ForumStatistics? StatisticsResult { get; init; }
        public ForumLevelInfo? ForumLevelResult { get; init; }

        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignForumsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCidAsync(ulong fid, string cname = "", CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumImageBytes> GetImageBytesAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumImage> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            LastImageUrl = imageUrl;
            return Task.FromResult(ImageResult ?? new ForumImage());
        }

        public Task<ForumImage> GetImageByHashAsync(string rawHash, ForumImageSize size = ForumImageSize.Small,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumImage> GetPortraitAsync(string portrait, ForumImageSize size = ForumImageSize.Small,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExactSearches> SearchExactAsync(string fname, string query, int pn, int rn,
            ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExactSearches> SearchExactAsync(ulong fid, string query, int pn, int rn,
            ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LastReplyers> GetLastReplyersAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LastReplyers> GetLastReplyersAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default)
        {
            LastLastReplyersFid = fid;
            LastLastReplyersPn = pn;
            LastLastReplyersRn = rn;
            LastLastReplyersSort = sort;
            LastLastReplyersIsGood = isGood;
            return Task.FromResult(LastReplyersResult ?? new LastReplyers([], new LastReplyersPage(), new Forum()));
        }

        public Task<MemberUsers> GetMemberUsersAsync(string fname, int pn,
            CancellationToken cancellationToken = default)
        {
            LastMemberUsersFname = fname;
            LastMemberUsersPn = pn;
            return Task.FromResult(MemberUsersResult ?? new MemberUsers([], new MemberUsersPage()));
        }

        public Task<MemberUsers> GetMemberUsersAsync(ulong fid, int pn, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RankForums> GetRankForumsAsync(string fname, int pn, ForumRankType rankType,
            CancellationToken cancellationToken = default)
        {
            LastRankForumsFname = fname;
            LastRankForumsPn = pn;
            LastRankForumsType = rankType;
            return Task.FromResult(RankForumsResult ?? new RankForums([], new RankForumsPage()));
        }

        public Task<RankForums> GetRankForumsAsync(ulong fid, int pn, ForumRankType rankType,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RecomStatus> GetRecomStatusAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RecomStatus> GetRecomStatusAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastRecomStatusFid = fid;
            return Task.FromResult(RecomStatusResult ?? new RecomStatus());
        }

        public Task<SquareForums> GetSquareForumsAsync(string cname, int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            LastSquareForumsClassName = cname;
            LastSquareForumsPn = pn;
            LastSquareForumsRn = rn;
            return Task.FromResult(SquareForumsResult ?? new SquareForums([], new SquareForumsPage()));
        }

        public Task<ForumStatistics> GetStatisticsAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastStatisticsFname = fname;
            return Task.FromResult(StatisticsResult ?? new ForumStatistics());
        }

        public Task<ForumStatistics> GetStatisticsAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumLevelInfo> GetForumLevelAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumLevelInfo> GetForumLevelAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastForumLevelFid = fid;
            return Task.FromResult(ForumLevelResult ?? new ForumLevelInfo());
        }

        public Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
