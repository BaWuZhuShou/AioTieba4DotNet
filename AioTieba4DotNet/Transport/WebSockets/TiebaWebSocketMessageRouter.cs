using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed class TiebaWebSocketMessageRouter
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<WSRes>> _pending = new();
    private readonly Channel<WSRes> _pushChannel = Channel.CreateUnbounded<WSRes>();

    internal TaskCompletionSource<WSRes> RegisterPending(int reqId)
    {
        var tcs = new TaskCompletionSource<WSRes>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(reqId, tcs))
            throw new InvalidOperationException($"A pending WebSocket request with reqId {reqId} already exists.");

        return tcs;
    }

    internal void CancelPending(int reqId, CancellationToken cancellationToken)
    {
        if (_pending.TryRemove(reqId, out var pending)) pending.TrySetCanceled(cancellationToken);
    }

    internal bool TryRemovePending(int reqId, out TaskCompletionSource<WSRes>? pending)
    {
        return _pending.TryRemove(reqId, out pending);
    }

    internal bool TryCompletePending(WSRes response)
    {
        if (response.ReqId == 0) return false;
        if (!_pending.TryRemove(response.ReqId, out var pending)) return false;

        pending.TrySetResult(response);
        return true;
    }

    internal Task PublishAsync(WSRes response, CancellationToken cancellationToken)
    {
        return _pushChannel.Writer.WriteAsync(response, cancellationToken).AsTask();
    }

    internal void FailPending(Exception exception)
    {
        foreach (var entry in _pending)
            if (_pending.TryRemove(entry.Key, out var pending))
                pending.TrySetException(exception);
    }

    internal void Complete(Exception? exception = null)
    {
        _pushChannel.Writer.TryComplete(exception);
    }

    internal async IAsyncEnumerable<WSRes> ListenAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await _pushChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_pushChannel.Reader.TryRead(out var response))
            {
                yield return response;
            }
        }

        await _pushChannel.Reader.Completion.WaitAsync(cancellationToken);
    }
}
