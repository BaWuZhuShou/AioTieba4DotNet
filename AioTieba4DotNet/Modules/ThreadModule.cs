using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetThreads;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Api.GetThreadPosts;
using AioTieba4DotNet.Api.GetThreadPosts.Entities;
using AioTieba4DotNet.Api.GetComments;
using AioTieba4DotNet.Api.GetComments.Entities;
using AioTieba4DotNet.Api.AddPost;
using AioTieba4DotNet.Api.DelThread;
using AioTieba4DotNet.Api.DelPost;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Modules;

/// <summary>
/// 帖子（主题帖及回复）功能模块
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="forumModule">贴吧模块组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
public class ThreadModule(ITiebaHttpCore httpCore, IForumModule forumModule, ITiebaWsCore wsCore) : IThreadModule
{
    /// <summary>
    /// 默认请求模式 (Http/Websocket)
    /// </summary>
    public TiebaRequestMode RequestMode { get; set; } = TiebaRequestMode.Http;

    /// <summary>
    /// 获取贴吧主题帖列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页条数</param>
    /// <param name="sort">排序类型</param>
    /// <param name="isGood">是否精品贴</param>
    /// <param name="mode">请求模式覆盖（可选）</param>
    /// <returns>主题帖列表实体</returns>
    public async Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false, TiebaRequestMode? mode = null)
    {
        var api = new GetThreads(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(fname, pn, rn, (int)sort, isGood ? 1 : 0);
    }

    /// <summary>
    /// 获取贴吧主题帖列表 (通过吧 ID)
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页条数</param>
    /// <param name="sort">排序类型</param>
    /// <param name="isGood">是否精品贴</param>
    /// <param name="mode">请求模式覆盖（可选）</param>
    /// <returns>主题帖列表实体</returns>
    public async Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false, TiebaRequestMode? mode = null)
    {
        var fname = await forumModule.GetFnameAsync(fid);
        return await GetThreadsAsync(fname, pn, rn, sort, isGood, mode);
    }

    /// <summary>
    /// 获取主题帖内回复列表
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页条数</param>
    /// <param name="sort">排序类型</param>
    /// <param name="onlyThreadAuthor">是否只看楼主</param>
    /// <param name="withComments">是否包含楼中楼</param>
    /// <param name="commentRn">楼中楼显示条数</param>
    /// <param name="commentSortByAgree">楼中楼是否按赞数排序</param>
    /// <param name="mode">请求模式覆盖（可选）</param>
    /// <returns>回复列表实体</returns>
    public async Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc,
        bool onlyThreadAuthor = false,
        bool withComments = false, int commentRn = 0, bool commentSortByAgree = false, TiebaRequestMode? mode = null)
    {
        var api = new GetThreadPosts(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(tid, pn, rn, (int)sort, onlyThreadAuthor, withComments, commentRn,
            commentSortByAgree);
    }

    /// <summary>
    /// 获取楼中楼回复列表
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID (pid) 或楼中楼 ID (spid)</param>
    /// <param name="pn">页码</param>
    /// <param name="isComment">如果 pid 是楼中楼 ID (spid) 则为 true</param>
    /// <param name="mode">请求模式覆盖（可选）</param>
    /// <returns>楼中楼回复列表实体</returns>
    public async Task<Comments> GetCommentsAsync(long tid, long pid, int pn = 1, bool isComment = false,
        TiebaRequestMode? mode = null)
    {
        var api = new GetComments(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(tid, pid, pn, isComment);
    }

    /// <summary>
    /// 点赞/点踩
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID (为 0 表示对主题帖操作)</param>
    /// <param name="isComment">是否对楼中楼操作</param>
    /// <param name="isDisagree">是否点踩</param>
    /// <param name="isUndo">是否取消操作</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false,
        bool isUndo = false)
    {
        var api = new AioTieba4DotNet.Api.Agree.Agree(httpCore);
        return await api.RequestAsync(tid, pid, isComment, isDisagree, isUndo);
    }

    /// <summary>
    /// 点踩
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <param name="isComment">是否对楼中楼操作</param>
    /// <param name="isUndo">是否取消操作</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false)
    {
        return await AgreeAsync(tid, pid, isComment, true, isUndo);
    }

    /// <summary>
    /// 取消点赞
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <param name="isComment">是否对楼中楼操作</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false)
    {
        return await AgreeAsync(tid, pid, isComment, false, true);
    }

    /// <summary>
    /// 取消点踩
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <param name="isComment">是否对楼中楼操作</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false)
    {
        return await AgreeAsync(tid, pid, isComment, true, true);
    }


    /// <summary>
    /// 发布回复
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="content">内容</param>
    /// <param name="showName">显示名称 (可选)</param>
    /// <param name="mode">请求模式覆盖（可选）</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AddPostAsync(string fname, long tid, string content, string? showName = null,
        TiebaRequestMode? mode = null)
    {
        var fid = await forumModule.GetFidAsync(fname);
        var api = new AddPost(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(fname, fid, tid, content, showName);
    }

    /// <summary>
    /// 删除主题帖
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> DelThreadAsync(string fname, long tid)
    {
        var fid = await forumModule.GetFidAsync(fname);
        var api = new DelThread(httpCore);
        return await api.RequestAsync(fid, tid);
    }

    /// <summary>
    /// 删除回复
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> DelPostAsync(string fname, long tid, long pid)
    {
        var fid = await forumModule.GetFidAsync(fname);
        var api = new DelPost(httpCore);
        return await api.RequestAsync(fid, tid, pid);
    }
}
