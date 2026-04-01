using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Protocols;

namespace AioTieba4DotNet.Modules;

/// <summary>
///     客户端元数据模块默认实现
/// </summary>
public class ClientModule : IClientModule
{
    private readonly IClientProtocol _protocol;

    internal ClientModule(IClientProtocol protocol)
    {
        _protocol = protocol;
    }

    /// <inheritdoc/>
    public Task InitWebSocketAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.InitWebSocketAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<string> InitZIdAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.InitZIdAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<(string ClientId, string SampleId)> SyncAsync(CancellationToken cancellationToken = default)
    {
        return _protocol.SyncAsync(cancellationToken);
    }
}
