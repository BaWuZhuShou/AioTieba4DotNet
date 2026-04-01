#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.WebSockets;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Transport;

[TestClass]
public class TiebaOperationDispatcherTests
{
    [TestMethod]
    public async Task ExecuteAsync_HttpOnlyOperation_UsesHttpOnly()
    {
        var wsCore = new RecordingWsCore();
        var dispatcher = CreateDispatcher(new RecordingHttpCore(), wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;
        var wsCalls = 0;

        var result = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                "GetTbsAsync",
                TiebaOperationCapabilities.HttpOnly(),
                (_, _) =>
                {
                    httpCalls++;
                    return Task.FromResult("http");
                },
                (_, _) =>
                {
                    wsCalls++;
                    return Task.FromResult("ws");
                }));

        Assert.AreEqual("http", result);
        Assert.AreEqual(1, httpCalls);
        Assert.AreEqual(0, wsCalls);
        Assert.AreEqual(0, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task ExecuteAsync_WsPreferredOperation_UsesWebSocket()
    {
        var wsCore = new RecordingWsCore();
        var dispatcher = CreateDispatcher(new RecordingHttpCore(), wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;
        var wsCalls = 0;

        var result = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                "GetThreadsAsync",
                TiebaOperationCapabilities.WebSocketPreferred(),
                (_, _) =>
                {
                    httpCalls++;
                    return Task.FromResult("http");
                },
                (_, _) =>
                {
                    wsCalls++;
                    return Task.FromResult("ws");
                }));

        Assert.AreEqual("ws", result);
        Assert.AreEqual(0, httpCalls);
        Assert.AreEqual(1, wsCalls);
        Assert.AreEqual(1, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task ExecuteAsync_WsUnavailable_FallsBackToHttp()
    {
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        var dispatcher = CreateDispatcher(new RecordingHttpCore(), wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;
        var wsCalls = 0;

        var result = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                "GetThreadsAsync",
                TiebaOperationCapabilities.WebSocketPreferred(),
                (_, _) =>
                {
                    httpCalls++;
                    return Task.FromResult("http");
                },
                (_, _) =>
                {
                    wsCalls++;
                    return Task.FromResult("ws");
                }));

        Assert.AreEqual("http", result);
        Assert.AreEqual(1, httpCalls);
        Assert.AreEqual(0, wsCalls);
        Assert.AreEqual(1, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task ExecuteAsync_CentralizedWsUnavailableSignal_FallsBackToHttp()
    {
        var wsCore = new RecordingWsCore
        {
            ConnectException = new TiebaWebSocketUnavailableException("offline")
        };
        var dispatcher = CreateDispatcher(new RecordingHttpCore(), wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;

        var result = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                "GetThreadsAsync",
                TiebaOperationCapabilities.WebSocketPreferred(),
                (_, _) =>
                {
                    httpCalls++;
                    return Task.FromResult("http");
                },
                (_, _) => Task.FromResult("ws")));

        Assert.AreEqual("http", result);
        Assert.AreEqual(1, httpCalls);
        Assert.AreEqual(1, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task ExecuteAsync_ForcedHttpOverride_SkipsWebSocket()
    {
        var wsCore = new RecordingWsCore();
        var dispatcher = CreateDispatcher(new RecordingHttpCore(), wsCore, TiebaTransportMode.Http);
        var httpCalls = 0;
        var wsCalls = 0;

        var result = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                "GetThreadsAsync",
                TiebaOperationCapabilities.WebSocketPreferred(),
                (_, _) =>
                {
                    httpCalls++;
                    return Task.FromResult("http");
                },
                (_, _) =>
                {
                    wsCalls++;
                    return Task.FromResult("ws");
                }));

        Assert.AreEqual("http", result);
        Assert.AreEqual(1, httpCalls);
        Assert.AreEqual(0, wsCalls);
        Assert.AreEqual(0, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task ExecuteAsync_UnsupportedOperationWithoutValidPath_ThrowsTiebaUnsupportedOperationException()
    {
        var dispatcher = CreateDispatcher(new RecordingHttpCore(), new RecordingWsCore(), TiebaTransportMode.Auto);

        var exception = await AssertThrowsAsync<TiebaUnsupportedOperationException>(() => dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                "UnsupportedOperation",
                TiebaOperationCapabilities.WebSocketPreferred())));

        StringAssert.Contains(exception.Message, "UnsupportedOperation");
    }

    [TestMethod]
    public async Task ExecuteAsync_WebSocketCancellation_DoesNotFallbackToHttp()
    {
        var wsCore = new RecordingWsCore { ConnectException = new OperationCanceledException() };
        var dispatcher = CreateDispatcher(new RecordingHttpCore(), wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;

        await AssertThrowsAsync<OperationCanceledException>(() => dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                "GetThreadsAsync",
                TiebaOperationCapabilities.WebSocketPreferred(),
                (_, _) =>
                {
                    httpCalls++;
                    return Task.FromResult("http");
                },
                (_, _) => Task.FromResult("ws"))));

        Assert.AreEqual(0, httpCalls);
        Assert.AreEqual(1, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task ExecuteAsync_WebSocketProtocolFailure_DoesNotFallbackToHttp()
    {
        var wsCore = new RecordingWsCore();
        var dispatcher = CreateDispatcher(new RecordingHttpCore(), wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;

        var exception = await AssertThrowsAsync<TieBaServerException>(() => dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<string>(
                "GetThreadsAsync",
                TiebaOperationCapabilities.WebSocketPreferred(),
                (_, _) =>
                {
                    httpCalls++;
                    return Task.FromResult("http");
                },
                (_, _) => throw new TieBaServerException(500, "bad payload"))));

        Assert.AreEqual(500, exception.Code);
        Assert.AreEqual(0, httpCalls);
        Assert.AreEqual(1, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task ThreadProtocol_PropagatesCancellationToken_ToHttpTransportExecution()
    {
        var httpCore = new RecordingHttpCore
        {
            ProtoResponse = CreateThreadsResponse().ToByteArray()
        };
        var wsCore = new RecordingWsCore();
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            wsCore);
        var protocol = new ThreadProtocol(new TiebaOperationDispatcher(session), new StubForumProtocol());
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetThreadsAsync("dotnet", 1, 30, ThreadSortType.Reply, false, cts.Token);

        Assert.AreEqual("dotnet", result.Forum.Fname);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
    }

    private static TiebaOperationDispatcher CreateDispatcher(RecordingHttpCore httpCore, RecordingWsCore wsCore,
        TiebaTransportMode transportMode)
    {
        var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = transportMode },
            httpCore,
            wsCore);

        return new TiebaOperationDispatcher(session);
    }

    private static async Task<TException> AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
    }

    private static FrsPageResIdl CreateThreadsResponse()
    {
        return new FrsPageResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new FrsPageResIdl.Types.DataRes
            {
                Forum = new FrsPageResIdl.Types.DataRes.Types.ForumInfo
                {
                    Id = 1,
                    Name = "dotnet"
                },
                Page = new Page(),
                NavTabInfo = new FrsPageResIdl.Types.DataRes.Types.NavTabInfo(),
                ForumRule = new FrsPageResIdl.Types.DataRes.Types.ForumRuleStatus()
            }
        };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public byte[] ProtoResponse { get; init; } = [];

        public CancellationToken LastAppProtoCancellationToken { get; private set; }

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            LastAppProtoCancellationToken = cancellationToken;
            return Task.FromResult(ProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class RecordingWsCore : ITiebaWsCore
    {
        public int ConnectCalls { get; private set; }

        public Exception? ConnectException { get; init; }

        public Account? Account { get; private set; }

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            if (ConnectException is not null)
                throw ConnectException;

            return Task.CompletedTask;
        }

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubForumProtocol : IForumProtocol
    {
        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default) =>
            Task.FromResult(1UL);

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default) =>
            Task.FromResult("dotnet");

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignForumsAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
