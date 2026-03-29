using AioTieba4DotNet.Api.InitZId;
using AioTieba4DotNet.Api.Sync;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Protocols;

internal sealed class LegacyClientProtocol(LegacyTransportContext transport) : IClientProtocol
{
    public async Task InitWebSocketAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(InitWebSocketAsync));
        session.RequireAuthenticatedAccount(nameof(InitWebSocketAsync));
        await session.WsCore.ConnectAsync(cancellationToken);
    }

    public async Task<string> InitZIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(InitZIdAsync));
        session.RequireAuthenticatedAccount(nameof(InitZIdAsync));
        var api = new InitZId(transport.HttpCore);
        var zId = await api.RequestAsync(cancellationToken);
        session.UpdateZId(zId);
        return zId;
    }

    public async Task<(string ClientId, string SampleId)> SyncAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = transport.RequireSession(nameof(SyncAsync));
        session.RequireAuthenticatedAccount(nameof(SyncAsync));
        var api = new Sync(transport.HttpCore);
        var result = await api.RequestAsync(cancellationToken);
        session.UpdateClientIdentifiers(result.ClientId, result.SampleId);
        return result;
    }
}
