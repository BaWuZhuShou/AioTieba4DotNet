using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.DelBawu;
using AioTieba4DotNet.Api.GetFid;
using AioTieba4DotNet.Api.GetForum;
using AioTieba4DotNet.Api.GetForum.Entities;
using AioTieba4DotNet.Api.GetForumDetail;
using AioTieba4DotNet.Api.GetForumDetail.Entities;
using AioTieba4DotNet.Api.LikeForum;
using AioTieba4DotNet.Api.Sign;
using AioTieba4DotNet.Api.UnlikeForum;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Modules;

/// <summary>
///     贴吧吧务及基础信息功能模块
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
public class ForumModule(ITiebaHttpCore httpCore) : IForumModule
{
    private readonly ForumInfoCache _cache = new();

    /// <summary>
    ///     获取吧 ID (fid)
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>吧 ID (ulong)</returns>
    public async Task<ulong> GetFidAsync(string fname)
    {
        var forumId = _cache.GetForumId(fname);
        if (forumId != 0) return forumId;

        var api = new GetFid(httpCore);
        forumId = await api.RequestAsync(fname);
        _cache.SetForumName(forumId, fname);

        return forumId;
    }

    /// <summary>
    ///     获取吧名 (fname)
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <returns>吧名 (string)</returns>
    public async Task<string> GetFnameAsync(ulong fid)
    {
        var forumName = _cache.GetForumName(fid);
        if (!string.IsNullOrEmpty(forumName)) return forumName;

        var detail = await GetDetailAsync(fid);
        _cache.SetForumName(fid, detail.Fname);

        return detail.Fname;
    }

    /// <summary>
    ///     获取贴吧详情 (通过 Fid)
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <returns>包含贴吧详情信息的 <see cref="ForumDetail"/> 实体</returns>
    public async Task<ForumDetail> GetDetailAsync(ulong fid)
    {
        var api = new GetForumDetail(httpCore);
        return await api.RequestAsync((long)fid);
    }

    /// <summary>
    ///     获取贴吧详情 (通过吧名)
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>包含贴吧详情信息的 <see cref="ForumDetail"/> 实体</returns>
    public async Task<ForumDetail> GetDetailAsync(string fname)
    {
        var fid = await GetFidAsync(fname);
        return await GetDetailAsync(fid);
    }

    /// <summary>
    ///     关注贴吧
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> LikeAsync(string fname)
    {
        var fid = await GetFidAsync(fname);
        var api = new LikeForum(httpCore);
        return await api.RequestAsync(fid);
    }

    /// <summary>
    ///     取消关注贴吧
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> UnlikeAsync(string fname)
    {
        var fid = await GetFidAsync(fname);
        var api = new UnlikeForum(httpCore);
        return await api.RequestAsync(fid);
    }

    /// <summary>
    ///     贴吧签到
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> SignAsync(string fname)
    {
        var fid = await GetFidAsync(fname);
        var api = new Sign(httpCore);
        return await api.RequestAsync(fname, fid);
    }

    /// <summary>
    ///     获取贴吧基础信息 (主要用于检查贴吧是否存在)
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>包含贴吧基础信息的 <see cref="Forum"/> 实体</returns>
    public async Task<Forum> GetForumAsync(string fname)
    {
        var api = new GetForum(httpCore);
        return await api.RequestAsync(fname);
    }

    /// <summary>
    ///     移除吧务
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <param name="baWuType">吧务类型</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType)
    {
        var fid = await GetFidAsync(fname);
        var api = new DelBaWu(httpCore);
        return await api.RequestAsync((int)fid, portrait, baWuType);
    }
}
