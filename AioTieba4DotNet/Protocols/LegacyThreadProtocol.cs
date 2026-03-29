using System.Collections.Generic;
using AioTieba4DotNet.Api.AddPost;
using AioTieba4DotNet.Api.DelPosts;
using AioTieba4DotNet.Api.DelThreads;
using AioTieba4DotNet.Api.DelPost;
using AioTieba4DotNet.Api.DelThread;
using AioTieba4DotNet.Api.GetCid;
using AioTieba4DotNet.Api.GetComments;
using AioTieba4DotNet.Api.GetThreadPosts;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Api.GetThreads;
using AioTieba4DotNet.Api.Good;
using AioTieba4DotNet.Api.Move;
using AioTieba4DotNet.Api.Recommend;
using AioTieba4DotNet.Api.Recover;
using AioTieba4DotNet.Api.SetThreadPrivacy;
using AioTieba4DotNet.Api.Top;
using AioTieba4DotNet.Api.Ungood;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Protocols;

internal sealed class LegacyThreadProtocol(LegacyTransportContext transport, IForumProtocol forums) : IThreadProtocol
{
    public async Task<global::AioTieba4DotNet.Models.Threads.Threads> GetThreadsAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetThreads(transport.HttpCore, transport.WsCore);
        return await transport.Dispatcher.DispatchAsync(
            LegacyTransportOperation.GetThreads,
            ct => api.RequestHttpAsync(fname, pn, rn, (int)sort, isGood ? 1 : 0, ct),
            ct => api.RequestWsAsync(fname, pn, rn, (int)sort, isGood ? 1 : 0, ct),
            cancellationToken);
    }

    public async Task<global::AioTieba4DotNet.Models.Threads.Threads> GetThreadsAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fname = await forums.GetFnameAsync(fid, cancellationToken);
        return await GetThreadsAsync(fname, pn, rn, sort, isGood, cancellationToken);
    }

    public async Task<Posts> GetPostsAsync(long tid, int pn, int rn, PostSortType sort, bool onlyThreadAuthor,
        bool withComments, int commentRn, bool commentSortByAgree, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetThreadPosts(transport.HttpCore, transport.WsCore);
        return await transport.Dispatcher.DispatchAsync(
            LegacyTransportOperation.GetThreadPosts,
            ct => api.RequestHttpAsync(tid, pn, rn, (int)sort, onlyThreadAuthor, withComments, commentRn,
                commentSortByAgree, ct),
            ct => api.RequestWsAsync(tid, pn, rn, (int)sort, onlyThreadAuthor, withComments, commentRn,
                commentSortByAgree, ct),
            cancellationToken);
    }

    public async Task<Comments> GetCommentsAsync(long tid, long pid, int pn, bool isComment,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetComments(transport.HttpCore, transport.WsCore);
        return await transport.Dispatcher.DispatchAsync(
            LegacyTransportOperation.GetComments,
            ct => api.RequestHttpAsync(tid, pid, pn, isComment, ct),
            ct => api.RequestWsAsync(tid, pid, pn, isComment, ct),
            cancellationToken);
    }

    public async Task<bool> AgreeAsync(long tid, long pid, bool isComment, bool isDisagree, bool isUndo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(AgreeAsync));
        await session.EnsureTbsAsync(nameof(AgreeAsync), cancellationToken);
        var api = new Api.Agree.Agree(transport.HttpCore);
        return await api.RequestAsync(tid, pid, isComment, isDisagree, isUndo, cancellationToken);
    }

    public async Task<bool> AddPostAsync(string fname, long tid, string content, string? showName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(AddPostAsync));
        await session.EnsureTbsAsync(nameof(AddPostAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new AddPost(transport.HttpCore, transport.WsCore);
        return await transport.Dispatcher.DispatchAsync(
            LegacyTransportOperation.AddPost,
            ct => api.RequestHttpAsync(fname, fid, tid, content, showName, ct),
            ct => api.RequestWsAsync(fname, fid, tid, content, showName, ct),
            cancellationToken);
    }

    public async Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(DelThreadAsync));
        await session.EnsureTbsAsync(nameof(DelThreadAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new DelThread(transport.HttpCore);
        return await api.RequestAsync(fid, tid, isHide: false, cancellationToken);
    }

    public async Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(DelPostAsync));
        await session.EnsureTbsAsync(nameof(DelPostAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new DelPost(transport.HttpCore);
        return await api.RequestAsync(fid, tid, pid, cancellationToken);
    }

    public async Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateBatchIds(nameof(tids), tids);

        var session = transport.RequireSession(nameof(DelThreadsAsync));
        await session.EnsureTbsAsync(nameof(DelThreadsAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new DelThreads(transport.HttpCore);
        return await api.RequestAsync(fid, tids, block, cancellationToken);
    }

    public async Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateBatchIds(nameof(pids), pids);

        var session = transport.RequireSession(nameof(DelPostsAsync));
        await session.EnsureTbsAsync(nameof(DelPostsAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new DelPosts(transport.HttpCore);
        return await api.RequestAsync(fid, tid, pids, block, cancellationToken);
    }

    public async Task<bool> GoodAsync(string fname, long tid, string cname,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(GoodAsync));
        await session.EnsureTbsAsync(nameof(GoodAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var categoryId = await new GetCid(transport.HttpCore).RequestAsync(fname, cname, cancellationToken);
        var api = new Good(transport.HttpCore);
        return await api.RequestAsync(fname, fid, tid, categoryId, cancellationToken);
    }

    public async Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(UngoodAsync));
        await session.EnsureTbsAsync(nameof(UngoodAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new Ungood(transport.HttpCore);
        return await api.RequestAsync(fname, fid, tid, cancellationToken);
    }

    public async Task<bool> TopAsync(string fname, long tid, bool isVip, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(TopAsync));
        await session.EnsureTbsAsync(nameof(TopAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new Top(transport.HttpCore);
        return await api.RequestAsync(fname, fid, tid, isVip, isSet: true, cancellationToken);
    }

    public async Task<bool> UntopAsync(string fname, long tid, bool isVip,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(UntopAsync));
        await session.EnsureTbsAsync(nameof(UntopAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new Top(transport.HttpCore);
        return await api.RequestAsync(fname, fid, tid, isVip, isSet: false, cancellationToken);
    }

    public async Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (toTabId <= 0)
            throw new ArgumentOutOfRangeException(nameof(toTabId), toTabId, "Target tab id must be positive.");

        var session = transport.RequireSession(nameof(MoveAsync));
        await session.EnsureTbsAsync(nameof(MoveAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new Move(transport.HttpCore);
        return await api.RequestAsync(fid, tid, toTabId, fromTabId, cancellationToken);
    }

    public async Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        transport.RequireSession(nameof(RecommendAsync)).RequireAuthenticatedAccount(nameof(RecommendAsync));
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new Recommend(transport.HttpCore);
        return await api.RequestAsync(fid, tid, cancellationToken);
    }

    public async Task<bool> RecoverAsync(string fname, long tid, long pid, bool isHide,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRecoverTarget(tid, pid);

        var session = transport.RequireSession(nameof(RecoverAsync));
        await session.EnsureTbsAsync(nameof(RecoverAsync), cancellationToken);
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new Recover(transport.HttpCore);
        return await api.RequestAsync(fid, tid, pid, isHide, cancellationToken);
    }

    public async Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tid <= 0)
            throw new ArgumentOutOfRangeException(nameof(tid), tid, "Thread id must be positive.");

        if (pid <= 0)
            throw new ArgumentOutOfRangeException(nameof(pid), pid, "Post id must be positive.");

        transport.RequireSession(nameof(SetThreadPrivacyAsync)).RequireAuthenticatedAccount(nameof(SetThreadPrivacyAsync));
        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var api = new SetThreadPrivacy(transport.HttpCore);
        return await api.RequestAsync(fid, tid, pid, isPrivate, cancellationToken);
    }

    private static void ValidateBatchIds(string parameterName, IReadOnlyList<long> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
            throw new ArgumentException("At least one target id is required.", parameterName);

        if (ids.Count > 30)
            throw new ArgumentOutOfRangeException(parameterName, ids.Count,
                "Tieba batch moderation operations support at most 30 ids per request.");

        foreach (var id in ids)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(parameterName, id, "Target ids must be positive.");
        }
    }

    private static void ValidateRecoverTarget(long tid, long pid)
    {
        if (tid <= 0 && pid <= 0)
            throw new ArgumentException("Recover requires either a thread id or a post id.");

        if (tid > 0 && pid > 0)
            throw new ArgumentException("Recover accepts either a thread id or a post id, but not both at once.");
    }
}
