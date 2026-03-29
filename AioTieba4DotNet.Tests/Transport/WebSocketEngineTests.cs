#nullable enable
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Transport.WebSockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Transport;

[TestClass]
public class WebSocketEngineTests
{
    [TestMethod]
    public async Task ConnectAsync_WithAuthenticatedAccount_SendsHandshake_AndHeartbeat()
    {
        var account = new Account(new string('b', 192), new string('s', 64));
        var codec = new TiebaWebSocketFrameCodec();
        var delayStrategy = new ManualDelayStrategy();
        var connection = new FakeWebSocketConnection();
        var core = CreateCore(account, codec, delayStrategy, connection);

        var connectTask = core.ConnectAsync();
        var handshakeFrame = await connection.ReadSentFrameAsync();
        var (_, handshakeCmd, handshakeReqId) = codec.Parse(handshakeFrame, account);
        connection.EnqueueInbound(codec.Pack([], handshakeCmd, handshakeReqId, account, false));

        await connectTask;
        await delayStrategy.ReleaseNextAsync();

        var heartbeatFrame = await connection.ReadSentFrameAsync();
        var (_, heartbeatCmd, heartbeatReqId) = codec.Parse(heartbeatFrame, account);

        Assert.AreEqual(1, connection.ConnectCalls);
        Assert.AreEqual(1001, handshakeCmd);
        Assert.IsTrue(handshakeReqId > 0);
        Assert.AreEqual(0, heartbeatCmd);
        Assert.AreEqual(0, heartbeatReqId);
    }

    [TestMethod]
    public async Task SendAsync_PairsResponse_ByReqId()
    {
        var codec = new TiebaWebSocketFrameCodec();
        var connection = new FakeWebSocketConnection();
        var core = CreateCore(null, codec, new ManualDelayStrategy(), connection);
        var expectedPayload = "response"u8.ToArray();

        var sendTask = core.SendAsync(301001, "request"u8.ToArray(), false);
        var requestFrame = await connection.ReadSentFrameAsync();
        var (_, cmd, reqId) = codec.Parse(requestFrame, null);
        connection.EnqueueInbound(codec.Pack(expectedPayload, cmd, reqId, null, false));

        var response = await sendTask;

        Assert.AreEqual(301001, response.Cmd);
        Assert.AreEqual(reqId, response.ReqId);
        CollectionAssert.AreEqual(expectedPayload, response.Payload.Data.ToByteArray());
    }

    [TestMethod]
    public async Task CloseAsync_ClosesActiveConnection_Gracefully()
    {
        var connection = new FakeWebSocketConnection();
        var core = CreateCore(null, new TiebaWebSocketFrameCodec(), new ManualDelayStrategy(), connection);

        await core.ConnectAsync();
        await core.CloseAsync();

        Assert.AreEqual(1, connection.CloseCalls);
        Assert.AreEqual(WebSocketCloseStatus.NormalClosure, connection.LastCloseStatus);
    }

    [TestMethod]
    public async Task RequestFailure_Reconnects_OnNextExplicitSend()
    {
        var codec = new TiebaWebSocketFrameCodec();
        var firstConnection = new FakeWebSocketConnection();
        var secondConnection = new FakeWebSocketConnection();
        var factory = new QueueConnectionFactory(firstConnection, secondConnection);
        var core = new WebsocketCore(null, codec, new TiebaWebSocketHandshakeBuilder(), factory,
            new TiebaWebSocketOptions(new Uri("ws://unit.test"), TimeSpan.FromMinutes(5)), new ManualDelayStrategy());

        var failedSend = core.SendAsync(301001, "request"u8.ToArray(), false);
        await firstConnection.ReadSentFrameAsync();
        firstConnection.EnqueueFailure(new WebSocketException("offline"));

        await AssertConnectionLostAsync(failedSend);

        var recoveredSend = core.SendAsync(301001, "request-2"u8.ToArray(), false);
        var recoveredFrame = await secondConnection.ReadSentFrameAsync();
        var (_, recoveredCmd, recoveredReqId) = codec.Parse(recoveredFrame, null);
        secondConnection.EnqueueInbound(codec.Pack("ok"u8.ToArray(), recoveredCmd, recoveredReqId, null, false));

        var response = await recoveredSend;

        Assert.AreEqual(2, factory.CreateCalls);
        Assert.AreEqual(301001, response.Cmd);
    }

