using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Protocols;

namespace AioTieba4DotNet.Modules;

/// <summary>
///     吧务 / 后台管理模块默认实现
/// </summary>
public sealed class AdminModule : IAdminModule
{
    private readonly IAdminProtocol _protocol;

    internal AdminModule(IAdminProtocol protocol)
    {
        _protocol = protocol;
    }

    /// <inheritdoc/>
    public Task<bool> AddBawuAsync(string fname, string userName, BawuType bawuType,
        CancellationToken cancellationToken = default)
    {
        return _protocol.AddBawuAsync(fname, userName, bawuType, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> DelBawuAsync(string fname, string portrait, BawuType bawuType,
        CancellationToken cancellationToken = default)
    {
        return _protocol.DelBawuAsync(fname, portrait, bawuType, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> AddBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default)
    {
        return _protocol.AddBawuBlacklistAsync(fname, userId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> DelBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default)
    {
        return _protocol.DelBawuBlacklistAsync(fname, userId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BawuBlacklistUsers> GetBawuBlacklistAsync(string fname, int pn = 1,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetBawuBlacklistAsync(fname, pn, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BawuInfo> GetBawuInfoAsync(string fname, CancellationToken cancellationToken = default)
    {
        return _protocol.GetBawuInfoAsync(fname, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BawuPerm> GetBawuPermAsync(string fname, string portrait,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetBawuPermAsync(fname, portrait, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> SetBawuPermAsync(string fname, string portrait, BawuPermType permissions,
        CancellationToken cancellationToken = default)
    {
        return _protocol.SetBawuPermAsync(fname, portrait, permissions, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BawuPostLogs> GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetBawuPostLogsAsync(fname, options, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BawuUserLogs> GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetBawuUserLogsAsync(fname, options, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Appeals> GetUnblockAppealsAsync(string fname, int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetUnblockAppealsAsync(fname, pn, rn, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false,
        CancellationToken cancellationToken = default)
    {
        return _protocol.HandleUnblockAppealsAsync(fname, appealIds, refuse, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Blocks> GetBlocksAsync(string fname, string userName = "", int pn = 1,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetBlocksAsync(fname, userName, pn, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "",
        CancellationToken cancellationToken = default)
    {
        return _protocol.BlockAsync(fname, portrait, day, reason, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> UnblockAsync(string fname, long userId, CancellationToken cancellationToken = default)
    {
        return _protocol.UnblockAsync(fname, userId, cancellationToken);
    }
}
