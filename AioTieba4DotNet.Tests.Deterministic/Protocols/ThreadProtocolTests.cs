#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Protocols;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public class ThreadProtocolTests
{
    private const string CanonicalSafeForumName = "lol欧服";
    private const ulong SafeForumId = 3581744;
    private const long SafeThreadId = 10377929712;
    private const long SafePostId = 153071185710;
    private const long SafeCommentId = 153071185799;
    private const long ThreadAuthorId = 11;
    private const long CommentAuthorId = 22;
    private const long ReplyToUserId = 33;
    private static readonly string ValidBduss = new('b', 192);
    private static readonly string ValidStoken = new('s', 64);

    [TestMethod]
    public async Task GetThreadsAsync_UsesWebSocketWhenAvailable_AndMapsTabDictionary()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore { Response = CreateWsResponse(CreateThreadsResponse().ToByteArray()) };
        var protocol = CreateProtocol(httpCore, wsCore);

        var result = await protocol.GetThreadsAsync(
            CanonicalSafeForumName,
            2,
            20,
            ThreadSortType.Create,
            true);

        var request = FrsPageReqIdl.Parser.ParseFrom(wsCore.LastRequestData);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, wsCore.SendCalls);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(CanonicalSafeForumName, request.Data.Kw);
        Assert.AreEqual(2, request.Data.Pn);
        Assert.AreEqual(20, request.Data.Rn);
        Assert.AreEqual(25, request.Data.RnNeed);
        Assert.AreEqual((int)ThreadSortType.Create, request.Data.SortType);
        Assert.AreEqual(1, request.Data.IsGood);
        Assert.AreEqual(CanonicalSafeForumName, result.Forum.Fname);
        Assert.AreEqual(2, result.Page.CurrentPage);
        Assert.AreEqual(101, result.TabDictionary["全部"]);
        Assert.AreEqual(202, result.TabDictionary["攻略"]);
        Assert.AreEqual(ThreadAuthorId, result.Objs[0].AuthorId);
        Assert.AreEqual("thread-author", result.Objs[0].User?.UserName);
    }

    [TestMethod]
    public async Task GetThreadsAsync_WsUnavailable_FallsBackToHttp()
    {
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateThreadsResponse().ToByteArray() };
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        var protocol = CreateProtocol(httpCore, wsCore);

        var result = await protocol.GetThreadsAsync(
            CanonicalSafeForumName,
            1,
            10,
            ThreadSortType.Reply,
            false);

        var request = FrsPageReqIdl.Parser.ParseFrom(httpCore.LastAppProtoRequestData);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(0, wsCore.SendCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual(CanonicalSafeForumName, request.Data.Kw);
        Assert.AreEqual(10, request.Data.Rn);
        Assert.AreEqual((int)ThreadSortType.Reply, request.Data.SortType);
        Assert.IsFalse(result.HasMore == false && result.Objs.Count == 0,
            "Fallback returned an empty result unexpectedly.");
    }

    [TestMethod]
    public async Task GetPostsAsync_UsesWebSocket_AndPreservesCommentPreviewParameters()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore { Response = CreateWsResponse(CreatePostsResponse().ToByteArray()) };
        var protocol = CreateProtocol(httpCore, wsCore);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetPostsAsync(
            SafeThreadId,
            3,
            15,
            PostSortType.Hot,
            true,
            true,
            2,
            true,
            cts.Token);

        var request = PbPageReqIdl.Parser.ParseFrom(wsCore.LastRequestData);
        var previewComment = result.Objs[0].Comments[0];

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, wsCore.SendCalls);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(cts.Token, wsCore.LastCancellationToken);
        Assert.AreEqual(SafeThreadId, request.Data.Kz);
        Assert.AreEqual(3, request.Data.Pn);
        Assert.AreEqual(15, request.Data.Rn);
        Assert.AreEqual((int)PostSortType.Hot, request.Data.R);
        Assert.AreEqual(1, request.Data.Lz);
        Assert.AreEqual(1, request.Data.WithFloor);
        Assert.AreEqual(2, request.Data.FloorRn);
        Assert.AreEqual(1, request.Data.FloorSortType);
        Assert.AreEqual(1u, result.Objs[0].Floor);
        Assert.IsTrue(result.Objs[0].IsThreadAuthor);
        Assert.HasCount(1, result.Objs[0].Comments);
        Assert.AreEqual(ReplyToUserId, previewComment.ReplyToId);
        Assert.AreEqual("preview reply", previewComment.Text);
        Assert.AreEqual(SafeThreadId, previewComment.Tid);
        Assert.AreEqual(result.Objs[0].Pid, previewComment.Ppid);
        Assert.AreEqual("comment-author", previewComment.User?.UserName);
    }

    [TestMethod]
    public async Task GetCommentsAsync_WsUnavailable_FallsBackToHttp_AndUsesSpidWhenRequested()
    {
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateCommentsResponse().ToByteArray() };
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        var protocol = CreateProtocol(httpCore, wsCore);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetCommentsAsync(
            SafeThreadId,
            SafeCommentId,
            4,
            true,
            cts.Token);

        var request = PbFloorReqIdl.Parser.ParseFrom(httpCore.LastAppProtoRequestData);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(0, wsCore.SendCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
        Assert.AreEqual(SafeThreadId, request.Data.Kz);
        Assert.AreEqual(0, request.Data.Pid);
        Assert.AreEqual(SafeCommentId, request.Data.Spid);
        Assert.AreEqual(4, request.Data.Pn);
        Assert.AreEqual(SafePostId, result.Post.Pid);
        Assert.AreEqual(ReplyToUserId, result.Objs[0].ReplyToId);
        Assert.AreEqual("deep comment", result.Objs[0].Text);
        Assert.AreEqual(SafeThreadId, result.Objs[0].Tid);
    }

    [TestMethod]
    public async Task GetCommentsAsync_WsUnavailable_FallsBackToHttp_AndUsesPidWhenNotComment()
    {
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateCommentsResponse().ToByteArray() };
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        var protocol = CreateProtocol(httpCore, wsCore);
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetCommentsAsync(
            SafeThreadId,
            SafePostId,
            4,
            false,
            cts.Token);

        var request = PbFloorReqIdl.Parser.ParseFrom(httpCore.LastAppProtoRequestData);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(0, wsCore.SendCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
        Assert.AreEqual(SafeThreadId, request.Data.Kz);
        Assert.AreEqual(SafePostId, request.Data.Pid);
        Assert.AreEqual(0, request.Data.Spid);
        Assert.AreEqual(4, request.Data.Pn);
        Assert.AreEqual(SafePostId, result.Post.Pid);
        Assert.AreEqual(ReplyToUserId, result.Objs[0].ReplyToId);
        Assert.AreEqual("deep comment", result.Objs[0].Text);
        Assert.AreEqual(SafeThreadId, result.Objs[0].Tid);
    }

    [TestMethod]
    public async Task GetTabMapAsync_UsesWebSocketWhenAvailable_AndMapsStandaloneTabMap()
    {
        var httpCore = new RecordingHttpCore();
        var wsCore = new RecordingWsCore { Response = CreateWsResponse(CreateTabMapResponse(true).ToByteArray()) };
        var protocol = CreateAuthenticatedProtocol(httpCore, wsCore);

        var result = await protocol.GetTabMapAsync(CanonicalSafeForumName);

        var request = SearchPostForumReqIdl.Parser.ParseFrom(wsCore.LastRequestData);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, wsCore.SendCalls);
        Assert.AreEqual(0, httpCore.SendAppProtoCalls);
        Assert.AreEqual(CanonicalSafeForumName, request.Data.Fname);
        Assert.AreEqual(ValidBduss, request.Data.Common.BDUSS);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(101, result["全部"]);
        Assert.AreEqual(202, result["攻略"]);
    }

    [TestMethod]
    public async Task GetTabMapAsync_WsUnavailable_FallsBackToHttp_AndHandlesMissingMatch()
    {
        var httpCore = new RecordingHttpCore { AppProtoResponse = CreateTabMapResponse(false).ToByteArray() };
        var wsCore = new RecordingWsCore { ConnectException = new WebSocketException("offline") };
        var protocol = CreateAuthenticatedProtocol(httpCore, wsCore);

        var result = await protocol.GetTabMapAsync((ulong)SafeForumId);

        var request = SearchPostForumReqIdl.Parser.ParseFrom(httpCore.LastAppProtoRequestData);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(0, wsCore.SendCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual(CanonicalSafeForumName, request.Data.Fname);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetRecoversAsync_UsesWebGet_AndMapsRecoverEntries()
    {
        var httpCore = new RecordingHttpCore { WebGetResponse = CreateRecoversResponse().ToString() };
        var protocol = CreateAuthenticatedProtocol(httpCore, new RecordingWsCore());
        using var cts = new CancellationTokenSource();

        var result = await protocol.GetRecoversAsync(CanonicalSafeForumName, 2, 10, 99, cts.Token);

        Assert.AreEqual(1, httpCore.SendWebGetCalls);
        Assert.AreEqual(cts.Token, httpCore.LastWebGetCancellationToken);
        Assert.AreEqual("/mo/q/manage/getRecoverList", httpCore.LastWebGetUri.AbsolutePath);
        Assert.AreEqual("3581744", GetQueryValue(httpCore.LastWebGetParameters, "forum_id"));
        Assert.AreEqual("2", GetQueryValue(httpCore.LastWebGetParameters, "pn"));
        Assert.AreEqual("10", GetQueryValue(httpCore.LastWebGetParameters, "rn"));
        Assert.AreEqual("99", GetQueryValue(httpCore.LastWebGetParameters, "uid"));
        Assert.AreEqual(2, result.Page.CurrentPage);
        Assert.AreEqual(10, result.Page.PageSize);
        Assert.IsTrue(result.HasMore);
        Assert.HasCount(2, result);
        Assert.AreEqual(SafeThreadId, result[0].Tid);
        Assert.AreEqual(0L, result[0].Pid);
        Assert.AreEqual("thread-author", result[0].User.UserName);
        Assert.AreEqual("portrait-thread", result[0].User.Portrait);
        Assert.IsTrue(result[0].IsHide);
        Assert.AreEqual("moderator-a", result[0].OperatorShowName);
        Assert.AreEqual(SafePostId, result[1].Pid);
        Assert.IsTrue(result[1].IsFloor);
        Assert.IsFalse(result[1].IsHide);
    }

    [TestMethod]
    public async Task GetRecoversAsync_RejectsInvalidPageArgumentsBeforeForumLookup()
    {
        var forum = new CountingForumProtocol();
        var protocol = new ThreadProtocol(
            new TiebaOperationDispatcher(new TiebaClientSession(
                new TiebaOptions { Bduss = ValidBduss, Stoken = ValidStoken, TransportMode = TiebaTransportMode.Http },
                new RecordingHttpCore(),
                new RecordingWsCore())),
            forum);

        await AssertThrowsAsync<ArgumentOutOfRangeException>(() =>
            protocol.GetRecoversAsync(CanonicalSafeForumName, 0, 10, null));
        await AssertThrowsAsync<ArgumentOutOfRangeException>(() =>
            protocol.GetRecoversAsync(CanonicalSafeForumName, 1, 51, null));

        Assert.AreEqual(0, forum.GetFidCalls);
    }

    [TestMethod]
    public async Task GetRecoversAsync_RejectsInvalidForumAndOptionalUserArgumentsBeforeForumLookup()
    {
        var forum = new CountingForumProtocol();
        var protocol = new ThreadProtocol(
            new TiebaOperationDispatcher(new TiebaClientSession(
                new TiebaOptions { Bduss = ValidBduss, Stoken = ValidStoken, TransportMode = TiebaTransportMode.Http },
                new RecordingHttpCore(),
                new RecordingWsCore())),
            forum);

        await AssertThrowsAsync<ArgumentException>(() => protocol.GetRecoversAsync(" ", 1, 10, null));
        await AssertThrowsAsync<ArgumentOutOfRangeException>(() =>
            protocol.GetRecoversAsync(CanonicalSafeForumName, 1, 0, null));
        await AssertThrowsAsync<ArgumentOutOfRangeException>(() =>
            protocol.GetRecoversAsync(CanonicalSafeForumName, 1, 10, 0));
        await AssertThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetRecoversAsync(0UL, 1, 10, null));

        Assert.AreEqual(0, forum.GetFidCalls);
    }

    [TestMethod]
    public async Task GetRecoverInfoAsync_UsesWebGet_AndMapsContentDetails()
    {
        var httpCore = new RecordingHttpCore { WebGetResponse = CreateRecoverInfoResponse().ToString() };
        var protocol = CreateAuthenticatedProtocol(httpCore, new RecordingWsCore());

        var result = await protocol.GetRecoverInfoAsync(CanonicalSafeForumName, SafeThreadId, SafePostId);

        Assert.AreEqual(1, httpCore.SendWebGetCalls);
        Assert.AreEqual("/mo/q/bawu/getRecoverInfo", httpCore.LastWebGetUri.AbsolutePath);
        Assert.AreEqual("3581744", GetQueryValue(httpCore.LastWebGetParameters, "forum_id"));
        Assert.AreEqual(SafeThreadId.ToString(), GetQueryValue(httpCore.LastWebGetParameters, "thread_id"));
        Assert.AreEqual(SafePostId.ToString(), GetQueryValue(httpCore.LastWebGetParameters, "post_id"));
        Assert.AreEqual("2", GetQueryValue(httpCore.LastWebGetParameters, "sub_type"));
        Assert.AreEqual("Recover detail title", result.Title);
        Assert.AreEqual(SafeThreadId, result.Tid);
        Assert.AreEqual(SafePostId, result.Pid);
        Assert.AreEqual("recover-author", result.User.UserName);
        Assert.AreEqual("recover-portrait", result.User.Portrait);
        Assert.Contains("Recover detail title", result.Text);
        Assert.AreEqual("第一段第二段", result.Content.Text);
        Assert.HasCount(1, result.Content.Images);
        Assert.AreEqual("1234567890abcdef1234567890abcdef", result.Content.Images[0].Hash);
    }

    [TestMethod]
    public async Task GetRecoverInfoAsync_RejectsMissingThreadIdBeforeForumLookup()
    {
        var forum = new CountingForumProtocol();
        var protocol = new ThreadProtocol(
            new TiebaOperationDispatcher(new TiebaClientSession(
                new TiebaOptions { Bduss = ValidBduss, Stoken = ValidStoken, TransportMode = TiebaTransportMode.Http },
                new RecordingHttpCore(),
                new RecordingWsCore())),
            forum);

        await AssertThrowsAsync<ArgumentOutOfRangeException>(() =>
            protocol.GetRecoverInfoAsync(CanonicalSafeForumName, 0, 0));

        Assert.AreEqual(0, forum.GetFidCalls);
    }

    [TestMethod]
    public async Task GetRecoverInfoAsync_RejectsNegativePostIdBeforeForumLookup()
    {
        var forum = new CountingForumProtocol();
        var protocol = new ThreadProtocol(
            new TiebaOperationDispatcher(new TiebaClientSession(
                new TiebaOptions { Bduss = ValidBduss, Stoken = ValidStoken, TransportMode = TiebaTransportMode.Http },
                new RecordingHttpCore(),
                new RecordingWsCore())),
            forum);

        await AssertThrowsAsync<ArgumentOutOfRangeException>(() =>
            protocol.GetRecoverInfoAsync(CanonicalSafeForumName, SafeThreadId, -1));

        Assert.AreEqual(0, forum.GetFidCalls);
    }

    [TestMethod]
    public async Task AgreeAsync_PropagatesCancellationToken_ToTransport()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":""}
                              """
        };
        using var session = new TiebaClientSession(
            new TiebaOptions { Bduss = ValidBduss, Stoken = ValidStoken, TransportMode = TiebaTransportMode.Http },
            httpCore,
            new RecordingWsCore(),
            _ => Task.FromResult("tbs-123"));
        var protocol = new ThreadProtocol(new TiebaOperationDispatcher(session), new StubForumProtocol());
        using var cts = new CancellationTokenSource();

        var result = await protocol.AgreeAsync(SafeThreadId, SafePostId, false, false,
            false, cts.Token);

        Assert.IsTrue(result);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
    }

    private static ThreadProtocol CreateProtocol(RecordingHttpCore httpCore, RecordingWsCore wsCore)
    {
        var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Auto },
            httpCore,
            wsCore);

        return new ThreadProtocol(
            new TiebaOperationDispatcher(session),
            new StubForumProtocol());
    }

    private static ThreadProtocol CreateAuthenticatedProtocol(RecordingHttpCore httpCore, RecordingWsCore wsCore)
    {
        var session = new TiebaClientSession(
            new TiebaOptions { Bduss = ValidBduss, Stoken = ValidStoken, TransportMode = TiebaTransportMode.Auto },
            httpCore,
            wsCore);

        return new ThreadProtocol(
            new TiebaOperationDispatcher(session),
            new StubForumProtocol());
    }

    private static WSRes CreateWsResponse(byte[] payload)
    {
        return new WSRes { Payload = new WSRes.Types.Payload { Data = ByteString.CopyFrom(payload) } };
    }

    private static FrsPageResIdl CreateThreadsResponse()
    {
        return new FrsPageResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new FrsPageResIdl.Types.DataRes
            {
                Forum =
                    new FrsPageResIdl.Types.DataRes.Types.ForumInfo
                    {
                        Id = (long)SafeForumId,
                        Name = CanonicalSafeForumName,
                        FirstClass = "游戏",
                        SecondClass = "网络游戏",
                        MemberNum = 46807,
                        PostNum = 526788,
                        ThreadNum = 14003
                    },
                Page = new Page
                {
                    CurrentPage = 2,
                    PageSize = 20,
                    TotalPage = 9,
                    TotalCount = 180,
                    HasMore = 1
                },
                NavTabInfo =
                    new FrsPageResIdl.Types.DataRes.Types.NavTabInfo
                    {
                        Tab =
                        {
                            new FrsTabInfo { TabId = 101, TabName = "全部" },
                            new FrsTabInfo { TabId = 202, TabName = "攻略" }
                        }
                    },
                ForumRule = new FrsPageResIdl.Types.DataRes.Types.ForumRuleStatus { HasForumRule = 1 },
                ThreadList =
                {
                    new ThreadInfo
                    {
                        Id = SafeThreadId,
                        Title = "Safe thread title",
                        ReplyNum = 42,
                        ViewNum = 100,
                        LastTimeInt = 1700000000,
                        IsGood = 1,
                        FirstPostId = SafePostId,
                        CreateTime = 1699999000,
                        AuthorId = ThreadAuthorId,
                        TabId = 101,
                        Author = CreateUser(ThreadAuthorId, "thread-author"),
                        FirstPostContent = { CreateTextContent("thread body") },
                        Agree = new Agree { AgreeNum = 9, DisagreeNum = 1 }
                    }
                },
                UserList = { CreateUser(ThreadAuthorId, "thread-author") }
            }
        };
    }

    private static SearchPostForumResIdl CreateTabMapResponse(bool includeTabs)
    {
        var response = new SearchPostForumResIdl
        {
            Error = new Error { Errorno = 0 }, Data = new SearchPostForumResIdl.Types.DataRes()
        };

        if (includeTabs)
        {
            response.Data.ExactMatch = new SearchPostForumResIdl.Types.DataRes.Types.SearchForum
            {
                ForumId = (long)SafeForumId, ForumName = CanonicalSafeForumName
            };
            response.Data.ExactMatch.TabInfo.Add(new FrsTabInfo { TabId = 101, TabName = "全部" });
            response.Data.ExactMatch.TabInfo.Add(new FrsTabInfo { TabId = 202, TabName = "攻略" });
        }

        return response;
    }

    private static JObject CreateRecoversResponse()
    {
        return JObject.Parse(
            $$"""
              {
                "no": 0,
                "error": "",
                "data": {
                  "thread_list": [
                    {
                      "thread_info": {
                        "tid": "{{SafeThreadId}}",
                        "abstract": "hidden thread text",
                        "portrait": "portrait-thread?foo=bar",
                        "user_name": "thread-author",
                        "user_nickname": "Thread Author"
                      },
                      "post_info": null,
                      "is_foor": 0,
                      "is_frs_mask": "1",
                      "op_info": {
                        "name": "moderator-a",
                        "time": "1711111111"
                      }
                    },
                    {
                      "thread_info": {
                        "tid": "{{SafeThreadId}}",
                        "abstract": "reply thread text",
                        "portrait": "portrait-thread?foo=bar",
                        "user_name": "thread-author",
                        "user_nickname": "Thread Author"
                      },
                      "post_info": {
                        "abstract": "reply abstract",
                        "pid": "{{SafePostId}}",
                        "portrait": "portrait-post?foo=bar",
                        "user_name": "post-author",
                        "user_nickname": "Post Author"
                      },
                      "is_foor": 1,
                      "is_frs_mask": "0",
                      "op_info": {
                        "name": "moderator-b",
                        "time": "1711112222"
                      }
                    }
                  ],
                  "page": {
                    "rn": 10,
                    "pn": 2,
                    "has_more": 1
                  }
                }
              }
              """);
    }

    private static JObject CreateRecoverInfoResponse()
    {
        return JObject.Parse(
            $$"""
              {
                "no": 0,
                "error": "",
                "data": {
                  "thread_info": {
                    "title": "Recover detail title",
                    "thread_id": {{SafeThreadId}},
                    "post_id": {{SafePostId}},
                    "content_detail": [
                      { "type": 1, "value": "第一段" },
                      { "type": 3, "value": "ignored" },
                      { "type": 1, "value": "第二段" }
                    ],
                    "all_pics": [
                      {
                        "url": "https://imgsrc.baidu.com/forum/pic/item/1234567890abcdef1234567890abcdef.jpg",
                        "width": 640,
                        "height": 480
                      }
                    ]
                  },
                  "user_info": {
                    "portrait": "recover-portrait?foo=bar",
                    "user_name": "recover-author",
                    "show_nickname": "Recover Author"
                  }
                }
              }
              """);
    }

    private static PbPageResIdl CreatePostsResponse()
    {
        return new PbPageResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new PbPageResIdl.Types.DataRes
            {
                Forum =
                    new SimpleForum
                    {
                        Id = (long)SafeForumId,
                        Name = CanonicalSafeForumName,
                        FirstClass = "游戏",
                        SecondClass = "网络游戏",
                        MemberNum = 46807,
                        PostNum = 526788
                    },
                Page = new Page
                {
                    CurrentPage = 3,
                    PageSize = 15,
                    TotalPage = 7,
                    TotalCount = 90,
                    HasMore = 1
                },
                Thread =
                    new ThreadInfo
                    {
                        Id = SafeThreadId,
                        Title = "Safe thread title",
                        FirstPostId = SafePostId,
                        AuthorId = ThreadAuthorId,
                        Author = CreateUser(ThreadAuthorId, "thread-author"),
                        FirstPostContent = { CreateTextContent("thread body") }
                    },
                PostList =
                {
                    new Post
                    {
                        Id = SafePostId,
                        Floor = 1,
                        Time = 1699999999,
                        AuthorId = ThreadAuthorId,
                        Author = CreateUser(ThreadAuthorId, "thread-author"),
                        Content = { CreateTextContent("post body") },
                        SubPostNumber = 1,
                        SubPostList =
                            new Post.Types.SubPost
                            {
                                SubPostList =
                                {
                                    CreateReplyComment(SafeCommentId, CommentAuthorId, ReplyToUserId,
                                        "preview reply")
                                }
                            },
                        Agree = new Agree { AgreeNum = 5, DisagreeNum = 0 }
                    }
                },
                UserList =
                {
                    CreateUser(ThreadAuthorId, "thread-author"), CreateUser(CommentAuthorId, "comment-author")
                }
            }
        };
    }

    private static PbFloorResIdl CreateCommentsResponse()
    {
        return new PbFloorResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new PbFloorResIdl.Types.DataRes
            {
                Forum =
                    new SimpleForum
                    {
                        Id = (long)SafeForumId,
                        Name = CanonicalSafeForumName,
                        FirstClass = "游戏",
                        SecondClass = "网络游戏",
                        MemberNum = 46807,
                        PostNum = 526788
                    },
                Page = new Page { CurrentPage = 4, PageSize = 10, TotalPage = 5, TotalCount = 41 },
                Thread = new ThreadInfo
                {
                    Id = SafeThreadId,
                    Title = "Safe thread title",
                    FirstPostId = SafePostId,
                    AuthorId = ThreadAuthorId,
                    Author = CreateUser(ThreadAuthorId, "thread-author"),
                    FirstPostContent = { CreateTextContent("thread body") }
                },
                Post = new Post
                {
                    Id = SafePostId,
                    Floor = 1,
                    Time = 1699999999,
                    AuthorId = ThreadAuthorId,
                    Author = CreateUser(ThreadAuthorId, "thread-author"),
                    Content = { CreateTextContent("post body") }
                },
                SubpostList = { CreateReplyComment(SafeCommentId, CommentAuthorId, ReplyToUserId, "deep comment") }
            }
        };
    }

    private static User CreateUser(long userId, string userName)
    {
        return new User
        {
            Id = userId,
            Name = userName,
            NameShow = userName,
            Portrait = $"portrait-{userId}",
            LevelId = 12
        };
    }

    private static SubPostList CreateReplyComment(long commentId, long authorId, long replyToUserId, string text)
    {
        return new SubPostList
        {
            Id = commentId,
            AuthorId = authorId,
            Author = CreateUser(authorId, "comment-author"),
            Time = 1699999999,
            Content =
            {
                CreateTextContent("回复 "),
                new PbContent { Type = 2, Text = "@target", Uid = replyToUserId },
                CreateTextContent($" :{text}")
            },
            Agree = new Agree { AgreeNum = 7, DisagreeNum = 1 }
        };
    }

    private static PbContent CreateTextContent(string text)
    {
        return new PbContent { Type = 0, Text = text };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string AppFormResponse { get; init; } = "{}";

        public string WebGetResponse { get; init; } = "{}";

        public byte[] AppProtoResponse { get; init; } = [];

        public int SendAppFormCalls { get; private set; }

        public int SendAppProtoCalls { get; private set; }

        public int SendWebGetCalls { get; private set; }

        public byte[] LastAppProtoRequestData { get; private set; } = [];

        public Uri LastWebGetUri { get; private set; } = new("https://tieba.baidu.com");

        public List<KeyValuePair<string, string>> LastWebGetParameters { get; private set; } = [];

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
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

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
            LastAppProtoRequestData = data;
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
            throw new NotImplementedException();
        }
    }

    private sealed class RecordingWsCore : ITiebaWsCore
    {
        public int ConnectCalls { get; private set; }

        public int SendCalls { get; private set; }

        public Exception? ConnectException { get; init; }

        public WSRes Response { get; init; } = new();

        public byte[] LastRequestData { get; private set; } = [];

        public CancellationToken LastCancellationToken { get; private set; }

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

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default)
        {
            SendCalls++;
            LastRequestData = data;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Response);
        }

        public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
            return Task.FromResult(SafeForumId);
        }

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CanonicalSafeForumName);
        }

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default)
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

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CountingForumProtocol : IForumProtocol
    {
        public int GetFidCalls { get; private set; }

        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
        {
            GetFidCalls++;
            return Task.FromResult(SafeForumId);
        }

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CanonicalSafeForumName);
        }

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default)
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

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private static string GetQueryValue(IReadOnlyList<KeyValuePair<string, string>> parameters, string key)
    {
        return parameters.Single(pair => pair.Key == key).Value;
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
}
