using System.Collections.Generic;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models;

namespace AioTieba4DotNet.Contracts;

/// <summary>
///     主题帖模块契约
/// </summary>
public interface IThreadModule
{
    /// <summary>
    ///     按吧名获取主题帖列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否只看精品贴</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>主题帖列表 <see cref="Threads"/></returns>
    Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取主题帖列表
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否只看精品贴</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>主题帖列表 <see cref="Threads"/></returns>
    Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取帖子楼层列表
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="sort">排序方式</param>
    /// <param name="onlyThreadAuthor">是否只看楼主</param>
    /// <param name="withComments">是否附带楼中楼</param>
    /// <param name="commentRn">每层附带的楼中楼数量</param>
    /// <param name="commentSortByAgree">楼中楼是否按赞同排序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>帖子列表 <see cref="Posts"/></returns>
    Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc,
        bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0, bool commentSortByAgree = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取楼中楼回复列表
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID 或楼中楼回复 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="isComment">传入的 pid 是否为楼中楼回复 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>楼中楼列表 <see cref="Comments"/></returns>
    Task<Comments> GetCommentsAsync(long tid, long pid, int pn = 1,
        bool isComment = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧名获取回收站列表。
    /// </summary>
    /// <param name="fname">A forum name.</param>
    /// <param name="pn">A page number.</param>
    /// <param name="rn">A page size.</param>
    /// <param name="userId">A deleted-post author user id filter, or <see langword="null" /> to query all authors.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A recover list.</returns>
    Task<Recovers> GetRecoversAsync(string fname, int pn = 1, int rn = 10, long? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取回收站列表。
    /// </summary>
    /// <param name="fid">A forum id.</param>
    /// <param name="pn">A page number.</param>
    /// <param name="rn">A page size.</param>
    /// <param name="userId">A deleted-post author user id filter, or <see langword="null" /> to query all authors.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A recover list.</returns>
    Task<Recovers> GetRecoversAsync(ulong fid, int pn = 1, int rn = 10, long? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧名获取回收站条目的正文详情。
    /// </summary>
    /// <param name="fname">A forum name.</param>
    /// <param name="tid">A thread id.</param>
    /// <param name="pid">A post id, or <c>0</c> to inspect a recovered thread entry.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A recover-detail model.</returns>
    Task<RecoverInfo> GetRecoverInfoAsync(string fname, long tid, long pid = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取回收站条目的正文详情。
    /// </summary>
    /// <param name="fid">A forum id.</param>
    /// <param name="tid">A thread id.</param>
    /// <param name="pid">A post id, or <c>0</c> to inspect a recovered thread entry.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A recover-detail model.</returns>
    Task<RecoverInfo> GetRecoverInfoAsync(ulong fid, long tid, long pid = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧名获取贴吧分区映射。
    /// </summary>
    /// <param name="fname">A forum name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A tab-name to tab-id map.</returns>
    Task<TabMap> GetTabMapAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取贴吧分区映射。
    /// </summary>
    /// <param name="fid">A forum id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A tab-name to tab-id map.</returns>
    Task<TabMap> GetTabMapAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     点赞或点踩内容
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID，0 表示主题帖</param>
    /// <param name="isComment">是否为楼中楼回复</param>
    /// <param name="isDisagree">是否执行点踩</param>
    /// <param name="isUndo">是否撤销当前操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false,
        bool isUndo = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     点踩内容
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID，0 表示主题帖</param>
    /// <param name="isComment">是否为楼中楼回复</param>
    /// <param name="isUndo">是否撤销点踩</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消点赞
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID，0 表示主题帖</param>
    /// <param name="isComment">是否为楼中楼回复</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消点踩
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID，0 表示主题帖</param>
    /// <param name="isComment">是否为楼中楼回复</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     回复主题帖
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="content">回复内容</param>
    /// <param name="showName">显示名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> AddPostAsync(string fname, long tid, string content, string? showName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除主题帖
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DelThreadAsync(string fname, long tid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除回复
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DelPostAsync(string fname, long tid, long pid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量删除主题帖
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tids">主题帖 ID 列表</param>
    /// <param name="block">是否同时拉黑作者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DelThreadsAsync(string fname, IReadOnlyList<long> tids, bool block = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量删除回复
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pids">回复 ID 列表</param>
    /// <param name="block">是否同时拉黑作者</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DelPostsAsync(string fname, long tid, IReadOnlyList<long> pids, bool block = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     加精主题帖
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="cname">分类名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> GoodAsync(string fname, long tid, string cname = "", CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消加精
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UngoodAsync(string fname, long tid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     置顶主题帖
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="isVip">是否为会员置顶</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> TopAsync(string fname, long tid, bool isVip = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消置顶主题帖
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="isVip">是否为会员置顶</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UntopAsync(string fname, long tid, bool isVip = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     移动主题帖分区
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="toTabId">目标分区 ID</param>
    /// <param name="fromTabId">源分区 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     推荐主题帖
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RecommendAsync(string fname, long tid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     恢复主题帖或回复
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <param name="isHide">是否恢复被隐藏内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RecoverAsync(string fname, long tid = 0, long pid = 0, bool isHide = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     设置主题帖回复隐私
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <param name="isPrivate">是否设为隐私</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetThreadPrivacyAsync(string fname, long tid, long pid, bool isPrivate = true,
        CancellationToken cancellationToken = default);
}
