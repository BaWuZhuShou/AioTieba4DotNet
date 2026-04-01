#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Transport.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Session;

[TestClass]
public sealed class TiebaClientSessionTests
{
    private static readonly string ValidBduss = new('b', 192);
    private static readonly string ValidStoken = new('s', 64);

    [TestMethod]
    public async Task AuthenticatedWriteOperation_WithoutCredentials_FailsLocally()
    {
        using var client = new TiebaClient(new TiebaOptions { TransportMode = TiebaTransportMode.Http });

        try
        {
            await client.Users.FollowAsync("portrait");
            Assert.Fail("Expected TiebaAuthenticationException was not thrown.");
        }
        catch (TiebaAuthenticationException)
        {
        }
    }

    [TestMethod]
    public async Task GuestUserPostsOperation_WithoutCredentials_FailsLocally()
    {
        using var client = new TiebaClient(new TiebaOptions { TransportMode = TiebaTransportMode.Http });

        try
        {
            await client.Users.GetPostsAsync(1);
            Assert.Fail("Expected TiebaAuthenticationException was not thrown.");
        }
        catch (TiebaAuthenticationException)
        {
        }
    }

    [TestMethod]
    public async Task GuestUserThreadsOperation_WithoutCredentials_FailsLocally()
    {
        using var client = new TiebaClient(new TiebaOptions { TransportMode = TiebaTransportMode.Http });

        try
        {
            await client.Users.GetThreadsAsync(1);
            Assert.Fail("Expected TiebaAuthenticationException was not thrown.");
        }
        catch (TiebaAuthenticationException)
        {
        }
    }

    [TestMethod]
    public async Task GetTbsAsync_AuthenticatedSession_LoadsAndStoresTbs()
    {
        var httpCore = new StubHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, new StubWsCore(), _ => Task.FromResult("tbs-123"));

