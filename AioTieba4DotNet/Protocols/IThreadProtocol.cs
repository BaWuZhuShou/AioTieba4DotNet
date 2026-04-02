using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models;

namespace AioTieba4DotNet.Protocols;

internal interface IThreadProtocol
{
    Task<Threads> GetThreadsAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default);

    Task<Threads> GetThreadsAsync(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default);

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "The internal thread-post protocol keeps the upstream Tieba request shape explicit so callers can map one-to-one onto transport options.")]
    Task<Posts> GetPostsAsync(long tid, int pn, int rn, PostSortType sort, bool onlyThreadAuthor,
        bool withComments, int commentRn, bool commentSortByAgree, CancellationToken cancellationToken = default);

    Task<Comments> GetCommentsAsync(long tid, long pid, int pn, bool isComment,
        CancellationToken cancellationToken = default);

    Task<Recovers> GetRecoversAsync(string fname, int pn, int rn, long? userId,
        CancellationToken cancellationToken = default);

    Task<Recovers> GetRecoversAsync(ulong fid, int pn, int rn, long? userId,
        CancellationToken cancellationToken = default);

    Task<RecoverInfo> GetRecoverInfoAsync(string fname, long tid, long pid,
        CancellationToken cancellationToken = default);

    Task<RecoverInfo> GetRecoverInfoAsync(ulong fid, long tid, long pid,
        CancellationToken cancellationToken = default);

    Task<TabMap> GetTabMapAsync(string fname, CancellationToken cancellationToken = default);

    Task<TabMap> GetTabMapAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<bool> AgreeAsync(long tid, long pid, bool isComment, bool isDisagree, bool isUndo,
        CancellationToken cancellationToken = default);

    Task<bool> AddPostAsync(string fname, long tid, string content, string? showName,
        CancellationToken cancellationToken = default);

    Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default);

    Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default);

    Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block,
        CancellationToken cancellationToken = default);

    Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block,
        CancellationToken cancellationToken = default);

    Task<bool> GoodAsync(string fname, long tid, string cname, CancellationToken cancellationToken = default);

    Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default);

    Task<bool> TopAsync(string fname, long tid, bool isVip, CancellationToken cancellationToken = default);

    Task<bool> UntopAsync(string fname, long tid, bool isVip, CancellationToken cancellationToken = default);

    Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId,
        CancellationToken cancellationToken = default);

    Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default);

    Task<bool> RecoverAsync(string fname, long tid, long pid, bool isHide,
        CancellationToken cancellationToken = default);

    Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate,
        CancellationToken cancellationToken = default);
}
