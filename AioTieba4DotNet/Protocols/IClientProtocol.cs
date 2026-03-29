namespace AioTieba4DotNet.Protocols;

internal interface IClientProtocol
{
    Task InitWebSocketAsync(CancellationToken cancellationToken = default);

    Task<string> InitZIdAsync(CancellationToken cancellationToken = default);

    Task<(string ClientId, string SampleId)> SyncAsync(CancellationToken cancellationToken = default);
}
