using AioTieba4DotNet.Abstractions;
using System.Net.WebSockets;
using AioTieba4DotNet.Transport.WebSockets;

namespace AioTieba4DotNet.Transport;

internal enum LegacyTransportOperation
{
    GetTbs,
    GetThreads,
    GetThreadPosts,
    GetComments,
    AddPost,
    GetUserPosts,
    GetUserThreads
}

internal static class LegacyTransportPolicy
{
    internal static bool SupportsWebSocket(LegacyTransportOperation operation) => operation switch
    {
        LegacyTransportOperation.GetThreads => true,
        LegacyTransportOperation.GetThreadPosts => true,
        LegacyTransportOperation.GetComments => true,
        LegacyTransportOperation.AddPost => true,
        LegacyTransportOperation.GetUserPosts => true,
        LegacyTransportOperation.GetUserThreads => true,
        _ => false
    };

    internal static bool ShouldUseWebSocket(LegacyTransportOperation operation, TiebaTransportMode mode,
        bool hasWebSocketRequest) =>
        mode == TiebaTransportMode.Auto && hasWebSocketRequest && SupportsWebSocket(operation);

    internal static bool ShouldFallbackToHttp(Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || exception is OperationCanceledException)
            return false;

        return exception is NotImplementedException or WebSocketException or TiebaWebSocketUnavailableException;
    }
}

internal sealed class LegacyTransportDispatcher(ITiebaWsCore wsCore, TiebaTransportMode transportMode)
{
    internal async Task<TResult> DispatchAsync<TResult>(LegacyTransportOperation operation,
        Func<CancellationToken, Task<TResult>> httpRequest,
        Func<CancellationToken, Task<TResult>>? wsRequest = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!LegacyTransportPolicy.ShouldUseWebSocket(operation, transportMode, wsRequest is not null))
            return await httpRequest(cancellationToken);

        try
        {
            await wsCore.ConnectAsync(cancellationToken);
            return await wsRequest!(cancellationToken);
        }
        catch (Exception exception) when (LegacyTransportPolicy.ShouldFallbackToHttp(exception, cancellationToken))
        {
            return await httpRequest(cancellationToken);
        }
    }
}
