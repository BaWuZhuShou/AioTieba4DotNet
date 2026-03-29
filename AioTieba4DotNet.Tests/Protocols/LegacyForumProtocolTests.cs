#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public class LegacyForumProtocolTests
{
    private const string SafeForumName = "lol欧服吧";
    private const ulong SafeForumId = 7356044;
    private static readonly string ValidBduss = new('b', 192);
    private static readonly string ValidStoken = new('s', 64);

    [TestMethod]
    public async Task GetFidAsync_ReusesInjectedCacheAcrossRepeatedCalls()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = $"{{\"no\":0,\"error\":\"\",\"data\":{{\"fid\":{SafeForumId}}}}}"
        };
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        var first = await protocol.GetFidAsync(SafeForumName);
        var second = await protocol.GetFidAsync(SafeForumName);

        Assert.AreEqual(SafeForumId, first);
        Assert.AreEqual(SafeForumId, second);
        Assert.AreEqual(1, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetDetailAsync_PrimesNameCacheForGetFnameAsync()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateForumDetailResponse(SafeForumId, SafeForumName).ToByteArray()
        };
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        var detail = await protocol.GetDetailAsync(SafeForumId);
        var fname = await protocol.GetFnameAsync(SafeForumId);

        Assert.AreEqual(SafeForumName, detail.Fname);
        Assert.AreEqual(SafeForumName, fname);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
    }

    [TestMethod]
    public async Task GetForumAsync_PrimesFidCacheForGetFidAsync()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = CreateForumResponseJson(SafeForumId, SafeForumName)
        };
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        var forum = await protocol.GetForumAsync(SafeForumName);
        var fid = await protocol.GetFidAsync(SafeForumName);

        Assert.AreEqual((long)SafeForumId, forum.Fid);
        Assert.AreEqual(SafeForumId, fid);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetForumAsync_PropagatesCancellationToken_ToHttpTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = CreateForumResponseJson(SafeForumId, SafeForumName)
        };
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());
        using var cts = new CancellationTokenSource();

        var forum = await protocol.GetForumAsync(SafeForumName, cts.Token);

        Assert.AreEqual(SafeForumName, forum.Fname);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    [TestMethod]
    public async Task SignAsync_WithoutCredentials_FailsBeforeForumLookup()
    {
        var httpCore = new RecordingHttpCore();
        using var session = new TiebaClientSession(
            new global::AioTieba4DotNet.TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());
        var protocol = new LegacyForumProtocol(new LegacyTransportContext(session), new ForumInfoCache());

        try
        {
            await protocol.SignAsync(SafeForumName);
            Assert.Fail("Expected TiebaAuthenticationException was not thrown.");
        }
        catch (TiebaAuthenticationException)
        {
        }

        Assert.AreEqual(0, httpCore.SendWebGetCalls);
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task SignAsync_PropagatesCancellationToken_ToMutationTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":""}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var cache = new ForumInfoCache();
        cache.SetForumName(SafeForumId, SafeForumName);
        var protocol = new LegacyForumProtocol(new LegacyTransportContext(session), cache);
        using var cts = new CancellationTokenSource();

        var result = await protocol.SignAsync(SafeForumName, cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    private static LegacyForumProtocol CreateProtocol(RecordingHttpCore httpCore, ForumInfoCache cache)
    {
        return new LegacyForumProtocol(
            new LegacyTransportContext(httpCore, new StubWsCore(), TiebaTransportMode.Http),
            cache);
    }

    private static TiebaClientSession CreateAuthenticatedSession(RecordingHttpCore httpCore,
        Func<CancellationToken, Task<string>> loadTbsAsync)
    {
        var options = new global::AioTieba4DotNet.TiebaOptions();
        options.Bduss = ValidBduss;
        options.Stoken = ValidStoken;
        options.TransportMode = TiebaTransportMode.Http;

        return new TiebaClientSession(
            options,
            httpCore,
            new StubWsCore(),
            loadTbsAsync);
    }

    private static string CreateForumResponseJson(ulong fid, string fname)
    {
        return $"{{\"error_code\":0,\"error_msg\":\"\",\"forum\":{{\"id\":{fid},\"name\":\"{fname}\",\"first_class\":\"游戏\",\"second_class\":\"英雄联盟\",\"avatar\":\"avatar\",\"slogan\":\"safe forum\",\"member_num\":1,\"post_num\":2,\"thread_num\":3,\"managers\":[]}}}}";
    }

    private static GetForumDetailResIdl CreateForumDetailResponse(ulong fid, string fname)
    {
        return new GetForumDetailResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new GetForumDetailResIdl.Types.DataRes
            {
                ForumInfo = new GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
                {
                    ForumId = fid,
                    ForumName = fname,
                    Lv1Name = "游戏",
                    Avatar = "avatar",
                    AvatarOrigin = "avatar-origin",
                    Slogan = "safe forum",
                    MemberCount = 1,
                    ThreadCount = 2
                },
                ElectionTab = new GetForumDetailResIdl.Types.DataRes.Types.ManagerElectionTab
                {
                    NewStrategyText = "已有吧主"
                }
            }
        };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string WebGetResponse { get; init; } = "{}";
        public string AppFormResponse { get; init; } = "{}";
        public byte[] AppProtoResponse { get; init; } = [];

        public int SendWebGetCalls { get; private set; }
        public int SendAppFormCalls { get; private set; }
        public int SendAppProtoCalls { get; private set; }
        public int SendWebFormCalls { get; private set; }
        public CancellationToken LastAppFormCancellationToken { get; private set; }

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            SendAppFormCalls++;
            LastAppFormCancellationToken = cancellationToken;
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            SendAppProtoCalls++;
            return Task.FromResult(AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            SendWebGetCalls++;
            return Task.FromResult(WebGetResponse);
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            SendWebFormCalls++;
            return Task.FromResult("{}");
        }
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
}
