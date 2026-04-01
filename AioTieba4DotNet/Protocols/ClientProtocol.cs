using AioTieba4DotNet.Api.InitZId;
using AioTieba4DotNet.Api.Sync;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Protocols;

internal sealed class ClientProtocol(TiebaOperationDispatcher dispatcher) : IClientProtocol
{
    public async Task InitWebSocketAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(InitWebSocketAsync),
                TiebaOperationCapabilities.WebSocketOnly(true),
                ExecuteWebSocketAsync: static (_, _) => Task.FromResult(true)),
            cancellationToken);
    }

    public async Task<string> InitZIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                nameof(InitZIdAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => session.ExecuteZIdInitializationAsync(
                    nameof(InitZIdAsync),
                    innerCancellationToken => new InitZId(session.HttpCore).RequestAsync(innerCancellationToken),
                    ct),
                ApplySessionMutation: static (session, zId) => session.UpdateZId(zId)),
            cancellationToken);
    }

    public async Task<(string ClientId, string SampleId)> SyncAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<(string ClientId, string SampleId)>(
                nameof(SyncAsync),
                TiebaOperationCapabilities.HttpOnly(true),
                (session, ct) => session.ExecuteClientSyncAsync(
                    nameof(SyncAsync),
                    innerCancellationToken => new Sync(session.HttpCore).RequestAsync(innerCancellationToken),
                    ct),
                ApplySessionMutation: static (session, result) =>
                    session.UpdateClientIdentifiers(result.ClientId, result.SampleId)),
            cancellationToken);
    }
}
