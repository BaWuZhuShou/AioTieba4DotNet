using AioTieba4DotNet.Protocols;

namespace AioTieba4DotNet.Modules;

public class ClientModule : IClientModule
{
    private readonly IClientProtocol _protocol;

    internal ClientModule(IClientProtocol protocol)
    {
        _protocol = protocol;
    }

    public Task InitWebSocketAsync(CancellationToken cancellationToken = default) =>
        _protocol.InitWebSocketAsync(cancellationToken);

    public Task<string> InitZIdAsync(CancellationToken cancellationToken = default) =>
        _protocol.InitZIdAsync(cancellationToken);

    public Task<(string ClientId, string SampleId)> SyncAsync(CancellationToken cancellationToken = default) =>
        _protocol.SyncAsync(cancellationToken);
}
