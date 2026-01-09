using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetFid;
using AioTieba4DotNet.Api.GetForumDetail;
using AioTieba4DotNet.Api.GetForumDetail.Entities;
using AioTieba4DotNet.Api.LikeForum;
using AioTieba4DotNet.Api.UnlikeForum;
using AioTieba4DotNet.Api.Sign;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Modules;

public class ForumModule(ITiebaHttpCore httpCore) : IForumModule
{
    private readonly ForumInfoCache _cache = new();

    public async Task<ulong> GetFidAsync(string fname)
    {
        var forumId = _cache.GetForumId(fname);
        if (forumId != 0) return forumId;

        var api = new GetFid(httpCore);
        forumId = await api.RequestAsync(fname);
        _cache.SetForumName(forumId, fname);

        return forumId;
    }

    public async Task<string> GetFnameAsync(ulong fid)
    {
        var forumName = _cache.GetForumName(fid);
        if (!string.IsNullOrEmpty(forumName)) return forumName;

        var detail = await GetDetailAsync(fid);
        _cache.SetForumName(fid, detail.Fname);

        return detail.Fname;
    }

    public async Task<ForumDetail> GetDetailAsync(ulong fid)
    {
        var api = new GetForumDetail(httpCore);
        return await api.RequestAsync((long)fid);
    }

    public async Task<ForumDetail> GetDetailAsync(string fname)
    {
        var fid = await GetFidAsync(fname);
        return await GetDetailAsync(fid);
    }

    public async Task<bool> LikeAsync(string fname)
    {
        var fid = await GetFidAsync(fname);
        var api = new LikeForum(httpCore);
        return await api.RequestAsync(fid);
    }

    public async Task<bool> UnlikeAsync(string fname)
    {
        var fid = await GetFidAsync(fname);
        var api = new UnlikeForum(httpCore);
        return await api.RequestAsync(fid);
    }

    public async Task<bool> SignAsync(string fname)
    {
        var fid = await GetFidAsync(fname);
        var api = new Sign(httpCore);
        return await api.RequestAsync(fname, fid);
    }
}
