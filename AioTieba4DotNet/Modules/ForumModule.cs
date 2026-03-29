using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Protocols;

namespace AioTieba4DotNet.Modules;

/// <summary>
///     贴吧模块默认实现
/// </summary>
public sealed class ForumModule : IForumModule
{
    private readonly IForumProtocol _protocol;

    internal ForumModule(IForumProtocol protocol)
    {
        _protocol = protocol;
    }

    /// <inheritdoc/>
    public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetFidAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.GetFnameAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default) =>
        _protocol.GetDetailAsync(fid, cancellationToken);

    /// <inheritdoc/>
    public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetDetailAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.LikeAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.UnlikeAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.SignAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default) =>
        _protocol.GetForumAsync(fname, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
        CancellationToken cancellationToken = default) =>
        _protocol.DelBaWuAsync(fname, portrait, baWuType, cancellationToken);
}
