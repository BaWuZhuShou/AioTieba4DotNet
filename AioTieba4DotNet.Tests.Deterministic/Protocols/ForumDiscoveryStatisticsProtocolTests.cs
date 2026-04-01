#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.WebSockets;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public sealed class ForumDiscoveryStatisticsProtocolTests
{
    private const string SafeForumName = "lol欧服吧";
    private const ulong SafeForumId = 7356044;
    private static readonly string ValidBduss = new('b', 192);
    private static readonly string ValidStoken = new('s', 64);

    [TestMethod]
    public async Task GetImageAsync_ReturnsMappedPngImageAndReferrer()
    {
        var png = CreatePng(2, 3);
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(png)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return response;
        });
        var protocol = CreateProtocol(new RecordingHttpCore(new HttpClient(handler)));

        var image = await protocol.GetImageAsync("https://imgsrc.baidu.com/forum/test.png");

        Assert.AreEqual(ForumImageFormat.Png, image.Format);
        Assert.AreEqual(2, image.Width);
        Assert.AreEqual(3, image.Height);
        CollectionAssert.AreEqual(png, image.Data);
        Assert.AreEqual("https://tieba.baidu.com/", handler.LastRequest?.Headers.Referrer?.ToString());
    }

    [TestMethod]
    public async Task GetImageBytesAsync_ReturnsRawBytes()
    {
        var bytes = new byte[] { 0x42, 0x4D, 0x01, 0x02, 0x03 };
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/bmp");
            return response;
        });
        var protocol = CreateProtocol(new RecordingHttpCore(new HttpClient(handler)));

        var image = await protocol.GetImageBytesAsync("https://tb.himg.baidu.com/sys/portrait/item/test");

        CollectionAssert.AreEqual(bytes, image.Data);
        Assert.IsFalse(image.IsEmpty);
    }

    [TestMethod]
    public async Task GetImageAsync_WithInvalidSize_ReturnsEmptyImage()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 })
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return response;
        });
        var protocol = CreateProtocol(new RecordingHttpCore(new HttpClient(handler)));

        var image = await protocol.GetImageAsync("https://imgsrc.baidu.com/forum/invalid.png");

        Assert.IsTrue(image.IsEmpty);
        Assert.AreEqual(0, image.Width);
        Assert.AreEqual(0, image.Height);
    }

    [TestMethod]
    public async Task GetImageByHashAsync_WithInvalidSize_ReturnsEmptyWithoutTransport()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("Should not send request."));
        var protocol = CreateProtocol(new RecordingHttpCore(new HttpClient(handler)));

        var image = await protocol.GetImageByHashAsync("abcdef", (ForumImageSize)999);

        Assert.IsTrue(image.IsEmpty);
        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task GetPortraitAsync_LargeSize_UsesPortraitHPath()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreatePng(1, 1))
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return response;
        });
        var protocol = CreateProtocol(new RecordingHttpCore(new HttpClient(handler)));

        _ = await protocol.GetPortraitAsync("tb.1.testportrait", ForumImageSize.Large);

        StringAssert.Contains(handler.LastRequest?.RequestUri?.AbsoluteUri, "/sys/portraith/item/tb.1.testportrait");
    }

    [TestMethod]
    public async Task GetLastReplyersAsync_FallsBackToHttp_AndNormalizesCurrentPage()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateLastReplyersResponse(currentPage: 0, hasMore: true).ToByteArray()
        };
        var wsCore = new StubWsCore { ConnectException = new TiebaWebSocketUnavailableException("offline") };
        var protocol = CreateAuthenticatedProtocol(httpCore, wsCore, TiebaTransportMode.Auto);

        var replyers = await protocol.GetLastReplyersAsync(SafeForumName, 1, 30, ThreadSortType.Reply, false);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual(1, replyers.Page.CurrentPage);
        Assert.IsTrue(replyers.Page.HasMore);
        Assert.AreEqual("author-show", replyers[0].User.ShowName);
        Assert.AreEqual("tb.1.author", replyers[0].User.Portrait);
        Assert.AreEqual("last-show", replyers[0].LastReplyer.ShowName);
    }

    [TestMethod]
    public async Task GetMemberUsersAsync_WithoutStoken_FailsBeforeTransport()
    {
        var httpCore = new RecordingHttpCore();
        var protocol = CreateAuthenticatedProtocol(httpCore, transportMode: TiebaTransportMode.Http, stoken: string.Empty);

        await Assert.ThrowsAsync<TiebaAuthenticationException>(() => protocol.GetMemberUsersAsync(SafeForumName, 1));
        Assert.AreEqual(0, httpCore.SendWebGetCalls);
    }

    [TestMethod]
    public async Task GetMemberUsersAsync_ReturnsParsedHtmlUsersAndPagination()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             <div class="name_wrap"><a title="Alice" href="/home/main?id=tb.1.alice"><span class="level_9"></span></a></div>
                             <div class="tbui_pagination"><li class="active">2</li><a href="?pn=3">(5)</a></div>
                             """
        };
        var protocol = CreateAuthenticatedProtocol(httpCore);

        var users = await protocol.GetMemberUsersAsync(SafeForumName, 2);

        Assert.HasCount(1, users);
        Assert.AreEqual("Alice", users[0].UserName);
        Assert.AreEqual("tb.1.alice", users[0].Portrait);
        Assert.AreEqual(9, users[0].Level);
        Assert.AreEqual(2, users.Page.CurrentPage);
        Assert.AreEqual(5, users.Page.TotalPage);
        Assert.IsTrue(users.HasMore);
    }

    [TestMethod]
    public async Task GetRankForumsAsync_ReturnsParsedRowsAndPagination()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             <tr class="j_rank_row"><td>1</td><td><a>DNF</a></td><td>123</td><td>456</td><td class="has_bawu">吧务</td></tr>
                             <div class="pagination"><span>3</span><a href="?pn=4">尾页</a></div>
                             """
        };
        var protocol = CreateProtocol(httpCore);

        var ranks = await protocol.GetRankForumsAsync(SafeForumName, 3, ForumRankType.Monthly);

        Assert.HasCount(1, ranks);
        Assert.AreEqual("DNF", ranks[0].Fname);
        Assert.AreEqual(123, ranks[0].SignNum);
        Assert.AreEqual(456, ranks[0].MemberNum);
        Assert.IsTrue(ranks[0].HasBaWu);
        Assert.AreEqual(3, ranks.Page.CurrentPage);
        Assert.AreEqual(4, ranks.Page.TotalPage);
    }

    [TestMethod]
    public async Task GetRecomStatusAsync_ByName_ResolvesFidAndMapsJson()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = $"{{\"no\":0,\"error\":\"\",\"data\":{{\"fid\":{SafeForumId}}}}}",
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "total_recommend_num":12,
                                "used_recommend_num":5
                              }
                              """
        };
        var protocol = CreateAuthenticatedProtocol(httpCore);

        var status = await protocol.GetRecomStatusAsync(SafeForumName);

        Assert.AreEqual(12, status.TotalRecommendNum);
        Assert.AreEqual(5, status.UsedRecommendNum);
        Assert.AreEqual(1, httpCore.SendWebGetCalls);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
    }

    [TestMethod]
    public async Task GetSquareForumsAsync_FallsBackToHttp_AndMapsIsLikeToIsFollowed()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateSquareForumsResponse(hasMore: true).ToByteArray()
        };
        var wsCore = new StubWsCore { ConnectException = new TiebaWebSocketUnavailableException("offline") };
        var protocol = CreateAuthenticatedProtocol(httpCore, wsCore, TiebaTransportMode.Auto);

        var forums = await protocol.GetSquareForumsAsync("游戏", 2, 20);

        Assert.AreEqual(1, wsCore.ConnectCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.HasCount(1, forums);
        Assert.AreEqual(1, forums.Page.CurrentPage);
        Assert.IsTrue(forums.Page.HasMore);
        Assert.IsTrue(forums[0].IsFollowed);
        Assert.AreEqual(1234, forums[0].MemberNum);
    }

    [TestMethod]
    public async Task GetStatisticsAsync_ByName_ResolvesFidAndMapsOrderedSeries()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = $"{{\"no\":0,\"error\":\"\",\"data\":{{\"fid\":{SafeForumId}}}}}",
            AppFormResponse = """
                              {
                                "error_code":0,
                                "error_msg":"",
                                "data":[
                                  {"group":[{}, {"values":[{"value":1},{"value":2}]}]},
                                  {"group":[{}, {"values":[{"value":3},{"value":4}]}]},
                                  {"group":[{}, {"values":[{"value":5},{"value":6}]}]},
                                  {"group":[{}, {"values":[{"value":7},{"value":8}]}]},
                                  {"group":[{}, {"values":[{"value":9},{"value":10}]}]},
                                  {"group":[{}, {"values":[{"value":11},{"value":12}]}]},
                                  {"group":[{}, {"values":[{"value":13},{"value":14}]}]},
                                  {"group":[{}, {"values":[{"value":15},{"value":16}]}]}
                                ]
                              }
                              """
        };
        var protocol = CreateAuthenticatedProtocol(httpCore);

        var statistics = await protocol.GetStatisticsAsync(SafeForumName);

        CollectionAssert.AreEqual(new[] { 1, 2 }, statistics.View.ToArray());
        CollectionAssert.AreEqual(new[] { 3, 4 }, statistics.Thread.ToArray());
        CollectionAssert.AreEqual(new[] { 15, 16 }, statistics.Recommend.ToArray());
    }

    [TestMethod]
    public async Task GetForumLevelAsync_ByName_BootstrapsThenMapsLevelInfo()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponseFactory = (uri, _) => uri.AbsolutePath switch
            {
                "/f/commit/share/fnameShareApi" => $"{{\"no\":0,\"error\":\"\",\"data\":{{\"fid\":{SafeForumId}}}}}",
                "/mo/q/newmoindex" => "{" + "\"no\":0,\"error\":\"\",\"data\":{\"user\":{\"id\":1}}}" ,
                _ => "{}"
            },
            AppFormResponseFactory = (uri, _) => uri.AbsolutePath switch
            {
                "/c/s/initNickname" => "{" + "\"error_code\":0,\"error_msg\":\"\",\"user_info\":{\"user_id\":1}}",
                _ => "{}"
            },
            AppProtoResponseFactory = (uri, _) => CreateForumLevelResponse().ToByteArray()
        };
        var protocol = CreateAuthenticatedProtocol(httpCore);

        var level = await protocol.GetForumLevelAsync(SafeForumName);

        Assert.AreEqual("铁杆吧友", level.LevelName);
        Assert.AreEqual(9, level.UserLevel);
        Assert.IsTrue(level.IsLike);
        Assert.AreEqual(2, httpCore.SendWebGetCalls);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
    }

    private static ForumProtocol CreateProtocol(RecordingHttpCore httpCore)
    {
        using var session = new TiebaClientSession(
            new TiebaOptions { TransportMode = TiebaTransportMode.Http },
            httpCore,
            new StubWsCore());

        return new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());
    }

    private static ForumProtocol CreateAuthenticatedProtocol(RecordingHttpCore httpCore,
        StubWsCore? wsCore = null,
        TiebaTransportMode transportMode = TiebaTransportMode.Http,
        string? stoken = null)
    {
        var options = new TiebaOptions
        {
            Bduss = ValidBduss,
            Stoken = stoken ?? ValidStoken,
            TransportMode = transportMode
        };
        var session = new TiebaClientSession(options, httpCore, wsCore ?? new StubWsCore(), _ => Task.FromResult("tbs-123"));
        return new ForumProtocol(new TiebaOperationDispatcher(session), new ForumInfoCache());
    }

    private static byte[] CreatePng(int width, int height)
    {
        var bytes = new byte[24];
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
        bytes[4] = 0x0D; bytes[5] = 0x0A; bytes[6] = 0x1A; bytes[7] = 0x0A;
        bytes[8] = 0x00; bytes[9] = 0x00; bytes[10] = 0x00; bytes[11] = 0x0D;
        bytes[12] = 0x49; bytes[13] = 0x48; bytes[14] = 0x44; bytes[15] = 0x52;
        WriteInt32BigEndian(bytes, 16, width);
        WriteInt32BigEndian(bytes, 20, height);
        return bytes;
    }

    private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static FrsPageResIdl4lp CreateLastReplyersResponse(int currentPage, bool hasMore)
    {
        return new FrsPageResIdl4lp
        {
            Error = new Error { Errorno = 0 },
            Data = new FrsPageResIdl4lp.Types.DataRes
            {
                Forum = new FrsPageResIdl4lp.Types.DataRes.Types.ForumInfo { Id = (long)SafeForumId, Name = SafeForumName },
                Page = new Page { CurrentPage = currentPage, PageSize = 30, TotalPage = 9, TotalCount = 270, HasMore = hasMore ? 1 : 0 },
                ThreadList =
                {
                    new ThreadInfo
                    {
                        Id = 123456,
                        Title = "safe title",
                        FirstPostId = 654321,
                        CreateTime = 1700000001,
                        LastTimeInt = 1700000100,
                        IsGood = 1,
                        IsTop = 0,
                        Author = new User { Id = 111, Name = "author", NameShow = "author-show", Portrait = "tb.1.author?abc123456789" },
                        LastReplyer = new User { Id = 222, Name = "last", NameShow = "last-show" }
                    }
                }
            }
        };
    }

    private static GetForumSquareResIdl CreateSquareForumsResponse(bool hasMore)
    {
        return new GetForumSquareResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new GetForumSquareResIdl.Types.DataRes
            {
                Page = new Page { CurrentPage = 1, PageSize = 20, TotalPage = 5, TotalCount = 100, HasMore = hasMore ? 1 : 0 },
                ForumInfo =
                {
                    new GetForumSquareResIdl.Types.DataRes.Types.RecommendForumInfo
                    {
                        ForumId = SafeForumId,
                        ForumName = SafeForumName,
                        IsLike = 1,
                        MemberCount = 1234,
                        ThreadCount = 5678
                    }
                }
            }
        };
    }

    private static GetLevelInfoResIdl CreateForumLevelResponse()
    {
        return new GetLevelInfoResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new GetLevelInfoResIdl.Types.DataRes
            {
                LevelName = "铁杆吧友",
                UserLevel = 9,
                IsLike = 1
            }
        };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        private readonly HttpClient _httpClient;

        internal RecordingHttpCore(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public string WebGetResponse { get; init; } = "{}";
        public string AppFormResponse { get; init; } = "{}";
        public byte[] AppProtoResponse { get; init; } = [];
        public Func<Uri, List<KeyValuePair<string, string>>, string>? WebGetResponseFactory { get; init; }
        public Func<Uri, List<KeyValuePair<string, string>>, string>? AppFormResponseFactory { get; init; }
        public Func<Uri, byte[], byte[]>? AppProtoResponseFactory { get; init; }

        public int SendWebGetCalls { get; private set; }
        public int SendAppFormCalls { get; private set; }
        public int SendAppProtoCalls { get; private set; }
        public Uri? LastWebGetUri { get; private set; }
        public List<KeyValuePair<string, string>> LastWebGetParameters { get; private set; } = [];
        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];
        public Uri? LastAppProtoUri { get; private set; }

        public Account? Account { get; private set; }

        public HttpClient HttpClient => _httpClient;

        public void SetAccount(Account newAccount) => Account = newAccount;

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            SendAppFormCalls++;
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            return Task.FromResult(AppFormResponseFactory?.Invoke(uri, data) ?? AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            SendAppProtoCalls++;
            LastAppProtoUri = uri;
            return Task.FromResult(AppProtoResponseFactory?.Invoke(uri, data) ?? AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            SendWebGetCalls++;
            LastWebGetUri = uri;
            LastWebGetParameters = [.. parameters];
            return Task.FromResult(WebGetResponseFactory?.Invoke(uri, parameters) ?? WebGetResponse);
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubWsCore : ITiebaWsCore
    {
        public Exception? ConnectException { get; init; }
        public byte[] SendResponsePayload { get; init; } = [];
        public int ConnectCalls { get; private set; }
        public int SendCalls { get; private set; }

        public Account? Account { get; private set; }

        public void SetAccount(Account newAccount) => Account = newAccount;

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

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        internal RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        internal HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = await CloneAsync(request, cancellationToken);
            return _responseFactory(request);
        }

        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            if (request.Content is null)
                return clone;

            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = content;
            return clone;
        }
    }
}
