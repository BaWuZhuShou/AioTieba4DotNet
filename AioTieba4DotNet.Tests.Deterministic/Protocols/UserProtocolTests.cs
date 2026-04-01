#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Tests.Infrastructure;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public class UserProtocolTests
{
    private static readonly string ValidBduss = new('b', 192);
    private static readonly string ValidStoken = new('s', 64);

    [TestMethod]
    public void UserSources_FreezePeerFamilies_AndRejectRemovedNames()
    {
        var userSource = RepositorySourceTextAssert.ReadRepositoryFiles(
            "AioTieba4DotNet/Contracts/IUserModule.cs",
            "AioTieba4DotNet/Protocols/IUserProtocol.cs",
            "AioTieba4DotNet/Protocols/UserProtocol.cs");

        RepositorySourceTextAssert.ContainsAll(
            userSource,
            "GetUserInfoAppAsync",
            "GetUserInfoWebAsync",
            "GetBlacklistAsync",
            "GetBlacklistOldAsync",
            "AddBlacklistOldAsync",
            "RemoveBlacklistOldAsync",
            "SetBlacklistAsync",
            "SetNicknameAsync",
            "UserPostGroups");
        RepositorySourceTextAssert.DoesNotContainAny(
            userSource,
            "GetBlacklistPermissionsAsync",
            "SetBlacklistPermissionsAsync",
            "GetBlacklistMutedAsync",
            "AddBlacklistMutedAsync",
            "RemoveBlacklistMutedAsync",
            "GetBlacklistLegacyAsync",
            "AddBlacklistLegacyAsync",
            "RemoveBlacklistLegacyAsync",
            "GetBasicInfoAppAsync",
            "GetBasicInfoWebAsync",
            "SetNicknameLegacyAsync",
            "UserPostss");
    }

    [TestMethod]
    public async Task GetFansAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.GetFansAsync(1, 1));

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
    public async Task GetBlacklistOldAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.GetBlacklistOldAsync(1, 20));

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
    public async Task AddBlacklistOldAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.AddBlacklistOldAsync(1));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task RemoveBlacklistOldAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.RemoveBlacklistOldAsync(1));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task SetNicknameAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.SetNicknameAsync("safe-name"));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task SetProfileAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.SetProfileAsync("safe-name", "hello", Gender.Male));

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

        await AssertThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await protocol.SetBlacklistAsync(0, BlacklistType.All));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task UserProtocol_GuardsInvalidPageSizesForumIdsAndTiebaUids()
    {
        var httpCore = new RecordingHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<ArgumentOutOfRangeException>(async () => await protocol.GetFollowsAsync(1, 0));
        await AssertThrowsAsync<ArgumentOutOfRangeException>(async () => await protocol.GetBlacklistOldAsync(1, 0));
        await AssertThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await protocol.GetUserForumInfoAsync(0UL, "tb.1.safe"));
        await AssertThrowsAsync<ArgumentOutOfRangeException>(async () => await protocol.GetUserByTiebaUidAsync(0));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task GetBlacklistOldAsync_UsesWebSocketPreferredPath_AndMapsUsers()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore { ResponsePayload = CreateMutedBlacklistResponse().ToByteArray() };
        using var session = CreateAuthenticatedSession(httpCore, wsCore, _ => Task.FromResult("tbs-123"),
            TiebaTransportMode.Auto);
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetBlacklistOldAsync(2, 30, cts.Token);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(303028, wsCore.LastCmd);
        Assert.AreEqual(cts.Token, wsCore.LastCancellationToken);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(123L, result[0].UserId);
        Assert.AreEqual("tb.1.safe", result[0].Portrait);
        Assert.AreEqual("Safe User", result[0].NickNameOld);
        var oldPage = GetBlacklistOldPage(result);
        Assert.AreEqual(2, oldPage.CurrentPage);
        Assert.IsTrue(oldPage.HasMore);
        Assert.IsFalse(oldPage.HasPrevious);

        var request = UserMuteQueryReqIdl.Parser.ParseFrom(wsCore.LastData);
        Assert.AreEqual(2U, request.Data.Pn);
        Assert.AreEqual(30U, request.Data.Rn);
        Assert.AreEqual(ValidBduss, request.Data.Common.BDUSS);
    }

    [TestMethod]
    public async Task GetBlacklistOldAsync_WhenWebSocketUnavailable_FallsBackToHttp()
    {
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateMutedBlacklistResponse().ToByteArray() };
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        using var session = CreateAuthenticatedSession(httpCore, wsCore, _ => Task.FromResult("tbs-123"),
            TiebaTransportMode.Auto);
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetBlacklistOldAsync(2, 30, cts.Token);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual("/c/u/user/userMuteQuery", httpCore.LastAppProtoUri?.AbsolutePath);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
        Assert.AreEqual(1, result.Count);

        var request = UserMuteQueryReqIdl.Parser.ParseFrom(httpCore.LastAppProtoData);
        Assert.AreEqual(2U, request.Data.Pn);
        Assert.AreEqual(30U, request.Data.Rn);
    }

    [TestMethod]
    public async Task AddBlacklistOldAsync_PropagatesCancellationToken_AndPacksRequest()
    {
        var httpCore = new RecordingHttpCore { AppFormResponse = SuccessResponse };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.AddBlacklistOldAsync(12345, cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
        Assert.AreEqual("/c/c/user/userMuteAdd", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(ValidBduss, httpCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual("12345", httpCore.GetAppFormValue("mute_user"));
    }

    [TestMethod]
    public async Task RemoveBlacklistOldAsync_PropagatesCancellationToken_AndPacksRequest()
    {
        var httpCore = new RecordingHttpCore { AppFormResponse = SuccessResponse };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.RemoveBlacklistOldAsync(12345, cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
        Assert.AreEqual("/c/c/user/userMuteDel", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(ValidBduss, httpCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual("12345", httpCore.GetAppFormValue("mute_user"));
    }

    [TestMethod]
    public async Task SetNicknameAsync_BlankNickname_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<ArgumentException>(async () => await protocol.SetNicknameAsync(" "));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task SetNicknameAsync_UsesWebFormQuery_AndPropagatesCancellationToken()
    {
        var httpCore = new RecordingHttpCore { WebFormResponse = "{\"no\":0,\"error\":\"\"}" };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.SetNicknameAsync("Safe Nick", cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual(cts.Token, httpCore.LastWebFormCancellationToken);
        Assert.AreEqual("/mo/q/submit/modifyNickname", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.Contains("nickname=Safe%20Nick", httpCore.LastWebFormUri?.Query ?? string.Empty);
        Assert.Contains("tbs=1", httpCore.LastWebFormUri?.Query ?? string.Empty);
    }

    [TestMethod]
    public async Task SetProfileAsync_InvalidGender_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        await AssertThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await protocol.SetProfileAsync("safe-name", "hello", Gender.Unknown));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task SetProfileAsync_PropagatesCancellationToken_AndPacksRequest()
    {
        var httpCore = new RecordingHttpCore { AppFormResponse = SuccessResponse };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.SetProfileAsync("safe-name", "hello", Gender.Female, cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
        Assert.AreEqual("/c/c/profile/modify", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(ValidBduss, httpCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual("safe-name", httpCore.GetAppFormValue("nick_name"));
        Assert.AreEqual("hello", httpCore.GetAppFormValue("intro"));
        Assert.AreEqual(((int)Gender.Female).ToString(), httpCore.GetAppFormValue("sex"));
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

        var exception =
            await AssertThrowsAsync<TieBaServerException>(async () => await protocol.FollowAsync("tb.1.nonexistent"));

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

        var exception =
            await AssertThrowsAsync<TieBaServerException>(async () => await protocol.UnfollowAsync("tb.1.safe"));

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
    public async Task SetBlacklistAsync_PacksDisabledFlags_WhenNoPermissionsAreRequested()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = new SetUserBlackResIdl { Error = new Error { Errorno = 0, Errmsg = string.Empty } }
                .ToByteArray()
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var result = await protocol.SetBlacklistAsync(12345, BlacklistType.None);
        var request = SetUserBlackReqIdl.Parser.ParseFrom(httpCore.LastAppProtoData);

        Assert.IsTrue(result);
        Assert.AreEqual(12345L, request.Data.BlackUid);
        Assert.AreEqual(2, request.Data.PermList.Follow);
        Assert.AreEqual(2, request.Data.PermList.Interact);
        Assert.AreEqual(2, request.Data.PermList.Chat);
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
    public async Task GetSelfInfoAsync_FallsBackToMoIndexUserName_WhenInitNicknameIsBlank()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user_info":{"user_name":"","name_show":"Old Name","tieba_uid":778899}}
                              """,
            WebGetResponse = """
                             {"no":0,"error":"","data":{"id":123,"portrait":"tb.1.safe","name":"mo-user","user_sex":1,"post_num":12,"fans_num":34,"concern_num":56,"like_forum_num":78,"intro":"hello","vipInfo":{"v_status":0}}}
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var result = await protocol.GetSelfInfoAsync();

        Assert.AreEqual("mo-user", result.UserName);
        Assert.AreEqual("Old Name", result.NickNameOld);
    }

    [TestMethod]
    public async Task GetSelfInfoCompatibilityMethods_ExposeIndividualFamilies()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user_info":{"user_name":"self-user","name_show":"Old Name","tieba_uid":778899}}
                              """,
            WebGetResponse = """
                             {"no":0,"error":"","data":{"id":123,"portrait":"tb.1.safe","name":"mo-user","user_sex":1,"post_num":12,"fans_num":34,"concern_num":56,"like_forum_num":78,"intro":"hello","vipInfo":{"v_status":0}}}
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var initNickname = await protocol.GetSelfInfoInitNicknameAsync();
        var moIndex = await protocol.GetSelfInfoMoIndexAsync();

        Assert.AreEqual("self-user", initNickname.UserName);
        Assert.AreEqual("Old Name", initNickname.NickNameOld);
        Assert.AreEqual(123, moIndex.UserId);
        Assert.AreEqual("mo-user", moIndex.UserName);
    }

    [TestMethod]
    public async Task LoginAsync_ReturnsUserAndUpdatesSessionTbs()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "user":{"id":123,"name":"self-user","name_show":"Old Name","portrait":"tb.1.safe","sex":1,"fans_num":34,"concern_num":56,"like_forum_num":78,"intro":"hello","vipInfo":{"v_status":0}},
                                "anti":{"tbs":"tbs-login-123"}
                              }
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-old"));
        var protocol = CreateProtocol(session);

        var result = await protocol.LoginAsync();

        Assert.AreEqual("self-user", result.User.UserName);
        Assert.AreEqual("tbs-login-123", result.Tbs);
        Assert.AreEqual("tbs-login-123", httpCore.Account?.Tbs);
        Assert.AreEqual("/c/s/login", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(ValidBduss, httpCore.GetAppFormValue("bdusstoken"));
    }

    [TestMethod]
    public async Task GetUserInfoAppAsync_PropagatesCancellationToken_ToTransport()
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

        var result = await protocol.GetUserInfoAppAsync(1, cts.Token);

        Assert.AreEqual("safe-user", result.UserName);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
    }

    [TestMethod]
    public async Task GetUserInfoWebAsync_ParsesWebShape_AndTrimsPortraitQuery()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             {"errno":0,"errmsg":"","chatUser":{"uid":123,"uname":"123","portrait":"tb.1.safe?from=pc","show_nickname":"Safe User"}}
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var result = await protocol.GetUserInfoWebAsync(123);

        Assert.AreEqual(123, result.UserId);
        Assert.AreEqual(string.Empty, result.UserName);
        Assert.AreEqual("tb.1.safe", result.Portrait);
        Assert.AreEqual("Safe User", result.NickNameNew);
        Assert.AreEqual("/im/pcmsg/query/getUserInfo", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("123", httpCore.GetWebGetValue("chatUid"));
    }

    [TestMethod]
    public async Task GetUserForumInfoAsync_WithoutCredentials_FailsLocally_BeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateGuestProtocol(httpCore);

        await AssertAuthFailure(async () => await protocol.GetUserForumInfoAsync(1UL, "tb.1.safe"));

        AssertTransportUnused(httpCore);
    }

    [TestMethod]
    public async Task GetUserForumInfoAsync_ByForumName_ResolvesFid_AndParsesResponse()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","data":{"user_info":{"id":123,"portrait":"tb.1.safe?from=app","name":"Safe User","is_like":1},"forum_info":{"forum_name":"csharp","forum_avatar":"https://example/avatar.png"},"user_forum_info":{"is_follow":1,"follow_days":8,"sign_days":3,"thread_num":5,"day_post_num":2,"member_no":10,"day_sign_no":6,"level_id":4,"level_name":"核心吧友","cur_score":50,"levelup_score":80,"role_name":"吧友","identify":"active","high_light_sign_days":7}}}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var forumProtocol = new RecordingForumProtocol { FidResult = 7356044 };
        var protocol = CreateProtocol(session, forumProtocol);

        var result = await protocol.GetUserForumInfoAsync("csharp", "tb.1.safe");

        Assert.AreEqual(7356044UL, forumProtocol.LastRequestedFidByName);
        Assert.AreEqual("csharp", forumProtocol.LastRequestedForumName);
        Assert.AreEqual("csharp", result.Fname);
        Assert.AreEqual("tb.1.safe", result.User.Portrait);
        Assert.IsTrue(result.User.IsLike);
        Assert.IsTrue(result.IsFollow);
        Assert.AreEqual(8, result.FollowDays);
        Assert.AreEqual(4, result.Level);
        Assert.AreEqual("核心吧友", result.LevelName);
        Assert.AreEqual("/c/f/forum/getUserForumLevelInfo", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetAppFormValue("forum_id"));
        Assert.AreEqual("tb.1.safe", httpCore.GetAppFormValue("friend_portrait"));
    }

    [TestMethod]
    public async Task GetRankUsersAsync_ParsesHtmlRows_AndPageMetadata()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             <html>
                               <table>
                                 <tr class="drl_list_item">
                                   <td>1</td>
                                   <td><a>safe-user</a><span class="drl_item_vip"></span></td>
                                   <td><span class="bg_lv12"></span></td>
                                   <td>345</td>
                                 </tr>
                               </table>
                               <ul class="p_rank_pager" data-field='{"cur_page":2,"total_num":5}'></ul>
                             </html>
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);

        var result = await protocol.GetRankUsersAsync("csharp", 2);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("safe-user", result[0].UserName);
        Assert.AreEqual(12, result[0].Level);
        Assert.AreEqual(345, result[0].Exp);
        Assert.IsTrue(result[0].IsVip);
        Assert.AreEqual(2, result.Page.CurrentPage);
        Assert.AreEqual(5, result.Page.TotalPage);
        Assert.IsTrue(result.Page.HasMore);
        Assert.IsTrue(result.Page.HasPrevious);
        Assert.AreEqual("/f/like/furank", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("csharp", httpCore.GetWebGetValue("kw"));
        Assert.AreEqual("2", httpCore.GetWebGetValue("pn"));
    }

    [TestMethod]
    public async Task GetHomepageAsync_UsesWebSocketPreferredPath()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore { ResponsePayload = CreateHomepageResponse().ToByteArray() };
        using var session = CreateAuthenticatedSession(httpCore, wsCore, _ => Task.FromResult("tbs-123"),
            TiebaTransportMode.Auto);
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetHomepageAsync(123, 2, cts.Token);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(303012, wsCore.LastCmd);
        Assert.AreEqual(cts.Token, wsCore.LastCancellationToken);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(123L, result.User.UserId);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("homepage-thread", result[0].Title);
        Assert.AreSame(result.User, result[0].User);

        var request = ProfileReqIdl.Parser.ParseFrom(wsCore.LastData);
        Assert.AreEqual(123, request.Data.Uid);
        Assert.AreEqual(2, request.Data.Page);
        Assert.AreEqual(1U, request.Data.NeedPostCount);
    }

    [TestMethod]
    public async Task GetHomepageAsync_WhenWebSocketUnavailable_FallsBackToHttp()
    {
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateHomepageResponse().ToByteArray() };
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        using var session = CreateAuthenticatedSession(httpCore, wsCore, _ => Task.FromResult("tbs-123"),
            TiebaTransportMode.Auto);
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetHomepageAsync(123, 2, cts.Token);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual("/c/u/user/profile", httpCore.LastAppProtoUri?.AbsolutePath);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
        Assert.AreEqual(1, result.Count);

        var request = ProfileReqIdl.Parser.ParseFrom(httpCore.LastAppProtoData);
        Assert.AreEqual(123, request.Data.Uid);
        Assert.AreEqual(2, request.Data.Page);
    }

    [TestMethod]
    public async Task GetUserByTiebaUidAsync_UsesWebSocketPreferredPath_AndMapsUser()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore { ResponsePayload = CreateTiebaUidResponse().ToByteArray() };
        using var session = CreateAuthenticatedSession(httpCore, wsCore, _ => Task.FromResult("tbs-123"),
            TiebaTransportMode.Auto);
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetUserByTiebaUidAsync(778899, cts.Token);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(309702, wsCore.LastCmd);
        Assert.AreEqual(cts.Token, wsCore.LastCancellationToken);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(123L, result.UserId);
        Assert.AreEqual(778899L, result.TiebaUid);
        Assert.AreEqual("tb.1.safe", result.Portrait);
        Assert.AreEqual("Safe User", result.NickNameNew);
        Assert.AreEqual(3.5f, result.Age);
        Assert.IsTrue(result.IsGod);

        var request = GetUserByTiebaUidReqIdl.Parser.ParseFrom(wsCore.LastData);
        Assert.AreEqual("778899", request.Data.TiebaUid);
    }

    [TestMethod]
    public async Task GetUserByTiebaUidAsync_WhenWebSocketUnavailable_FallsBackToHttp()
    {
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateTiebaUidResponse().ToByteArray() };
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        using var session = CreateAuthenticatedSession(httpCore, wsCore, _ => Task.FromResult("tbs-123"),
            TiebaTransportMode.Auto);
        var protocol = CreateProtocol(session);

        var result = await protocol.GetUserByTiebaUidAsync(778899);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual("/c/u/user/getUserByTiebaUid", httpCore.LastAppProtoUri?.AbsolutePath);
        Assert.AreEqual(123L, result.UserId);

        var request = GetUserByTiebaUidReqIdl.Parser.ParseFrom(httpCore.LastAppProtoData);
        Assert.AreEqual("778899", request.Data.TiebaUid);
    }

    [TestMethod]
    public async Task GetProfileAsync_Int_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateProfileResponse().ToByteArray() };
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
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateProfileResponse().ToByteArray() };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = CreateProtocol(session);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetProfileAsync("tb.1.safe", cts.Token);

        Assert.AreEqual("safe-user", result.UserName);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
    }

    [TestMethod]
    public async Task FollowAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore { AppFormResponse = SuccessResponse };
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
        var httpCore = new RecordingHttpCore { AppFormResponse = SuccessResponse };
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

    private static UserProtocol CreateGuestProtocol(RecordingHttpCore httpCore)
    {
        var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new RecordingWsCore());

        return CreateProtocol(session);
    }

    private static UserProtocol CreateProtocol(TiebaClientSession session, IForumProtocol? forumProtocol = null)
    {
        return new UserProtocol(new TiebaOperationDispatcher(session), forumProtocol ?? new StubForumProtocol());
    }

    private static TiebaClientSession CreateAuthenticatedSession(RecordingHttpCore httpCore,
        Func<CancellationToken, Task<string>> loadTbsAsync)
    {
        return CreateAuthenticatedSession(httpCore, new RecordingWsCore(), loadTbsAsync, TiebaTransportMode.Http);
    }

    private static TiebaClientSession CreateAuthenticatedSession(RecordingHttpCore httpCore, ITiebaWsCore wsCore,
        Func<CancellationToken, Task<string>> loadTbsAsync, TiebaTransportMode transportMode)
    {
        return new TiebaClientSession(
            new TiebaOptions { Bduss = ValidBduss, Stoken = ValidStoken, TransportMode = transportMode },
            httpCore,
            wsCore,
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
                User = new User { Id = 1, Portrait = "tb.1.safe", Name = "safe-user", NameShow = "Safe User" }
            }
        };
    }

    private static ProfileResIdl CreateHomepageResponse()
    {
        return new ProfileResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new ProfileResIdl.Types.DataRes
            {
                User = new User
                {
                    Id = 123,
                    Portrait = "tb.1.safe?from=app",
                    Name = "safe-user",
                    NameShow = "Safe User",
                    TbAge = "3.5",
                    Intro = "hello",
                    NewGodData = new User.Types.NewGodInfo { Status = 1 },
                    VirtualImageInfo = new User.Types.VirtualImageInfo()
                },
                PostList =
                {
                    new PostInfoList
                    {
                        Title = "homepage-thread",
                        ForumId = 99,
                        ForumName = "csharp",
                        ThreadId = 1001,
                        PostId = 1002,
                        ThreadType = 0,
                        FreqNum = 33,
                        ReplyNum = 7,
                        ShareNum = 2,
                        CreateTime = 123456
                    }
                }
            }
        };
    }

    private static GetUserByTiebaUidResIdl CreateTiebaUidResponse()
    {
        return new GetUserByTiebaUidResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new GetUserByTiebaUidResIdl.Types.DataRes
            {
                User = new User
                {
                    Id = 123,
                    Portrait = "tb.1.safe?from=tuid",
                    Name = "safe-user",
                    NameShow = "Safe User",
                    TiebaUid = "778899",
                    TbAge = "3.5",
                    Intro = "hello",
                    NewGodData = new User.Types.NewGodInfo { Status = 1 }
                }
            }
        };
    }

    private static UserMuteQueryResIdl CreateMutedBlacklistResponse()
    {
        return new UserMuteQueryResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new UserMuteQueryResIdl.Types.DataRes
            {
                Page = new Page { CurrentPage = 2, HasMore = 1, HasPrev = 0 },
                MuteUser =
                {
                    new UserMuteQueryResIdl.Types.DataRes.Types.MuteUser
                    {
                        UserId = 123,
                        Portrait = "tb.1.safe?from=muted",
                        UserName = "safe-user",
                        NameShow = "Safe User",
                        MuteTime = 1234567890
                    }
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

        public CancellationToken LastWebFormCancellationToken { get; private set; }

        public Uri? LastAppFormUri { get; private set; }

        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];

        public Uri? LastAppProtoUri { get; private set; }

        public byte[] LastAppProtoData { get; private set; } = [];

        public Uri? LastWebGetUri { get; private set; }

        public List<KeyValuePair<string, string>> LastWebGetParameters { get; private set; } = [];

        public Uri? LastWebFormUri { get; private set; }

        public List<KeyValuePair<string, string>> LastWebFormData { get; private set; } = [];

        public string WebFormResponse { get; init; } = "{}";

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            SendAppFormCalls++;
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            LastAppFormCancellationToken = cancellationToken;
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            SendAppProtoCalls++;
            LastAppProtoUri = uri;
            LastAppProtoData = data;
            LastAppProtoCancellationToken = cancellationToken;
            return Task.FromResult(AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            SendWebGetCalls++;
            LastWebGetUri = uri;
            LastWebGetParameters = [.. parameters];
            LastWebGetCancellationToken = cancellationToken;
            return Task.FromResult(WebGetResponse);
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            SendWebFormCalls++;
            LastWebFormUri = uri;
            LastWebFormData = [.. data];
            LastWebFormCancellationToken = cancellationToken;
            return Task.FromResult(WebFormResponse);
        }

        public string GetAppFormValue(string key)
        {
            return LastAppFormData.Single(entry => entry.Key == key).Value;
        }

        public string GetWebGetValue(string key)
        {
            return LastWebGetParameters.Single(entry => entry.Key == key).Value;
        }
    }

    private sealed class RecordingWsCore : ITiebaWsCore
    {
        public byte[] ResponsePayload { get; init; } = [];

        public Exception? ConnectException { get; init; }

        public Account? Account { get; private set; } = new();

        public int ConnectCalls { get; private set; }

        public int LastCmd { get; private set; }

        public byte[] LastData { get; private set; } = [];

        public CancellationToken LastCancellationToken { get; private set; }

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

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default)
        {
            LastCmd = cmd;
            LastData = data;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(new WSRes
            {
                Cmd = cmd,
                Payload = new WSRes.Types.Payload { Data = ByteString.CopyFrom(ResponsePayload) },
                Error = new WSRes.Types.Error { Errno = 0, Errmsg = string.Empty }
            });
        }

        public async IAsyncEnumerable<WSRes> ListenAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubForumProtocol : IForumProtocol
    {
        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumDetail> GetDetailAsync(ulong fid,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumDetail> GetDetailAsync(string fname,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignForumsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Forum> GetForumAsync(string fname,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class RecordingForumProtocol : IForumProtocol
    {
        public ulong FidResult { get; init; }

        public string? LastRequestedForumName { get; private set; }

        public ulong? LastRequestedFidByName { get; private set; }

        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastRequestedForumName = fname;
            LastRequestedFidByName = FidResult;
            return Task.FromResult(FidResult);
        }

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignForumsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<FollowForums> GetFollowForumsAsync(long userId, int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DislikeForums> GetDislikeForumsAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private static Models.Threads.PageT GetBlacklistOldPage(BlacklistOldUsers users)
    {
        return (Models.Threads.PageT)typeof(BlacklistOldUsers).GetProperty("Page")!.GetValue(users)!;
    }
}
