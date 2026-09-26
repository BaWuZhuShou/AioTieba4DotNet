using AioTieba4DotNet.Api.GetFid;
using AioTieba4DotNet.Api.GetForumDetail;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Protocols;

internal sealed class ForumIdentityResolver(TiebaOperationDispatcher dispatcher, ForumInfoCache cache)
    : IForumIdentityResolver
{
    private readonly ForumInfoCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var forumId = _cache.GetForumId(fname);
        if (forumId != 0)
            return forumId;

        forumId = await RequestFidAsync(nameof(GetFidAsync), fname, cancellationToken);

        RememberForum(forumId, fname);
        return forumId;
    }

    public async Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var forumName = _cache.GetForumName(fid);
        if (!string.IsNullOrEmpty(forumName))
            return forumName;

        var detail = await GetDetailAsync(fid, cancellationToken);
        // 详情已按响应 ID 回填；这里仍需无条件按请求 ID 写入，包括空名称。
        _cache.SetForumName(fid, detail.Fname);
        return detail.Fname;
    }

    internal async Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var detail = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ForumDetail>(
                nameof(GetDetailAsync),
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetForumDetail(session.HttpCore).RequestAsync((long)fid, ct)),
            cancellationToken);

        RememberForum(detail.Fid, detail.Fname);
        return detail;
    }

    internal async Task<ulong> ResolveFidForOperationAsync(string operationName,
        TiebaOperationCapabilities capabilities, string fname, CancellationToken cancellationToken)
    {
        var forumId = _cache.GetForumId(fname);
        if (forumId != 0)
            return forumId;

        await dispatcher.EnsureCanExecuteAsync(operationName, capabilities, cancellationToken);

        // 保留管理路径：认证准备后不重查缓存，仍发送本次未命中的请求。
        forumId = await RequestFidAsync($"{operationName}ResolveFid", fname, cancellationToken);

        if (forumId != 0)
            _cache.SetForumName(forumId, fname);

        return forumId;
    }

    internal void RememberForum(ulong forumId, string forumName)
    {
        if (forumId == 0 || string.IsNullOrWhiteSpace(forumName))
            return;

        _cache.SetForumName(forumId, forumName);
    }

    private Task<ulong> RequestFidAsync(string operationName, string fname, CancellationToken cancellationToken)
    {
        return dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ulong>(
                operationName,
                TiebaOperationCapabilities.HttpOnly(),
                (session, ct) => new GetFid(session.HttpCore).RequestAsync(fname, ct)),
            cancellationToken);
    }
}