        Assert.AreEqual(TiebaSessionKind.Authenticated, session.CurrentState.Kind);
        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.TbsState);

        var tbs = await session.GetTbsAsync();

        Assert.AreEqual("tbs-123", tbs);
        Assert.AreEqual(TiebaSessionKind.Authenticated, session.CurrentState.Kind);
        Assert.AreEqual(TiebaSessionResourceState.Ready, session.CurrentState.TbsState);
        Assert.AreEqual("tbs-123", session.CurrentState.Tbs);
        Assert.AreEqual("tbs-123", httpCore.Account?.Tbs);
    }

    [TestMethod]
    public async Task RefreshTbsAsync_ReplacesCachedTbs_AndKeepsReadyStateVisible()
    {
        var httpCore = new StubHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, new StubWsCore(), _ => Task.FromResult("tbs-456"));
        session.UpdateTbs("tbs-123");

        var refreshedTbs = await session.RefreshTbsAsync(nameof(RefreshTbsAsync_ReplacesCachedTbs_AndKeepsReadyStateVisible));

        Assert.AreEqual("tbs-456", refreshedTbs);
        Assert.AreEqual(TiebaSessionResourceState.Ready, session.CurrentState.TbsState);
        Assert.AreEqual("tbs-456", session.CurrentState.Tbs);
        Assert.AreEqual("tbs-456", httpCore.Account?.Tbs);
    }

    [TestMethod]
    public async Task GetTbsAsync_Cancellation_DoesNotPersistTbs()
    {
        var httpCore = new StubHttpCore();
        var loaderStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var session = CreateAuthenticatedSession(httpCore, new StubWsCore(), async cancellationToken =>
        {
            loaderStarted.SetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return "unreachable";
        });
        using var cancellationSource = new CancellationTokenSource();

        var tbsTask = session.GetTbsAsync(cancellationSource.Token);
        await loaderStarted.Task;
        Assert.AreEqual(TiebaSessionResourceState.Initializing, session.CurrentState.TbsState);
        cancellationSource.Cancel();

        try
        {
            await tbsTask;
            Assert.Fail("Expected OperationCanceledException was not thrown.");
        }
        catch (OperationCanceledException)
        {
        }

        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.TbsState);
        Assert.IsNull(session.CurrentState.Tbs);
        Assert.IsNull(httpCore.Account?.Tbs);
    }

    [TestMethod]
    public async Task SyncAsync_UpdatesVisibleSessionState()
    {
        var httpCore = new StubHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"client":{"client_id":"client-1"},"wl_config":{"sample_id":"sample-1"}}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, new StubWsCore(), _ => Task.FromResult("tbs-123"));
        var protocol = new ClientProtocol(new TiebaOperationDispatcher(session));

        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.ClientState);

        var result = await protocol.SyncAsync();

        Assert.AreEqual("client-1", result.ClientId);
        Assert.AreEqual("sample-1", result.SampleId);
        Assert.AreEqual(TiebaSessionKind.Authenticated, session.CurrentState.Kind);
        Assert.AreEqual(TiebaSessionResourceState.Ready, session.CurrentState.ClientState);
        Assert.AreEqual("client-1", session.CurrentState.ClientId);
        Assert.AreEqual("sample-1", session.CurrentState.SampleId);
    }

    [TestMethod]
    public void GuestSession_UsesExplicitUnavailableLifecycleState()
    {
        using var session = new TiebaClientSession(new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            new StubHttpCore(),
            new StubWsCore());

        Assert.AreEqual(TiebaSessionKind.Guest, session.CurrentState.Kind);
        Assert.AreEqual(TiebaSessionResourceState.Unavailable, session.CurrentState.TbsState);
        Assert.AreEqual(TiebaSessionResourceState.Unavailable, session.CurrentState.ZIdState);
        Assert.AreEqual(TiebaSessionResourceState.Unavailable, session.CurrentState.ClientState);
        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.WebSocketState);
    }

    [TestMethod]
    public void Dispose_WithOwnedHttpCore_DisposesHttpClient()
    {
        var handler = new TrackingHandler();
        var httpCore = new HttpCore(new TiebaOptions(), new HttpClient(handler),
            ownsHttpClient: true);
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());

        session.Dispose();

        Assert.IsTrue(handler.IsDisposed);
    }

    [TestMethod]
    public void Dispose_WithExternallyProvidedHttpClient_DoesNotDisposeHttpClient()
    {
        var handler = new TrackingHandler();
        var httpClient = new HttpClient(handler);
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpClient);

        session.Dispose();

        Assert.IsFalse(handler.IsDisposed);
        httpClient.Dispose();
        Assert.IsTrue(handler.IsDisposed);
    }

    [TestMethod]
    public void UpdateMembers_ValidateInputsAndPersistStateToAccount()
    {
        var httpCore = new StubHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, new StubWsCore(), _ => Task.FromResult("tbs-123"));

        session.UpdateTbs("tbs-updated");
        session.UpdateClientIdentifiers("client-2", "sample-2");
        session.UpdateZId("zid-2");

        Assert.AreEqual("tbs-updated", session.CurrentState.Tbs);
        Assert.AreEqual("client-2", session.CurrentState.ClientId);
        Assert.AreEqual("sample-2", session.CurrentState.SampleId);
        Assert.AreEqual("zid-2", session.CurrentState.ZId);
        Assert.AreEqual("tbs-updated", httpCore.Account?.Tbs);
        Assert.AreEqual("client-2", httpCore.Account?.ClientId);
        Assert.AreEqual("sample-2", httpCore.Account?.SampleId);
        Assert.AreEqual("zid-2", httpCore.Account?.ZId);

        AssertThrows<TiebaConfigurationException>(() => session.UpdateTbs(" "));
        AssertThrows<TiebaConfigurationException>(() => session.UpdateClientIdentifiers(" ", "sample"));
        AssertThrows<TiebaConfigurationException>(() => session.UpdateClientIdentifiers("client", " "));
        AssertThrows<TiebaConfigurationException>(() => session.UpdateZId(" "));
    }

    [TestMethod]
    public void AuthenticatedSession_CreatesAccount_WhenStokenIsMissing()
    {
        var httpCore = new StubHttpCore();
        var wsCore = new StubWsCore();
        using var session = new TiebaClientSession(
            new TiebaOptions
            {
                Bduss = ValidBduss,
                TransportMode = TiebaTransportMode.Http
            },
            httpCore,
            wsCore);

        Assert.IsTrue(session.IsAuthenticated);
        Assert.IsNotNull(httpCore.Account);
        Assert.AreEqual(string.Empty, httpCore.Account!.Stoken);
        Assert.IsNotNull(wsCore.Account);
    }

    [TestMethod]
    public void GuestSession_UpdateMembers_FailWithAuthenticationErrors()
    {
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            new StubHttpCore(),
            new StubWsCore());

        AssertThrows<TiebaAuthenticationException>(() => session.UpdateTbs("tbs"));
        AssertThrows<TiebaAuthenticationException>(() => session.UpdateClientIdentifiers("client", "sample"));
        AssertThrows<TiebaAuthenticationException>(() => session.UpdateZId("zid"));
    }

    private static TException AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
    }

    private static TiebaClientSession CreateAuthenticatedSession(StubHttpCore httpCore, StubWsCore wsCore,
        Func<CancellationToken, Task<string>> loadTbsAsync)
    {
        return new TiebaClientSession(
            new TiebaOptions
            {
                Bduss = ValidBduss,
                Stoken = ValidStoken,
                TransportMode = TiebaTransportMode.Http
            },
            httpCore,
            wsCore,
            loadTbsAsync);
    }

    private sealed class StubHttpCore : ITiebaHttpCore
    {
        public string AppFormResponse { get; init; } = "{}";

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => Task.FromResult(AppFormResponse);

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubWsCore : ITiebaWsCore
    {
        public Account? Account { get; private set; }

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TrackingHandler : HttpMessageHandler
    {
        internal bool IsDisposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
