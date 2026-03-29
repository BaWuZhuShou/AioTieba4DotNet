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
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public class LegacyUserProtocolTests
{
    private static readonly string ValidBduss = new('b', 192);
    private static readonly string ValidStoken = new('s', 64);

    [TestMethod]
    public async Task GetFansAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.GetFansAsync(1, 1));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task GetAtsAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.GetAtsAsync(1));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task GetRepliesAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.GetRepliesAsync(1));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task GetBlacklistAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.GetBlacklistAsync());

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task GetSelfInfoAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.GetSelfInfoAsync());

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task SetBlacklistAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.SetBlacklistAsync(1, BlacklistType.All));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task RemoveFanAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.RemoveFanAsync(1));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task RemoveFanAsync_InvalidUserId_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<ArgumentOutOfRangeException>(async () => await protocol.RemoveFanAsync(0));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task SetBlacklistAsync_InvalidUserId_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<ArgumentOutOfRangeException>(async () => await protocol.SetBlacklistAsync(0, BlacklistType.All));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task FollowAsync_NonexistentPortrait_SurfacesStableServerError()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":340011,"error_msg":"user not found"}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var exception = await AssertThrowsAsync<TieBaServerException>(async () => await protocol.FollowAsync("tb.1.nonexistent"));

        Assert.AreEqual(340011, exception.Code);
    }

    [TestMethod]
    public async Task UnfollowAsync_UnauthorizedTarget_SurfacesStableServerError()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":3254004,"error_msg":"no permission"}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var exception = await AssertThrowsAsync<TieBaServerException>(async () => await protocol.UnfollowAsync("tb.1.safe"));

        Assert.AreEqual(3254004, exception.Code);
    }

    [TestMethod]
    public async Task SetBlacklistAsync_UnauthorizedTarget_SurfacesStableServerError()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = new SetUserBlackResIdl
            {
                Error = new Error { Errorno = 3254004, Errmsg = "no permission" }
            }.ToByteArray()
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var exception = await AssertThrowsAsync<TieBaServerException>(async () =>
            await protocol.SetBlacklistAsync(12345, BlacklistType.All));

        Assert.AreEqual(3254004, exception.Code);
    }

    [TestMethod]
    public async Task GetFansAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user_list":[],"page":{"current_page":1,"has_more":0,"has_prev":0}}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetFansAsync(1, 1, cts.Token);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    [TestMethod]
    public async Task GetRepliesAsync_ParsesProtobufResponse_AndPropagatesCancellationToken()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = new ReplyMeResIdl
            {
                Error = new Error { Errorno = 0 },
                Data = new ReplyMeResIdl.Types.DataRes
                {
                    Page = new Page { CurrentPage = 1, HasMore = 0, HasPrev = 0 },
                    ReplyList =
                    {
                        new ReplyMeResIdl.Types.DataRes.Types.ReplyList
                        {
                            ThreadId = 1001,
                            PostId = 1002,
                            QuotePid = 1003,
                            Fname = "lol欧服",
                            Content = "reply body",
                            IsFloor = 1,
                            Time = 1234567890,
                            Replyer = new User { Id = 1, Name = "replyer", NameShow = "Replyer" },
                            QuoteUser = new User { Id = 2, Name = "quoted", NameShow = "Quoted" },
                            ThreadAuthorUser = new User { Id = 3, Name = "author", NameShow = "Author" }
                        }
                    }
                }
            }.ToByteArray()
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetRepliesAsync(1, cts.Token);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("reply body", result[0].Content);
        Assert.AreEqual("Replyer", result[0].Replyer?.ShowName);
        Assert.AreEqual(1003, result[0].QuotePostId);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
    }

    [TestMethod]
    public async Task GetSelfInfoAsync_ComposesInitNicknameAndMoIndex()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user_info":{"user_name":"self-user","name_show":"Old Name","tieba_uid":778899}}
                              """,
            WebGetResponse = """
                             {"no":0,"error":"","data":{"id":123,"portrait":"tb.1.safe","name":"self-user","user_sex":1,"post_num":12,"fans_num":34,"concern_num":56,"like_forum_num":78,"intro":"hello","vipInfo":{"v_status":3}}}
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var result = await protocol.GetSelfInfoAsync();

        Assert.AreEqual(123, result.UserId);
        Assert.AreEqual("self-user", result.UserName);
        Assert.AreEqual("Old Name", result.NickNameOld);
        Assert.AreEqual(778899, result.TiebaUid);
        Assert.AreEqual(34, result.FanNum);
        Assert.IsTrue(result.IsVip);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
        Assert.AreEqual(1, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetBasicInfoAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = new GetUserInfoResIdl
            {
                Error = new Error { Errorno = 0 },
                Data = new GetUserInfoResIdl.Types.DataRes
                {
                    User = new User
                    {
                        Id = 1,
                        Portrait = "tb.1.safe",
                        Name = "safe-user",
                        NameShow = "Safe User",
                        VipInfo = new User.Types.UserVipInfo { VStatus = 0 },
                        NewGodData = new User.Types.NewGodInfo { Status = 0 }
                    }
                }
            }.ToByteArray()
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetBasicInfoAsync(1, cts.Token);

        Assert.AreEqual("safe-user", result.UserName);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
    }

    [TestMethod]
    public async Task GetProfileAsync_Int_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateProfileResponse().ToByteArray()
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetProfileAsync(1, cts.Token);

        Assert.AreEqual("safe-user", result.UserName);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
    }

    [TestMethod]
    public async Task GetProfileAsync_String_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateProfileResponse().ToByteArray()
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetProfileAsync("tb.1.safe", cts.Token);

        Assert.AreEqual("safe-user", result.UserName);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
    }

    [TestMethod]
    public async Task BlockAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = SuccessResponse
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.BlockAsync(1, "tb.1.safe", 1, "reason", cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    [TestMethod]
    public async Task FollowAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = SuccessResponse
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.FollowAsync("tb.1.safe", cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    [TestMethod]
    public async Task UnfollowAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = SuccessResponse
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.UnfollowAsync("tb.1.safe", cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    [TestMethod]
    public async Task GetFollowsAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","follow_list":[],"pn":1,"total_follow_num":0,"has_more":0}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetFollowsAsync(1, 1, cts.Token);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    [TestMethod]
    public async Task GetPanelInfoAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"no":0,"error":"","data":{"portrait":"tb.1.safe","name":"safe-user","show_nickname":"Safe User","name_show":"Safe User","gender":"male","tb_age":"2","post_num":"1","followed_count":"2","vipInfo":{"v_status":3}}}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetPanelInfoAsync("tb.1.safe", cts.Token);

        Assert.AreEqual("safe-user", result.UserName);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    [TestMethod]
    public async Task GetUserInfoJsonAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             {"creator":{"id":1,"portrait":"tb.1.safe","name":"safe-user","name_show":"Safe User"}}
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetUserInfoJsonAsync("safe-user", cts.Token);

        Assert.AreEqual(1L, result.UserId);
        Assert.AreEqual(cts.Token, httpCore.LastWebGetCancellationToken);
    }

    private const string SuccessResponse = """
                                           {"error_code":0,"error_msg":""}
                                           """;

    private static LegacyUserProtocol CreateGuestProtocol(RecordingHttpCore httpCore)
    {
        var session = new TiebaClientSession(
            new global::AioTieba4DotNet.TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());

        return CreateProtocol(session);
    }

    private static LegacyUserProtocol CreateProtocol(TiebaClientSession session)
    {
        return new LegacyUserProtocol(new LegacyTransportContext(session), new StubForumProtocol());
    }

    private static TiebaClientSession CreateAuthenticatedSession(RecordingHttpCore httpCore,
        Func<CancellationToken, Task<string>> loadTbsAsync)
    {
        return new TiebaClientSession(
            new global::AioTieba4DotNet.TiebaOptions
            {
                Bduss = ValidBduss,
                Stoken = ValidStoken,
                TransportMode = TiebaTransportMode.Http
            },
            httpCore,
            new StubWsCore(),
            loadTbsAsync);
    }

    private static void AssertTransportUnused(RecordingHttpCore httpCore)
    {
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    private static async Task AssertAuthFailure(Func<Task> action)
    {
        await AssertThrowsAsync<TiebaAuthenticationException>(action);
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

    private static ProfileResIdl CreateProfileResponse()
    {
        return new ProfileResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new ProfileResIdl.Types.DataRes
            {
                User = new User
                {
                    Id = 1,
                    Portrait = "tb.1.safe",
                    Name = "safe-user",
                    NameShow = "Safe User"
                }
            }
        };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string AppFormResponse { get; init; } = "{}";

        public byte[] AppProtoResponse { get; init; } = [];

        public string WebGetResponse { get; init; } = "{}";

        public int SendAppFormCalls { get; private set; }

        public int SendAppProtoCalls { get; private set; }

        public int SendWebGetCalls { get; private set; }

        public int SendWebFormCalls { get; private set; }

        public CancellationToken LastAppFormCancellationToken { get; private set; }

        public CancellationToken LastAppProtoCancellationToken { get; private set; }

        public CancellationToken LastWebGetCancellationToken { get; private set; }

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
            LastAppProtoCancellationToken = cancellationToken;
            return Task.FromResult(AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            SendWebGetCalls++;
            LastWebGetCancellationToken = cancellationToken;
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

    private sealed class StubForumProtocol : IForumProtocol
    {
        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<global::AioTieba4DotNet.Models.Forums.ForumDetail> GetDetailAsync(ulong fid,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<global::AioTieba4DotNet.Models.Forums.ForumDetail> GetDetailAsync(string fname,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<global::AioTieba4DotNet.Models.Forums.Forum> GetForumAsync(string fname,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
