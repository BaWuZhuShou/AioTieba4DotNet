using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Abstractions;

public interface ITiebaWsCore
{
    Account? Account { get; }
    void SetAccount(Account newAccount);
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task SendAsync(WSReq req, CancellationToken cancellationToken = default);
    Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true, CancellationToken cancellationToken = default);
    IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
}
