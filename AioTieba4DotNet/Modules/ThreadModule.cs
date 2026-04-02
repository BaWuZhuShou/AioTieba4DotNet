using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Protocols;

namespace AioTieba4DotNet.Modules;

/// <summary>
///     主题帖模块默认实现
/// </summary>
public class ThreadModule : IThreadModule
{
    private readonly IThreadProtocol _protocol;

    internal ThreadModule(IThreadProtocol protocol)
    {
        _protocol = protocol;
    }

    /// <inheritdoc />
    public Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetThreadsAsync(fname, pn, rn, sort, isGood, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetThreadsAsync(fid, pn, rn, sort, isGood, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc,
        bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0, bool commentSortByAgree = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetPostsAsync(tid, pn, rn, sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Comments> GetCommentsAsync(long tid, long pid, int pn = 1, bool isComment = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetCommentsAsync(tid, pid, pn, isComment, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Recovers> GetRecoversAsync(string fname, int pn = 1, int rn = 10, long? userId = null,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetRecoversAsync(fname, pn, rn, userId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Recovers> GetRecoversAsync(ulong fid, int pn = 1, int rn = 10, long? userId = null,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetRecoversAsync(fid, pn, rn, userId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RecoverInfo> GetRecoverInfoAsync(string fname, long tid, long pid = 0,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetRecoverInfoAsync(fname, tid, pid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RecoverInfo> GetRecoverInfoAsync(ulong fid, long tid, long pid = 0,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetRecoverInfoAsync(fid, tid, pid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TabMap> GetTabMapAsync(string fname, CancellationToken cancellationToken = default)
    {
        return _protocol.GetTabMapAsync(fname, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TabMap> GetTabMapAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        return _protocol.GetTabMapAsync(fid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false,
        bool isUndo = false, CancellationToken cancellationToken = default)
    {
        return _protocol.AgreeAsync(tid, pid, isComment, isDisagree, isUndo, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.AgreeAsync(tid, pid, isComment, true, isUndo, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.AgreeAsync(tid, pid, isComment, false, true, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.AgreeAsync(tid, pid, isComment, true, true, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AddPostAsync(string fname, long tid, string content, string? showName = null,
        CancellationToken cancellationToken = default)
    {
        return _protocol.AddPostAsync(fname, tid, content, showName, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        return _protocol.DelThreadAsync(fname, tid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default)
    {
        return _protocol.DelPostAsync(fname, tid, pid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.DelThreadsAsync(fname, tids, block, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.DelPostsAsync(fname, tid, pids, block, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> GoodAsync(string fname, long tid, string cname = "",
        CancellationToken cancellationToken = default)
    {
        return _protocol.GoodAsync(fname, tid, cname, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        return _protocol.UngoodAsync(fname, tid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> TopAsync(string fname, long tid, bool isVip = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.TopAsync(fname, tid, isVip, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UntopAsync(string fname, long tid, bool isVip = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.UntopAsync(fname, tid, isVip, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0,
        CancellationToken cancellationToken = default)
    {
        return _protocol.MoveAsync(fname, tid, toTabId, fromTabId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        return _protocol.RecommendAsync(fname, tid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> RecoverAsync(string fname, long tid = 0, long pid = 0, bool isHide = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.RecoverAsync(fname, tid, pid, isHide, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate = true,
        CancellationToken cancellationToken = default)
    {
        return _protocol.SetThreadPrivacyAsync(fname, tid, pid, isPrivate, cancellationToken);
    }
}
