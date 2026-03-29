using System.Collections.Generic;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet;

public interface IThreadModule
{
    Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default);

    Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default);

    Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc,
        bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0, bool commentSortByAgree = false,
        CancellationToken cancellationToken = default);

    Task<Comments> GetCommentsAsync(long tid, long pid, int pn = 1,
        bool isComment = false, CancellationToken cancellationToken = default);

    Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false,
        bool isUndo = false, CancellationToken cancellationToken = default);

    Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false,
        CancellationToken cancellationToken = default);

    Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false,
        CancellationToken cancellationToken = default);

    Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false,
        CancellationToken cancellationToken = default);

    Task<bool> AddPostAsync(string fname, long tid, string content, string? showName = null,
        CancellationToken cancellationToken = default);

    Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default);

    Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default);

    Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block = false,
        CancellationToken cancellationToken = default);

    Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block = false,
        CancellationToken cancellationToken = default);

    Task<bool> GoodAsync(string fname, long tid, string cname = "", CancellationToken cancellationToken = default);

    Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default);

    Task<bool> TopAsync(string fname, long tid, bool isVip = false, CancellationToken cancellationToken = default);

    Task<bool> UntopAsync(string fname, long tid, bool isVip = false, CancellationToken cancellationToken = default);

    Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0,
        CancellationToken cancellationToken = default);

    Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default);

    Task<bool> RecoverAsync(string fname, long tid = 0, long pid = 0, bool isHide = false,
        CancellationToken cancellationToken = default);

    Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate = true,
        CancellationToken cancellationToken = default);
}
