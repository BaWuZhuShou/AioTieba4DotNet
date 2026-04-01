#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public class ForumModuleBehaviorTests
{
    private const string CanonicalSafeForumName = "lol欧服";

    [TestMethod]
    public async Task ForumModule_DelegatesToInternalProtocol()
    {
        var expected = new Forum { Fid = 7356044, Fname = CanonicalSafeForumName };
        var protocol = new RecordingForumProtocol(expected);
        var module = new ForumModule(protocol);

        var actual = await module.GetForumAsync(CanonicalSafeForumName);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(CanonicalSafeForumName, protocol.LastFname);
    }

    [TestMethod]
    public async Task ForumModule_FollowDelegatesToInternalProtocol()
    {
        var protocol = new RecordingForumProtocol(new Forum { Fid = 7356044, Fname = CanonicalSafeForumName })
        {
            FollowResult = true
        };
        var module = new ForumModule(protocol);

        var result = await module.FollowAsync(7356044);

        Assert.IsTrue(result);
        Assert.AreEqual(7356044UL, protocol.LastFollowFid);
    }

    [TestMethod]
    public async Task ForumModule_GetDislikeForumsDelegatesToInternalProtocol()
    {
        var expected = new DislikeForums([
            new DislikeForum { Fid = 7356044, Fname = CanonicalSafeForumName }
        ], new DislikeForumsPage { CurrentPage = 1, HasMore = false, HasPrevious = false });
        var protocol = new RecordingForumProtocol(new Forum { Fid = 7356044, Fname = CanonicalSafeForumName })
        {
            DislikeForumsResult = expected
        };
        var module = new ForumModule(protocol);

        var actual = await module.GetDislikeForumsAsync();

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1, protocol.GetDislikeForumsCalls);
    }

    [TestMethod]
    public async Task ForumModule_GetCidDelegatesToInternalProtocol()
    {
        var protocol = new RecordingForumProtocol(new Forum { Fid = 7356044, Fname = CanonicalSafeForumName })
        {
            CidResult = 42
        };
        var module = new ForumModule(protocol);

        var actual = await module.GetCidAsync(CanonicalSafeForumName, "目标分类");

        Assert.AreEqual(42, actual);
        Assert.AreEqual(CanonicalSafeForumName, protocol.LastCidFname);
        Assert.AreEqual("目标分类", protocol.LastCidCategoryName);
    }

    [TestMethod]
    public async Task ForumModule_SignFamiliesDelegateToInternalProtocol()
    {
        var protocol = new RecordingForumProtocol(new Forum { Fid = 7356044, Fname = CanonicalSafeForumName })
        {
            SignForumsResult = true,
            SignGrowthResult = true
        };
        var module = new ForumModule(protocol);

        var signForumsResult = await module.SignForumsAsync();
        var signGrowthResult = await module.SignGrowthAsync();

        Assert.IsTrue(signForumsResult);
        Assert.IsTrue(signGrowthResult);
        Assert.AreEqual(1, protocol.SignForumsCalls);
        Assert.AreEqual(1, protocol.SignGrowthCalls);
    }

    [TestMethod]
    public async Task ForumModule_SearchExactDelegatesToInternalProtocol()
    {
        var expected = new ExactSearches([
            new ExactSearch { Pid = 654321, Title = "命中标题" }
        ], new ExactSearchesPage { CurrentPage = 1, PageSize = 30, HasMore = false });
        var protocol = new RecordingForumProtocol(new Forum { Fid = 7356044, Fname = CanonicalSafeForumName })
        {
            SearchExactResult = expected
        };
        var module = new ForumModule(protocol);

        var actual = await module.SearchExactAsync(CanonicalSafeForumName, "关键字", 2, 20, ForumSearchType.Time, true);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(CanonicalSafeForumName, protocol.LastSearchExactFname);
        Assert.AreEqual("关键字", protocol.LastSearchExactQuery);
        Assert.AreEqual(2, protocol.LastSearchExactPageNumber);
        Assert.AreEqual(20, protocol.LastSearchExactPageSize);
        Assert.AreEqual(ForumSearchType.Time, protocol.LastSearchExactType);
        Assert.IsTrue(protocol.LastSearchExactOnlyThread);
    }

    [TestMethod]
    public async Task ForumModule_GetRoomListByFidDelegatesToInternalProtocol()
    {
        var expected = new RoomList([
            new Dictionary<string, object?> { ["room_id"] = 1001L }
        ]);
        var protocol = new RecordingForumProtocol(new Forum { Fid = 7356044, Fname = CanonicalSafeForumName })
        {
            RoomListResult = expected
        };
        var module = new ForumModule(protocol);

        var actual = await module.GetRoomListByFidAsync(7356044);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(7356044UL, protocol.LastRoomListFid);
    }

    private sealed class RecordingForumProtocol(Forum forum) : IForumProtocol
    {
        public string? LastFname { get; private set; }
        public string? LastCidFname { get; private set; }
        public string? LastCidCategoryName { get; private set; }
        public string? LastSearchExactFname { get; private set; }
        public string? LastSearchExactQuery { get; private set; }
        public int LastSearchExactPageNumber { get; private set; }
        public int LastSearchExactPageSize { get; private set; }
        public ForumSearchType LastSearchExactType { get; private set; }
        public bool LastSearchExactOnlyThread { get; private set; }
        public ulong LastFollowFid { get; private set; }
        public int GetDislikeForumsCalls { get; private set; }
        public int SignForumsCalls { get; private set; }
        public int SignGrowthCalls { get; private set; }
        public int CidResult { get; init; }
        public bool FollowResult { get; init; }
        public bool SignForumsResult { get; init; }
        public bool SignGrowthResult { get; init; }
        public DislikeForums? DislikeForumsResult { get; init; }
        public ExactSearches? SearchExactResult { get; init; }
        public RoomList? RoomListResult { get; init; }
        public ulong LastRoomListFid { get; private set; }

        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default) =>
            Task.FromResult(7356044UL);

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default) =>
            Task.FromResult(CanonicalSafeForumName);

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default) =>
            Task.FromResult(RecordFollow(fid));

        public Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignForumsAsync(CancellationToken cancellationToken = default)
        {
            SignForumsCalls++;
            return Task.FromResult(SignForumsResult);
        }

        public Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default)
        {
            SignGrowthCalls++;
            return Task.FromResult(SignGrowthResult);
        }

        public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(forum);
        }

        public Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default)
        {
            LastCidFname = fname;
            LastCidCategoryName = cname;
            return Task.FromResult(CidResult);
        }

        public Task<ExactSearches> SearchExactAsync(string fname, string query, int pn, int rn,
            ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
            CancellationToken cancellationToken = default)
        {
            LastSearchExactFname = fname;
            LastSearchExactQuery = query;
            LastSearchExactPageNumber = pn;
            LastSearchExactPageSize = rn;
            LastSearchExactType = searchType;
            LastSearchExactOnlyThread = onlyThread;
            return Task.FromResult(SearchExactResult ?? new ExactSearches([], new ExactSearchesPage()));
        }

        public Task<RoomList> GetRoomListByFidAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastRoomListFid = fid;
            return Task.FromResult(RoomListResult ?? new RoomList([]));
        }

        public Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            GetDislikeForumsCalls++;
            return Task.FromResult(DislikeForumsResult ?? new DislikeForums([], new DislikeForumsPage()));
        }

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        private bool RecordFollow(ulong fid)
        {
            LastFollowFid = fid;
            return FollowResult;
        }
    }
}
