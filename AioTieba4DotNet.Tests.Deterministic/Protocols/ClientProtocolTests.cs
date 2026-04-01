#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public sealed class ClientProtocolTests
{
    [TestMethod]
    public async Task InitWebSocketAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        using var session = CreateGuestSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<TiebaAuthenticationException>(() => protocol.InitWebSocketAsync());

        Assert.AreEqual(0, httpCore.SendCalls);
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task InitWebSocketAsync_ConnectsBoundWebSocket_AndPropagatesCancellationToken()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        using var session = CreateAuthenticatedSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.WebSocketState);

        await protocol.InitWebSocketAsync(cts.Token);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(cts.Token, wsCore.LastConnectCancellationToken);
        Assert.AreEqual(TiebaSessionResourceState.Ready, session.CurrentState.WebSocketState);
    }

    [TestMethod]
    public async Task InitWebSocketAsync_VisibleWarmupState_IsObservableDuringConnect()
    {
        var connectStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseConnect = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore
        {
            BeforeConnectAsync = async cancellationToken =>
            {
                connectStarted.TrySetResult(true);
                await releaseConnect.Task.WaitAsync(cancellationToken);
            }
        };
        using var session = CreateAuthenticatedSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);

        var warmupTask = protocol.InitWebSocketAsync();
        await connectStarted.Task;

        Assert.AreEqual(TiebaSessionResourceState.Initializing, session.CurrentState.WebSocketState);

        releaseConnect.SetResult(true);
        await warmupTask;

        Assert.AreEqual(TiebaSessionResourceState.Ready, session.CurrentState.WebSocketState);
    }

    [TestMethod]
    public async Task InitZIdAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        using var session = CreateGuestSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<TiebaAuthenticationException>(() => protocol.InitZIdAsync());

        Assert.AreEqual(0, httpCore.SendCalls);
        Assert.AreEqual(0, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task InitZIdAsync_UpdatesVisibleSessionState_AndPropagatesCancellationToken()
    {
        var sendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var httpCore = new RecordingHttpCore
        {
            InitZIdToken = "zid-123",
            BeforeSendAsync = async cancellationToken =>
            {
                sendStarted.TrySetResult(true);
                await releaseSend.Task.WaitAsync(cancellationToken);
            }
        };
        var wsCore = new RecordingWsCore();
        using var session = CreateAuthenticatedSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.ZIdState);

        var resultTask = protocol.InitZIdAsync(cts.Token);
        await sendStarted.Task;

        Assert.AreEqual(TiebaSessionResourceState.Initializing, session.CurrentState.ZIdState);

        releaseSend.SetResult(true);
        var result = await resultTask;

        Assert.AreEqual("zid-123", result);
        Assert.AreEqual(TiebaSessionResourceState.Ready, session.CurrentState.ZIdState);
        Assert.AreEqual("zid-123", session.CurrentState.ZId);
        Assert.AreEqual(cts.Token, httpCore.LastSendCancellationToken);
    }

    [TestMethod]
    public async Task SyncAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        using var session = CreateGuestSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<TiebaAuthenticationException>(() => protocol.SyncAsync());

        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, wsCore.ConnectCalls);
    }

    [TestMethod]
    public async Task SyncAsync_UpdatesVisibleSessionState_AndPropagatesCancellationToken()
    {
        var sendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"client":{"client_id":"client-1"},"wl_config":{"sample_id":"sample-1"}}
                              """,
            BeforeSendAppFormAsync = async cancellationToken =>
            {
                sendStarted.TrySetResult(true);
                await releaseSend.Task.WaitAsync(cancellationToken);
            }
        };
        var wsCore = new RecordingWsCore();
        using var session = CreateAuthenticatedSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.ClientState);

        var resultTask = protocol.SyncAsync(cts.Token);
        await sendStarted.Task;

        Assert.AreEqual(TiebaSessionResourceState.Initializing, session.CurrentState.ClientState);

        releaseSend.SetResult(true);
        var result = await resultTask;

        Assert.AreEqual(("client-1", "sample-1"), result);
        Assert.AreEqual(TiebaSessionResourceState.Ready, session.CurrentState.ClientState);
        Assert.AreEqual("client-1", session.CurrentState.ClientId);
        Assert.AreEqual("sample-1", session.CurrentState.SampleId);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    [TestMethod]
    public async Task InitZIdAsync_FailedRequest_DoesNotMutateSessionState()
    {
        var httpCore = new RecordingHttpCore { InitZIdToken = string.Empty };
        var wsCore = new RecordingWsCore();
        using var session = CreateAuthenticatedSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<TiebaConfigurationException>(() => protocol.InitZIdAsync());

        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.ZIdState);
        Assert.IsNull(session.CurrentState.ZId);
    }

    [TestMethod]
    public async Task SyncAsync_FailedRequest_DoesNotMutateSessionState()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"client":{"client_id":"client-1"},"wl_config":{"sample_id":""}}
                              """
        };
        var wsCore = new RecordingWsCore();
        using var session = CreateAuthenticatedSession(httpCore, wsCore);
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<TiebaConfigurationException>(() => protocol.SyncAsync());

        Assert.AreEqual(TiebaSessionResourceState.Pending, session.CurrentState.ClientState);
        Assert.IsNull(session.CurrentState.ClientId);
        Assert.IsNull(session.CurrentState.SampleId);
    }

    private static ClientProtocol CreateProtocol(TiebaClientSession session) =>
        new(new TiebaOperationDispatcher(session));

    private static TiebaClientSession CreateAuthenticatedSession(RecordingHttpCore httpCore, RecordingWsCore wsCore)
    {
        return new TiebaClientSession(
            new TiebaOptions
            {
                Bduss = new string('b', 192),
                Stoken = new string('s', 64),
                TransportMode = TiebaTransportMode.Http
            },
            httpCore,
            wsCore,
            _ => Task.FromResult("tbs-123"));
    }

    private static TiebaClientSession CreateGuestSession(RecordingHttpCore httpCore, RecordingWsCore wsCore)
    {
        return new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            wsCore,
            _ => Task.FromResult("tbs-123"));
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

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string InitZIdToken { get; init; } = "zid-123";

        public string AppFormResponse { get; init; } = "{}";

        public Func<CancellationToken, Task>? BeforeSendAsync { get; init; }

        public Func<CancellationToken, Task>? BeforeSendAppFormAsync { get; init; }

        public int SendCalls { get; private set; }

        public int SendAppFormCalls { get; private set; }

        public CancellationToken LastSendCancellationToken { get; private set; }

        public CancellationToken LastAppFormCancellationToken { get; private set; }

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public async Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            SendCalls++;
            LastSendCancellationToken = cancellationToken;
            _ = requestFactory();
            if (BeforeSendAsync is not null) await BeforeSendAsync(cancellationToken);
            return CreateInitZIdResponse(Account ?? throw new InvalidOperationException(
                "An authenticated account is required to create the InitZId response."), InitZIdToken);
        }

        public async Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            SendAppFormCalls++;
            LastAppFormCancellationToken = cancellationToken;
            if (BeforeSendAppFormAsync is not null) await BeforeSendAppFormAsync(cancellationToken);
            return AppFormResponse;
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        private static string CreateInitZIdResponse(Account account, string token)
        {
            var xyus = global::AioTieba4DotNet.Api.InitZId.InitZId.GetMd5Hash(account.AndroidId + account.Uuid) + "|0";
            var xyusMd5Str = global::AioTieba4DotNet.Api.InitZId.InitZId.GetMd5Hash(xyus).ToLowerInvariant();
            var responseSecKey = Enumerable.Repeat((byte)0x42, 16).ToArray();
            var responsePayload = Encoding.UTF8.GetBytes($"{{\"token\":\"{token}\"}}");
            var paddedPayload = Utils.ApplyPkcs7Padding(responsePayload, 16);
            var payloadWithTrailer = new byte[paddedPayload.Length + 16];
            Buffer.BlockCopy(paddedPayload, 0, payloadWithTrailer, 0, paddedPayload.Length);

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = responseSecKey;
            aes.IV = new byte[16];
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var encryptedPayload = encryptor.TransformFinalBlock(payloadWithTrailer, 0, payloadWithTrailer.Length);

            var rc4 = new Rc4(Encoding.UTF8.GetBytes(xyusMd5Str));
            var encryptedSkey = rc4.Crypt(responseSecKey);

            return $"{{\"skey\":\"{Convert.ToBase64String(encryptedSkey)}\",\"data\":\"{Convert.ToBase64String(encryptedPayload)}\"}}";
        }
    }

    private sealed class RecordingWsCore : ITiebaWsCore
    {
        public Func<CancellationToken, Task>? BeforeConnectAsync { get; init; }

        public Exception? ConnectException { get; init; }

        public int ConnectCalls { get; private set; }

        public CancellationToken LastConnectCancellationToken { get; private set; }

        public Account? Account { get; private set; }

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            LastConnectCancellationToken = cancellationToken;
            if (BeforeConnectAsync is not null) await BeforeConnectAsync(cancellationToken);
            if (ConnectException is not null) throw ConnectException;
        }

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
