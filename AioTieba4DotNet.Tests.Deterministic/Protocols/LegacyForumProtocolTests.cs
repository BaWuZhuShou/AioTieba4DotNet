#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.WebSockets;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Protocols;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public class ForumProtocolTests
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
        var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

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
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), cache);
        using var cts = new CancellationTokenSource();

        var result = await protocol.SignAsync(SafeForumName, cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task SignForumsAsync_WithoutCredentials_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        await Assert.ThrowsAsync<TiebaAuthenticationException>(() => protocol.SignForumsAsync());

        Assert.AreEqual(0, httpCore.SendCustomCalls);
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, httpCore.SendWebFormCalls);
    }

    [TestMethod]
    public async Task SignForumsAsync_UsesHybridWebFormAndPropagatesCancellationToken()
    {
        var httpCore = new RecordingHttpCore
        {
            CustomResponse = """
                             {"error_code":0,"error_msg":"","error":{"errno":0,"errmsg":""}}
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());
        using var cts = new CancellationTokenSource();

        var result = await protocol.SignForumsAsync(cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(1, httpCore.SendCustomCalls);
        Assert.AreEqual(cts.Token, httpCore.LastCustomCancellationToken);
        Assert.AreEqual("/c/c/forum/msign", httpCore.LastCustomRequest!.RequestUri!.AbsolutePath);
        Assert.AreEqual("hybrid", httpCore.LastCustomRequest.Headers.GetValues("Subapp-Type").Single());
    }

    [TestMethod]
    public async Task SignGrowthAsync_WithoutUsableTbs_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult(string.Empty));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        await Assert.ThrowsAsync<TiebaConfigurationException>(() => protocol.SignGrowthAsync());

        Assert.AreEqual(0, httpCore.SendWebFormCalls);
        Assert.AreEqual(0, httpCore.SendCustomCalls);
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
    }

    [TestMethod]
    public async Task SignGrowthAsync_UsesWebFormAndPropagatesCancellationToken()
    {
        var httpCore = new RecordingHttpCore
        {
            WebFormResponse = """
                              {"no":0,"error":""}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());
        using var cts = new CancellationTokenSource();

        var result = await protocol.SignGrowthAsync(cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(1, httpCore.SendWebFormCalls);
        Assert.AreEqual(cts.Token, httpCore.LastWebFormCancellationToken);
        CollectionAssert.Contains(httpCore.LastWebFormData, new KeyValuePair<string, string>("tbs", "tbs-123"));
        CollectionAssert.Contains(httpCore.LastWebFormData, new KeyValuePair<string, string>("act_type", "page_sign"));
        CollectionAssert.Contains(httpCore.LastWebFormData, new KeyValuePair<string, string>("cuid", "-"));
    }

    [TestMethod]
    public async Task FollowAsync_ByName_ResolvesForumIdBeforeMutation()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = $"{{\"no\":0,\"error\":\"\",\"data\":{{\"fid\":{SafeForumId}}}}}",
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","error":{"errno":0,"errmsg":""}}
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var result = await protocol.FollowAsync(SafeForumName);

        Assert.IsTrue(result);
        Assert.AreEqual(1, httpCore.SendWebGetCalls);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
        CollectionAssert.Contains(httpCore.LastAppFormData, new KeyValuePair<string, string>("fid", SafeForumId.ToString()));
    }

    [TestMethod]
    public async Task GetFollowForumsAsync_ReturnsMappedForumEntries()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "has_more":1,
                                "forum_list":{
                                  "non-gconforum":[{"id":7356044,"name":"lol欧服","level_id":12,"cur_score":2048}],
                                  "gconforum":[{"id":81570,"name":"地下城与勇士","level_id":9,"cur_score":1024}]
                                }
                              }
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var forums = await protocol.GetFollowForumsAsync(4954297652, 1, 50);

        Assert.HasCount(2, forums);
        Assert.IsTrue(forums.HasMore);
        Assert.AreEqual(7356044UL, forums[0].Fid);
        Assert.AreEqual("lol欧服", forums[0].Fname);
        Assert.AreEqual(12, forums[0].Level);
        Assert.AreEqual(2048, forums[0].Exp);
    }

    [TestMethod]
    public async Task GetFollowForumsAsync_WithoutCredentials_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        await Assert.ThrowsAsync<TiebaAuthenticationException>(() => protocol.GetFollowForumsAsync(4954297652, 1, 50));
        Assert.AreEqual(0, httpCore.SendCustomCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
    }

    [TestMethod]
    public async Task GetSelfFollowForumsAsync_WithoutCredentials_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        await Assert.ThrowsAsync<TiebaAuthenticationException>(() => protocol.GetSelfFollowForumsAsync(1, 200));
        Assert.AreEqual(0, httpCore.SendCustomCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetSelfFollowForumsAsync_ReturnsMappedSignedEntries()
    {
        var httpCore = new RecordingHttpCore
        {
            CustomResponse = """
                             {
                               "error_code":0,
                               "error_msg":"",
                               "like_forum":[{"forum_id":7356044,"forum_name":"lol欧服","level_id":12,"is_sign":1}],
                               "like_forum_has_more":1
                             }
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var forums = await protocol.GetSelfFollowForumsAsync(1, 200);

        Assert.HasCount(1, forums);
        Assert.IsTrue(forums.HasMore);
        Assert.AreEqual(7356044UL, forums[0].Fid);
        Assert.IsTrue(forums[0].IsSigned);
        Assert.AreEqual(1, httpCore.SendCustomCalls);
        Assert.AreEqual("hybrid", httpCore.LastCustomRequest!.Headers.GetValues("Subapp-Type").Single());
    }

    [TestMethod]
    public async Task GetSelfFollowForumsV1Async_ReturnsLegacyPageShape()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             {
                               "errno":0,
                               "errmsg":"",
                               "data":{
                                 "like_forum":{
                                   "list":[{"forum_id":7356044,"forum_name":"lol欧服","level_id":12}],
                                   "page":{"cur_page":1,"total_page":3}
                                 }
                               }
                             }
                             """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var forums = await protocol.GetSelfFollowForumsV1Async(1, 20);

        Assert.HasCount(1, forums);
        Assert.AreEqual(1, forums.Page.CurrentPage);
        Assert.AreEqual(3, forums.Page.TotalPage);
        Assert.IsTrue(forums.Page.HasMore);
        Assert.IsFalse(forums.Page.HasPrevious);
    }

    [TestMethod]
    public async Task GetCidAsync_WithBlankCategory_ReturnsZeroWithoutTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var cid = await protocol.GetCidAsync(SafeForumName, "   ");

        Assert.AreEqual(0, cid);
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetCidAsync_WithoutCredentials_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        await Assert.ThrowsAsync<TiebaAuthenticationException>(() => protocol.GetCidAsync(SafeForumName, "分类"));
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetCidAsync_ReturnsExactClassId_WhenCategoryNameMatches()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "cates":[
                                  {"class_name":"别的分类","class_id":41},
                                  {"class_name":"目标分类","class_id":42}
                                ]
                              }
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var cid = await protocol.GetCidAsync(SafeForumName, "目标分类");

        Assert.AreEqual(42, cid);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
        CollectionAssert.Contains(httpCore.LastAppFormData, new KeyValuePair<string, string>("word", SafeForumName));
    }

    [TestMethod]
    public async Task GetCidAsync_ReturnsZero_WhenCategoryNameDoesNotMatchExactly()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "cates":[
                                  {"class_name":"目标分类 ","class_id":42}
                                ]
                              }
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var cid = await protocol.GetCidAsync(SafeForumName, "目标分类");

        Assert.AreEqual(0, cid);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
    }

    [TestMethod]
    public async Task SearchExactAsync_ReturnsMappedEntriesAndPaging()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "page":{
                                  "page_size":30,
                                  "current_page":2,
                                  "total_page":5,
                                  "total_count":42,
                                  "has_more":1,
                                  "has_prev":1
                                },
                                "post_list":[
                                  {
                                    "content":"命中内容",
                                    "title":"命中标题",
                                    "fname":"lol欧服吧",
                                    "tid":123456,
                                    "pid":654321,
                                    "is_floor":1,
                                    "time":1712345678,
                                    "author":{"name_show":"显示名A"}
                                  },
                                  {
                                    "content":"第二条",
                                    "title":"第二个标题",
                                    "fname":"lol欧服吧",
                                    "tid":123457,
                                    "pid":654322,
                                    "is_floor":0,
                                    "time":1712345680,
                                    "author":{"name_show":"显示名B"}
                                  }
                                ]
                              }
                              """
        };
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        var searches = await protocol.SearchExactAsync(SafeForumName, "关键字", 2, 30, ForumSearchType.Time, true);

        Assert.HasCount(2, searches);
        Assert.AreEqual(30, searches.Page.PageSize);
        Assert.AreEqual(2, searches.Page.CurrentPage);
        Assert.AreEqual(5, searches.Page.TotalPage);
        Assert.AreEqual(42, searches.Page.TotalCount);
        Assert.IsTrue(searches.Page.HasMore);
        Assert.IsTrue(searches.Page.HasPrevious);
        Assert.IsTrue(searches[0].IsComment);
        Assert.AreEqual("显示名A", searches[0].ShowName);
        Assert.AreEqual("命中标题", searches[0].Title);
        CollectionAssert.Contains(httpCore.LastAppFormData, new KeyValuePair<string, string>("kw", SafeForumName));
        CollectionAssert.Contains(httpCore.LastAppFormData, new KeyValuePair<string, string>("word", "关键字"));
        CollectionAssert.Contains(httpCore.LastAppFormData, new KeyValuePair<string, string>("sm", ((int)ForumSearchType.Time).ToString()));
        CollectionAssert.Contains(httpCore.LastAppFormData, new KeyValuePair<string, string>("only_thread", "1"));
    }

    [TestMethod]
    public async Task SearchExactAsync_ReturnsEmptyContainer_WhenServerReturnsNoPosts()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "page":{
                                  "page_size":30,
                                  "current_page":1,
                                  "total_page":0,
                                  "total_count":0,
                                  "has_more":0,
                                  "has_prev":0
                                }
                              }
                              """
        };
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        var searches = await protocol.SearchExactAsync(SafeForumName, "关键字", 1, 30);

        Assert.HasCount(0, searches);
        Assert.AreEqual(1, searches.Page.CurrentPage);
        Assert.AreEqual(0, searches.Page.TotalCount);
        Assert.IsFalse(searches.HasMore);
    }

    [TestMethod]
    public async Task SearchExactAsync_WithInvalidPageNumber_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            protocol.SearchExactAsync(SafeForumName, "关键字", 0, 30));
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
    }

    [TestMethod]
    public async Task GetRoomListByFidAsync_PacksAuthenticatedFormAndFlattensRooms()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "data":{
                                  "list":[
                                    {"room_list":[{"room_id":1001,"room_name":"safe-room-1"}]},
                                    {"room_list":[{"room_id":1002,"room_name":"safe-room-2"}]}
                                  ]
                                }
                              }
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var result = await protocol.GetRoomListByFidAsync(SafeForumId);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("1001", result[0]["room_id"]?.ToString());
        Assert.AreEqual("safe-room-2", result[1]["room_name"]?.ToString());
        Assert.AreEqual("/c/f/chat/getRoomListByFid", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(ValidBduss, httpCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual(SafeForumId.ToString(), httpCore.GetAppFormValue("fid"));
        Assert.AreEqual("frs", httpCore.GetAppFormValue("call_from"));
    }

    [TestMethod]
    public async Task GetRoomListByFidAsync_WithoutRoomGroups_ReturnsEmptyContainer()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "data":{}
                              }
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var result = await protocol.GetRoomListByFidAsync(SafeForumId);

        Assert.AreEqual(0, result.Count);
        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public async Task GetRoomListByFidAsync_WithoutDataSection_ReturnsEmptyContainer()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":""
                              }
                              """
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"));
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var result = await protocol.GetRoomListByFidAsync(SafeForumId);

        Assert.AreEqual(0, result.Count);
        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public async Task GetRoomListByFidAsync_InvalidForumId_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetRoomListByFidAsync(0));
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
    }

    [TestMethod]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            new RecordingHttpCore(),
            new StubWsCore());

        try
        {
            _ = new ForumProtocol(new TiebaOperationDispatcher(session), null!);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
        }
    }

    [TestMethod]
    public async Task GetDetailAsync_DoesNotCacheInvalidForumIdentity()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateForumDetailResponse(0, " ").ToByteArray()
        };
        var cache = new ForumInfoCache();
        var protocol = CreateProtocol(httpCore, cache);

        var detail = await protocol.GetDetailAsync(SafeForumId);
        var cachedName = cache.GetForumName(SafeForumId);

        Assert.AreEqual(0UL, detail.Fid);
        Assert.AreEqual(" ", detail.Fname);
        Assert.AreEqual(string.Empty, cachedName);
    }

    [TestMethod]
    public async Task ValidationGuards_RejectInvalidInputsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.FollowAsync(0));
        await Assert.ThrowsAsync<ArgumentException>(() => protocol.SearchExactAsync(" ", "关键字", 1, 30));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetFollowForumsAsync(0, 1, 30));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetFollowForumsAsync(1, 1, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetLastReplyersAsync(SafeForumName, 1, 101, ThreadSortType.Reply, false));
        await Assert.ThrowsAsync<ArgumentException>(() => protocol.SearchExactAsync(SafeForumName, " ", 1, 30));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            protocol.SearchExactAsync(SafeForumName, "关键字", 1, 30, (ForumSearchType)999));

        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetImageAsync_RejectsRelativeAndUnsupportedAbsoluteUris()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateProtocol(httpCore, new ForumInfoCache());

        await Assert.ThrowsAsync<ArgumentException>(() => protocol.GetImageAsync("images/forum.png"));
        await Assert.ThrowsAsync<ArgumentException>(() => protocol.GetImageAsync("ftp://example.com/forum.png"));

        Assert.AreEqual(0, httpCore.SendCustomCalls);
    }

    [TestMethod]
    public void ExactSearch_Equality_UsesPidSemantics()
    {
        var left = new ExactSearch { Pid = 123, Tid = 1, Title = "A" };
        var right = new ExactSearch { Pid = 123, Tid = 2, Title = "B" };
        var other = new ExactSearch { Pid = 124, Tid = 1, Title = "A" };

        Assert.AreEqual(left, right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        Assert.AreNotEqual(left, other);
    }

    [TestMethod]
    public async Task DislikeAsync_ByName_DoesNotInitializeTbsBeforeMutation()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = $"{{\"no\":0,\"error\":\"\",\"data\":{{\"fid\":{SafeForumId}}}}}",
            AppFormResponse = """
                              {"error_code":0,"error_msg":""}
                              """
        };
        var tbsLoadCalls = 0;
        using var session = CreateAuthenticatedSession(httpCore, _ =>
        {
            tbsLoadCalls++;
            return Task.FromResult("tbs-should-not-be-used");
        });
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var result = await protocol.DislikeAsync(SafeForumName);

        Assert.IsTrue(result);
        Assert.AreEqual(0, tbsLoadCalls);
        Assert.AreEqual(1, httpCore.SendWebGetCalls);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
    }

    [TestMethod]
    public async Task GetDislikeForumsAsync_FallsBackToHttp_WhenWebSocketIsUnavailable()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateDislikeForumsResponse(SafeForumId, SafeForumName, hasMore: true).ToByteArray()
        };
        var wsCore = new StubWsCore
        {
            ConnectException = new TiebaWebSocketUnavailableException("ws unavailable")
        };
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"),
            TiebaTransportMode.Auto, wsCore);
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var forums = await protocol.GetDislikeForumsAsync(1, 20);

        Assert.HasCount(1, forums);
        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.IsTrue(forums.Page.HasMore);
        Assert.AreEqual(2L, forums[0].PostNum);
    }

    [TestMethod]
    public async Task GetDislikeForumsAsync_HttpMode_DoesNotProbeWebSocket()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateDislikeForumsResponse(SafeForumId, SafeForumName, hasMore: false).ToByteArray()
        };
        var wsCore = new StubWsCore();
        using var session = CreateAuthenticatedSession(httpCore, _ => Task.FromResult("tbs-123"),
            TiebaTransportMode.Http, wsCore);
        var protocol = new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());

        var forums = await protocol.GetDislikeForumsAsync(1, 20);

        Assert.HasCount(1, forums);
        Assert.AreEqual(0, wsCore.ConnectCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.IsFalse(forums.Page.HasMore);
    }

    private static ForumProtocol CreateProtocol(RecordingHttpCore httpCore, ForumInfoCache cache)
    {
        var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());

        return new ForumProtocol(new TiebaOperationDispatcher(session), cache);
    }

    private static TiebaClientSession CreateAuthenticatedSession(RecordingHttpCore httpCore,
        Func<CancellationToken, Task<string>> loadTbsAsync,
        TiebaTransportMode transportMode = TiebaTransportMode.Http,
        StubWsCore? wsCore = null)
    {
        var options = new TiebaOptions();
        options.Bduss = ValidBduss;
        options.Stoken = ValidStoken;
        options.TransportMode = transportMode;

        return new TiebaClientSession(
            options,
            httpCore,
            wsCore ?? new StubWsCore(),
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

    private static GetDislikeListResIdl CreateDislikeForumsResponse(ulong fid, string fname, bool hasMore)
    {
        return new GetDislikeListResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new GetDislikeListResIdl.Types.DataRes
            {
                HasMore = hasMore ? 1 : 0,
                CurPage = 1,
                ForumList =
                {
                    new global::ForumList
                    {
                        ForumId = (long)fid,
                        ForumName = fname,
                        MemberCount = 1,
                        PostNum = 2,
                        ThreadNum = 3
                    }
                }
            }
        };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string WebGetResponse { get; init; } = "{}";
        public string AppFormResponse { get; init; } = "{}";
        public byte[] AppProtoResponse { get; init; } = [];
        public string CustomResponse { get; init; } = "{}";
        public string WebFormResponse { get; init; } = "{}";

        public int SendWebGetCalls { get; private set; }
        public int SendAppFormCalls { get; private set; }
        public int SendAppProtoCalls { get; private set; }
        public int SendWebFormCalls { get; private set; }
        public int SendCustomCalls { get; private set; }
        public CancellationToken LastAppFormCancellationToken { get; private set; }
        public CancellationToken LastCustomCancellationToken { get; private set; }
        public CancellationToken LastWebFormCancellationToken { get; private set; }
        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];
        public List<KeyValuePair<string, string>> LastWebFormData { get; private set; } = [];
        public HttpRequestMessage? LastCustomRequest { get; private set; }

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            SendCustomCalls++;
            LastCustomCancellationToken = cancellationToken;
            LastCustomRequest = requestFactory();
            return Task.FromResult(CustomResponse);
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            SendAppFormCalls++;
            LastAppFormUri = uri;
            LastAppFormCancellationToken = cancellationToken;
            LastAppFormData = [.. data];
            return Task.FromResult(AppFormResponse);
        }

        public string GetAppFormValue(string key) => LastAppFormData.Last(entry => entry.Key == key).Value;

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
            LastWebFormCancellationToken = cancellationToken;
            LastWebFormData = [.. data];
            return Task.FromResult(WebFormResponse);
        }
    }

    private sealed class StubWsCore : ITiebaWsCore
    {
        public Exception? ConnectException { get; init; }
        public byte[] SendResponsePayload { get; init; } = [];
        public int ConnectCalls { get; private set; }
        public int SendCalls { get; private set; }

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

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default)
        {
            SendCalls++;
            return Task.FromResult(new WSRes
            {
                Payload = new WSRes.Types.Payload
                {
                    Data = ByteString.CopyFrom(SendResponsePayload)
                }
            });
        }

        public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
