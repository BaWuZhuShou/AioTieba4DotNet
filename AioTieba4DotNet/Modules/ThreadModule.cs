using System.Collections.Generic;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Protocols;

namespace AioTieba4DotNet.Modules;

public class ThreadModule : IThreadModule
{
    private readonly IThreadProtocol _protocol;

    internal ThreadModule(IThreadProtocol protocol)
    {
        _protocol = protocol;
    }

    public Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default) =>
        _protocol.GetThreadsAsync(fname, pn, rn, sort, isGood, cancellationToken);

    public Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default) =>
        _protocol.GetThreadsAsync(fid, pn, rn, sort, isGood, cancellationToken);

    public Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc,
        bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0, bool commentSortByAgree = false,
        CancellationToken cancellationToken = default) =>
        _protocol.GetPostsAsync(tid, pn, rn, sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree,
            cancellationToken);

    public Task<Comments> GetCommentsAsync(long tid, long pid, int pn = 1, bool isComment = false,
        CancellationToken cancellationToken = default) =>
        _protocol.GetCommentsAsync(tid, pid, pn, isComment, cancellationToken);

    public Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false,
        bool isUndo = false, CancellationToken cancellationToken = default) =>
        _protocol.AgreeAsync(tid, pid, isComment, isDisagree, isUndo, cancellationToken);

    public Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false,
        CancellationToken cancellationToken = default) =>
        _protocol.AgreeAsync(tid, pid, isComment, true, isUndo, cancellationToken);

    public Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false,
        CancellationToken cancellationToken = default) =>
        _protocol.AgreeAsync(tid, pid, isComment, false, true, cancellationToken);

    public Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false,
        CancellationToken cancellationToken = default) =>
        _protocol.AgreeAsync(tid, pid, isComment, true, true, cancellationToken);

    public Task<bool> AddPostAsync(string fname, long tid, string content, string? showName = null,
        CancellationToken cancellationToken = default) =>
        _protocol.AddPostAsync(fname, tid, content, showName, cancellationToken);

    public Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default) =>
        _protocol.DelThreadAsync(fname, tid, cancellationToken);

    public Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default) =>
        _protocol.DelPostAsync(fname, tid, pid, cancellationToken);

    public Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block = false,
        CancellationToken cancellationToken = default) =>
        _protocol.DelThreadsAsync(fname, tids, block, cancellationToken);

    public Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block = false,
        CancellationToken cancellationToken = default) =>
        _protocol.DelPostsAsync(fname, tid, pids, block, cancellationToken);

    public Task<bool> GoodAsync(string fname, long tid, string cname = "", CancellationToken cancellationToken = default) =>
        _protocol.GoodAsync(fname, tid, cname, cancellationToken);

    public Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default) =>
        _protocol.UngoodAsync(fname, tid, cancellationToken);

    public Task<bool> TopAsync(string fname, long tid, bool isVip = false, CancellationToken cancellationToken = default) =>
        _protocol.TopAsync(fname, tid, isVip, cancellationToken);

    public Task<bool> UntopAsync(string fname, long tid, bool isVip = false, CancellationToken cancellationToken = default) =>
        _protocol.UntopAsync(fname, tid, isVip, cancellationToken);

    public Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0,
        CancellationToken cancellationToken = default) =>
        _protocol.MoveAsync(fname, tid, toTabId, fromTabId, cancellationToken);

    public Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default) =>
        _protocol.RecommendAsync(fname, tid, cancellationToken);

    public Task<bool> RecoverAsync(string fname, long tid = 0, long pid = 0, bool isHide = false,
        CancellationToken cancellationToken = default) =>
        _protocol.RecoverAsync(fname, tid, pid, isHide, cancellationToken);

    public Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate = true,
        CancellationToken cancellationToken = default) =>
        _protocol.SetThreadPrivacyAsync(fname, tid, pid, isPrivate, cancellationToken);
}
