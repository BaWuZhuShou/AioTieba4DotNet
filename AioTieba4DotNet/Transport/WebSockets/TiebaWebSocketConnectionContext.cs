using System.Net.WebSockets;

namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed class TiebaWebSocketConnectionContext(
    ITiebaWebSocketConnection connection,
    CancellationTokenSource lifetimeSource)
    : IDisposable
{
    private int _shutdownStarted;

    internal ITiebaWebSocketConnection Connection { get; } = connection;

    internal CancellationTokenSource LifetimeSource { get; } = lifetimeSource;

    internal CancellationToken CancellationToken => LifetimeSource.Token;

    internal TiebaWebSocketMessageRouter Router { get; } = new();

    internal Task? ListenTask { get; set; }

    internal Task? HeartbeatTask { get; set; }

    internal bool IsOpen => Connection.State == WebSocketState.Open;

    internal bool TryBeginShutdown() => Interlocked.Exchange(ref _shutdownStarted, 1) == 0;

    public void Dispose()
    {
        LifetimeSource.Dispose();
        Connection.Dispose();
    }
}
