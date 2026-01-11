using AioTieba4DotNet.Api.GetForumDetail.Entities;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet;

/// <summary>
///     客户端
/// </summary>
public class Client : TiebaClient
{
    private readonly ForumInfoCache _forumInfoCache = new();

    /// <summary>
    ///     构造函数
    /// </summary>
    [Obsolete("Use TiebaClient instead.")]
    public Client() : base(new HttpCore())
    {
    }

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="bduss">BDUSS</param>
    /// <param name="stoken">STOKEN</param>
    [Obsolete("Use TiebaClient instead.")]
    public Client(string bduss, string stoken) : base(new HttpCore())
    {
        HttpCore.SetAccount(new Account(bduss, stoken));
    }

    /// <summary>
    ///     默认主题帖排序方式
    /// </summary>
    public ThreadSortType ThreadSortType { get; init; } = ThreadSortType.Reply;

    /// <summary>
    ///     默认是否获取精品帖
    /// </summary>
    public bool ThreadIsGood { get; init; }

    /// <summary>
    ///     默认主题帖页大小
    /// </summary>
    public int ThreadRn { get; init; } = 30;

    /// <summary>
    ///     获取吧 ID
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>吧 ID</returns>
    [Obsolete("Use Forums.GetFidAsync instead.")]
    public Task<ulong> GetFid(string fname)
    {
        return Forums.GetFidAsync(fname);
    }

    /// <summary>
    ///     获取吧名
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <returns>吧名</returns>
    [Obsolete("Use Forums.GetFnameAsync instead.")]
    public Task<string> GetFname(ulong fid)
    {
        return Forums.GetFnameAsync(fid);
    }

    /// <summary>
    ///     获取吧详情
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <returns>吧详情</returns>
    [Obsolete("Use Forums.GetDetailAsync instead.")]
    public Task<ForumDetail> GetForumDetail(ulong fid)
    {
        return Forums.GetDetailAsync(fid);
    }

    /// <summary>
    ///     获取吧详情
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>吧详情</returns>
    [Obsolete("Use Forums.GetDetailAsync instead.")]
    public Task<ForumDetail> GetForumDetail(string fname)
    {
        return Forums.GetDetailAsync(fname);
    }

    /// <summary>
    ///     获取主题帖列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">页大小</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否精品</param>
    /// <returns>主题帖列表</returns>
    [Obsolete("Use Threads.GetThreadsAsync instead.")]
    public Task<Threads> GetThreads(string fname, int pn, int rn, ThreadSortType sort, bool isGood)
    {
        return Threads.GetThreadsAsync(fname, pn, rn, sort, isGood);
    }

    /// <summary>
    ///     获取主题帖列表
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">页大小</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否精品</param>
    /// <returns>主题帖列表</returns>
    [Obsolete("Use Threads.GetThreadsAsync instead.")]
    public Task<Threads> GetThreads(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood)
    {
        return Threads.GetThreadsAsync(fid, pn, rn, sort, isGood);
    }

    /// <summary>
    ///     获取主题帖列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <returns>主题帖列表</returns>
    [Obsolete("Use Threads.GetThreadsAsync instead.")]
    public Task<Threads> GetThreads(string fname, int pn)
    {
        return Threads.GetThreadsAsync(fname, pn, ThreadRn, ThreadSortType, ThreadIsGood);
    }

    /// <summary>
    ///     获取主题帖列表
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="pn">页码</param>
    /// <returns>主题帖列表</returns>
    [Obsolete("Use Threads.GetThreadsAsync instead.")]
    public Task<Threads> GetThreads(ulong fid, int pn)
    {
        return Threads.GetThreadsAsync(fid, pn, ThreadRn, ThreadSortType, ThreadIsGood);
    }

    /// <summary>
    ///     获取用户信息
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <returns>用户信息</returns>
    [Obsolete("Use Users.GetProfileAsync instead.")]
    public Task<UserInfoPf> GetUserInfo(int userId)
    {
        return Users.GetProfileAsync(userId);
    }

    /// <summary>
    ///     获取用户信息
    /// </summary>
    /// <param name="userNameOrPortrait">用户名或头像 ID</param>
    /// <returns>用户信息</returns>
    [Obsolete("Use Users.GetProfileAsync instead.")]
    public Task<UserInfoPf> GetUserInfo(string userNameOrPortrait)
    {
        return Users.GetProfileAsync(userNameOrPortrait);
    }

    /// <summary>
    ///     封禁用户
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="portrait">头像 ID</param>
    /// <param name="day">封禁天数</param>
    /// <param name="reason">原因</param>
    /// <returns>是否成功</returns>
    [Obsolete("Use Users.BlockAsync instead.")]
    public Task<bool> Block(ulong fid, string portrait, int day = 1, string reason = "")
    {
        return Users.BlockAsync(fid, portrait, day, reason);
    }

    /// <summary>
    ///     封禁用户
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">头像 ID</param>
    /// <param name="day">封禁天数</param>
    /// <param name="reason">原因</param>
    /// <returns>是否成功</returns>
    [Obsolete("Use Users.BlockAsync instead.")]
    public Task<bool> Block(string fname, string portrait, int day = 1, string reason = "")
    {
        return Users.BlockAsync(fname, portrait, day, reason);
    }
}
