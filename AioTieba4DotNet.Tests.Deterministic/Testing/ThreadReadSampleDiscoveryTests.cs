#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommentModel = AioTieba4DotNet.Models.Threads.Comment;
using PostModel = AioTieba4DotNet.Models.Threads.Post;
using ThreadModel = AioTieba4DotNet.Models.Threads.Thread;

namespace AioTieba4DotNet.Tests.Testing;

[TestClass]
public sealed class ThreadReadSampleDiscoveryTests
{
    private static readonly SafeForumFixture SafeForum = new("lol欧服吧", "lol欧服", 3581744, "lol欧服");

    [TestMethod]
    public async Task RequireThreadSampleAsync_ReturnsFirstThreadFromLaterPage_WhenEarlierPagesAreEmpty()
    {
        var threadPages = new Queue<Threads>([
            CreateThreadsPage(),
            CreateThreadsPage(CreateThread(1002))
        ]);
        var module = new StubThreadModule(
            () => Task.FromResult(threadPages.Dequeue()),
            _ => throw new NotSupportedException());

        var sample = await ThreadReadSampleDiscovery.RequireThreadSampleAsync(
            module,
            SafeForum,
            nameof(RequireThreadSampleAsync_ReturnsFirstThreadFromLaterPage_WhenEarlierPagesAreEmpty),
            3,
            10,
            ThreadSortType.Reply);

        Assert.AreEqual(1002L, sample.Tid);
        Assert.AreEqual(2, sample.ThreadsPageNumber);
        Assert.AreEqual(10, sample.ThreadsPageSize);
        Assert.AreEqual(ThreadSortType.Reply, sample.ThreadsSort);
        Assert.AreEqual("lol欧服", sample.Forum.ResolvedName);
        Assert.AreEqual(2, module.GetThreadsCalls.Count);
    }

    [TestMethod]
    public async Task RequireThreadSampleAsync_ThrowsInconclusive_WhenSampleWindowHasNoThreads()
    {
        var module = new StubThreadModule(
            () => Task.FromResult(CreateThreadsPage()),
            _ => throw new NotSupportedException());

        var exception = await Assert.ThrowsExactlyAsync<AssertInconclusiveException>(async () =>
            await ThreadReadSampleDiscovery.RequireThreadSampleAsync(
                module,
                SafeForum,
                nameof(RequireThreadSampleAsync_ThrowsInconclusive_WhenSampleWindowHasNoThreads),
                2,
                10,
                ThreadSortType.Reply));

        Assert.Contains("returned zero threads across 2 sampled page(s)", exception.Message);
    }

    [TestMethod]
    public async Task RequireCommentSourceSampleAsync_SkipsExpiredThreadAndReturnsLaterCommentCandidate()
    {
        var threadPages = new Queue<Threads>([
            CreateThreadsPage(CreateThread(1001), CreateThread(1002)),
            CreateThreadsPage(CreateThread(1003))
        ]);
        var postResults = new Dictionary<long, Func<Task<Posts>>>
        {
            [1001] = () => throw new TieBaServerException(110004, "expired sample"),
            [1002] = () => Task.FromResult(CreatePostsPage(1002, CreatePost(2002, 0, 0))),
            [1003] = () => Task.FromResult(CreatePostsPage(1003, CreatePost(2003, 2, 1)))
        };
        var module = new StubThreadModule(
            () => Task.FromResult(threadPages.Dequeue()),
            tid => postResults[tid]());

        var sample = await ThreadReadSampleDiscovery.RequireCommentSourceSampleAsync(
            module,
            SafeForum,
            nameof(RequireCommentSourceSampleAsync_SkipsExpiredThreadAndReturnsLaterCommentCandidate),
            3,
            10,
            2,
            20,
            2,
            ThreadSortType.Reply,
            PostSortType.Hot);

        Assert.AreEqual(1003L, sample.ThreadSample.Tid);
        Assert.AreEqual(2, sample.ThreadSample.ThreadsPageNumber);
        Assert.AreEqual(2003L, sample.Pid);
        Assert.AreEqual<uint>(2u, sample.ReplyCount);
        Assert.AreEqual(1, sample.PreviewCommentCount);
    }

