#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Enums;
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
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            Thread = CreateThread(),
            Objs = []
        };
        var protocol = new RecordingThreadProtocol(expected, new Comments
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            Thread = CreateThread(),
            Post = CreatePost(),
            Objs = []
        });
        var module = new ThreadModule(protocol);

        var actual = await module.GetPostsAsync(1001, pn: 2, rn: 15, sort: PostSortType.Hot, onlyThreadAuthor: true,
            withComments: true, commentRn: 2, commentSortByAgree: true);

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
        var protocol = new RecordingThreadProtocol(new Posts
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            Thread = CreateThread(),
            Objs = []
        }, expected);
        var module = new ThreadModule(protocol);

        var actual = await module.GetCommentsAsync(1001, 2002, pn: 3, isComment: true);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1001, protocol.LastCommentsTid);
        Assert.AreEqual(2002, protocol.LastCommentsPid);
        Assert.AreEqual(3, protocol.LastCommentsPn);
        Assert.IsTrue(protocol.LastIsComment);
    }

    [TestMethod]
    public async Task DelPostsAsync_DelegatesToInternalProtocol()
    {
        var protocol = new RecordingThreadProtocol(new Posts
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            Thread = CreateThread(),
            Objs = []
        }, new Comments
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            Thread = CreateThread(),
            Post = CreatePost(),
            Objs = []
        });
        var module = new ThreadModule(protocol);
        IReadOnlyList<long> pids = [2001, 2002];

        await module.DelPostsAsync("lol欧服", 1001, pids, block: true);

        Assert.AreEqual("lol欧服", protocol.LastDelPostsFname);
        Assert.AreEqual(1001, protocol.LastDelPostsTid);
        CollectionAssert.AreEqual((System.Collections.ICollection)pids, (System.Collections.ICollection)protocol.LastDelPostsPids!);
        Assert.IsTrue(protocol.LastDelPostsBlock);
    }

    [TestMethod]
    public async Task SetThreadPrivacyAsync_DelegatesToInternalProtocol()
    {
        var protocol = new RecordingThreadProtocol(new Posts
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            Thread = CreateThread(),
            Objs = []
        }, new Comments
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "lol欧服" },
            Thread = CreateThread(),
            Post = CreatePost(),
            Objs = []
        });
        var module = new ThreadModule(protocol);

        await module.SetThreadPrivacyAsync("lol欧服", 1001, 2002, isPrivate: false);

        Assert.AreEqual("lol欧服", protocol.LastPrivacyFname);
        Assert.AreEqual(1001, protocol.LastPrivacyTid);
        Assert.AreEqual(2002, protocol.LastPrivacyPid);
        Assert.IsFalse(protocol.LastPrivacyIsPrivate);
    }

    private static ThreadModel CreateThread() => new()
    {
        Content = new ContentModel(),
        VirtualImage = new VirtualImagePfModel()
    };

    private static PostModel CreatePost() => new()
    {
        Content = new ContentModel()
    };

    private sealed class RecordingThreadProtocol(Posts postsResult, Comments commentsResult) : IThreadProtocol
    {
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
        public string? LastDelPostsFname { get; private set; }
        public long LastDelPostsTid { get; private set; }
        public IReadOnlyList<long>? LastDelPostsPids { get; private set; }
        public bool LastDelPostsBlock { get; private set; }
        public string? LastPrivacyFname { get; private set; }
        public long LastPrivacyTid { get; private set; }
        public long LastPrivacyPid { get; private set; }
        public bool LastPrivacyIsPrivate { get; private set; }

        public Task<Threads> GetThreadsAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Threads> GetThreadsAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

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

        public Task<bool> AgreeAsync(long tid, long pid, bool isComment, bool isDisagree, bool isUndo,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> AddPostAsync(string fname, long tid, string content, string? showName,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DelPostAsync(string fname, long tid, long pid,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

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
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> TopAsync(string fname, long tid, bool isVip, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> UntopAsync(string fname, long tid, bool isVip,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> RecoverAsync(string fname, long tid, long pid, bool isHide,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

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
