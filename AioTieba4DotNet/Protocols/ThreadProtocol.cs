using System.Collections.Generic;
using AioTieba4DotNet.Api.AddPost;
using AioTieba4DotNet.Api.DelPost;
using AioTieba4DotNet.Api.DelPosts;
using AioTieba4DotNet.Api.DelThread;
using AioTieba4DotNet.Api.DelThreads;
using AioTieba4DotNet.Api.GetComments;
using AioTieba4DotNet.Api.GetRecoverInfo;
using AioTieba4DotNet.Api.GetRecovers;
using AioTieba4DotNet.Api.GetTabMap;
using AioTieba4DotNet.Api.GetThreadPosts;
using AioTieba4DotNet.Api.GetThreads;
using AioTieba4DotNet.Api.Good;
using AioTieba4DotNet.Api.Move;
using AioTieba4DotNet.Api.Recommend;
using AioTieba4DotNet.Api.SetThreadPrivacy;
using AioTieba4DotNet.Api.Top;
using AioTieba4DotNet.Api.Ungood;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Transport;
using RecoverApi = AioTieba4DotNet.Api.Recover.Recover;

namespace AioTieba4DotNet.Protocols;

internal sealed class ThreadProtocol(TiebaOperationDispatcher dispatcher, IForumProtocol forums) : IThreadProtocol
{
    public async Task<Threads> GetThreadsAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<Threads>(
                nameof(GetThreadsAsync),
                TiebaOperationCapabilities.WebSocketPreferred(),
                (session, ct) => new GetThreads(session.HttpCore, session.WsCore)
                    .RequestHttpAsync(fname, pn, rn, (int)sort, isGood ? 1 : 0, ct),
                (session, ct) => new GetThreads(session.HttpCore, session.WsCore)
                    .RequestWsAsync(fname, pn, rn, (int)sort, isGood ? 1 : 0, ct)),
            cancellationToken);
    }

    public async Task<Threads> GetThreadsAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
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

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<Posts>(
                nameof(GetPostsAsync),
                TiebaOperationCapabilities.WebSocketPreferred(),
                (session, ct) => new GetThreadPosts(session.HttpCore, session.WsCore)
                    .RequestHttpAsync(tid, pn, rn, (int)sort, onlyThreadAuthor, withComments, commentRn,
                        commentSortByAgree, ct),
                (session, ct) => new GetThreadPosts(session.HttpCore, session.WsCore)
                    .RequestWsAsync(tid, pn, rn, (int)sort, onlyThreadAuthor, withComments, commentRn,
                        commentSortByAgree, ct)),
            cancellationToken);
    }

    public async Task<Comments> GetCommentsAsync(long tid, long pid, int pn, bool isComment,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<Comments>(
                nameof(GetCommentsAsync),
                TiebaOperationCapabilities.WebSocketPreferred(),
                (session, ct) => new GetComments(session.HttpCore, session.WsCore).RequestHttpAsync(tid, pid, pn,
                    isComment, ct),
                (session, ct) => new GetComments(session.HttpCore, session.WsCore).RequestWsAsync(tid, pid, pn,
                    isComment, ct)),
            cancellationToken);
    }

    public async Task<Recovers> GetRecoversAsync(string fname, int pn, int rn, long? userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(fname, nameof(fname));
        ValidatePageNumber(pn, nameof(pn));
        ValidatePageSize(rn, nameof(rn), 50);
        ValidateOptionalUserId(userId);

        await dispatcher.EnsureCanExecuteAsync(nameof(GetRecoversAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await GetRecoversAsync(fid, pn, rn, userId, cancellationToken);
    }

    public async Task<Recovers> GetRecoversAsync(ulong fid, int pn, int rn, long? userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);
        ValidatePageNumber(pn, nameof(pn));
        ValidatePageSize(rn, nameof(rn), 50);
        ValidateOptionalUserId(userId);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<Recovers>(
                nameof(GetRecoversAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetRecovers(session.HttpCore).RequestAsync(fid, userId, pn, rn, ct)),
            cancellationToken);
    }

    public async Task<RecoverInfo> GetRecoverInfoAsync(string fname, long tid, long pid,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(fname, nameof(fname));
        ValidateRecoverInfoTarget(tid, pid);

        await dispatcher.EnsureCanExecuteAsync(nameof(GetRecoverInfoAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await GetRecoverInfoAsync(fid, tid, pid, cancellationToken);
    }

    public async Task<RecoverInfo> GetRecoverInfoAsync(ulong fid, long tid, long pid,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);
        ValidateRecoverInfoTarget(tid, pid);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<RecoverInfo>(
                nameof(GetRecoverInfoAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetRecoverInfo(session.HttpCore).RequestAsync(fid, tid, pid, ct)),
            cancellationToken);
    }

    public async Task<TabMap> GetTabMapAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(fname, nameof(fname));

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<TabMap>(
                nameof(GetTabMapAsync),
                TiebaOperationCapabilities.WebSocketPreferred(requiresAuthentication: true),
                (session, ct) => new GetTabMap(session.HttpCore, session.WsCore).RequestHttpAsync(fname, ct),
                (session, ct) => new GetTabMap(session.HttpCore, session.WsCore).RequestWsAsync(fname, ct)),
            cancellationToken);
    }

    public async Task<TabMap> GetTabMapAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateForumId(fid);

        await dispatcher.EnsureCanExecuteAsync(nameof(GetTabMapAsync),
            TiebaOperationCapabilities.WebSocketPreferred(requiresAuthentication: true), cancellationToken);

        var fname = await forums.GetFnameAsync(fid, cancellationToken);
        return await GetTabMapAsync(fname, cancellationToken);
    }

    public async Task<bool> AgreeAsync(long tid, long pid, bool isComment, bool isDisagree, bool isUndo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(AgreeAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new Api.Agree.Agree(session.HttpCore).RequestAsync(tid, pid, isComment, isDisagree,
                    isUndo, ct)),
            cancellationToken);
    }

    public async Task<bool> AddPostAsync(string fname, long tid, string content, string? showName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(AddPostAsync),
            TiebaOperationCapabilities.WebSocketPreferred(requiresAuthentication: true, requiresTbs: true),
            cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(AddPostAsync),
                TiebaOperationCapabilities.WebSocketPreferred(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new AddPost(session.HttpCore, session.WsCore)
                    .RequestHttpAsync(fname, fid, tid, content, showName, ct),
                (session, ct) => new AddPost(session.HttpCore, session.WsCore)
                    .RequestWsAsync(fname, fid, tid, content, showName, ct)),
            cancellationToken);
    }

    public async Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(DelThreadAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(DelThreadAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new DelThread(session.HttpCore).RequestAsync(fid, tid, isHide: false, ct)),
            cancellationToken);
    }

    public async Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(DelPostAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(DelPostAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new DelPost(session.HttpCore).RequestAsync(fid, tid, pid, ct)),
            cancellationToken);
    }

    public async Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateBatchIds(nameof(tids), tids);

        await dispatcher.EnsureCanExecuteAsync(nameof(DelThreadsAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(DelThreadsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new DelThreads(session.HttpCore).RequestAsync(fid, tids, block, ct)),
            cancellationToken);
    }

    public async Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateBatchIds(nameof(pids), pids);

        await dispatcher.EnsureCanExecuteAsync(nameof(DelPostsAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(DelPostsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new DelPosts(session.HttpCore).RequestAsync(fid, tid, pids, block, ct)),
            cancellationToken);
    }

    public async Task<bool> GoodAsync(string fname, long tid, string cname,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(GoodAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        var categoryId = await forums.GetCidAsync(fname, cname, cancellationToken);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(GoodAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new Good(session.HttpCore).RequestAsync(fname, fid, tid, categoryId, ct)),
            cancellationToken);
    }

    public async Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(UngoodAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(UngoodAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new Ungood(session.HttpCore).RequestAsync(fname, fid, tid, ct)),
            cancellationToken);
    }

    public async Task<bool> TopAsync(string fname, long tid, bool isVip, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(TopAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(TopAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new Top(session.HttpCore).RequestAsync(fname, fid, tid, isVip, isSet: true, ct)),
            cancellationToken);
    }

    public async Task<bool> UntopAsync(string fname, long tid, bool isVip,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(UntopAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(UntopAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new Top(session.HttpCore).RequestAsync(fname, fid, tid, isVip, isSet: false, ct)),
            cancellationToken);
    }

    public async Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (toTabId <= 0)
            throw new ArgumentOutOfRangeException(nameof(toTabId), toTabId, "Target tab id must be positive.");

        await dispatcher.EnsureCanExecuteAsync(nameof(MoveAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(MoveAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new Move(session.HttpCore).RequestAsync(fid, tid, toTabId, fromTabId, ct)),
            cancellationToken);
    }

    public async Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dispatcher.EnsureCanExecuteAsync(nameof(RecommendAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(RecommendAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new Recommend(session.HttpCore).RequestAsync(fid, tid, ct)),
            cancellationToken);
    }

    public async Task<bool> RecoverAsync(string fname, long tid, long pid, bool isHide,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRecoverTarget(tid, pid);

        await dispatcher.EnsureCanExecuteAsync(nameof(RecoverAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(RecoverAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true, requiresTbs: true),
                (session, ct) => new RecoverApi(session.HttpCore).RequestAsync(fid, tid, pid, isHide, ct)),
            cancellationToken);
    }

    public async Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (tid <= 0)
            throw new ArgumentOutOfRangeException(nameof(tid), tid, "Thread id must be positive.");

        if (pid <= 0)
            throw new ArgumentOutOfRangeException(nameof(pid), pid, "Post id must be positive.");

        await dispatcher.EnsureCanExecuteAsync(nameof(SetThreadPrivacyAsync),
            TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true), cancellationToken);

        var fid = await forums.GetFidAsync(fname, cancellationToken);
        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SetThreadPrivacyAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new SetThreadPrivacy(session.HttpCore).RequestAsync(fid, tid, pid, isPrivate, ct)),
            cancellationToken);
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

    private static void ValidateRecoverInfoTarget(long tid, long pid)
    {
        if (tid <= 0)
            throw new ArgumentOutOfRangeException(nameof(tid), tid, "Thread id must be positive.");

        if (pid < 0)
            throw new ArgumentOutOfRangeException(nameof(pid), pid, "Post id cannot be negative.");
    }

    private static void ValidateForumId(ulong fid)
    {
        if (fid == 0)
            throw new ArgumentOutOfRangeException(nameof(fid), fid, "Forum id must be positive.");
    }

    private static void ValidateRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
    }

    private static void ValidatePageNumber(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, "Page number must be positive.");
    }

    private static void ValidatePageSize(int value, string parameterName, int maximum)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, "Page size must be positive.");

        if (value > maximum)
            throw new ArgumentOutOfRangeException(parameterName, value,
                $"Page size must be less than or equal to {maximum}.");
    }

    private static void ValidateOptionalUserId(long? userId)
    {
        if (userId is <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "User id must be positive when provided.");
    }
}
