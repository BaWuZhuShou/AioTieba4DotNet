#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.GetLastReplyers;
using AioTieba4DotNet.Api.GetSelfFollowForumsV1;
using AioTieba4DotNet.Api.GetThreadPosts;
using AioTieba4DotNet.Api.Recommend;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Api;

[TestClass]
public sealed class ForumReadDiscoveryCoverageTests
{
    [TestMethod]
    public void ForumDiscoveryMappers_CoverFallbackAndHappyShapes()
    {
        var forumDetailOn = ForumDetailMapper.FromTbData(new global::GetForumDetailResIdl.Types.DataRes
        {
            ForumInfo = new global::GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
            {
                ForumId = 7356044,
                ForumName = "csharp",
                Lv1Name = "programming",
                Avatar = "small.png",
                AvatarOrigin = "origin.png",
                Slogan = "safe forum",
                MemberCount = 11,
                ThreadCount = 22
            },
            ElectionTab = new global::GetForumDetailResIdl.Types.DataRes.Types.ManagerElectionTab
            {
                NewStrategyText = "已有吧主"
            }
        });
        var forumDetailOff = ForumDetailMapper.FromTbData(new global::GetForumDetailResIdl.Types.DataRes
        {
            ForumInfo = new global::GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
            {
                ForumId = 1,
                ForumName = "plain",
                Lv1Name = "cat",
                Avatar = "avatar.png",
                AvatarOrigin = "origin.png",
                Slogan = "plain forum",
                MemberCount = 1,
                ThreadCount = 2
            },
            ElectionTab = new global::GetForumDetailResIdl.Types.DataRes.Types.ManagerElectionTab
            {
                NewStrategyText = "暂无吧主"
            }
        });

        var followEmpty = FollowForumsMapper.FromTbData(new JObject());
        var followMapped = FollowForumsMapper.FromTbData(new JObject
        {
            ["forum_list"] = new JObject
            {
                ["non-gconforum"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = 7,
                        ["name"] = "forum-a",
                        ["level_id"] = 3,
                        ["cur_score"] = 4
                    },
                    new JValue("skip")
                },
                ["gconforum"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = 8,
                        ["name"] = "forum-b",
                        ["level_id"] = 5,
                        ["cur_score"] = 6
                    }
                }
            },
            ["has_more"] = 1
        });

        var selfFollowEmpty = SelfFollowForumsV1Mapper.FromTbData(new JObject());
        var selfFollowMapped = SelfFollowForumsV1Mapper.FromTbData(new JObject
        {
            ["list"] = new JArray
            {
                new JObject
                {
                    ["forum_id"] = 11,
                    ["forum_name"] = "forum-c",
                    ["level_id"] = 2
                },
                new JObject()
            },
            ["page"] = new JObject
            {
                ["cur_page"] = 2,
                ["total_page"] = 4
            }
        });

        Assert.IsTrue(forumDetailOn.HasBaWu);
        Assert.IsFalse(forumDetailOff.HasBaWu);
        Assert.AreEqual("csharp", forumDetailOn.Fname);
        Assert.AreEqual("plain", forumDetailOff.Fname);

        Assert.AreEqual(0, followEmpty.Count);
        Assert.IsFalse(followEmpty.HasMore);
        Assert.AreEqual(2, followMapped.Count);
        Assert.AreEqual(7UL, followMapped[0].Fid);
        Assert.AreEqual("forum-a", followMapped[0].Fname);
        Assert.AreEqual(8UL, followMapped[1].Fid);
        Assert.IsTrue(followMapped.HasMore);

        Assert.AreEqual(0, selfFollowEmpty.Count);
        Assert.AreEqual(0, selfFollowEmpty.Page.CurrentPage);
        Assert.AreEqual(2, selfFollowMapped.Count);
        Assert.AreEqual(11UL, selfFollowMapped[0].Fid);
        Assert.IsTrue(selfFollowMapped.Page.HasPrevious);
        Assert.IsTrue(selfFollowMapped.Page.HasMore);
    }

    [TestMethod]
    public async Task LastReplyersAndThreadPostsApis_CoverPackingBranchesAndResultMapping()
    {
        var lastReplyersHttp = new RecordingHttpCore
        {
            AppProtoResponse = CreateLastReplyersResponse(currentPage: 0, hasMore: true).ToByteArray()
        };
        var lastReplyersWs = new RecordingWsCore
        {
            ResponsePayload = CreateLastReplyersResponse(currentPage: 2, hasMore: false).ToByteArray()
        };
        var lastReplyersApi = new GetLastReplyers(lastReplyersHttp, lastReplyersWs);

        var httpReplyers = await lastReplyersApi.RequestHttpAsync("csharp", 1, 30, ThreadSortType.Create, true);
        var httpRequest = FrsPageReqIdl4lp.Parser.ParseFrom(lastReplyersHttp.LastAppProtoData);
        var wsReplyers = await lastReplyersApi.RequestWsAsync("csharp", 2, 30, ThreadSortType.Reply, false);
        var wsRequest = FrsPageReqIdl4lp.Parser.ParseFrom(lastReplyersWs.LastData);

        var threadPostsHttp = new RecordingHttpCore
        {
            AppProtoResponse = CreateThreadPostsResponse().ToByteArray()
        };
        var threadPostsWs = new RecordingWsCore
        {
            ResponsePayload = CreateThreadPostsResponse().ToByteArray()
        };
        threadPostsHttp.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        threadPostsWs.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        var threadPostsApi = new GetThreadPosts(threadPostsHttp, threadPostsWs);

        var httpPosts = await threadPostsApi.RequestHttpAsync(77, 3, 1, 4, true, false, 9, true);
        var httpPostsRequest = PbPageReqIdl.Parser.ParseFrom(threadPostsHttp.LastAppProtoData);
        var wsPosts = await threadPostsApi.RequestWsAsync(88, 5, 7, 1, false, true, 11, false);
        var wsPostsRequest = PbPageReqIdl.Parser.ParseFrom(threadPostsWs.LastData);

        Assert.AreEqual(0, httpRequest.Data.Pn);
        Assert.AreEqual(30, httpRequest.Data.Rn);
        Assert.AreEqual(35, httpRequest.Data.RnNeed);
        Assert.AreEqual(1, httpRequest.Data.IsGood);
        Assert.AreEqual((int)ThreadSortType.Create, httpRequest.Data.SortType);
        Assert.AreEqual(2, wsRequest.Data.Pn);
        Assert.AreEqual(0, wsRequest.Data.IsGood);
        Assert.AreEqual((int)ThreadSortType.Reply, wsRequest.Data.SortType);
        Assert.AreEqual(1, httpReplyers.Page.CurrentPage);
        Assert.IsTrue(httpReplyers.Page.HasMore);
        Assert.AreEqual(2, wsReplyers.Page.CurrentPage);
        Assert.IsFalse(wsReplyers.Page.HasMore);
        Assert.AreEqual("author-show", httpReplyers[0].User.ShowName);
        Assert.AreEqual("last-show", httpReplyers[0].LastReplyer.ShowName);

        Assert.AreEqual(string.Empty, httpPostsRequest.Data.Common.BDUSS);
        Assert.AreEqual(77L, httpPostsRequest.Data.Kz);
        Assert.AreEqual(2, httpPostsRequest.Data.Rn);
        Assert.AreEqual(1, httpPostsRequest.Data.Lz);
        Assert.AreEqual(0, httpPostsRequest.Data.WithFloor);
        Assert.AreEqual(9, httpPostsRequest.Data.FloorRn);
        Assert.AreEqual(1, httpPostsRequest.Data.FloorSortType);
        Assert.AreEqual(new string('b', 192), wsPostsRequest.Data.Common.BDUSS);
        Assert.AreEqual(88L, wsPostsRequest.Data.Kz);
        Assert.AreEqual(7, wsPostsRequest.Data.Rn);
        Assert.AreEqual(0, wsPostsRequest.Data.Lz);
        Assert.AreEqual(1, wsPostsRequest.Data.WithFloor);
        Assert.AreEqual(11, wsPostsRequest.Data.FloorRn);
        Assert.AreEqual(0, wsPostsRequest.Data.FloorSortType);
        Assert.AreEqual(1, httpPosts.Objs.Count);
        Assert.AreEqual("Safe thread title", httpPosts.Thread.Title);
        Assert.AreEqual("post body", httpPosts.Objs[0].Text);
        Assert.AreEqual("thread-author", httpPosts.Thread.User!.UserName);
        Assert.AreEqual("thread-author", httpPosts.Objs[0].User!.UserName);
        Assert.AreEqual(1, wsPosts.Objs.Count);
    }

    [TestMethod]
    public async Task SelfFollowForumsV1AndRecommendApis_CoverErrorAndDefaultMessageBranches()
    {
        var selfFollowHttp = new RecordingHttpCore
        {
            WebGetResponse = """
                             {"errno":0,"errmsg":"","data":{}}
                             """
        };
        var selfFollowApi = new GetSelfFollowForumsV1(selfFollowHttp);

        var exception = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => selfFollowApi.RequestAsync(3, 20));

        Assert.AreEqual(-1, exception.Code);
        StringAssert.Contains(exception.Message, "Unable to parse self follow forums v1 data.");
        Assert.AreEqual("/mg/o/getForumHome", selfFollowHttp.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("3", selfFollowHttp.GetWebGetValue("pn"));
        Assert.AreEqual("20", selfFollowHttp.GetWebGetValue("rn"));

        var recommendOkHttp = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","data":{"is_push_success":1}}
                              """
        };
        recommendOkHttp.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        var recommendOkApi = new Recommend(recommendOkHttp);
        var recommendOk = await recommendOkApi.RequestAsync(12, 34);

        var recommendFailHttp = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","data":{"is_push_success":0}}
                              """
        };
        recommendFailHttp.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        var recommendFailApi = new Recommend(recommendFailHttp);
        var recommendFailException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => recommendFailApi.RequestAsync(12, 34));

        Assert.IsTrue(recommendOk);
        Assert.AreEqual("http", recommendOkHttp.LastAppFormUri?.Scheme);
        Assert.AreEqual("12", recommendOkHttp.GetAppFormValue("forum_id"));
        Assert.AreEqual("34", recommendOkHttp.GetAppFormValue("thread_id"));
        StringAssert.Contains(recommendFailException.Message, "Recommend failed.");
        Assert.AreEqual("12", recommendFailHttp.GetAppFormValue("forum_id"));
        Assert.AreEqual("34", recommendFailHttp.GetAppFormValue("thread_id"));
    }

    private static FrsPageResIdl4lp CreateLastReplyersResponse(int currentPage, bool hasMore)
    {
        return new FrsPageResIdl4lp
        {
            Error = new Error { Errorno = 0 },
            Data = new FrsPageResIdl4lp.Types.DataRes
            {
                Forum = new FrsPageResIdl4lp.Types.DataRes.Types.ForumInfo
                {
                    Id = 7356044,
                    Name = "csharp"
                },
                Page = new Page
                {
                    CurrentPage = currentPage,
                    PageSize = 30,
                    TotalPage = 9,
                    TotalCount = 270,
                    HasMore = hasMore ? 1 : 0
                },
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
                        Author = CreateUser(111, "author", "author-show", "tb.1.author?abc123456789"),
                        LastReplyer = CreateUser(222, "last", "last-show", "tb.1.last?abc123456789")
                    }
                }
            }
        };
    }

    private static PbPageResIdl CreateThreadPostsResponse()
    {
        return new PbPageResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new PbPageResIdl.Types.DataRes
            {
                Forum = new SimpleForum
                {
                    Id = 7356044,
                    Name = "csharp",
                    FirstClass = "programming",
                    SecondClass = "dotnet",
                    MemberNum = 10,
                    PostNum = 20
                },
                Page = new Page
                {
                    CurrentPage = 2,
                    PageSize = 10,
                    TotalPage = 3,
                    TotalCount = 30,
                    HasMore = 1
                },
                Thread = new ThreadInfo
                {
                    Id = 77,
                    Title = "Safe thread title",
                    FirstPostId = 88,
                    AuthorId = 111,
                    Author = CreateUser(111, "thread-author", "thread-author", "tb.1.thread-author?abc123456789"),
                    FirstPostContent = { CreateTextContent("thread body") }
                },
                PostList =
                {
                    new Post
                    {
                        Id = 88,
                        Floor = 1,
                        Time = 1699999999,
                        AuthorId = 111,
                        Author = CreateUser(111, "thread-author", "thread-author", "tb.1.thread-author?abc123456789"),
                        Content = { CreateTextContent("post body") }
                    }
                },
                UserList =
                {
                    CreateUser(111, "thread-author", "thread-author", "tb.1.thread-author?abc123456789")
                }
            }
        };
    }

    private static User CreateUser(long userId, string userName, string showName, string portrait)
    {
        return new User
        {
            Id = userId,
            Name = userName,
            NameShow = showName,
            Portrait = portrait,
            LevelId = 12
        };
    }

    private static PbContent CreateTextContent(string text)
    {
        return new PbContent
        {
            Type = 0,
            Text = text
        };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string AppFormResponse { get; init; } = "{}";

        public string WebGetResponse { get; init; } = "{}";

        public byte[] AppProtoResponse { get; init; } = [];

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];
        public Uri? LastWebGetUri { get; private set; }
        public List<KeyValuePair<string, string>> LastWebGetParameters { get; private set; } = [];
        public Uri? LastAppProtoUri { get; private set; }
        public byte[] LastAppProtoData { get; private set; } = [];

        public void SetAccount(Account newAccount) => Account = newAccount;

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            LastAppProtoUri = uri;
            LastAppProtoData = data.ToArray();
            return Task.FromResult(AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            LastWebGetUri = uri;
            LastWebGetParameters = [.. parameters];
            return Task.FromResult(WebGetResponse);
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public string GetAppFormValue(string key) => LastAppFormData.Last(entry => entry.Key == key).Value;

        public string GetWebGetValue(string key) => LastWebGetParameters.Last(entry => entry.Key == key).Value;
    }

    private sealed class RecordingWsCore : ITiebaWsCore
    {
        public Account? Account { get; private set; }

        public byte[] ResponsePayload { get; init; } = [];

        public int LastCmd { get; private set; }
        public byte[] LastData { get; private set; } = [];
        public CancellationToken LastCancellationToken { get; private set; }

        public void SetAccount(Account newAccount) => Account = newAccount;

        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default)
        {
            LastCmd = cmd;
            LastData = data.ToArray();
            LastCancellationToken = cancellationToken;
            return Task.FromResult(new WSRes
            {
                Payload = new WSRes.Types.Payload
                {
                    Data = ByteString.CopyFrom(ResponsePayload)
                }
            });
        }

        public async IAsyncEnumerable<WSRes> ListenAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
