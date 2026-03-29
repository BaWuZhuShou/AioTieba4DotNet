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

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public class V1PublicBehaviorBaselineTests
{
    [TestMethod]
    public void TiebaClient_PublicShell_ExposesRetainedModules()
    {
        using var client = new TiebaClient(new TiebaOptions { TransportMode = TiebaTransportMode.Http });

        Assert.IsNotNull(client.Forums);
        Assert.IsNotNull(client.Threads);
        Assert.IsNotNull(client.Users);
        Assert.IsNotNull(client.Client);
    }

    [TestMethod]
    public async Task ThreadModule_Delegates_ToInternalProtocol()
    {
        var expected = new Threads
        {
            Page = new PageT(),
            Forum = new ForumT { Fname = "dotnet" },
            Objs = [],
            TabDictionary = []
        };
        var protocol = new RecordingThreadProtocol(expected);
        var module = new ThreadModule(protocol);

        var actual = await module.GetThreadsAsync("dotnet", rn: 20, sort: ThreadSortType.Create);

        Assert.AreSame(expected, actual);
        Assert.AreEqual("dotnet", protocol.LastFname);
        Assert.AreEqual(20, protocol.LastRn);
        Assert.AreEqual(ThreadSortType.Create, protocol.LastSort);
    }

    private sealed class RecordingThreadProtocol(Threads result) : IThreadProtocol
    {
        public string? LastFname { get; private set; }
        public int LastRn { get; private set; }
        public ThreadSortType LastSort { get; private set; }

        public Task<Threads> GetThreadsAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            LastRn = rn;
            LastSort = sort;
            return Task.FromResult(result);
        }

        public Task<Threads> GetThreadsAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
            CancellationToken cancellationToken = default) => Task.FromResult(result);

        public Task<Posts> GetPostsAsync(long tid, int pn, int rn, PostSortType sort, bool onlyThreadAuthor,
            bool withComments, int commentRn, bool commentSortByAgree, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Comments> GetCommentsAsync(long tid, long pid, int pn, bool isComment,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> AgreeAsync(long tid, long pid, bool isComment, bool isDisagree, bool isUndo,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> AddPostAsync(string fname, long tid, string content, string? showName,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> GoodAsync(string fname, long tid, string cname,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> TopAsync(string fname, long tid, bool isVip, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> UntopAsync(string fname, long tid, bool isVip, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> RecoverAsync(string fname, long tid, long pid, bool isHide,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
