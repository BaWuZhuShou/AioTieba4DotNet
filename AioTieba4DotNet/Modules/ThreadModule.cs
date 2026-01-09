using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetThreads;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Api.GetThreadPosts;
using AioTieba4DotNet.Api.GetThreadPosts.Entities;
using AioTieba4DotNet.Api.GetComments;
using AioTieba4DotNet.Api.GetComments.Entities;
using AioTieba4DotNet.Api.Agree;
using AioTieba4DotNet.Api.AddThread;
using AioTieba4DotNet.Api.AddPost;
using AioTieba4DotNet.Api.DelThread;
using AioTieba4DotNet.Api.DelPost;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Modules;

public class ThreadModule(ITiebaHttpCore httpCore, IForumModule forumModule, ITiebaWsCore wsCore) : IThreadModule
{
    public TiebaRequestMode RequestMode { get; set; } = TiebaRequestMode.Http;

    public async Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply, bool isGood = false, TiebaRequestMode? mode = null)
    {
        var api = new GetThreads(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(fname, pn, rn, (int)sort, isGood ? 1 : 0);
    }

    public async Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply, bool isGood = false, TiebaRequestMode? mode = null)
    {
        var fname = await forumModule.GetFnameAsync(fid);
        return await GetThreadsAsync(fname, pn, rn, sort, isGood, mode);
    }

    public async Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc, bool onlyThreadAuthor = false,
        bool withComments = false, int commentRn = 0, bool commentSortByAgree = false, TiebaRequestMode? mode = null)
    {
        var api = new GetThreadPosts(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(tid, pn, rn, (int)sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree);
    }

    public async Task<AioTieba4DotNet.Api.GetComments.Entities.Comments> GetCommentsAsync(long tid, long pid, int pn = 1, bool isComment = false, TiebaRequestMode? mode = null)
    {
        var api = new GetComments(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(tid, pid, pn, isComment);
    }

    public async Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false, bool isUndo = false)
    {
        var api = new AioTieba4DotNet.Api.Agree.Agree(httpCore);
        return await api.RequestAsync(tid, pid, isComment, isDisagree, isUndo);
    }

    public async Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false)
    {
        return await AgreeAsync(tid, pid, isComment, true, isUndo);
    }

    public async Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false)
    {
        return await AgreeAsync(tid, pid, isComment, false, true);
    }

    public async Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false)
    {
        return await AgreeAsync(tid, pid, isComment, true, true);
    }

    public async Task<long> AddThreadAsync(string fname, string title, string content)
    {
        return await AddThreadAsync(fname, title, [new FragText { Text = content }]);
    }

    public async Task<long> AddThreadAsync(string fname, string title, List<IFrag> contents)
    {
        var fid = await forumModule.GetFidAsync(fname);
        var api = new AddThread(httpCore);
        return await api.RequestAsync(fname, fid, title, contents);
    }

    public async Task<long> AddPostAsync(string fname, long tid, string content, long quoteId = 0, uint floor = 0)
    {
        return await AddPostAsync(fname, tid, [new FragText { Text = content }], quoteId, floor);
    }

    public async Task<long> AddPostAsync(string fname, long tid, List<IFrag> contents, long quoteId = 0, uint floor = 0)
    {
        var fid = await forumModule.GetFidAsync(fname);
        var api = new AddPost(httpCore);
        return await api.RequestAsync(fname, fid, tid, contents, quoteId, floor);
    }

    public async Task<bool> DelThreadAsync(string fname, long tid)
    {
        var fid = await forumModule.GetFidAsync(fname);
        var api = new DelThread(httpCore);
        return await api.RequestAsync(fid, tid);
    }

    public async Task<bool> DelPostAsync(string fname, long tid, long pid)
    {
        var fid = await forumModule.GetFidAsync(fname);
        var api = new DelPost(httpCore);
        return await api.RequestAsync(fid, tid, pid);
    }
}
