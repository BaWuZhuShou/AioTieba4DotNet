#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport.WebSockets;
using Google.Protobuf;
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
        Assert.IsGreaterThan(0, handshakeReqId);
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

    [TestMethod]
    public async Task ListenLoop_WithMalformedGzipFrame_FailsPendingRequests()
    {
        var codec = new TiebaWebSocketFrameCodec();
        var connection = new FakeWebSocketConnection();
        var core = CreateCore(null, codec, new ManualDelayStrategy(), connection);

        var pendingSend = core.SendAsync(301001, "request"u8.ToArray(), false);
        var sentFrame = await connection.ReadSentFrameAsync();
        var (_, cmd, reqId) = codec.Parse(sentFrame, null);

        connection.EnqueueInbound(CreateMalformedGzipFrame(cmd, reqId));

        await AssertConnectionLostAsync(pendingSend);
        Assert.AreEqual(WebSocketState.Closed, connection.State);
    }

    [TestMethod]
    public async Task Dispose_WithPendingRequest_FailsPendingOperations()
    {
        var codec = new TiebaWebSocketFrameCodec();
        var connection = new FakeWebSocketConnection();
        var core = CreateCore(null, codec, new ManualDelayStrategy(), connection);

        var pendingSend = core.SendAsync(301001, "request"u8.ToArray(), false);
        await connection.ReadSentFrameAsync();

        core.Dispose();

        try
        {
            await pendingSend;
            Assert.Fail("Expected disposal to interrupt the pending WebSocket request.");
        }
        catch (TiebaWebSocketConnectionLostException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        Assert.AreEqual(WebSocketState.Closed, connection.State);
    }

    [TestMethod]
    public void CombineFailures_ReturnsPrimarySecondaryOrAggregate()
    {
        var method =
            typeof(TiebaWebSocketEngine).GetMethod("CombineFailures", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("CombineFailures not found.");
        var primary = new InvalidOperationException("primary");
        var secondary = new WebSocketException("secondary");

        var whenPrimaryNull = (Exception?)method.Invoke(null, [null, secondary]);
        var whenSecondaryNull = (Exception?)method.Invoke(null, [primary, null]);
        var whenBothPresent = (Exception?)method.Invoke(null, [primary, secondary]);

        Assert.AreSame(secondary, whenPrimaryNull);
        Assert.AreSame(primary, whenSecondaryNull);
        Assert.IsInstanceOfType<AggregateException>(whenBothPresent);
        CollectionAssert.AreEquivalent(new Exception[] { primary, secondary },
            ((AggregateException)whenBothPresent!).InnerExceptions);
    }

    [TestMethod]
    public async Task MessageRouter_CoversDuplicateCancelCompletionRemovalAndFailurePaths()
    {
        var router = new TiebaWebSocketMessageRouter();
        using var cts = new CancellationTokenSource();
        var duplicate = router.RegisterPending(1);
        var toCancel = router.RegisterPending(2);
        var toComplete = router.RegisterPending(3);
        var toFail = router.RegisterPending(4);

        Assert.ThrowsExactly<InvalidOperationException>(() => router.RegisterPending(1));

        router.CancelPending(2, cts.Token);
        router.CancelPending(999, cts.Token);

        var completed = router.TryCompletePending(new WSRes
        {
            ReqId = 3, Payload = new WSRes.Types.Payload { Data = ByteString.CopyFromUtf8("ok") }
        });

        Assert.IsTrue(completed);
        Assert.IsFalse(router.TryCompletePending(new WSRes { ReqId = 0 }));
        Assert.IsFalse(router.TryCompletePending(new WSRes { ReqId = 123 }));
        Assert.IsTrue(router.TryRemovePending(1, out var removed));
        Assert.AreSame(duplicate, removed);
        Assert.IsFalse(router.TryRemovePending(1, out _));

        var failure = new InvalidOperationException("boom");
        router.FailPending(failure);

        await AssertTaskCanceledAsync(toCancel.Task);
        Assert.AreEqual("ok", (await toComplete.Task).Payload.Data.ToStringUtf8());
        await AssertThrowsAsync<InvalidOperationException>(toFail.Task, ex => Assert.AreSame(failure, ex));
    }

    [TestMethod]
    public void FrameCodec_Parse_ThrowsOnShortFrame_AndUnzipsGzipPayload()
    {
        var codec = new TiebaWebSocketFrameCodec();
        var payload = "gzip-payload"u8.ToArray();
        var compressed = Compress(payload);
        var frame = new byte[9 + compressed.Length];
        frame[0] = 0x40;
        Buffer.BlockCopy(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(1234)), 0, frame, 1, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(5678)), 0, frame, 5, 4);
        compressed.CopyTo(frame.AsSpan(9));

        var parsed = codec.Parse(frame, null);

        CollectionAssert.AreEqual(payload, parsed.Data);
        Assert.AreEqual(1234, parsed.Cmd);
        Assert.AreEqual(5678, parsed.ReqId);
        Assert.ThrowsExactly<TiebaProtocolException>(() => codec.Parse([1, 2, 3], null));
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

    private static async Task AssertTaskCanceledAsync(Task task)
    {
        try
        {
            await task;
            Assert.Fail("Expected the task to be canceled.");
        }
        catch (TaskCanceledException)
        {
        }
    }

    private static async Task AssertThrowsAsync<TException>(Task task, Action<TException> assert)
        where TException : Exception
    {
        try
        {
            await task;
            Assert.Fail($"Expected {typeof(TException).Name}.");
        }
        catch (TException ex)
        {
            assert(ex);
        }
    }

    private static byte[] Compress(byte[] payload)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, true))
        {
            gzip.Write(payload, 0, payload.Length);
        }

        return output.ToArray();
    }

    private static byte[] CreateMalformedGzipFrame(int cmd, int reqId)
    {
        byte[] invalidPayload = [0x1F, 0x8B, 0x00, 0x00];
        var frame = new byte[9 + invalidPayload.Length];
        frame[0] = 0x40;
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1, 4), cmd);
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(5, 4), reqId);
        invalidPayload.CopyTo(frame.AsSpan(9));
        return frame;
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

        internal void EnqueueInbound(byte[] frame)
        {
            _inbound.Writer.TryWrite(new ReceiveFrame(frame));
        }

        internal void EnqueueFailure(Exception exception)
        {
            _inbound.Writer.TryWrite(new ReceiveFailure(exception));
        }

        internal void EnqueueClose()
        {
            _inbound.Writer.TryWrite(new ReceiveClosed());
        }

        internal ValueTask<byte[]> ReadSentFrameAsync(CancellationToken cancellationToken = default)
        {
            return _sent.Reader.ReadAsync(cancellationToken);
        }

        private abstract record ReceiveItem;

        private sealed record ReceiveFrame(byte[] Buffer) : ReceiveItem;

        private sealed record ReceiveFailure(Exception Exception) : ReceiveItem;

        private sealed record ReceiveClosed : ReceiveItem;
    }
}
