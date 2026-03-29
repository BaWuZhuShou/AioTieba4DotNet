using System.Net.WebSockets;

namespace AioTieba4DotNet.Transport.WebSockets;

internal interface ITiebaWebSocketConnection : IDisposable
{
    WebSocketState State { get; }

    Task ConnectAsync(CancellationToken cancellationToken);

    Task SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

    Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken);

    Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
}

internal interface ITiebaWebSocketConnectionFactory
{
    ITiebaWebSocketConnection CreateConnection();
}

internal sealed class ClientWebSocketConnectionFactory(TiebaWebSocketOptions options) : ITiebaWebSocketConnectionFactory
{
    public ITiebaWebSocketConnection CreateConnection() => new ClientWebSocketConnection(options);
}

internal sealed class ClientWebSocketConnection(TiebaWebSocketOptions options) : ITiebaWebSocketConnection
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ClientWebSocket _socket = new();

    public WebSocketState State => _socket.State;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _socket.Options.AddSubProtocol(TiebaWebSocketOptions.SubProtocol);
        _socket.Options.SetRequestHeader(TiebaWebSocketOptions.ExtensionsHeaderName,
            TiebaWebSocketOptions.ExtensionsHeaderValue);
        _socket.Options.SetRequestHeader(TiebaWebSocketOptions.AcceptEncodingHeaderName,
            TiebaWebSocketOptions.AcceptEncodingHeaderValue);

        await _socket.ConnectAsync(options.Endpoint, cancellationToken);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _socket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            var buffer = new byte[4096];
            result = await _socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                return null;
            }

            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return ms.ToArray();
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
        CancellationToken cancellationToken)
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            await _socket.CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    public void Dispose()
    {
        _sendLock.Dispose();
        _socket.Dispose();
    }
}
