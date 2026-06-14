#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.GetComments;
using AioTieba4DotNet.Api.GetThreadPosts;
using AioTieba4DotNet.Api.GetThreads;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.WebSockets;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionAccount = AioTieba4DotNet.Session.Account;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class ThreadWebSocketDirectContractTests
{
    private const string ForumName = "地下城与勇士";
    private const int ForumId = 246810;
    private const long ThreadId = 1357911;
    private const long RootPostId = 2468022;
    private const long AuthorId = 11223344;
    private static readonly string ValidBduss = new('b', 192);

    [TestMethod]
    public async Task GetThreadsRequestWsAsyncEmptyPayloadRaisesWebSocketTransportFailure()
    {
        var httpCore = new RecordingHttpCore(CreateValidThreadsResponse(ForumName, ForumId));
        var wsCore = new RecordingWsCore(static () => new WSRes
        {
            Payload = new WSRes.Types.Payload { Data = ByteString.Empty }
        });
        var api = new GetThreads(httpCore, wsCore);

        var exception = await Assert.ThrowsExactlyAsync<TiebaWebSocketUnavailableException>(
            () => api.RequestWsAsync(ForumName, 1, 10, (int)ThreadSortType.Reply, 0));

        Assert.AreEqual(0, httpCore.SendAppProtoCount, "Direct WS execution must not touch HTTP fallback helpers.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
        StringAssert.Contains(exception.Message, "empty get-threads payload");
    }

    [TestMethod]
    public async Task GetThreadsRequestHttpAndWsAsyncUseSameUpstreamCompatiblePayload()
    {
        var responseBytes = CreateValidThreadsResponse(ForumName, ForumId);
        var httpCore = new RecordingHttpCore(responseBytes);
        var wsCore = new RecordingWsCore(() => CreateWsResponse(responseBytes));
        var api = new GetThreads(httpCore, wsCore);

        var httpThreads = await api.RequestHttpAsync(ForumName, 1, 10, (int)ThreadSortType.Reply, 0);
        var wsThreads = await api.RequestWsAsync(ForumName, 1, 10, (int)ThreadSortType.Reply, 0);

        CollectionAssert.AreEqual(httpCore.LastAppProtoData, wsCore.LastWsData,
            "HTTP and WS get-threads must share the same upstream-compatible protobuf payload.");

        var request = FrsPageReqIdl.Parser.ParseFrom(httpCore.LastAppProtoData);
        Assert.AreEqual(2, request.Data.Common.ClientType);
        Assert.AreEqual(Const.MainVersion, request.Data.Common.ClientVersion);
        Assert.AreEqual(ForumName, request.Data.Kw);
        Assert.AreEqual(0, request.Data.Pn, "Upstream parity expects page 1 to normalize to protobuf pn=0.");
        Assert.AreEqual(10, request.Data.Rn);
        Assert.AreEqual(15, request.Data.RnNeed);
        Assert.AreEqual(0, request.Data.IsGood);
        Assert.AreEqual((int)ThreadSortType.Reply, request.Data.SortType);
        Assert.AreEqual(0, request.Data.LoadType,
            "Get-threads should not send the extra load_type=1 workaround field.");

        Assert.AreEqual(ForumName, httpThreads.Forum.Fname);
        Assert.AreEqual(ForumName, wsThreads.Forum.Fname);
        Assert.AreEqual(ForumId, httpThreads.Forum.Fid);
        Assert.AreEqual(ForumId, wsThreads.Forum.Fid);
    }

    [TestMethod]
    public async Task GetThreadsRequestWsAsyncValidPayloadReturnsThreadsWithoutHttpFallback()
    {
        var responseBytes = CreateValidThreadsResponse(ForumName, ForumId);
        var httpCore = new RecordingHttpCore(responseBytes);
        var wsCore = new RecordingWsCore(() => CreateWsResponse(responseBytes));
        var api = new GetThreads(httpCore, wsCore);

        var threads = await api.RequestWsAsync(ForumName, 1, 10, (int)ThreadSortType.Reply, 0);

        Assert.AreEqual(ForumName, threads.Forum.Fname);
        Assert.AreEqual(ForumId, threads.Forum.Fid);
        Assert.AreEqual(0, httpCore.SendAppProtoCount, "A valid WS get-threads response must not rely on HTTP fallback.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
    }

    [TestMethod]
    public async Task GetThreadsRequestWsAsyncInvalidPayloadRaisesWebSocketTransportFailure()
    {
        var httpCore = new RecordingHttpCore(CreateValidThreadsResponse(ForumName, ForumId));
        var wsCore = new RecordingWsCore(static () => new WSRes
        {
            Payload = new WSRes.Types.Payload
            {
                Data = ByteString.CopyFrom(new byte[] { 0xFF, 0x00, 0x7F })
            }
        });
        var api = new GetThreads(httpCore, wsCore);

        var exception = await Assert.ThrowsExactlyAsync<TiebaWebSocketUnavailableException>(
            () => api.RequestWsAsync(ForumName, 1, 10, (int)ThreadSortType.Reply, 0));

        Assert.AreEqual(0, httpCore.SendAppProtoCount, "Direct WS execution must not touch HTTP fallback helpers.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
        StringAssert.Contains(exception.Message, "invalid get-threads payload");
    }

    [TestMethod]
    public async Task GetThreadsRequestWsAsyncServerErrorPreservesServerFailure()
    {
        var httpCore = new RecordingHttpCore(CreateValidThreadsResponse(ForumName, ForumId));
        var wsCore = new RecordingWsCore(static () => new WSRes
        {
            Payload = new WSRes.Types.Payload
            {
                Data = ByteString.CopyFrom(new FrsPageResIdl
                {
                    Error = new Error { Errorno = 4, Errmsg = "forum denied" },
                    Data = new FrsPageResIdl.Types.DataRes()
                }.ToByteArray())
            }
        });
        var api = new GetThreads(httpCore, wsCore);

        var exception = await Assert.ThrowsExactlyAsync<TieBaServerException>(
            () => api.RequestWsAsync(ForumName, 1, 10, (int)ThreadSortType.Reply, 0));

        Assert.AreEqual(4, exception.Code);
        Assert.AreEqual(0, httpCore.SendAppProtoCount, "Server-side WS failures must not be silently retried in the API-only path.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
    }

    [TestMethod]
    public async Task GetPostsRequestHttpAndWsAsyncUseSameUpstreamCompatiblePayload()
    {
        var responseBytes = CreateValidPostsResponse(ForumName, ForumId, ThreadId, RootPostId, AuthorId);
        var httpCore = new RecordingHttpCore(responseBytes);
        var wsCore = new RecordingWsCore(() => CreateWsResponse(responseBytes));
        var account = new SessionAccount(ValidBduss, string.Empty);
        httpCore.SetAccount(account);
        wsCore.SetAccount(account);
        var api = new GetThreadPosts(httpCore, wsCore);

        var httpPosts = await api.RequestHttpAsync(ThreadId, 1, 1, (int)PostSortType.Hot, true, true, 2, true);
        var wsPosts = await api.RequestWsAsync(ThreadId, 1, 1, (int)PostSortType.Hot, true, true, 2, true);

        CollectionAssert.AreEqual(httpCore.LastAppProtoData, wsCore.LastWsData,
            "HTTP and WS get-posts must share the same upstream-compatible protobuf payload.");

        var request = PbPageReqIdl.Parser.ParseFrom(httpCore.LastAppProtoData);
        Assert.AreEqual(2, request.Data.Common.ClientType);
        Assert.AreEqual(Const.MainVersion, request.Data.Common.ClientVersion);
        Assert.AreEqual(ValidBduss, request.Data.Common.BDUSS);
        Assert.AreEqual(ThreadId, request.Data.Kz);
        Assert.AreEqual(1, request.Data.Pn);
        Assert.AreEqual(2, request.Data.Rn, "Get-posts should clamp rn to at least 2 to match upstream behavior.");
        Assert.AreEqual((int)PostSortType.Hot, request.Data.R);
        Assert.AreEqual(1, request.Data.Lz);
        Assert.AreEqual(1, request.Data.WithFloor);
        Assert.AreEqual(2, request.Data.FloorRn);
        Assert.AreEqual(1, request.Data.FloorSortType);

        await api.RequestWsAsync(ThreadId, 1, 10, (int)PostSortType.Asc, false, false, 2, true);
        var requestWithoutPreviewComments = PbPageReqIdl.Parser.ParseFrom(wsCore.LastWsData);
        Assert.AreEqual(string.Empty, requestWithoutPreviewComments.Data.Common.BDUSS,
            "Get-posts should only carry BDUSS when preview comments are requested.");
        Assert.AreEqual(0, requestWithoutPreviewComments.Data.WithFloor);

        Assert.AreEqual(ThreadId, httpPosts.Thread.Tid);
        Assert.AreEqual(ThreadId, wsPosts.Thread.Tid);
        Assert.AreEqual(RootPostId, httpPosts.Objs[0].Pid);
        Assert.AreEqual(RootPostId, wsPosts.Objs[0].Pid);
        Assert.AreEqual(ForumName, httpPosts.Forum.Fname);
        Assert.AreEqual(ForumName, wsPosts.Forum.Fname);
        Assert.AreEqual(ForumId, httpPosts.Forum.Fid);
        Assert.AreEqual(ForumId, wsPosts.Forum.Fid);
    }

    [TestMethod]
    public async Task GetPostsRequestWsAsyncValidPayloadReturnsPostsWithoutHttpFallback()
    {
        var responseBytes = CreateValidPostsResponse(ForumName, ForumId, ThreadId, RootPostId, AuthorId);
        var httpCore = new RecordingHttpCore(responseBytes);
        var wsCore = new RecordingWsCore(() => CreateWsResponse(responseBytes));
        var api = new GetThreadPosts(httpCore, wsCore);

        var posts = await api.RequestWsAsync(ThreadId, 1, 10, (int)PostSortType.Hot, false, false, 0, false);

        Assert.AreEqual(ThreadId, posts.Thread.Tid);
        Assert.AreEqual(RootPostId, posts.Objs[0].Pid);
        Assert.AreEqual(ForumName, posts.Forum.Fname);
        Assert.AreEqual(ForumId, posts.Forum.Fid);
        Assert.AreEqual(0, httpCore.SendAppProtoCount, "A valid WS get-posts response must not rely on HTTP fallback.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
    }

    [TestMethod]
    public async Task GetPostsRequestWsAsyncEmptyPayloadRaisesWebSocketTransportFailure()
    {
        var httpCore = new RecordingHttpCore(CreateValidPostsResponse(ForumName, ForumId, ThreadId, RootPostId, AuthorId));
        var wsCore = new RecordingWsCore(static () => new WSRes
        {
            Payload = new WSRes.Types.Payload { Data = ByteString.Empty }
        });
        var api = new GetThreadPosts(httpCore, wsCore);

        var exception = await Assert.ThrowsExactlyAsync<TiebaWebSocketUnavailableException>(
            () => api.RequestWsAsync(ThreadId, 1, 10, (int)PostSortType.Hot, false, false, 0, false));

        Assert.AreEqual(0, httpCore.SendAppProtoCount, "Direct WS execution must not touch HTTP fallback helpers.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
        StringAssert.Contains(exception.Message, "empty get-posts payload");
    }

    [TestMethod]
    public async Task GetPostsRequestWsAsyncInvalidPayloadRaisesWebSocketTransportFailure()
    {
        var httpCore = new RecordingHttpCore(CreateValidPostsResponse(ForumName, ForumId, ThreadId, RootPostId, AuthorId));
        var wsCore = new RecordingWsCore(static () => new WSRes
        {
            Payload = new WSRes.Types.Payload
            {
                Data = ByteString.CopyFrom(new byte[] { 0xFF, 0x00, 0x7F })
            }
        });
        var api = new GetThreadPosts(httpCore, wsCore);

        var exception = await Assert.ThrowsExactlyAsync<TiebaWebSocketUnavailableException>(
            () => api.RequestWsAsync(ThreadId, 1, 10, (int)PostSortType.Hot, false, false, 0, false));

        Assert.AreEqual(0, httpCore.SendAppProtoCount, "Direct WS execution must not touch HTTP fallback helpers.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
        StringAssert.Contains(exception.Message, "invalid get-posts payload");
    }

    [TestMethod]
    public async Task GetPostsRequestWsAsyncServerErrorPreservesServerFailure()
    {
        var httpCore = new RecordingHttpCore(CreateValidPostsResponse(ForumName, ForumId, ThreadId, RootPostId, AuthorId));
        var wsCore = new RecordingWsCore(static () => new WSRes
        {
            Payload = new WSRes.Types.Payload
            {
                Data = ByteString.CopyFrom(new PbPageResIdl
                {
                    Error = new Error { Errorno = 4, Errmsg = "thread denied" },
                    Data = new PbPageResIdl.Types.DataRes()
                }.ToByteArray())
            }
        });
        var api = new GetThreadPosts(httpCore, wsCore);

        var exception = await Assert.ThrowsExactlyAsync<TieBaServerException>(
            () => api.RequestWsAsync(ThreadId, 1, 10, (int)PostSortType.Hot, false, false, 0, false));

        Assert.AreEqual(4, exception.Code);
        Assert.AreEqual(0, httpCore.SendAppProtoCount, "Server-side WS failures must not be silently retried in the API-only path.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
    }

    [TestMethod]
    public async Task GetCommentsRequestWsAsyncPayloadWithoutErrorReturnsCommentsWithoutHttpFallback()
    {
        var responseBytes = CreateValidCommentsResponseWithoutError(ForumName, ForumId, ThreadId, RootPostId, AuthorId);
        var httpCore = new RecordingHttpCore(responseBytes);
        var wsCore = new RecordingWsCore(() => CreateWsResponse(responseBytes));
        var api = new GetComments(httpCore, wsCore);

        var comments = await api.RequestWsAsync(ThreadId, RootPostId, 1, false);

        Assert.AreEqual(ThreadId, comments.Thread.Tid);
        Assert.AreEqual(RootPostId, comments.Post.Pid);
        Assert.AreEqual(ForumName, comments.Forum.Fname);
        Assert.AreEqual(ForumId, comments.Forum.Fid);
        Assert.IsEmpty(comments.Objs);
        Assert.AreEqual(0, httpCore.SendAppProtoCount,
            "A valid WS get-comments response without an error field must not rely on HTTP fallback.");
        Assert.AreEqual(1, wsCore.SendCount, "Direct WS execution should send exactly one websocket request.");
    }

    private static WSRes CreateWsResponse(byte[] responseBytes)
    {
        return new WSRes
        {
            Payload = new WSRes.Types.Payload { Data = ByteString.CopyFrom(responseBytes) }
        };
    }

    private static byte[] CreateValidThreadsResponse(string forumName, long forumId)
    {
        return new FrsPageResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty },
            Data = new FrsPageResIdl.Types.DataRes
            {
                Forum = new FrsPageResIdl.Types.DataRes.Types.ForumInfo
                {
                    Id = forumId,
                    Name = forumName,
                    FirstClass = "游戏",
                    SecondClass = "网游",
                    MemberNum = 1,
                    ThreadNum = 0,
                    PostNum = 0
                },
                Page = new Page
                {
                    PageSize = 10,
                    CurrentPage = 1,
                    TotalCount = 0,
                    TotalPage = 1,
                    HasMore = 0,
                    HasPrev = 0
                },
                NavTabInfo = new FrsPageResIdl.Types.DataRes.Types.NavTabInfo(),
                ForumRule = new FrsPageResIdl.Types.DataRes.Types.ForumRuleStatus { HasForumRule = 0 }
            }
        }.ToByteArray();
    }

    private static byte[] CreateValidPostsResponse(string forumName, long forumId, long threadId, long rootPostId,
        long authorId)
    {
        return new PbPageResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty },
            Data = new PbPageResIdl.Types.DataRes
            {
                Forum = new SimpleForum
                {
                    Id = forumId,
                    Name = forumName,
                    FirstClass = "游戏",
                    SecondClass = "网游",
                    MemberNum = 1,
                    PostNum = 1
                },
                Page = new Page
                {
                    PageSize = 10,
                    CurrentPage = 1,
                    TotalCount = 1,
                    TotalPage = 1,
                    HasMore = 0,
                    HasPrev = 0
                },
                Thread = new ThreadInfo
                {
                    Id = threadId,
                    FirstPostId = rootPostId,
                    AuthorId = authorId,
                    Title = "测试主题",
                    ReplyNum = 1,
                    ViewNum = 1
                },
                PostList =
                {
                    new global::Post
                    {
                        Id = rootPostId,
                        AuthorId = authorId,
                        Floor = 1,
                        SubPostNumber = 0,
                        Time = 1
                    }
                }
            }
        }.ToByteArray();
    }

    private static byte[] CreateValidCommentsResponseWithoutError(string forumName, long forumId, long threadId,
        long rootPostId, long authorId)
    {
        return new PbFloorResIdl
        {
            Data = new PbFloorResIdl.Types.DataRes
            {
                Forum = new SimpleForum
                {
                    Id = forumId,
                    Name = forumName,
                    FirstClass = "游戏",
                    SecondClass = "网游",
                    MemberNum = 1,
                    PostNum = 1
                },
                Page = new Page
                {
                    PageSize = 10,
                    CurrentPage = 1,
                    TotalCount = 0,
                    TotalPage = 1,
                    HasMore = 0,
                    HasPrev = 0
                },
                Thread = new ThreadInfo
                {
                    Id = threadId,
                    FirstPostId = rootPostId,
                    AuthorId = authorId,
                    Title = "测试主题",
                    ReplyNum = 1,
                    ViewNum = 1
                },
                Post = new global::Post
                {
                    Id = rootPostId,
                    AuthorId = authorId,
                    Floor = 1,
                    SubPostNumber = 0,
                    Time = 1
                }
            }
        }.ToByteArray();
    }

    private sealed class RecordingHttpCore(byte[] responseBytes) : ITiebaHttpCore, IDisposable
    {
        public SessionAccount? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public byte[] LastAppProtoData { get; private set; } = [];

        public int SendAppProtoCount { get; private set; }

        public void Dispose()
        {
            HttpClient.Dispose();
        }

        public void SetAccount(SessionAccount newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            SendAppProtoCount++;
            LastAppProtoData = (byte[])data.Clone();
            return Task.FromResult(responseBytes);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingWsCore(Func<WSRes> responseFactory) : ITiebaWsCore
    {
        public SessionAccount? Account { get; private set; }

        public int ConnectCount { get; private set; }

        public byte[] LastWsData { get; private set; } = [];

        public int SendCount { get; private set; }

        public void SetAccount(SessionAccount newAccount)
        {
            Account = newAccount;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ConnectCount++;
            return Task.CompletedTask;
        }

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            LastWsData = (byte[])data.Clone();
            return Task.FromResult(responseFactory());
        }

        public async IAsyncEnumerable<WSRes> ListenAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
