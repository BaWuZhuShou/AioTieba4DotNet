namespace AioTieba4DotNet;

public interface IClientModule
{
    Task InitWebSocketAsync(CancellationToken cancellationToken = default);

    Task<string> InitZIdAsync(CancellationToken cancellationToken = default);

    Task<(string ClientId, string SampleId)> SyncAsync(CancellationToken cancellationToken = default);
}
