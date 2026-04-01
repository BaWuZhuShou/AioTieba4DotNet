using AioTieba4DotNet.Models.Admins;

namespace AioTieba4DotNet.Protocols;

internal interface IAdminProtocol
{
    Task<bool> AddBawuAsync(string fname, string userName, BawuType bawuType,
        CancellationToken cancellationToken = default);

    Task<bool> DelBawuAsync(string fname, string portrait, BawuType bawuType,
        CancellationToken cancellationToken = default);

    Task<bool> AddBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default);

    Task<bool> DelBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default);

    Task<BawuBlacklistUsers> GetBawuBlacklistAsync(string fname, int pn, CancellationToken cancellationToken = default);

    Task<BawuInfo> GetBawuInfoAsync(string fname, CancellationToken cancellationToken = default);

    Task<BawuPerm> GetBawuPermAsync(string fname, string portrait, CancellationToken cancellationToken = default);

    Task<bool> SetBawuPermAsync(string fname, string portrait, BawuPermType permissions,
        CancellationToken cancellationToken = default);

    Task<BawuPostLogs> GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<BawuUserLogs> GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Appeals> GetUnblockAppealsAsync(string fname, int pn, int rn, CancellationToken cancellationToken = default);

    Task<bool> HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse,
        CancellationToken cancellationToken = default);

    Task<Blocks> GetBlocksAsync(string fname, string userName, int pn, CancellationToken cancellationToken = default);

    Task<bool> BlockAsync(ulong fid, string portrait, int day, string reason,
        CancellationToken cancellationToken = default);

    Task<bool> BlockAsync(string fname, string portrait, int day, string reason,
        CancellationToken cancellationToken = default);

    Task<bool> UnblockAsync(ulong fid, long userId, CancellationToken cancellationToken = default);

    Task<bool> UnblockAsync(string fname, long userId, CancellationToken cancellationToken = default);
}
