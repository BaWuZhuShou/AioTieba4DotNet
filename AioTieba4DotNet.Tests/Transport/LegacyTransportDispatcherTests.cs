#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.WebSockets;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Transport;

[TestClass]
public class LegacyTransportDispatcherTests
{
    [TestMethod]
    public async Task DispatchAsync_HttpOnlyOperation_UsesHttpOnly()
    {
        var wsCore = new RecordingWsCore();
        var dispatcher = new LegacyTransportDispatcher(wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;
        var wsCalls = 0;

        var result = await dispatcher.DispatchAsync(
            LegacyTransportOperation.GetTbs,
            _ =>
            {
                httpCalls++;
                return Task.FromResult("http");
            },
            _ =>
            {
                wsCalls++;
                return Task.FromResult("ws");
            });

        Assert.AreEqual("http", result);
        Assert.AreEqual(1, httpCalls);
        Assert.AreEqual(0, wsCalls);
        Assert.AreEqual(0, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task DispatchAsync_WsSupportedOperation_UsesWebSocket()
    {
        var wsCore = new RecordingWsCore();
        var dispatcher = new LegacyTransportDispatcher(wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;
        var wsCalls = 0;

        var result = await dispatcher.DispatchAsync(
            LegacyTransportOperation.GetThreads,
            _ =>
            {
                httpCalls++;
                return Task.FromResult("http");
            },
            _ =>
            {
                wsCalls++;
                return Task.FromResult("ws");
            });

        Assert.AreEqual("ws", result);
        Assert.AreEqual(0, httpCalls);
        Assert.AreEqual(1, wsCalls);
        Assert.AreEqual(1, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task DispatchAsync_WsUnavailable_FallsBackToHttp()
    {
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        var dispatcher = new LegacyTransportDispatcher(wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;
        var wsCalls = 0;

        var result = await dispatcher.DispatchAsync(
            LegacyTransportOperation.GetThreads,
            _ =>
            {
                httpCalls++;
                return Task.FromResult("http");
            },
            _ =>
            {
                wsCalls++;
                return Task.FromResult("ws");
            });

        Assert.AreEqual("http", result);
        Assert.AreEqual(1, httpCalls);
        Assert.AreEqual(0, wsCalls);
        Assert.AreEqual(1, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task DispatchAsync_CentralizedWsUnavailableSignal_FallsBackToHttp()
    {
        var wsCore = new RecordingWsCore
        {
            ConnectException = new TiebaWebSocketUnavailableException("offline")
        };
        var dispatcher = new LegacyTransportDispatcher(wsCore, TiebaTransportMode.Auto);
        var httpCalls = 0;

        var result = await dispatcher.DispatchAsync(
            LegacyTransportOperation.GetThreads,
            _ =>
            {
                httpCalls++;
                return Task.FromResult("http");
            },
            _ => Task.FromResult("ws"));

        Assert.AreEqual("http", result);
        Assert.AreEqual(1, httpCalls);
        Assert.AreEqual(1, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task DispatchAsync_ForcedHttpOverride_SkipsWebSocket()
    {
        var wsCore = new RecordingWsCore();
        var dispatcher = new LegacyTransportDispatcher(wsCore, TiebaTransportMode.Http);
        var httpCalls = 0;
        var wsCalls = 0;

        var result = await dispatcher.DispatchAsync(
            LegacyTransportOperation.GetThreads,
            _ =>
            {
                httpCalls++;
                return Task.FromResult("http");
            },
            _ =>
            {
                wsCalls++;
                return Task.FromResult("ws");
            });

        Assert.AreEqual("http", result);
        Assert.AreEqual(1, httpCalls);
        Assert.AreEqual(0, wsCalls);
        Assert.AreEqual(0, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task ThreadProtocol_PropagatesCancellationToken_ToHttpTransportExecution()
    {
        var httpCore = new RecordingHttpCore
        {
            ProtoResponse = CreateThreadsResponse().ToByteArray()
        };
        var wsCore = new RecordingWsCore();
        var transport = new LegacyTransportContext(httpCore, wsCore, TiebaTransportMode.Http);
        var protocol = new LegacyThreadProtocol(transport, new StubForumProtocol());
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetThreadsAsync("dotnet", 1, 30, ThreadSortType.Reply, false, cts.Token);

        Assert.AreEqual("dotnet", result.Forum.Fname);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
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
            if (ConnectException != null)
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

        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
