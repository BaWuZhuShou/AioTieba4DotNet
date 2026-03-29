using AioTieba4DotNet.Api.DelBawu;
using AioTieba4DotNet.Api.GetFid;
using AioTieba4DotNet.Api.GetForum;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Api.GetForumDetail;
using AioTieba4DotNet.Api.LikeForum;
using AioTieba4DotNet.Api.Sign;
using AioTieba4DotNet.Api.UnlikeForum;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Protocols;

internal sealed class LegacyForumProtocol(LegacyTransportContext transport, ForumInfoCache cache) : IForumProtocol
{
    private readonly ForumInfoCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var forumId = _cache.GetForumId(fname);
        if (forumId != 0) return forumId;

        var api = new GetFid(transport.HttpCore);
        forumId = await api.RequestAsync(fname, cancellationToken);
        CacheForum(forumId, fname);
        return forumId;
    }

    public async Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var forumName = _cache.GetForumName(fid);
        if (!string.IsNullOrEmpty(forumName)) return forumName;

        var detail = await GetDetailAsync(fid, cancellationToken);
        _cache.SetForumName(fid, detail.Fname);
        return detail.Fname;
    }

    public async Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetForumDetail(transport.HttpCore);
        var detail = await api.RequestAsync((long)fid, cancellationToken);
        CacheForum(detail.Fid, detail.Fname);
        return detail;
    }

    public async Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fid = await GetFidAsync(fname, cancellationToken);
        return await GetDetailAsync(fid, cancellationToken);
    }

    public async Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(LikeAsync));
        await session.EnsureTbsAsync(nameof(LikeAsync), cancellationToken);
        var fid = await GetFidAsync(fname, cancellationToken);
        var api = new LikeForum(transport.HttpCore);
        return await api.RequestAsync(fid, cancellationToken);
    }

    public async Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(UnlikeAsync));
        await session.EnsureTbsAsync(nameof(UnlikeAsync), cancellationToken);
        var fid = await GetFidAsync(fname, cancellationToken);
        var api = new UnlikeForum(transport.HttpCore);
        return await api.RequestAsync(fid, cancellationToken);
    }

    public async Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(SignAsync));
        await session.EnsureTbsAsync(nameof(SignAsync), cancellationToken);
        var fid = await GetFidAsync(fname, cancellationToken);
        var api = new Sign(transport.HttpCore);
        return await api.RequestAsync(fname, fid, cancellationToken);
    }

    public async Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var api = new GetForum(transport.HttpCore);
        var forum = await api.RequestAsync(fname, cancellationToken);
        CacheForum((ulong)forum.Fid, forum.Fname);
        return forum;
    }

    public async Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(DelBaWuAsync));
        await session.EnsureTbsAsync(nameof(DelBaWuAsync), cancellationToken);
        var fid = await GetFidAsync(fname, cancellationToken);
        var api = new DelBaWu(transport.HttpCore);
        return await api.RequestAsync((long)fid, portrait, baWuType, cancellationToken);
    }

    private void CacheForum(ulong forumId, string forumName)
    {
        if (forumId == 0 || string.IsNullOrWhiteSpace(forumName))
            return;

        _cache.SetForumName(forumId, forumName);
    }
}