    [TestMethod]
    public async Task RequireCommentSourceSampleAsync_ThrowsInconclusive_WhenNoCommentCandidateExists()
    {
        var threadPages = new Queue<Threads>([
            CreateThreadsPage(CreateThread(1001), CreateThread(1002)),
            CreateThreadsPage()
        ]);
        var module = new StubThreadModule(
            () => Task.FromResult(threadPages.Dequeue()),
            tid => Task.FromResult(CreatePostsPage(tid, CreatePost(tid + 1000, 0, 0))));

        var exception = await Assert.ThrowsExactlyAsync<AssertInconclusiveException>(async () =>
            await ThreadReadSampleDiscovery.RequireCommentSourceSampleAsync(
                module,
                SafeForum,
                nameof(RequireCommentSourceSampleAsync_ThrowsInconclusive_WhenNoCommentCandidateExists),
                2,
                10,
                2,
                20,
                2,
                ThreadSortType.Reply,
                PostSortType.Hot));

        Assert.Contains("no post with comments was found", exception.Message);
        Assert.Contains("2 sampled page(s) and 2 sampled thread(s)", exception.Message);
    }

    private static Threads CreateThreadsPage(params ThreadModel[] threads)
    {
        return new Threads
        {
            Page = new PageT { CurrentPage = 1, HasMore = false },
            Forum = new ForumT { Fname = SafeForum.ResolvedName, Fid = (long)SafeForum.Fid },
            TabDictionary = [],
            Objs = [.. threads]
        };
    }

    private static Posts CreatePostsPage(long tid, params PostModel[] posts)
    {
        return new Posts
        {
            Page = new PageT { CurrentPage = 1, HasMore = false },
            Forum = new ForumT { Fname = SafeForum.ResolvedName, Fid = (long)SafeForum.Fid },
            Thread = CreateThread(tid),
            Objs = [.. posts]
        };
    }

    private static ThreadModel CreateThread(long tid)
    {
        return new ThreadModel
        {
            Content = CreateContent(),
            Tid = tid,
            Pid = tid + 100,
            Title = $"thread-{tid}",
            VirtualImage = new VirtualImagePf()
        };
    }

    private static PostModel CreatePost(long pid, int replyNum, int commentCount)
    {
        var comments = new List<CommentModel>();
        for (var index = 0; index < commentCount; index++)
            comments.Add(new CommentModel { Content = CreateContent(), Pid = pid + index + 1 });

        return new PostModel { Content = CreateContent(), Pid = pid, ReplyNum = (uint)replyNum, Comments = comments };
    }

    private static Content CreateContent()
    {
        return new Content();
    }

    private sealed class StubThreadModule(
        Func<Task<Threads>> getThreadsAsync,
        Func<long, Task<Posts>> getPostsAsync) : IThreadModule
    {
        public List<(string Fname, int Pn, int Rn, ThreadSortType Sort)> GetThreadsCalls { get; } = [];

        public Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30,
            ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
            CancellationToken cancellationToken = default)
        {
            GetThreadsCalls.Add((fname, pn, rn, sort));
            return getThreadsAsync();
        }

        public Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30,
            ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc,
            bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0,
            bool commentSortByAgree = false,
            CancellationToken cancellationToken = default)
        {
            return getPostsAsync(tid);
        }

        public Task<Comments> GetCommentsAsync(long tid, long pid, int pn = 1, bool isComment = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Recovers> GetRecoversAsync(string fname, int pn = 1, int rn = 10, long? userId = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Recovers> GetRecoversAsync(ulong fid, int pn = 1, int rn = 10, long? userId = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<RecoverInfo> GetRecoverInfoAsync(string fname, long tid, long pid = 0,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<RecoverInfo> GetRecoverInfoAsync(ulong fid, long tid, long pid = 0,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TabMap> GetTabMapAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TabMap> GetTabMapAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false,
            bool isUndo = false, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> AddPostAsync(string fname, long tid, string content, string? showName = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> GoodAsync(string fname, long tid, string cname = "",
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> TopAsync(string fname, long tid, bool isVip = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UntopAsync(string fname, long tid, bool isVip = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> RecoverAsync(string fname, long tid = 0, long pid = 0, bool isHide = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