    [TestMethod]
    public async Task ListenAsync_Yields_UnmatchedPushMessages()
    {
        var codec = new TiebaWebSocketFrameCodec();
        var connection = new FakeWebSocketConnection();
        var core = CreateCore(null, codec, new ManualDelayStrategy(), connection);
        var payload = "push"u8.ToArray();

        await using var enumerator = core.ListenAsync().GetAsyncEnumerator();
        var moveNextTask = enumerator.MoveNextAsync().AsTask();
        connection.EnqueueInbound(codec.Pack(payload, 777, 0, null, false));

        Assert.IsTrue(await moveNextTask);
        Assert.AreEqual(777, enumerator.Current.Cmd);
        CollectionAssert.AreEqual(payload, enumerator.Current.Payload.Data.ToByteArray());
    }

    private static WebsocketCore CreateCore(Account? account, TiebaWebSocketFrameCodec codec,
        ManualDelayStrategy delayStrategy, FakeWebSocketConnection connection)
    {
        return new WebsocketCore(account, codec, new TiebaWebSocketHandshakeBuilder(),
            new QueueConnectionFactory(connection), new TiebaWebSocketOptions(new Uri("ws://unit.test"),
                TimeSpan.FromMilliseconds(10)), delayStrategy);
    }

    private static async Task AssertConnectionLostAsync(Task task)
    {
        try
        {
            await task;
            Assert.Fail("Expected the WebSocket request to fail with a connection-lost error.");
        }
        catch (TiebaWebSocketConnectionLostException)
        {
        }
    }

    private sealed class QueueConnectionFactory(params FakeWebSocketConnection[] connections)
        : ITiebaWebSocketConnectionFactory
    {
        private readonly Queue<FakeWebSocketConnection> _connections = new(connections);

        internal int CreateCalls { get; private set; }

        public ITiebaWebSocketConnection CreateConnection()
        {
            CreateCalls++;
            if (_connections.Count == 0)
                throw new InvalidOperationException("No fake WebSocket connections are left in the test factory.");

            return _connections.Dequeue();
        }
    }

    private sealed class ManualDelayStrategy : ITiebaWebSocketDelayStrategy
    {
        private readonly Channel<TaskCompletionSource<bool>> _delays =
            Channel.CreateUnbounded<TaskCompletionSource<bool>>();

        public async Task DelayAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            var delay = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = cancellationToken.Register(() => delay.TrySetCanceled(cancellationToken));
            await _delays.Writer.WriteAsync(delay, cancellationToken);
            await delay.Task;
        }

        internal async Task ReleaseNextAsync(CancellationToken cancellationToken = default)
        {
            var delay = await _delays.Reader.ReadAsync(cancellationToken);
            delay.TrySetResult(true);
        }
    }

    private sealed class FakeWebSocketConnection : ITiebaWebSocketConnection
    {
        private readonly Channel<ReceiveItem> _inbound = Channel.CreateUnbounded<ReceiveItem>();
        private readonly Channel<byte[]> _sent = Channel.CreateUnbounded<byte[]>();

        public int ConnectCalls { get; private set; }

        public int CloseCalls { get; private set; }

        public WebSocketCloseStatus? LastCloseStatus { get; private set; }

        public WebSocketState State { get; private set; } = WebSocketState.None;

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectCalls++;
            State = WebSocketState.Open;
            return Task.CompletedTask;
        }

        public async Task SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _sent.Writer.WriteAsync(buffer.ToArray(), cancellationToken);
        }

        public async Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken)
        {
            var item = await _inbound.Reader.ReadAsync(cancellationToken);
            return item switch
            {
                ReceiveFrame frame => frame.Buffer,
                ReceiveFailure failure => throw failure.Exception,
                ReceiveClosed => null,
                _ => throw new InvalidOperationException("Unknown fake receive item.")
            };
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CloseCalls++;
            LastCloseStatus = closeStatus;
            State = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            State = WebSocketState.Closed;
            _inbound.Writer.TryComplete();
            _sent.Writer.TryComplete();
        }

        internal void EnqueueInbound(byte[] frame) => _inbound.Writer.TryWrite(new ReceiveFrame(frame));

        internal void EnqueueFailure(Exception exception) => _inbound.Writer.TryWrite(new ReceiveFailure(exception));

        internal void EnqueueClose() => _inbound.Writer.TryWrite(new ReceiveClosed());

        internal ValueTask<byte[]> ReadSentFrameAsync(CancellationToken cancellationToken = default) =>
            _sent.Reader.ReadAsync(cancellationToken);

        private abstract record ReceiveItem;

        private sealed record ReceiveFrame(byte[] Buffer) : ReceiveItem;

        private sealed record ReceiveFailure(Exception Exception) : ReceiveItem;

        private sealed record ReceiveClosed : ReceiveItem;
    }
}
