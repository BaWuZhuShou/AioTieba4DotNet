using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed class TiebaWebSocketEngine(
    Func<Account?> accountProvider,
    TiebaWebSocketFrameCodec frameCodec,
    TiebaWebSocketHandshakeBuilder handshakeBuilder,
    ITiebaWebSocketConnectionFactory connectionFactory,
    TiebaWebSocketOptions options,
    ITiebaWebSocketDelayStrategy delayStrategy)
    : IDisposable
{
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly CancellationTokenSource _lifetimeSource = new();
    private readonly object _syncRoot = new();
    private TiebaWebSocketConnectionContext? _currentConnection;
    private bool _disposed;
    private int _reqIdCounter = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    internal async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (TryGetOpenConnection(out _)) return;

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            if (TryGetOpenConnection(out _)) return;

            if (TryGetCurrentConnection(out var staleConnection))
                await ShutdownConnectionAsync(staleConnection!, false, null,
                    CancellationToken.None);

            var connection = await OpenConnectionAsync(cancellationToken);
            SetCurrentConnection(connection);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    internal async Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
    {
        var connection = await EnsureConnectedAsync(cancellationToken);
        var data = req.Payload?.Data?.ToByteArray() ?? [];
        var buffer = frameCodec.Pack(data, req.Cmd, req.ReqId, accountProvider(), true);

        try
        {
            await connection.Connection.SendAsync(buffer, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failure = CreateConnectionLostException(
                $"WebSocket send failed for command {req.Cmd} before a response boundary was reached.", exception);
            await ShutdownConnectionAsync(connection, false, failure, CancellationToken.None);
            throw failure;
        }
    }

    internal async Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
        CancellationToken cancellationToken = default)
    {
        var connection = await EnsureConnectedAsync(cancellationToken);
        return await SendTrackedAsync(connection, cmd, data, encrypt, cancellationToken);
    }

    internal async IAsyncEnumerable<WSRes> ListenAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var connection = await EnsureConnectedAsync(cancellationToken);
        await foreach (var response in connection.Router.ListenAsync(cancellationToken))
            yield return response;
    }

    internal async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!TryGetCurrentConnection(out var connection)) return;

        await ShutdownConnectionAsync(connection!, true, null, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _lifetimeSource.Cancel();
        _lifetimeSource.Dispose();
        _connectLock.Dispose();

        if (TryGetCurrentConnection(out var connection))
        {
            lock (_syncRoot)
            {
                if (ReferenceEquals(_currentConnection, connection)) _currentConnection = null;
            }

            connection!.Router.FailPending(new TiebaWebSocketConnectionLostException(
                "The WebSocket engine was disposed before pending requests completed."));
            connection.Router.Complete();
            connection.LifetimeSource.Cancel();
            connection.Dispose();
        }
    }

    private async Task<TiebaWebSocketConnectionContext> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (TryGetOpenConnection(out var connection)) return connection!;
        await ConnectAsync(cancellationToken);
        if (TryGetOpenConnection(out connection)) return connection!;

        throw new TiebaWebSocketUnavailableException(
            "WebSocket transport was not available after the connection attempt finished.");
    }

    private async Task<TiebaWebSocketConnectionContext> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new TiebaWebSocketConnectionContext(connectionFactory.CreateConnection(),
            CancellationTokenSource.CreateLinkedTokenSource(_lifetimeSource.Token));
        SetCurrentConnection(connection);

        try
        {
            await connection.Connection.ConnectAsync(cancellationToken);
            connection.ListenTask = Task.Run(() => ListenLoopAsync(connection), CancellationToken.None);

            if (accountProvider() is { } account)
            {
                var handshakePayload = handshakeBuilder.Pack(account);
                await SendTrackedAsync(connection, 1001, handshakePayload, false, cancellationToken);
            }

            connection.HeartbeatTask = Task.Run(() => HeartbeatLoopAsync(connection), CancellationToken.None);
            return connection;
        }
        catch (OperationCanceledException)
        {
            await ShutdownConnectionAsync(connection, false, null, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            await ShutdownConnectionAsync(connection, false, null, CancellationToken.None);
            throw new TiebaWebSocketUnavailableException(
                "WebSocket connect/handshake failed before the request pipeline became available.", exception);
        }
    }

    private async Task<WSRes> SendTrackedAsync(TiebaWebSocketConnectionContext connection, int cmd, byte[] data,
        bool encrypt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reqId = Interlocked.Increment(ref _reqIdCounter);
        var pending = connection.Router.RegisterPending(reqId);
        var buffer = frameCodec.Pack(data, cmd, reqId, accountProvider(), encrypt);
        using var registration =
            cancellationToken.Register(() => connection.Router.CancelPending(reqId, cancellationToken));

        try
        {
            await connection.Connection.SendAsync(buffer, cancellationToken);
            return await pending.Task;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            connection.Router.CancelPending(reqId, cancellationToken);
            throw;
        }
        catch (Exception exception)
        {
            var failure = CreateConnectionLostException(
                $"WebSocket request {cmd} failed after transport selection; HTTP fallback is no longer safe.",
                exception);
            await ShutdownConnectionAsync(connection, false, failure, CancellationToken.None);
            throw failure;
        }
    }

    private async Task ListenLoopAsync(TiebaWebSocketConnectionContext connection)
    {
        try
        {
            while (!connection.CancellationToken.IsCancellationRequested)
            {
                var rawFrame = await connection.Connection.ReceiveAsync(connection.CancellationToken);
                if (rawFrame == null)
                {
                    var failure = new TiebaWebSocketConnectionLostException(
                        "The WebSocket server closed the connection before the engine was explicitly closed.");
                    await ShutdownConnectionAsync(connection, false, failure, CancellationToken.None,
                        false);
                    return;
                }

                var (payload, cmd, reqId) = frameCodec.Parse(rawFrame, accountProvider());
                var response = new WSRes
                {
                    Cmd = cmd,
                    ReqId = reqId,
                    Payload = new WSRes.Types.Payload { Data = Google.Protobuf.ByteString.CopyFrom(payload) }
                };

                if (!connection.Router.TryCompletePending(response))
                    await connection.Router.PublishAsync(response, connection.CancellationToken);
            }
        }
        catch (OperationCanceledException) when (connection.CancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            var failure = CreateConnectionLostException(
                "The WebSocket receive loop failed and the active connection was torn down.", exception);
            await ShutdownConnectionAsync(connection, false, failure, CancellationToken.None,
                false);
        }
    }

    private async Task HeartbeatLoopAsync(TiebaWebSocketConnectionContext connection)
    {
        try
        {
            while (!connection.CancellationToken.IsCancellationRequested)
            {
                await delayStrategy.DelayAsync(options.HeartbeatInterval, connection.CancellationToken);
                var heartbeat = frameCodec.Pack([], 0, 0, accountProvider(), true);
                await connection.Connection.SendAsync(heartbeat, connection.CancellationToken);
            }
        }
        catch (OperationCanceledException) when (connection.CancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            var failure = CreateConnectionLostException(
                "The WebSocket heartbeat loop failed and the active connection was torn down.", exception);
            await ShutdownConnectionAsync(connection, false, failure, CancellationToken.None,
                awaitHeartbeatTask: false);
        }
    }

    private async Task ShutdownConnectionAsync(TiebaWebSocketConnectionContext connection, bool graceful,
        Exception? failure, CancellationToken cancellationToken, bool awaitListenTask = true,
        bool awaitHeartbeatTask = true)
    {
        if (!connection.TryBeginShutdown()) return;

        ClearCurrentConnection(connection);
        await connection.LifetimeSource.CancelAsync();

        var combinedFailure = failure;

        try
        {
            var closeStatus = graceful ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.InternalServerError;
            var description = graceful ? "Closing" : "Connection failure";
            await connection.Connection.CloseAsync(closeStatus, description, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception closeException)
        {
            combinedFailure = CombineFailures(combinedFailure, closeException);
        }

        var listenFailure = ObserveBackgroundTaskAsync(connection.ListenTask, connection.CancellationToken,
            awaitListenTask);
        var heartbeatFailure = ObserveBackgroundTaskAsync(connection.HeartbeatTask, connection.CancellationToken,
            awaitHeartbeatTask);
        combinedFailure = CombineFailures(combinedFailure, listenFailure);
        combinedFailure = CombineFailures(combinedFailure, heartbeatFailure);

        if (combinedFailure == null)
        {
            connection.Router.Complete();
        }
        else
        {
            connection.Router.FailPending(combinedFailure);
            connection.Router.Complete(combinedFailure);
        }

        connection.Dispose();

        if (combinedFailure != null && graceful) throw combinedFailure;
    }

    private static Exception? ObserveBackgroundTaskAsync(Task? task, CancellationToken cancellationToken,
        bool shouldAwait)
    {
        if (!shouldAwait || task == null) return null;

        try
        {
            task.GetAwaiter().GetResult();
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static Exception? CombineFailures(Exception? primary, Exception? secondary)
    {
        return primary switch
        {
            null => secondary,
            not null when secondary == null => primary,
            _ => new AggregateException(primary, secondary)
        };
    }

    private static TiebaWebSocketConnectionLostException CreateConnectionLostException(string message, Exception exception)
    {
        return exception as TiebaWebSocketConnectionLostException ?? new TiebaWebSocketConnectionLostException(message,
            exception);
    }

    private bool TryGetOpenConnection(out TiebaWebSocketConnectionContext? connection)
    {
        lock (_syncRoot)
        {
            connection = _currentConnection;
            return connection is { IsOpen: true };
        }
    }

    private bool TryGetCurrentConnection(out TiebaWebSocketConnectionContext? connection)
    {
        lock (_syncRoot)
        {
            connection = _currentConnection;
            return connection != null;
        }
    }

    private void SetCurrentConnection(TiebaWebSocketConnectionContext connection)
    {
        lock (_syncRoot)
        {
            _currentConnection = connection;
        }
    }

    private void ClearCurrentConnection(TiebaWebSocketConnectionContext connection)
    {
        lock (_syncRoot)
        {
            if (ReferenceEquals(_currentConnection, connection)) _currentConnection = null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
