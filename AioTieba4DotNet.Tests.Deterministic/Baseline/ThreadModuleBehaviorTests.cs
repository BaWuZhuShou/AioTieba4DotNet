#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ContentModel = AioTieba4DotNet.Models.Contents.Content;
using PostModel = AioTieba4DotNet.Models.Threads.Post;
using ThreadModel = AioTieba4DotNet.Models.Threads.Thread;
using VirtualImagePfModel = AioTieba4DotNet.Models.Users.VirtualImagePf;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public class ThreadModuleBehaviorTests
{
    [TestMethod]
    public async Task GetPostsAsync_DelegatesToInternalProtocol()
    {
        var expected = new Posts
        {
            Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
        };
        var protocol = new RecordingThreadProtocol(expected,
            new Comments
            {
                Page = new PageT(),
                Forum = new ForumT { Fname = "lol欧服" },
                Thread = CreateThread(),
                Post = CreatePost(),
                Objs = []
            });
        var module = new ThreadModule(protocol);

        var actual = await module.GetPostsAsync(1001, 2, 15, PostSortType.Hot, true,
            true, 2, true);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1001, protocol.LastPostsTid);
        Assert.AreEqual(2, protocol.LastPostsPn);
        Assert.AreEqual(15, protocol.LastPostsRn);
        Assert.AreEqual(PostSortType.Hot, protocol.LastPostsSort);
        Assert.IsTrue(protocol.LastOnlyThreadAuthor);
        Assert.IsTrue(protocol.LastWithComments);
        Assert.AreEqual(2, protocol.LastCommentRn);
        Assert.IsTrue(protocol.LastCommentSortByAgree);
    }

    [TestMethod]
    public async Task GetCommentsAsync_DelegatesToInternalProtocol()
    {
        var expected = new Comments
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            Thread = CreateThread(),
            Post = CreatePost(),
            Objs = []
        };
        var protocol = new RecordingThreadProtocol(
            new Posts
            {
                Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
            }, expected);
        var module = new ThreadModule(protocol);

        var actual = await module.GetCommentsAsync(1001, 2002, 3, true);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1001, protocol.LastCommentsTid);
        Assert.AreEqual(2002, protocol.LastCommentsPid);
        Assert.AreEqual(3, protocol.LastCommentsPn);
        Assert.IsTrue(protocol.LastIsComment);
    }

    [TestMethod]
    public async Task GetRecoversAsync_DelegatesToInternalProtocol()
    {
        var expected = new Recovers(
            [new Recover { Text = "recover", User = new RecoverUser { UserName = "thread-author" } }],
            new RecoverPage { CurrentPage = 2, PageSize = 10, HasMore = true });
        var protocol = new RecordingThreadProtocol(
            new Posts
            {
                Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
            },
            new Comments
            {
                Page = new PageT(),
                Forum = new ForumT { Fname = "lol欧服" },
                Thread = CreateThread(),
                Post = CreatePost(),
                Objs = []
            }) { RecoversResult = expected };
        var module = new ThreadModule(protocol);

        var actual = await module.GetRecoversAsync("lol欧服", 2, 10, 99);

        Assert.AreSame(expected, actual);
        Assert.AreEqual("lol欧服", protocol.LastRecoversFname);
        Assert.AreEqual(2, protocol.LastRecoversPn);
        Assert.AreEqual(10, protocol.LastRecoversRn);
        Assert.AreEqual(99L, protocol.LastRecoversUserId);
    }

    [TestMethod]
    public async Task GetRecoverInfoAsync_DelegatesToInternalProtocol()
    {
        var expected = new RecoverInfo
        {
            Content = new ContentModel(),
            Title = "Recover detail",
            Tid = 1001,
            Pid = 2002,
            User = new RecoverUser { UserName = "thread-author" }
        };
        var protocol = new RecordingThreadProtocol(
            new Posts
            {
                Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
            },
            new Comments
            {
                Page = new PageT(),
                Forum = new ForumT { Fname = "lol欧服" },
                Thread = CreateThread(),
                Post = CreatePost(),
                Objs = []
            }) { RecoverInfoResult = expected };
        var module = new ThreadModule(protocol);

        var actual = await module.GetRecoverInfoAsync("lol欧服", 1001, 2002);

        Assert.AreSame(expected, actual);
        Assert.AreEqual("lol欧服", protocol.LastRecoverInfoFname);
        Assert.AreEqual(1001, protocol.LastRecoverInfoTid);
        Assert.AreEqual(2002, protocol.LastRecoverInfoPid);
    }

    [TestMethod]
    public async Task GetTabMapAsync_DelegatesToInternalProtocol()
    {
        var expected = new TabMap([new KeyValuePair<string, int>("全部", 1)]);
        var protocol = new RecordingThreadProtocol(
            new Posts
            {
                Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
            },
            new Comments
            {
                Page = new PageT(),
                Forum = new ForumT { Fname = "lol欧服" },
                Thread = CreateThread(),
                Post = CreatePost(),
                Objs = []
            }) { TabMapResult = expected };
        var module = new ThreadModule(protocol);

        var actual = await module.GetTabMapAsync("lol欧服");

        Assert.AreSame(expected, actual);
        Assert.AreEqual("lol欧服", protocol.LastTabMapFname);
    }

    [TestMethod]
    public async Task DelPostsAsync_DelegatesToInternalProtocol()
    {
        var protocol = new RecordingThreadProtocol(
            new Posts
            {
                Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
            },
            new Comments
            {
                Page = new PageT(),
                Forum = new ForumT { Fname = "lol欧服" },
                Thread = CreateThread(),
                Post = CreatePost(),
                Objs = []
            });
        var module = new ThreadModule(protocol);
        IReadOnlyList<long> pids = [2001, 2002];

        await module.DelPostsAsync("lol欧服", 1001, pids, true);

        Assert.AreEqual("lol欧服", protocol.LastDelPostsFname);
        Assert.AreEqual(1001, protocol.LastDelPostsTid);
        CollectionAssert.AreEqual((System.Collections.ICollection)pids,
            (System.Collections.ICollection)protocol.LastDelPostsPids!);
        Assert.IsTrue(protocol.LastDelPostsBlock);
    }

    [TestMethod]
    public async Task SetThreadPrivacyAsync_DelegatesToInternalProtocol()
    {
        var protocol = new RecordingThreadProtocol(
            new Posts
            {
                Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
            },
            new Comments
            {
                Page = new PageT(),
                Forum = new ForumT { Fname = "lol欧服" },
                Thread = CreateThread(),
                Post = CreatePost(),
                Objs = []
            });
        var module = new ThreadModule(protocol);

        await module.SetThreadPrivacyAsync("lol欧服", 1001, 2002, false);

        Assert.AreEqual("lol欧服", protocol.LastPrivacyFname);
        Assert.AreEqual(1001, protocol.LastPrivacyTid);
        Assert.AreEqual(2002, protocol.LastPrivacyPid);
        Assert.IsFalse(protocol.LastPrivacyIsPrivate);
    }

    [TestMethod]
    public async Task ThreadModule_DelegatesThreadListingAndFidBasedVariants()
    {
        var expectedThreads = new Threads
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            TabDictionary = new Dictionary<string, int>(),
            Objs = []
        };
        var protocol =
            new RecordingThreadProtocol(
                new Posts
                {
                    Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
                },
                new Comments
                {
                    Page = new PageT(),
                    Forum = new ForumT { Fname = "lol欧服" },
                    Thread = CreateThread(),
                    Post = CreatePost(),
                    Objs = []
                })
            {
                ThreadsResult = expectedThreads, TabMapResult = new TabMap([new KeyValuePair<string, int>("全部", 1)])
            };
        var module = new ThreadModule(protocol);

        var byName = await module.GetThreadsAsync("lol欧服", 2, 20, ThreadSortType.Create, true);
        Assert.AreSame(expectedThreads, byName);
        Assert.AreEqual("lol欧服", protocol.LastThreadsFname);
        Assert.AreEqual(ThreadSortType.Create, protocol.LastThreadSort);
        Assert.IsTrue(protocol.LastThreadIsGood);

        var byFid = await module.GetThreadsAsync(7356044, 3, 10, ThreadSortType.Reply, false);
        var recoversByFid = await module.GetRecoversAsync(7356044, 2, 10, 99);
        var recoverInfoByFid = await module.GetRecoverInfoAsync(7356044, 1001, 2002);
        var tabMapByFid = await module.GetTabMapAsync(7356044);

        Assert.AreSame(expectedThreads, byFid);
        Assert.AreEqual((ulong)7356044, protocol.LastThreadsFid);
        Assert.AreEqual(ThreadSortType.Reply, protocol.LastThreadSort);
        Assert.IsFalse(protocol.LastThreadIsGood);
        Assert.AreEqual((ulong)7356044, protocol.LastRecoversFid);
        Assert.AreSame(protocol.RecoversResult, recoversByFid);
        Assert.AreEqual((ulong)7356044, protocol.LastRecoverInfoFid);
        Assert.AreSame(protocol.RecoverInfoResult, recoverInfoByFid);
        Assert.AreEqual((ulong)7356044, protocol.LastTabMapFid);
        Assert.AreSame(protocol.TabMapResult, tabMapByFid);
    }

    [TestMethod]
    public async Task ThreadModule_DelegatesVoteAndModerationOperations()
    {
        var protocol = new RecordingThreadProtocol(
            new Posts
            {
                Page = new PageT(), Forum = new ForumT { Fname = "lol欧服" }, Thread = CreateThread(), Objs = []
            },
            new Comments
            {
                Page = new PageT(),
                Forum = new ForumT { Fname = "lol欧服" },
                Thread = CreateThread(),
                Post = CreatePost(),
                Objs = []
            });
        var module = new ThreadModule(protocol);

        await module.AgreeAsync(1, 2, true, false, false);
        await module.DisagreeAsync(3, 4, true, false);
        await module.UnagreeAsync(5, 6, false);
        await module.UndisagreeAsync(7, 8, true);
        await module.AddPostAsync("lol欧服", 1001, "hello", "show-name");
        await module.DelThreadAsync("lol欧服", 1001);
        await module.DelPostAsync("lol欧服", 1001, 2002);
        await module.DelThreadsAsync("lol欧服", [11, 22], true);
        await module.GoodAsync("lol欧服", 1001, "活动");
        await module.UngoodAsync("lol欧服", 1001);
        await module.TopAsync("lol欧服", 1001, true);
        await module.UntopAsync("lol欧服", 1001, true);
        await module.MoveAsync("lol欧服", 1001, 202, 101);
        await module.RecommendAsync("lol欧服", 1001);
        await module.RecoverAsync("lol欧服", 1001, 2002, true);

        Assert.AreEqual(7, protocol.LastAgreeTid);
        Assert.AreEqual(8, protocol.LastAgreePid);
        Assert.IsTrue(protocol.LastAgreeIsComment);
        Assert.IsTrue(protocol.LastAgreeIsDisagree);
        Assert.IsTrue(protocol.LastAgreeIsUndo);
        Assert.AreEqual("show-name", protocol.LastAddPostShowName);
        Assert.AreEqual("lol欧服", protocol.LastDelThreadFname);
        Assert.AreEqual(2002, protocol.LastDelPostPid);
        CollectionAssert.AreEqual((System.Collections.ICollection)new long[] { 11, 22 },
            (System.Collections.ICollection)protocol.LastDelThreadsTids!);
        Assert.IsTrue(protocol.LastDelThreadsBlock);
        Assert.AreEqual("活动", protocol.LastGoodCname);
        Assert.AreEqual("lol欧服", protocol.LastUngoodFname);
        Assert.IsTrue(protocol.LastTopIsVip);
        Assert.IsTrue(protocol.LastUntopIsVip);
        Assert.AreEqual(202, protocol.LastMoveToTabId);
        Assert.AreEqual(101, protocol.LastMoveFromTabId);
        Assert.AreEqual("lol欧服", protocol.LastRecommendFname);
        Assert.IsTrue(protocol.LastRecoverIsHide);
    }

    private static ThreadModel CreateThread()
    {
        return new ThreadModel { Content = new ContentModel(), VirtualImage = new VirtualImagePfModel() };
    }

    private static PostModel CreatePost()
    {
        return new PostModel { Content = new ContentModel() };
    }

    private sealed class RecordingThreadProtocol(Posts postsResult, Comments commentsResult) : IThreadProtocol
    {
        public Threads ThreadsResult { get; init; } = new()
        {
            Page = new PageT(), Forum = new ForumT(), TabDictionary = new Dictionary<string, int>(), Objs = []
        };

        public Recovers RecoversResult { get; init; } = new([], new RecoverPage());

        public RecoverInfo RecoverInfoResult { get; init; } = new()
        {
            Content = new ContentModel(), User = new RecoverUser()
        };

        public TabMap TabMapResult { get; init; } = new();
        public long LastPostsTid { get; private set; }
        public int LastPostsPn { get; private set; }
        public int LastPostsRn { get; private set; }
        public PostSortType LastPostsSort { get; private set; }
        public bool LastOnlyThreadAuthor { get; private set; }
        public bool LastWithComments { get; private set; }
        public int LastCommentRn { get; private set; }
        public bool LastCommentSortByAgree { get; private set; }
        public long LastCommentsTid { get; private set; }
        public long LastCommentsPid { get; private set; }
        public int LastCommentsPn { get; private set; }
        public bool LastIsComment { get; private set; }
        public string? LastRecoversFname { get; private set; }
        public string? LastThreadsFname { get; private set; }
        public ulong LastThreadsFid { get; private set; }
        public int LastThreadsPn { get; private set; }
        public int LastThreadsRn { get; private set; }
        public ThreadSortType LastThreadSort { get; private set; }
        public bool LastThreadIsGood { get; private set; }
        public ulong LastRecoversFid { get; private set; }
        public int LastRecoversPn { get; private set; }
        public int LastRecoversRn { get; private set; }
        public long? LastRecoversUserId { get; private set; }
        public string? LastRecoverInfoFname { get; private set; }
        public ulong LastRecoverInfoFid { get; private set; }
        public long LastRecoverInfoTid { get; private set; }
        public long LastRecoverInfoPid { get; private set; }
        public string? LastTabMapFname { get; private set; }
        public ulong LastTabMapFid { get; private set; }
        public string? LastDelPostsFname { get; private set; }
        public long LastDelPostsTid { get; private set; }
        public IReadOnlyList<long>? LastDelPostsPids { get; private set; }
        public bool LastDelPostsBlock { get; private set; }
        public string? LastPrivacyFname { get; private set; }
        public long LastPrivacyTid { get; private set; }
        public long LastPrivacyPid { get; private set; }
        public bool LastPrivacyIsPrivate { get; private set; }
        public long LastAgreeTid { get; private set; }
        public long LastAgreePid { get; private set; }
        public bool LastAgreeIsComment { get; private set; }
        public bool LastAgreeIsDisagree { get; private set; }
        public bool LastAgreeIsUndo { get; private set; }
        public string? LastAddPostFname { get; private set; }
        public long LastAddPostTid { get; private set; }
        public string? LastAddPostContent { get; private set; }
        public string? LastAddPostShowName { get; private set; }
        public string? LastDelThreadFname { get; private set; }
        public long LastDelThreadTid { get; private set; }
        public string? LastDelPostFname { get; private set; }
        public long LastDelPostTid { get; private set; }
        public long LastDelPostPid { get; private set; }
        public string? LastDelThreadsFname { get; private set; }
        public IReadOnlyList<long>? LastDelThreadsTids { get; private set; }
        public bool LastDelThreadsBlock { get; private set; }
        public string? LastGoodFname { get; private set; }
        public long LastGoodTid { get; private set; }
        public string? LastGoodCname { get; private set; }
        public string? LastUngoodFname { get; private set; }
        public long LastUngoodTid { get; private set; }
        public string? LastTopFname { get; private set; }
        public long LastTopTid { get; private set; }
        public bool LastTopIsVip { get; private set; }
        public string? LastUntopFname { get; private set; }
        public long LastUntopTid { get; private set; }
        public bool LastUntopIsVip { get; private set; }
        public string? LastMoveFname { get; private set; }
        public long LastMoveTid { get; private set; }
        public int LastMoveToTabId { get; private set; }
        public int LastMoveFromTabId { get; private set; }
        public string? LastRecommendFname { get; private set; }
        public long LastRecommendTid { get; private set; }
        public string? LastRecoverFname { get; private set; }
        public long LastRecoverTid { get; private set; }
        public long LastRecoverPid { get; private set; }
        public bool LastRecoverIsHide { get; private set; }

        public Task<Threads> GetThreadsAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default)
        {
            LastThreadsFname = fname;
            LastThreadsPn = pn;
            LastThreadsRn = rn;
            LastThreadSort = sort;
            LastThreadIsGood = isGood;
            return Task.FromResult(ThreadsResult);
        }

        public Task<Threads> GetThreadsAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default)
        {
            LastThreadsFid = fid;
            LastThreadsPn = pn;
            LastThreadsRn = rn;
            LastThreadSort = sort;
            LastThreadIsGood = isGood;
            return Task.FromResult(ThreadsResult);
        }

        public Task<Posts> GetPostsAsync(long tid, int pn, int rn, PostSortType sort, bool onlyThreadAuthor,
            bool withComments, int commentRn, bool commentSortByAgree, CancellationToken cancellationToken = default)
        {
            LastPostsTid = tid;
            LastPostsPn = pn;
            LastPostsRn = rn;
            LastPostsSort = sort;
            LastOnlyThreadAuthor = onlyThreadAuthor;
            LastWithComments = withComments;
            LastCommentRn = commentRn;
            LastCommentSortByAgree = commentSortByAgree;
            return Task.FromResult(postsResult);
        }

        public Task<Comments> GetCommentsAsync(long tid, long pid, int pn, bool isComment,
            CancellationToken cancellationToken = default)
        {
            LastCommentsTid = tid;
            LastCommentsPid = pid;
            LastCommentsPn = pn;
            LastIsComment = isComment;
            return Task.FromResult(commentsResult);
        }

        public Task<Recovers> GetRecoversAsync(string fname, int pn, int rn, long? userId,
            CancellationToken cancellationToken = default)
        {
            LastRecoversFname = fname;
            LastRecoversPn = pn;
            LastRecoversRn = rn;
            LastRecoversUserId = userId;
            return Task.FromResult(RecoversResult);
        }

        public Task<Recovers> GetRecoversAsync(ulong fid, int pn, int rn, long? userId,
            CancellationToken cancellationToken = default)
        {
            LastRecoversFid = fid;
            LastRecoversPn = pn;
            LastRecoversRn = rn;
            LastRecoversUserId = userId;
            return Task.FromResult(RecoversResult);
        }

        public Task<RecoverInfo> GetRecoverInfoAsync(string fname, long tid, long pid,
            CancellationToken cancellationToken = default)
        {
            LastRecoverInfoFname = fname;
            LastRecoverInfoTid = tid;
            LastRecoverInfoPid = pid;
            return Task.FromResult(RecoverInfoResult);
        }

        public Task<RecoverInfo> GetRecoverInfoAsync(ulong fid, long tid, long pid,
            CancellationToken cancellationToken = default)
        {
            LastRecoverInfoFid = fid;
            LastRecoverInfoTid = tid;
            LastRecoverInfoPid = pid;
            return Task.FromResult(RecoverInfoResult);
        }

        public Task<TabMap> GetTabMapAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastTabMapFname = fname;
            return Task.FromResult(TabMapResult);
        }

        public Task<TabMap> GetTabMapAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            LastTabMapFid = fid;
            return Task.FromResult(TabMapResult);
        }

        public Task<bool> AgreeAsync(long tid, long pid, bool isComment, bool isDisagree, bool isUndo,
            CancellationToken cancellationToken = default)
        {
            LastAgreeTid = tid;
            LastAgreePid = pid;
            LastAgreeIsComment = isComment;
            LastAgreeIsDisagree = isDisagree;
            LastAgreeIsUndo = isUndo;
            return Task.FromResult(true);
        }

        public Task<bool> AddPostAsync(string fname, long tid, string content, string? showName,
            CancellationToken cancellationToken = default)
        {
            LastAddPostFname = fname;
            LastAddPostTid = tid;
            LastAddPostContent = content;
            LastAddPostShowName = showName;
            return Task.FromResult(true);
        }

        public Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((LastDelThreadFname = fname) == fname && (LastDelThreadTid = tid) == tid);
        }

        public Task<bool> DelPostAsync(string fname, long tid, long pid,
            CancellationToken cancellationToken = default)
        {
            LastDelPostFname = fname;
            LastDelPostTid = tid;
            LastDelPostPid = pid;
            return Task.FromResult(true);
        }

        public Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block,
            CancellationToken cancellationToken = default)
        {
            LastDelThreadsFname = fname;
            LastDelThreadsTids = tids;
            LastDelThreadsBlock = block;
            return Task.FromResult(true);
        }

        public Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block,
            CancellationToken cancellationToken = default)
        {
            LastDelPostsFname = fname;
            LastDelPostsTid = tid;
            LastDelPostsPids = pids;
            LastDelPostsBlock = block;
            return Task.FromResult(true);
        }

        public Task<bool> GoodAsync(string fname, long tid, string cname,
            CancellationToken cancellationToken = default)
        {
            LastGoodFname = fname;
            LastGoodTid = tid;
            LastGoodCname = cname;
            return Task.FromResult(true);
        }

        public Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((LastUngoodFname = fname) == fname && (LastUngoodTid = tid) == tid);
        }

        public Task<bool> TopAsync(string fname, long tid, bool isVip, CancellationToken cancellationToken = default)
        {
            LastTopFname = fname;
            LastTopTid = tid;
            LastTopIsVip = isVip;
            return Task.FromResult(true);
        }

        public Task<bool> UntopAsync(string fname, long tid, bool isVip,
            CancellationToken cancellationToken = default)
        {
            LastUntopFname = fname;
            LastUntopTid = tid;
            LastUntopIsVip = isVip;
            return Task.FromResult(true);
        }

        public Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId,
            CancellationToken cancellationToken = default)
        {
            LastMoveFname = fname;
            LastMoveTid = tid;
            LastMoveToTabId = toTabId;
            LastMoveFromTabId = fromTabId;
            return Task.FromResult(true);
        }

        public Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((LastRecommendFname = fname) == fname && (LastRecommendTid = tid) == tid);
        }

        public Task<bool> RecoverAsync(string fname, long tid, long pid, bool isHide,
            CancellationToken cancellationToken = default)
        {
            LastRecoverFname = fname;
            LastRecoverTid = tid;
            LastRecoverPid = pid;
            LastRecoverIsHide = isHide;
            return Task.FromResult(true);
        }

        public Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate,
            CancellationToken cancellationToken = default)
        {
            LastPrivacyFname = fname;
            LastPrivacyTid = tid;
            LastPrivacyPid = pid;
            LastPrivacyIsPrivate = isPrivate;
            return Task.FromResult(true);
        }
    }
}
