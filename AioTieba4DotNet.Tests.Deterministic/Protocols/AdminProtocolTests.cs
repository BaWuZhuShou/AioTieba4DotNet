#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public sealed class AdminProtocolTests
{
    private const string SafeForumName = "lol欧服";
    private const ulong SafeForumId = 7356044;
    private static readonly string ValidBduss = new('b', 192);
    private static readonly string ValidStoken = new('s', 64);

    [TestMethod]
    public async Task GetBawuInfoAsync_WithCachedForumId_UsesWebSocketPreferredPath()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore
        {
            ResponsePayload = CreateResponseBytes()
        };
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetBawuInfoAsync(SafeForumName, cts.Token);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(301007, wsCore.LastCmd);
        Assert.AreEqual(cts.Token, wsCore.LastCancellationToken);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);

        var request = GetBawuInfoReqIdl.Parser.ParseFrom(wsCore.LastData);
        Assert.AreEqual((long)SafeForumId, (long)request.Data.Fid);
        Assert.AreEqual("admin-user", result.Admins[0].UserName);
    }

    [TestMethod]
    public async Task GetBawuInfoAsync_WhenWebSocketIsUnavailable_FallsBackToHttp()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateResponseBytes()
        };
        var wsCore = new RecordingWsCore
        {
            ConnectException = new WebSocketException("offline")
        };
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetBawuInfoAsync(SafeForumName, cts.Token);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual("/c/f/forum/getBawuInfo", httpCore.LastAppProtoUri?.AbsolutePath);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
        Assert.AreEqual("manager-user", result.Managers[0].UserName);
    }

    [TestMethod]
    public async Task GetBawuInfoAsync_BlankForumName_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, new ForumInfoCache());

        await Assert.ThrowsAsync<ArgumentException>(() => protocol.GetBawuInfoAsync(" "));

        Assert.AreEqual(0, wsCore.ConnectCalls);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetBawuPermAsync_WithoutCredentials_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), authenticated: false);

        await Assert.ThrowsAsync<TiebaAuthenticationException>(() =>
            protocol.GetBawuPermAsync(SafeForumName, "tb.1.target"));

        Assert.AreEqual(0, httpCore.SendWebGetCalls);
        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task AddBaWuAsync_WithoutCredentials_FailsBeforeForumLookup()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, new ForumInfoCache(), authenticated: false);

        await Assert.ThrowsAsync<TiebaAuthenticationException>(() =>
            protocol.AddBaWuAsync(SafeForumName, "target-user", BawuType.Manager));

        Assert.AreEqual(0, httpCore.SendWebGetCalls);
        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task AddBaWuAsync_WithCachedForumId_PacksExpectedForm()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), tbs: "tbs-admin");

        var success = await protocol.AddBaWuAsync(SafeForumName, "target-user", BawuType.Manager);

        Assert.IsTrue(success);
        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual("/mo/q/bawuteamadd", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("target-user", httpCore.GetWebFormValue("team_un"));
        Assert.AreEqual("assist", httpCore.GetWebFormValue("type"));
        Assert.AreEqual("tbs-admin", httpCore.GetWebFormValue("tbs"));
    }

    [TestMethod]
    public async Task DelBaWuAsync_WithCachedForumId_PacksExpectedForm()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());

        var success = await protocol.DelBaWuAsync(SafeForumName, "tb.1.target", BawuType.ImageEditor);

        Assert.IsTrue(success);
        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual("/mo/q/bawuteamclear", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("tb.1.target", httpCore.GetWebFormValue("team_uid"));
        Assert.AreEqual("picadmin", httpCore.GetWebFormValue("bawu_type"));
    }

    [TestMethod]
    public async Task AddBawuBlacklistAsync_WithoutCredentials_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), authenticated: false);

        await Assert.ThrowsAsync<TiebaAuthenticationException>(() =>
            protocol.AddBawuBlacklistAsync(SafeForumName, 42));

        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task DelBawuBlacklistAndReadBlacklist_SucceedWithExpectedPayloads()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             <div class="breadcrumbs"><em>1</em></div>
                             <table>
                               <tr>
                                 <td class="left_cell"><a data-user-name="target-user" data-user-id="42" href="/home/main?id=tb.1.target&amp;fr=home">target-user</a></td>
                               </tr>
                             </table>
                             """
        };
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), tbs: "tbs-blacklist");

        var users = await protocol.GetBawuBlacklistAsync(SafeForumName, 2);
        var removed = await protocol.DelBawuBlacklistAsync(SafeForumName, 42);

        Assert.AreEqual(1, httpCore.SendWebGetCalls);
        Assert.AreEqual("/bawu2/platform/listBlackUser", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual(SafeForumName, httpCore.GetWebGetValue("word"));
        Assert.AreEqual("2", httpCore.GetWebGetValue("pn"));
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual(42L, users[0].UserId);
        Assert.AreEqual("tb.1.target", users[0].Portrait);
        Assert.IsTrue(removed);
        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual("/bawu2/platform/cancelBlack", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual(SafeForumName, httpCore.GetWebFormValue("word"));
        Assert.AreEqual("42", httpCore.GetWebFormValue("list[]"));
        Assert.AreEqual("tbs-blacklist", httpCore.GetWebFormValue("tbs"));
    }

    [TestMethod]
    public async Task GetBawuPermAsync_WithCachedForumId_PacksExpectedQueryAndParsesPermissions()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             {"no":0,"error":"","data":{"perm_setting":{"category_user":[{"switch":1,"perm":4},{"switch":"0","perm":5}],"category_thread":[{"switch":true,"perm":2},{"switch":0,"perm":3}]}}}
                             """
        };
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());

        var permissions = await protocol.GetBawuPermAsync(SafeForumName, "tb.1.target");

        Assert.AreEqual(1, httpCore.SendWebGetCalls);
        Assert.AreEqual("/mo/q/getAuthToolPerm", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebGetValue("forum_id"));
        Assert.AreEqual("tb.1.target", httpCore.GetWebGetValue("portrait"));
        Assert.AreEqual(BawuPermType.Unblock | BawuPermType.RecoverAppeal, permissions.Permissions);
    }

    [TestMethod]
    public async Task AdminLogQueries_WithValidDateRange_SearchAndOperationType_PackExpectedParameters()
    {
        var postLogHttpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             <div class="breadcrumbs"><em>1</em></div>
                             <div class="tbui_pagination"><li class="active"><a>1</a></li>(2)</div>
                             <table>
                               <tr>
                                 <td>
                                   <div class="post_meta"><a href="/home/main?id=tb.1.reply#/feed"></a><time>03-05 12:34</time></div>
                                   <h1><a href="/p/123456#7890">回复：原帖标题</a></h1>
                                   <div>123456789012reply body</div>
                                 </td>
                                 <td>删帖</td>
                                 <td>operator-a</td>
                                 <td>2024-05-06 07:08</td>
                               </tr>
                             </table>
                             """
        };
        var postLogProtocol = CreateProtocol(postLogHttpCore, new RecordingWsCore(), CreateSeededCache());
        var userLogHttpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             <div class="breadcrumbs"><em>1</em></div>
                             <div class="tbui_pagination"><li class="active"><a>1</a></li>(2)</div>
                             <table>
                               <tr>
                                 <td><a href="/home/main?id=tb.1.reply#/feed">target-user</a></td>
                                 <td>封禁</td>
                                 <td>3 天</td>
                                 <td>operator-a</td>
                                 <td>2024-05-06 07:08</td>
                               </tr>
                             </table>
                             """
        };
        var userLogProtocol = CreateProtocol(userLogHttpCore, new RecordingWsCore(), CreateSeededCache());
        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero);

        var postLogs = await postLogProtocol.GetBawuPostLogsAsync(SafeForumName, new BawuPostLogQueryOptions
        {
            PageNumber = 3,
            SearchValue = "target-user",
            SearchType = BawuSearchType.Operator,
            StartTime = start,
            EndTime = end,
            OperationType = 7
        });
        var postLogParameters = postLogHttpCore.LastWebGetParameters.ToDictionary(entry => entry.Key, entry => entry.Value);
        var userLogs = await userLogProtocol.GetBawuUserLogsAsync(SafeForumName, new BawuUserLogQueryOptions
        {
            PageNumber = 4,
            SearchValue = "target-user",
            SearchType = BawuSearchType.User,
            StartTime = start,
            OperationType = 8
        });
        var userLogParameters = userLogHttpCore.LastWebGetParameters.ToDictionary(entry => entry.Key, entry => entry.Value);

        Assert.AreEqual(1, postLogHttpCore.SendWebGetCalls);
        Assert.AreEqual(1, userLogHttpCore.SendWebGetCalls);
        Assert.AreEqual(1, postLogs.Count);
        Assert.AreEqual(123456L, postLogs[0].Tid);
        Assert.AreEqual(7890L, postLogs[0].Pid);
        Assert.AreEqual("reply body", postLogs[0].Text);
        Assert.AreEqual("operator-a", postLogs[0].OperatorUserName);
        Assert.AreEqual("7", postLogParameters["op_type"]);
        Assert.AreEqual("target-user", postLogParameters["svalue"]);
        Assert.AreEqual("op_uname", postLogParameters["stype"]);
        Assert.AreEqual(start.ToUnixTimeSeconds().ToString(), postLogParameters["begin"]);
        Assert.AreEqual(end.ToUnixTimeSeconds().ToString(), postLogParameters["end"]);
        Assert.AreEqual(1, userLogs.Count);
        Assert.AreEqual("封禁", userLogs[0].OperationType);
        Assert.AreEqual(3, userLogs[0].OperationDurationDays);
        Assert.AreEqual("tb.1.reply", userLogs[0].UserPortrait);
        Assert.AreEqual("8", userLogParameters["op_type"]);
        Assert.AreEqual("target-user", userLogParameters["svalue"]);
        Assert.AreEqual("post_uname", userLogParameters["stype"]);
        Assert.AreEqual(start.ToUnixTimeSeconds().ToString(), userLogParameters["begin"]);
        Assert.IsTrue(userLogParameters.ContainsKey("end"));
    }

    [TestMethod]
    public async Task GetBawuPostLogsAsync_InvalidDateRange_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());

        var options = new BawuPostLogQueryOptions
        {
            StartTime = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetBawuPostLogsAsync(SafeForumName, options));

        Assert.AreEqual(0, httpCore.SendWebGetCalls);
        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task GetUnblockAppealsAsync_InvalidPageSize_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetUnblockAppealsAsync(SafeForumName, 1, 0));

        Assert.AreEqual(0, httpCore.SendWebGetCalls);
        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task GetUnblockAppealsAsync_WithCachedForumId_PacksExpectedFormAndParsesAppeals()
    {
        var httpCore = new RecordingHttpCore
        {
            WebFormResponse = """
                              {"no":0,"error":"","data":{"appeal_list":[{"appeal_id":"1001","appeal_reason":"reason","appeal_time":"1711700000","punish_reason":"spam","punish_start_time":"1711600000","punish_day_num":3,"operate_man":"moderator","user":{"id":42,"portrait":"tb.1.target?foo=bar","name":"target-user","name_show":"Target"}}],"has_more":1}}
                              """
        };
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), tbs: "tbs-appeal");

        var result = await protocol.GetUnblockAppealsAsync(SafeForumName, 2, 20);

        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual("/mo/q/getBawuAppealList", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("2", httpCore.GetWebFormValue("pn"));
        Assert.AreEqual("20", httpCore.GetWebFormValue("rn"));
        Assert.AreEqual("tbs-appeal", httpCore.GetWebFormValue("tbs"));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1001L, result[0].AppealId);
        Assert.IsTrue(result.HasMore);
    }

    [TestMethod]
    public async Task HandleUnblockAppealsAsync_EmptyAppealIds_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            protocol.HandleUnblockAppealsAsync(SafeForumName, Array.Empty<long>(), refuse: false));

        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task HandleUnblockAppealsAsync_WithCachedForumId_PacksIndexedAppealsAndStatus()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), tbs: "tbs-handle");

        var success = await protocol.HandleUnblockAppealsAsync(SafeForumName, new long[] { 1001, 1002 }, refuse: true);

        Assert.IsTrue(success);
        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual("/mo/q/multiAppealhandle", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("1001", httpCore.GetWebFormValue("appeal_list[0]"));
        Assert.AreEqual("1002", httpCore.GetWebFormValue("appeal_list[1]"));
        Assert.AreEqual("2", httpCore.GetWebFormValue("status"));
        Assert.AreEqual("tbs-handle", httpCore.GetWebFormValue("tbs"));
    }

    [TestMethod]
    public async Task SetBawuPermAsync_WithCachedForumId_PacksExpectedForm()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());
        using var cts = new CancellationTokenSource();

        var success = await protocol.SetBawuPermAsync(SafeForumName, "tb.1.target",
            BawuPermType.Unblock | BawuPermType.RecoverAppeal, cts.Token);

        Assert.IsTrue(success);
        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual("/mo/q/setAuthToolPerm", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("forum_id"));
        Assert.AreEqual("tb.1.target", httpCore.GetWebFormValue("auth_user_portrait"));
        Assert.AreEqual(
            "[{\"switch\":1,\"perm\":4},{\"switch\":0,\"perm\":5},{\"switch\":0,\"perm\":3},{\"switch\":1,\"perm\":2}]",
            httpCore.GetWebFormValue("perm_setting"));
        Assert.AreEqual(cts.Token, httpCore.LastWebFormCancellationToken);
    }

    [TestMethod]
    public async Task GetBlocksAsync_WithCachedForumId_PacksExpectedQueryAndParsesResponse()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             {"no":0,"error":"","data":{"content":"<li><a attr-uid=\"42\" attr-un=\"target-user\" attr-nn=\"Target\" attr-blockday=\"3\"></a></li>","page":{"size":20,"pn":2,"total_page":5,"total_count":91,"have_next":1}}}
                             """
        };
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetBlocksAsync(SafeForumName, "target-user", 2, cts.Token);

        Assert.AreEqual(1, httpCore.SendWebGetCalls);
        Assert.AreEqual("/mo/q/bawublock", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebGetValue("fid"));
        Assert.AreEqual("target-user", httpCore.GetWebGetValue("word"));
        Assert.AreEqual("2", httpCore.GetWebGetValue("pn"));
        Assert.AreEqual(cts.Token, httpCore.LastWebGetCancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(42L, result[0].UserId);
        Assert.IsTrue(result.Page.HasMore);
    }

    [TestMethod]
    public async Task UnblockAsync_InvalidUserId_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.UnblockAsync(SafeForumName, 0));

        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task UnblockAsync_WithCachedForumId_PacksExpectedForm()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), tbs: "tbs-unblock");

        var success = await protocol.UnblockAsync(SafeForumName, 42);

        Assert.IsTrue(success);
        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual("/mo/q/bawublockclear", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("42", httpCore.GetWebFormValue("block_uid"));
        Assert.AreEqual("tbs-unblock", httpCore.GetWebFormValue("tbs"));
    }

    [TestMethod]
    public async Task BlockAndUnblock_FidOverloads_PackExpectedForms()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), tbs: "tbs-direct");

        var blocked = await protocol.BlockAsync(SafeForumId, "tb.1.target", 5, "direct-reason");
        var firstForm = httpCore.LastAppFormData.ToDictionary(entry => entry.Key, entry => entry.Value);
        var unblocked = await protocol.UnblockAsync(SafeForumId, 42);
        var secondForm = httpCore.LastWebFormData.ToDictionary(entry => entry.Key, entry => entry.Value);

        Assert.IsTrue(blocked);
        Assert.IsTrue(unblocked);
        Assert.AreEqual("7356044", firstForm["fid"]);
        Assert.AreEqual("tb.1.target", firstForm["portrait"]);
        Assert.AreEqual("5", firstForm["day"]);
        Assert.AreEqual("direct-reason", firstForm["reason"]);
        Assert.AreEqual("7356044", secondForm["fid"]);
        Assert.AreEqual("42", secondForm["block_uid"]);
        Assert.AreEqual("tbs-direct", secondForm["tbs"]);
    }

    [TestMethod]
    public async Task AdminProtocol_ValidationBranches_FailBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.BlockAsync(0, "tb.1.target", 1, "reason"));
        await Assert.ThrowsAsync<ArgumentException>(() => protocol.BlockAsync(SafeForumId, " ", 1, "reason"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.BlockAsync(SafeForumId, "tb.1.target", 0, "reason"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetBawuBlacklistAsync(SafeForumName, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetBawuPostLogsAsync(SafeForumName,
            new BawuPostLogQueryOptions { OperationType = -1 }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetBawuUserLogsAsync(SafeForumName,
            new BawuUserLogQueryOptions { OperationType = -1 }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.HandleUnblockAppealsAsync(SafeForumName, new long[] { 0 }, false));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.AddBaWuAsync(SafeForumName, "target-user", (BawuType)999));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.DelBaWuAsync(SafeForumName, "tb.1.target", (BawuType)999));

        Assert.AreEqual(0, httpCore.SendWebFormCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task BlockAsync_StringOverload_ResolvesForumIdAndPacksForm()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore();
        var protocol = CreateProtocol(httpCore, wsCore, CreateSeededCache(), tbs: "tbs-block");

        var success = await protocol.BlockAsync(SafeForumName, "tb.1.target", 3, "reason");

        Assert.IsTrue(success);
        Assert.AreEqual("/c/c/bawu/commitprison", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(0, httpCore.SendWebFormCalls);
        Assert.AreEqual("7356044", httpCore.GetAppFormValue("fid"));
        Assert.AreEqual(httpCore.Account!.Bduss, httpCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual("tb.1.target", httpCore.GetAppFormValue("portrait"));
        Assert.AreEqual("3", httpCore.GetAppFormValue("day"));
        Assert.AreEqual("0", httpCore.GetAppFormValue("is_loop_ban"));
        Assert.AreEqual("reason", httpCore.GetAppFormValue("reason"));
        Assert.AreEqual("tbs-block", httpCore.GetAppFormValue("tbs"));
    }

    [TestMethod]
    public async Task AdminProtocol_ConstructorRejectsNullCache()
    {
        var session = new TiebaClientSession(new TiebaOptions
        {
            TransportMode = TiebaTransportMode.Auto,
            Bduss = ValidBduss,
            Stoken = ValidStoken
        }, new RecordingHttpCore(), new RecordingWsCore());

        Assert.ThrowsExactly<ArgumentNullException>(() => new AdminProtocol(new TiebaOperationDispatcher(session), null!));
        await session.WsCore.CloseAsync();
    }

    [TestMethod]
    public void BawuTypeWireMapper_MapsKnownAndUnknownValues()
    {
        Assert.AreEqual("assist", BawuTypeWireMapper.ToWireValue(BawuType.Manager));
        Assert.AreEqual("picadmin", BawuTypeWireMapper.ToWireValue(BawuType.ImageEditor));
        Assert.AreEqual("voiceadmin", BawuTypeWireMapper.ToWireValue(BawuType.VoiceEditor));

        Assert.IsTrue(BawuTypeWireMapper.TryFromWireValue("assist", out var manager));
        Assert.AreEqual(BawuType.Manager, manager);
        Assert.IsTrue(BawuTypeWireMapper.TryFromWireValue("picadmin", out var imageEditor));
        Assert.AreEqual(BawuType.ImageEditor, imageEditor);
        Assert.IsTrue(BawuTypeWireMapper.TryFromWireValue("voiceadmin", out var voiceEditor));
        Assert.AreEqual(BawuType.VoiceEditor, voiceEditor);
        Assert.IsFalse(BawuTypeWireMapper.TryFromWireValue("unknown", out var unknown));
        Assert.AreEqual(default, unknown);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BawuTypeWireMapper.ToWireValue((BawuType)999));
    }

    private static AdminProtocol CreateProtocol(RecordingHttpCore httpCore, RecordingWsCore wsCore,
        ForumInfoCache cache, bool authenticated = true, string tbs = "tbs")
    {
        var options = new TiebaOptions
        {
            TransportMode = TiebaTransportMode.Auto,
            Bduss = authenticated ? ValidBduss : null,
            Stoken = authenticated ? ValidStoken : null
        };
        var session = new TiebaClientSession(options, httpCore, wsCore, _ => Task.FromResult(tbs));
        return new AdminProtocol(new TiebaOperationDispatcher(session), cache);
    }

    private static ForumInfoCache CreateSeededCache()
    {
        var cache = new ForumInfoCache();
        cache.SetForumName(SafeForumId, SafeForumName);
        return cache;
    }

    private static byte[] CreateResponseBytes() => new GetBawuInfoResIdl
    {
        Error = new Error { Errorno = 0, Errmsg = string.Empty },
        Data = new GetBawuInfoResIdl.Types.DataRes
        {
            BawuTeamInfo = new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam
            {
                TotalNum = 2,
                BawuTeamList =
                {
                    new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes
                    {
                        RoleName = "吧主",
                        RoleInfo =
                        {
                            new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes.Types.BawuRoleInfoPub
                            {
                                UserId = 1,
                                Portrait = "tb.1.admin",
                                UserName = "admin-user",
                                NameShow = "Admin",
                                UserLevel = 18
                            }
                        }
                    },
                    new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes
                    {
                        RoleName = "小吧主",
                        RoleInfo =
                        {
                            new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes.Types.BawuRoleInfoPub
                            {
                                UserId = 2,
                                Portrait = "tb.1.manager",
                                UserName = "manager-user",
                                NameShow = "Manager",
                                UserLevel = 12
                            }
                        }
                    }
                }
            }
        }
    }.ToByteArray();

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public byte[] AppProtoResponse { get; init; } = CreateResponseBytes();

        public string AppFormResponse { get; init; } = """
                                                     {"error_code":0,"error_msg":""}
                                                     """;

        public Account? Account { get; private set; } = new();

        public HttpClient HttpClient { get; } = new();

        public int SendAppProtoCalls { get; private set; }

        public int SendWebGetCalls { get; private set; }

        public int SendWebFormCalls { get; private set; }

        public Uri? LastAppProtoUri { get; private set; }

        public Uri? LastAppFormUri { get; private set; }

        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];

        public Uri? LastWebGetUri { get; private set; }

        public List<KeyValuePair<string, string>> LastWebGetParameters { get; private set; } = [];

        public Uri? LastWebFormUri { get; private set; }

        public List<KeyValuePair<string, string>> LastWebFormData { get; private set; } = [];

        public CancellationToken LastAppProtoCancellationToken { get; private set; }

        public CancellationToken LastAppFormCancellationToken { get; private set; }

        public CancellationToken LastWebGetCancellationToken { get; private set; }

        public CancellationToken LastWebFormCancellationToken { get; private set; }

        public string WebGetResponse { get; init; } = """
                                                    {"no":0,"error":""}
                                                    """;

        public string WebFormResponse { get; init; } = """
                                                     {"no":0,"error":""}
                                                     """;

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            LastAppFormCancellationToken = cancellationToken;
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            SendAppProtoCalls++;
            LastAppProtoUri = uri;
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

        public string GetWebGetValue(string key) =>
            LastWebGetParameters.Single(entry => entry.Key == key).Value;

        public string GetAppFormValue(string key) =>
            LastAppFormData.Single(entry => entry.Key == key).Value;

        public string GetWebFormValue(string key) =>
            LastWebFormData.Single(entry => entry.Key == key).Value;
    }

    private sealed class RecordingWsCore : ITiebaWsCore
    {
        public byte[] ResponsePayload { get; init; } = [];

        public Exception? ConnectException { get; init; }

        public Account? Account { get; } = new();

        public int ConnectCalls { get; private set; }

        public int LastCmd { get; private set; }

        public byte[] LastData { get; private set; } = [];

        public CancellationToken LastCancellationToken { get; private set; }

        public void SetAccount(Account newAccount)
        {
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            if (ConnectException is not null)
                throw ConnectException;

            return Task.CompletedTask;
        }

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

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

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
