#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.GetGroupMsg;
using AioTieba4DotNet.Api.GetUserForumInfo;
using AioTieba4DotNet.Api.GetUserContents;
using AioTieba4DotNet.Api.Login;
using AioTieba4DotNet.Api.SendMsg;
using AioTieba4DotNet.Api.UnlikeForum;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api;

[TestClass]
public sealed class MessagesAndUserContentApiTests
{
    [TestMethod]
    public async Task GetGroupMsg_RequestAsync_PacksRequestAndMapsMessages()
    {
        var account = CreateAccount();
        var wsCore = new RecordingWsCore { Account = account };
        wsCore.Response = CreateWsResponse(new GetGroupMsgResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty },
            Data = new GetGroupMsgResIdl.Types.DataRes
            {
                GroupInfo =
                {
                    new GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg
                    {
                        GroupInfo =
                            new GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.GroupInfo
                            {
                                GroupId = 88, GroupType = 6
                            },
                        MsgList =
                        {
                            new GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.MsgInfo
                            {
                                MsgId = 1234,
                                MsgType = 1,
                                Content = "hello",
                                CreateTime = 1712345678,
                                UserInfo =
                                    new GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.
                                        MsgInfo.Types.UserInfo
                                        {
                                            UserId = 42, UserName = "sender", Portrait = "tb.1.sender?012345678901"
                                        }
                            }
                        }
                    }
                }
            }
        }.ToByteArray());
        var api = new GetGroupMsg(wsCore);
        using var cts = new CancellationTokenSource();

        var result = await api.RequestAsync([88, 99], [77], 2, cts.Token);

        var request = GetGroupMsgReqIdl.Parser.ParseFrom(wsCore.LastRequestData);

        Assert.AreEqual(202003, wsCore.LastCmd);
        Assert.AreEqual(cts.Token, wsCore.LastCancellationToken);
        StringAssert.Contains(request.Cuid, account.Cuid);
        Assert.AreEqual("2", request.Data.Gettype);
        Assert.AreEqual(2, request.Data.GroupMids.Count);
        Assert.AreEqual(88L, request.Data.GroupMids[0].GroupId);
        Assert.AreEqual(77L, request.Data.GroupMids[0].LastMsgId);
        Assert.AreEqual(99L, request.Data.GroupMids[1].GroupId);
        Assert.AreEqual(0L, request.Data.GroupMids[1].LastMsgId);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(88L, result[0].GroupId);
        Assert.AreEqual(1, result[0].Messages.Count);
        Assert.AreEqual("hello", result[0].Messages[0].Text);
        Assert.AreEqual(42L, result[0].Messages[0].User.UserId);
        Assert.AreEqual("tb.1.sender", result[0].Messages[0].User.Portrait);
    }

    [TestMethod]
    public async Task GetGroupMsg_RequestAsync_RequiresAuthenticatedAccount()
    {
        var api = new GetGroupMsg(new RecordingWsCore());

        var exception = await ThrowsAsync<InvalidOperationException>(() => api.RequestAsync([1], [0], 1));

        StringAssert.Contains(exception.Message, "authenticated account");
    }

    [TestMethod]
    public async Task SendMsg_RequestAsync_PacksPayloadAndReturnsMessageId()
    {
        var wsCore = new RecordingWsCore { Account = CreateAccount() };
        wsCore.Response = CreateWsResponse(new CommitPersonalMsgResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty },
            Data = new CommitPersonalMsgResIdl.Types.DataRes { MsgId = 998877 }
        }.ToByteArray());
        var api = new SendMsg(wsCore);

        var messageId = await api.RequestAsync(12345, "private hi", 56789);

        var request = CommitPersonalMsgReqIdl.Parser.ParseFrom(wsCore.LastRequestData);

        Assert.AreEqual(205001, wsCore.LastCmd);
        Assert.AreEqual(12345L, request.Data.ToUid);
        Assert.AreEqual("private hi", request.Data.Content);
        Assert.AreEqual(1, request.Data.MsgType);
        Assert.AreEqual(56789L, request.Data.RecordId);
        Assert.AreEqual(998877L, messageId);
    }

    [TestMethod]
    public async Task SendMsg_RequestAsync_ThrowsWhenBlockInfoReportsFailure()
    {
        var wsCore = new RecordingWsCore { Account = CreateAccount() };
        wsCore.Response = CreateWsResponse(new CommitPersonalMsgResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty },
            Data = new CommitPersonalMsgResIdl.Types.DataRes
            {
                BlockInfo = new CommitPersonalMsgResIdl.Types.DataRes.Types.BlockInfo
                {
                    BlockErrno = 12, BlockErrmsg = "blocked"
                }
            }
        }.ToByteArray());
        var api = new SendMsg(wsCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => api.RequestAsync(12345, "private hi", 1));

        Assert.AreEqual(12, exception.Code);
        StringAssert.Contains(exception.Message, "blocked");
    }

    [TestMethod]
    public async Task Login_RequestAsync_PacksFormAndNormalizesPortrait()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user":{"id":42,"portrait":"tb.1.login?012345678901","name":"login-user"},"anti":{"tbs":"tbs-123"}}
                              """
        };
        var api = new Login(httpCore);
        using var cts = new CancellationTokenSource();

        var (user, tbs) = await api.RequestAsync(cts.Token);

        Assert.AreEqual("/c/s/login", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
        Assert.AreEqual(Const.MainVersion, httpCore.GetAppFormValue("_client_version"));
        Assert.AreEqual(httpCore.Account!.Bduss, httpCore.GetAppFormValue("bdusstoken"));
        Assert.AreEqual(42L, user.UserId);
        Assert.AreEqual("tb.1.login", user.Portrait);
        Assert.AreEqual("login-user", user.UserName);
        Assert.AreEqual("tbs-123", tbs);
    }

    [TestMethod]
    public async Task UnlikeForum_RequestAsync_PacksFormAndHandlesEmbeddedErrorObject()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","error":{"errno":0,"errmsg":""}}
                              """
        };
        httpCore.Account!.Tbs = "tbs-456";
        var api = new UnlikeForum(httpCore);

        var success = await api.RequestAsync(7356044);

        Assert.IsTrue(success);
        Assert.AreEqual("/c/c/forum/unlike", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(httpCore.Account.Bduss, httpCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual(Const.MainVersion, httpCore.GetAppFormValue("_client_version"));
        Assert.AreEqual("7356044", httpCore.GetAppFormValue("fid"));
        Assert.AreEqual("tbs-456", httpCore.GetAppFormValue("tbs"));
    }

    [TestMethod]
    public async Task UnlikeForum_RequestAsync_ThrowsWhenEmbeddedErrorIsNonZero()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","error":{"errno":325,"errmsg":"denied"}}
                              """
        };
        var api = new UnlikeForum(httpCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => api.RequestAsync(7356044));

        Assert.AreEqual(325, exception.Code);
        StringAssert.Contains(exception.Message, "denied");
    }

    [TestMethod]
    public async Task UnlikeForum_RequestAsync_UsesEmptyFallbackMessageWhenErrmsgMissing()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","error":{"errno":325}}
                              """
        };
        var api = new UnlikeForum(httpCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => api.RequestAsync(7356044));

        Assert.AreEqual(325, exception.Code);
        StringAssert.Contains(exception.Message, "325");
    }

    [TestMethod]
    public async Task GetUserForumInfo_RequestAsync_PacksFormAndParsesErrorFallbackShape()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error":"","data":{"user_info":{"id":5,"portrait":"tb.1.api?012345678901","name":"api-user"},"forum_info":{"forum_name":"forum-name"},"user_forum_info":{"is_follow":1}}}
                              """
        };
        var api = new GetUserForumInfo(httpCore);
        using var cts = new CancellationTokenSource();

        var result = await api.RequestAsync(7356044, "tb.1.safe", cts.Token);

        Assert.AreEqual("/c/f/forum/getUserForumLevelInfo", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(cts.Token, httpCore.LastAppFormCancellationToken);
        Assert.AreEqual(httpCore.Account!.Bduss, httpCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual("7356044", httpCore.GetAppFormValue("forum_id"));
        Assert.AreEqual("tb.1.safe", httpCore.GetAppFormValue("friend_portrait"));
        Assert.AreEqual(5L, result.User.UserId);
        Assert.AreEqual("tb.1.api", result.User.Portrait);
        Assert.IsTrue(result.IsFollow);
        Assert.AreEqual("forum-name", result.Fname);
    }

    [TestMethod]
    public async Task GetUserForumInfo_RequestAsync_UsesErrmsgFallbackAndDefaultsMissingData()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"errmsg":"","data":null}
                              """
        };
        var api = new GetUserForumInfo(httpCore);

        var result = await api.RequestAsync(1, "tb.1.safe");

        Assert.AreEqual(0L, result.User.UserId);
        Assert.AreEqual(string.Empty, result.Fname);
        Assert.IsFalse(result.IsFollow);
    }

    [TestMethod]
    public async Task GetUserForumInfo_RequestAsync_UsesErrmsgWhenItIsTheOnlyErrorField()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"errmsg":"fallback","data":{}}
                              """
        };
        var api = new GetUserForumInfo(httpCore);

        var result = await api.RequestAsync(1, "tb.1.safe");

        Assert.AreEqual(0L, result.User.UserId);
        Assert.AreEqual(string.Empty, result.Fname);
    }

    [TestMethod]
    public async Task GetUserThreads_RequestHttpAsync_PacksProtoAndParsesEmptyResult()
    {
        var httpCore = new RecordingHttpCore { Account = CreateAccount() };
        httpCore.AppProtoResponse = new UserPostResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty }, Data = new UserPostResIdl.Types.DataRes()
        }.ToByteArray();
        var api = new GetUserThreads(httpCore, new RecordingWsCore());
        using var cts = new CancellationTokenSource();

        var result = await api.RequestHttpAsync(42, 3, true, cts.Token);

        var request = UserPostReqIdl.Parser.ParseFrom(httpCore.LastAppProtoRequestData);

        Assert.AreEqual("/c/u/feed/userpost", httpCore.LastAppProtoUri?.AbsolutePath);
        Assert.AreEqual("cmd=303002", httpCore.LastAppProtoUri?.Query.TrimStart('?'));
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
        Assert.AreEqual(42, request.Data.UserId);
        Assert.AreEqual((uint)3, request.Data.Pn);
        Assert.AreEqual(1, (int)request.Data.IsThread);
        Assert.AreEqual(1, (int)request.Data.NeedContent);
        Assert.AreEqual(2, (int)request.Data.IsViewCard);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetUserThreads_RequestWsAsync_PacksProtoAndParsesResponse()
    {
        var wsCore = new RecordingWsCore { Account = CreateAccount() };
        wsCore.Response = CreateWsResponse(new UserPostResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty },
            Data = new UserPostResIdl.Types.DataRes
            {
                PostList =
                {
                    CreateUserPostList("Thread title", 8, "forum", 9, 10,
                        42, "author", "tb.1.author?012345678901", "body")
                }
            }
        }.ToByteArray());
        var api = new GetUserThreads(new RecordingHttpCore(), wsCore);

        var result = await api.RequestWsAsync(42, 1, false);

        var request = UserPostReqIdl.Parser.ParseFrom(wsCore.LastRequestData);

        Assert.AreEqual(303002, wsCore.LastCmd);
        Assert.AreEqual(42, request.Data.UserId);
        Assert.AreEqual(1, (int)request.Data.IsViewCard);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Thread title", result[0].Title);
        Assert.AreEqual("body", result[0].Contents.Text);
        Assert.AreEqual("author", result[0].User?.UserName);
    }

    [TestMethod]
    public async Task GetPosts_RequestHttpAsync_PacksProtoAndParsesNestedPosts()
    {
        var httpCore = new RecordingHttpCore { Account = CreateAccount() };
        var response = new UserPostResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty }, Data = new UserPostResIdl.Types.DataRes()
        };
        var postList = CreateUserPostList("ignored", 8, "forum", 9, 10,
            42, "author", "tb.1.author?012345678901", "body");
        postList.Content.Add(new PostInfoList.Types.PostInfoContent
        {
            PostId = 10,
            PostType = 1,
            CreateTime = 1711111111,
            PostContent = { new PostInfoList.Types.PostInfoContent.Types.Abstract { Type = 0, Text = "reply" } }
        });
        response.Data.PostList.Add(postList);
        httpCore.AppProtoResponse = response.ToByteArray();
        var api = new GetPosts(httpCore, new RecordingWsCore());
        using var cts = new CancellationTokenSource();

        var result = await api.RequestHttpAsync(42, 3, 20, "9.9.9", cts.Token);

        var request = UserPostReqIdl.Parser.ParseFrom(httpCore.LastAppProtoRequestData);

        Assert.AreEqual("/c/u/feed/userpost", httpCore.LastAppProtoUri?.AbsolutePath);
        Assert.AreEqual("cmd=303002", httpCore.LastAppProtoUri?.Query.TrimStart('?'));
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);
        Assert.AreEqual(42, request.Data.UserId);
        Assert.AreEqual((uint)3, request.Data.Pn);
        Assert.AreEqual((uint)20, request.Data.Rn);
        Assert.AreEqual("9.9.9", request.Data.Common.ClientVersion);
        Assert.AreEqual(1, (int)request.Data.NeedContent);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(8L, result[0].Fid);
        Assert.AreEqual(9L, result[0].Tid);
        Assert.AreEqual(1, result[0].Count);
        Assert.AreEqual(10, result[0][0].Pid);
        Assert.IsTrue(result[0][0].IsComment);
        Assert.AreEqual("reply", result[0][0].Contents.Text);
        Assert.AreEqual("author", result[0][0].User?.UserName);
        Assert.AreEqual("tb.1.author", result[0][0].User?.Portrait);
    }

    [TestMethod]
    public async Task GetPosts_RequestWsAsync_PacksProtoAndParsesEmptyResult()
    {
        var wsCore = new RecordingWsCore { Account = CreateAccount() };
        wsCore.Response = CreateWsResponse(new UserPostResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty }, Data = new UserPostResIdl.Types.DataRes()
        }.ToByteArray());
        var api = new GetPosts(new RecordingHttpCore(), wsCore);
        using var cts = new CancellationTokenSource();

        var result = await api.RequestWsAsync(84, 5, 40, "10.0.0", cts.Token);

        var request = UserPostReqIdl.Parser.ParseFrom(wsCore.LastRequestData);

        Assert.AreEqual(303002, wsCore.LastCmd);
        Assert.AreEqual(cts.Token, wsCore.LastCancellationToken);
        Assert.AreEqual(84, request.Data.UserId);
        Assert.AreEqual((uint)5, request.Data.Pn);
        Assert.AreEqual((uint)40, request.Data.Rn);
        Assert.AreEqual("10.0.0", request.Data.Common.ClientVersion);
        Assert.AreEqual(0, result.Count);
    }

    private static Account CreateAccount()
    {
        return new Account(new string('b', 192), new string('s', 64));
    }

    private static PostInfoList CreateUserPostList(string title, ulong forumId, string forumName, ulong threadId,
        ulong postId, long userId, string userName, string portrait, string contentText)
    {
        return new PostInfoList
        {
            Title = title,
            ForumId = forumId,
            ForumName = forumName,
            ThreadId = threadId,
            PostId = postId,
            UserId = userId,
            UserName = userName,
            UserPortrait = portrait,
            NameShow = userName,
            FirstPostContent = { new PbContent { Type = 0, Text = contentText } }
        };
    }

    private static WSRes CreateWsResponse(byte[] payload)
    {
        return new WSRes { Payload = new WSRes.Types.Payload { Data = ByteString.CopyFrom(payload) } };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string AppFormResponse { get; init; } = "{\"error_code\":0,\"error_msg\":\"\"}";
        public byte[] AppProtoResponse { get; set; } = [];

        public Account? Account { get; set; } = CreateAccount();

        public HttpClient HttpClient { get; } = new();

        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];
        public CancellationToken LastAppFormCancellationToken { get; private set; }
        public Uri? LastAppProtoUri { get; private set; }
        public byte[] LastAppProtoRequestData { get; private set; } = [];
        public CancellationToken LastAppProtoCancellationToken { get; private set; }

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
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            LastAppFormCancellationToken = cancellationToken;
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            LastAppProtoUri = uri;
            LastAppProtoRequestData = data;
            LastAppProtoCancellationToken = cancellationToken;
            return Task.FromResult(AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public string GetAppFormValue(string key)
        {
            return LastAppFormData.Last(entry => entry.Key == key).Value;
        }
    }

    private sealed class RecordingWsCore : ITiebaWsCore
    {
        public WSRes Response { get; set; } = new();
        public int LastCmd { get; private set; }
        public byte[] LastRequestData { get; private set; } = [];
        public CancellationToken LastCancellationToken { get; private set; }
        public Account? Account { get; set; }

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default)
        {
            LastCmd = cmd;
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

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
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

        Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
    }
}
