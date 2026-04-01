#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using AioTieba4DotNet.Api;
using AioTieba4DotNet.Api.AddPost;
using AioTieba4DotNet.Api.GetAts;
using AioTieba4DotNet.Api.GetBlacklist;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile;
using AioTieba4DotNet.Api.RemoveFan;
using AioTieba4DotNet.Api.SignForums;
using AioTieba4DotNet.Api.UndislikeForum;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.Http;
using AioTieba4DotNet.Transport.WebSockets;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;

namespace AioTieba4DotNet.Tests.Coverage;

[TestClass]
public sealed class Task18HotspotCoverageTests
{
    [TestMethod]
    public async Task AsyncInitModelsAndLightweightMappers_CoverZeroLineHelpers()
    {
        var initCalls = 0;
        var init = new AsyncInit<int>(async () =>
        {
            initCalls++;
            await Task.Yield();
            return 41;
        });

        var first = await init.GetAsync();
        var second = await init.GetAsync();
        init.SetValue(99);
        var third = await init.GetAsync();

        var frag = FragItemMapper.FromTbData(new PbContent
        {
            Item = new PbContent.Types.Item { ItemName = "badge" }
        });
        var forum = new Forum
        {
            Fid = 1,
            Fname = "forum",
            Category = "cat",
            Subcategory = "sub",
            SmallAvatar = "small",
            Slogan = "slogan",
            MemberNum = 2,
            PostNum = 3,
            ThreadNum = 4,
            HasBaWu = true
        };
        var forumDetail = new ForumDetail
        {
            Fid = 5,
            Fname = "forum2",
            Category = "cat2",
            SmallAvatar = "small2",
            OriginAvatar = "origin",
            Slogan = "slogan2",
            MemberNum = 6,
            PostNum = 7,
            HasBaWu = false
        };
        var userPost = new UserPost
        {
            Contents = new Content(),
            Fid = 8,
            Tid = 9,
            Pid = 10,
            IsComment = true,
            User = new global::AioTieba4DotNet.Models.Shared.UserInfo { UserId = 11, UserName = "author" },
            CreateTime = 12
        };

        Assert.AreEqual(41, first);
        Assert.AreEqual(41, second);
        Assert.AreEqual(99, third);
        Assert.AreEqual(1, initCalls);
        Assert.IsTrue(init.IsValueCreated);
        Assert.AreEqual("badge", frag.Text);
        Assert.AreEqual("FragItem", frag.GetFragType());
        Assert.AreEqual(0, frag.ToDict().Count);
        StringAssert.Contains(forum.ToString(), "Fid: 1");
        StringAssert.Contains(forumDetail.ToString(), "OriginAvatar: origin");
        StringAssert.Contains(userPost.ToString(), "CreateTime: 12");
    }

    [TestMethod]
    public async Task ApiAndValidatorBranches_CoverFallbackAndErrorPaths()
    {
        var atsCore = new RecordingHttpCore { AppFormResponse = "{\"error_code\":0,\"error_msg\":\"\"}" };
        var blacklistCore = new RecordingHttpCore { AppFormResponse = "{\"error_code\":0,\"error_msg\":\"\"}" };
        var signCore = new RecordingHttpCore { CustomResponse = "{\"error_code\":0,\"error_msg\":\"\"}" };

        var ats = new GetAts(atsCore);
        var blacklist = new GetBlacklist(blacklistCore);
        var signForums = new SignForums(signCore);

        var atsResult = await ats.RequestAsync(2);
        var blacklistResult = await blacklist.RequestAsync();
        var signForumsResult = await signForums.RequestAsync();

        var recoversNull = RecoversMapper.FromTbData(null);
        var recoversMissing = RecoversMapper.FromTbData(new JObject());
        var recoversMapped = RecoversMapper.FromTbData(new JObject
        {
            ["data"] = new JObject
            {
                ["thread_list"] = new JArray(new JObject(), new JObject()),
                ["page"] = new JObject { ["pn"] = 2, ["rn"] = 3, ["has_more"] = 1 }
            }
        });
        var recoverPageNull = RecoverPageMapper.FromTbData(null);
        var recoverPageFalse = RecoverPageMapper.FromTbData(new JObject
        {
            ["pn"] = 1,
            ["rn"] = 0,
            ["has_more"] = 0
        });
        var optionsErrors = TiebaOptionsValidator.GetValidationErrors(new TiebaOptions
        {
            Stoken = "st",
            MaxReadRetryAttempts = -1,
            RequestTimeout = TimeSpan.Zero
        });

        Assert.IsNotNull(atsResult);
        Assert.AreEqual("/c/u/feed/atme", atsCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(string.Empty, atsCore.GetAppFormValue("BDUSS"));
        Assert.IsNotNull(blacklistResult);
        Assert.AreEqual("/c/u/user/userBlackPage", blacklistCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(string.Empty, blacklistCore.GetAppFormValue("BDUSS"));
        Assert.IsTrue(signForumsResult);
        Assert.AreEqual("/c/c/forum/msign", signCore.LastCustomRequest?.RequestUri?.AbsolutePath);
        Assert.AreEqual("hybrid", signCore.GetCustomFormValue("subapp_type"));
        Assert.AreEqual(0, recoversNull.Count);
        Assert.AreEqual(0, recoversMissing.Count);
        Assert.AreEqual(2, recoversMapped.Count);
        Assert.AreEqual(2, recoversMapped.Page.CurrentPage);
        Assert.IsTrue(recoversMapped.Page.HasMore);
        Assert.AreEqual(0, recoverPageNull.PageSize);
        Assert.IsFalse(recoverPageFalse.HasMore);
        Assert.IsFalse(recoverPageFalse.HasPrevious);
        CollectionAssert.AreEqual(new[]
        {
            "Stoken cannot be supplied without Bduss.",
            "MaxReadRetryAttempts must be greater than or equal to 0.",
            "RequestTimeout must be positive or Timeout.InfiniteTimeSpan."
        }, optionsErrors.ToArray());
        Throws<TiebaConfigurationException>(() => TiebaOptionsValidator.Validate(new TiebaOptions
        {
            Stoken = "st",
            MaxReadRetryAttempts = -1,
            RequestTimeout = TimeSpan.Zero
        }));
    }

    [TestMethod]
    public async Task GetUInfoProfile_CoversSupportedGenericTypesAndUnsupportedTypeGuard()
    {
        var intCore = new RecordingHttpCore { AppProtoResponse = CreateProfileResponse().ToByteArray() };
        var stringCore = new RecordingHttpCore { AppProtoResponse = CreateProfileResponse().ToByteArray() };

        var byId = await new GetUInfoProfile<int>(intCore).RequestAsync(42);
        var requestById = global::ProfileReqIdl.Parser.ParseFrom(intCore.LastAppProtoRequestData!);
        var byPortrait = await new GetUInfoProfile<string>(stringCore).RequestAsync("tb.1.profile");
        var requestByPortrait = global::ProfileReqIdl.Parser.ParseFrom(stringCore.LastAppProtoRequestData!);

        var unsupported = new GetUInfoProfile<DateTime>(new RecordingHttpCore());

        Assert.AreEqual(42L, byId.UserId);
        Assert.AreEqual(42L, requestById.Data.Uid);
        Assert.AreEqual(string.Empty, requestById.Data.FriendUidPortrait);
        Assert.AreEqual("tb.1.profile", requestByPortrait.Data.FriendUidPortrait);
        Assert.AreEqual(0L, requestByPortrait.Data.Uid);
        Assert.AreEqual("tb.1.profile", byPortrait.Portrait);
        await ThrowsAsync<InvalidOperationException>(() => unsupported.RequestAsync(default));
    }

    [TestMethod]
    public void TailModelsContainersAndClientHelpers_CoverRemainingSmallTypes()
    {
        var pythonApi = new AioTieba4DotNet.Attributes.PythonApiAttribute("aiotieba.api.tail");
        var transportException = new TiebaTransportException("boom", new InvalidOperationException("inner"));
        var client = new TiebaClient(new string('b', 192), new string('s', 64));
        var delayStrategy = SystemTiebaWebSocketDelayStrategy.Instance;
        var connectionFactory = new ClientWebSocketConnectionFactory(TiebaWebSocketOptions.Default);
        var connection = connectionFactory.CreateConnection();
        var rankForums = new RankForums(
            [new RankForum { Fname = "forum", SignNum = 1, MemberNum = 2, HasBaWu = true }],
            new RankForumsPage { CurrentPage = 1, TotalPage = 2, HasMore = true, HasPrevious = false });
        var blocks = new Blocks(
            [new Block { UserId = 1, UserName = "user", NickNameOld = "old", Day = 3 }],
            new BlocksPage { CurrentPage = 1, TotalPage = 2, HasMore = true, HasPrevious = false });
        var squareForums = new SquareForums(
            [new SquareForum { Fid = 1, Fname = "square", MemberNum = 2, PostNum = 3, IsFollowed = true }],
            new SquareForumsPage { CurrentPage = 1, TotalPage = 2, HasMore = true, HasPrevious = false });
        var blacklists = new BawuBlacklistUsers(
            [new BawuBlacklistUser { UserId = 1, Portrait = "tb.1.user", UserName = "user" }],
            new BawuBlacklistPage { CurrentPage = 1, TotalPage = 2, HasMore = true, HasPrevious = false });
        var postLogs = new BawuPostLogs(
            [new BawuPostLog { Title = "title", Text = "text", Medias = [], Tid = 1, Pid = 2, OperationType = "op", PostPortrait = "tb.1.user", PostTime = new DateTime(2026, 1, 1), OperatorUserName = "op", OperationTime = new DateTime(2026, 1, 2) }],
            new BawuPostLogPage { CurrentPage = 1, TotalPage = 2, TotalCount = 1, HasMore = true, HasPrevious = false });
        var userLogs = new BawuUserLogs(
            [new BawuUserLog { OperationType = "op", OperationDurationDays = 3, UserPortrait = "tb.1.user", OperatorUserName = "op", OperationTime = new DateTime(2026, 1, 2) }],
            new BawuUserLogPage { CurrentPage = 1, TotalPage = 2, TotalCount = 1, HasMore = true, HasPrevious = false });
        var recoverUserA = new RecoverUser { UserName = "user", Portrait = "portrait", NickNameNew = "nick" };
        var recoverUserB = new RecoverUser { UserName = string.Empty, Portrait = "portrait", NickNameNew = "nick" };
        var recoverUserC = new RecoverUser { UserName = string.Empty, Portrait = string.Empty, NickNameNew = string.Empty };
        var recoverUserD = new RecoverUser { UserName = "user", Portrait = string.Empty, NickNameNew = string.Empty };
        var lastReplyerUser = new LastReplyerUser { UserId = 1, Portrait = "portrait", UserName = "user", NickNameOld = "old" };
        var lastReplyerUserFallback = new LastReplyerUser { UserId = 3, Portrait = "portrait3", UserName = string.Empty, NickNameOld = "old3" };
        var lastReplyer = new LastReplyer { UserId = 2, UserName = "user2", NickNameOld = "old2" };
        var lastReplyers = new LastReplyers(
            [new LastReplyerThread
            {
                Title = "title",
                Fid = 1,
                Fname = "forum",
                Tid = 2,
                Pid = 3,
                User = lastReplyerUser,
                LastReplyer = lastReplyer,
                IsGood = true,
                IsTop = false,
                CreateTime = 4,
                LastTime = 5
            }],
            new LastReplyersPage { CurrentPage = 1, TotalPage = 2, HasMore = true, HasPrevious = false },
            new Forum { Fid = 1, Fname = "forum" });
        var comments = new Comments
        {
            Page = new PageT { CurrentPage = 1, TotalPage = 2, HasMore = true, HasPrevious = false },
            Forum = new ForumT(),
            Thread = new AioTieba4DotNet.Models.Threads.Thread { Content = new Content(), VirtualImage = new VirtualImagePf() },
            Post = new AioTieba4DotNet.Models.Threads.Post { Content = new Content() },
            Objs = []
        };
        var posts = new Posts
        {
            Page = new PageT { CurrentPage = 1, TotalPage = 2, HasMore = true, HasPrevious = false },
            Forum = new ForumT(),
            Thread = new AioTieba4DotNet.Models.Threads.Thread { Content = new Content(), VirtualImage = new VirtualImagePf() },
            Objs = []
        };
        var tailContainer = new TailContainer();

        tailContainer.Push(1);
        tailContainer.Push(2);

        Assert.AreEqual("aiotieba.api.tail", pythonApi.Path);
        Assert.AreEqual("boom", transportException.Message);
        Assert.AreEqual("inner", transportException.InnerException?.Message);
        Assert.IsNotNull(client.Forums);
        Assert.AreEqual(WebSocketState.None, connection.State);
        connection.Dispose();
        Assert.IsNotNull(delayStrategy);
        Assert.AreEqual(1, rankForums.Count);
        Assert.IsTrue(rankForums.HasMore);
        Assert.AreEqual(1, blocks.Count);
        Assert.IsTrue(blocks.HasMore);
        Assert.AreEqual(1, squareForums.Count);
        Assert.IsTrue(squareForums.HasMore);
        Assert.AreEqual(1, blacklists.Count);
        Assert.IsTrue(blacklists.HasMore);
        Assert.AreEqual(1, postLogs.Count);
        Assert.IsTrue(postLogs.HasMore);
        Assert.AreEqual(1, userLogs.Count);
        Assert.IsTrue(userLogs.HasMore);
        Assert.AreEqual("nick", recoverUserA.ShowName);
        Assert.AreEqual("nick/portrait", recoverUserB.LogName);
        Assert.AreEqual(string.Empty, recoverUserC.LogName);
        Assert.AreEqual("user", recoverUserD.ShowName);
        Assert.IsTrue(recoverUserA.Equals(recoverUserB));
        Assert.IsFalse(recoverUserA.Equals(new object()));
        Assert.AreEqual(recoverUserA.GetHashCode(), recoverUserB.GetHashCode());
        Assert.AreEqual("old", lastReplyerUser.ShowName);
        Assert.AreEqual("user", lastReplyerUser.LogName);
        Assert.AreEqual("old3/portrait3", lastReplyerUserFallback.LogName);
        Assert.AreEqual("user2", lastReplyer.LogName);
        Assert.IsTrue(lastReplyer.Equals(new LastReplyer { UserId = 2, UserName = "other" }));
        Assert.AreEqual(1UL, lastReplyers[0].Fid);
        Assert.IsTrue(lastReplyers.HasMore);
        Assert.IsTrue(comments.HasMore);
        Assert.IsTrue(posts.HasMore);
        Assert.AreEqual(2, tailContainer.Count);
        Assert.AreEqual(1, tailContainer[0]);
        Assert.IsTrue(((System.Collections.IEnumerable)tailContainer).GetEnumerator() is not null);
        Throws<ArgumentNullException>(() => _ = new RankForums([], null!));
        Throws<ArgumentNullException>(() => _ = new Blocks([], null!));
        Throws<ArgumentNullException>(() => _ = new SquareForums([], null!));
        Throws<ArgumentNullException>(() => _ = new BawuBlacklistUsers([], null!));
        Throws<ArgumentNullException>(() => _ = new BawuPostLogs([], null!));
        Throws<ArgumentNullException>(() => _ = new BawuUserLogs([], null!));
        Throws<ArgumentNullException>(() => _ = new LastReplyers([], null!, new Forum()));
        Throws<ArgumentNullException>(() => _ = new LastReplyers([], new LastReplyersPage(), null!));
    }

    [TestMethod]
    public void UtilityHelpers_CoverPaddingPortraitAndClientDisposePaths()
    {
        var androidId = Utils.GenerateAndroidId();
        var padded = Utils.ApplyPkcs7Padding([1, 2], 4);
        var unpadded = Utils.RemovePkcs7Padding([1, 2, 2, 2], 4);
        var client = new TiebaClient(new TiebaOptions
        {
            Bduss = new string('b', 192),
            Stoken = new string('s', 64),
            TransportMode = TiebaTransportMode.Http
        });

        Assert.AreEqual(16, androidId.Length);
        Assert.AreEqual(4, padded.Length);
        Assert.AreEqual(2, padded[2]);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, unpadded);
        Assert.IsTrue(Utils.IsPortrait("tb.1.user"));
        Assert.IsFalse(Utils.IsPortrait("plain"));
        Assert.AreEqual(12000, Utils.TbNumToInt("1.2万"));
        Assert.AreEqual(123, Utils.TbNumToInt("123"));
        Throws<ArgumentException>(() => Utils.RemovePkcs7Padding([], 4));
        Throws<ArgumentException>(() => Utils.RemovePkcs7Padding([0], 4));
        Throws<ArgumentException>(() => Utils.RemovePkcs7Padding([2], 4));
        client.Dispose();
    }

    [TestMethod]
    public async Task TailHelpersAndEqualityBranches_CoverFinalTinyMethods()
    {
        var delayStrategy = SystemTiebaWebSocketDelayStrategy.Instance;
        var transportException = new TiebaTransportException("plain");
        var core = new RecordingHttpCore { AppFormResponse = "{\"error_code\":0,\"error_msg\":\"\"}" };
        core.SetAccount(new Account(new string('b', 192), new string('s', 64)) { Tbs = "tbs" });

        await delayStrategy.DelayAsync(TimeSpan.Zero, default);
        await new UndislikeForum(core).RequestAsync(1);
        await new RemoveFan(core).RequestAsync(2);

        var fragVoiceFromAbstract = FragVoiceMapper.FromTbData(new global::PostInfoList.Types.PostInfoContent.Types.Abstract
        {
            VoiceMd5 = "voice-md5",
            DuringTime = "2500"
        });
        var fragItem = FragItemMapper.FromTbData(new PbContent { Item = new PbContent.Types.Item { ItemName = "badge" } });
        var block = new Block { UserId = 1, UserName = "user", NickNameOld = "old", Day = 3 };
        var blockFallback = new Block { UserId = 2, UserName = "user2", NickNameOld = string.Empty, Day = 4 };
        var squareForum = new SquareForum { Fid = 1, Fname = "square", MemberNum = 2, PostNum = 3, IsFollowed = true };
        var squareForumOther = new SquareForum { Fid = 2, Fname = "square2", MemberNum = 3, PostNum = 4, IsFollowed = false };
        var blacklisted = new BawuBlacklistUser { UserId = 1, Portrait = "portrait", UserName = "user" };
        var blacklistedOther = new BawuBlacklistUser { UserId = 2, Portrait = "portrait2", UserName = string.Empty };
        var lastReplyerUser = new LastReplyerUser { UserId = 1, Portrait = "portrait", UserName = "user", NickNameOld = "old" };
        var lastReplyerUserFallback = new LastReplyerUser { UserId = 2, Portrait = "portrait2", UserName = string.Empty, NickNameOld = "old2" };
        var lastReplyer = new LastReplyer { UserId = 3, UserName = "user3", NickNameOld = "old3" };
        var lastReplyerOther = new LastReplyer { UserId = 4, UserName = string.Empty, NickNameOld = string.Empty };
        var lastReplyerThread = new LastReplyerThread
        {
            Title = "title",
            Fid = 1,
            Fname = "forum",
            Tid = 2,
            Pid = 3,
            User = lastReplyerUser,
            LastReplyer = lastReplyer,
            IsGood = true,
            IsTop = false,
            CreateTime = 4,
            LastTime = 5
        };
        var recoverUser = new RecoverUser { UserName = "recover", Portrait = "portrait", NickNameNew = "nick" };
        var fragItemText = fragItem.ToString();
        var content = new Content();
        var setFragsIndex = typeof(Content).GetMethod("SetFragsIndex", BindingFlags.NonPublic | BindingFlags.Static);
        var frags = new List<IFrag>
        {
            new FragText { Text = "a" },
            new FragText { Text = "b" }
        };
        var comments = new Comments
        {
            Page = new PageT { CurrentPage = 1, TotalPage = 1 },
            Forum = new ForumT(),
            Thread = new AioTieba4DotNet.Models.Threads.Thread { Content = new Content(), VirtualImage = new VirtualImagePf() },
            Post = new AioTieba4DotNet.Models.Threads.Post { Content = new Content() },
            Objs = []
        };
        var posts = new Posts
        {
            Page = new PageT { CurrentPage = 1, TotalPage = 1 },
            Forum = new ForumT(),
            Thread = new AioTieba4DotNet.Models.Threads.Thread { Content = new Content(), VirtualImage = new VirtualImagePf() },
            Objs = []
        };

        setFragsIndex!.Invoke(null, [frags]);

        Assert.AreEqual("plain", transportException.Message);
        Assert.AreEqual("FragItem Text: badge", fragItemText);
        Assert.AreEqual("voice-md5", fragVoiceFromAbstract.Md5);
        Assert.AreEqual(2, fragVoiceFromAbstract.Duration);
        Assert.AreEqual("old", block.NickName);
        Assert.AreEqual("old", block.ShowName);
        Assert.IsTrue(block.Equals(new Block { UserId = 1, UserName = "different" }));
        Assert.IsFalse(block.Equals(blockFallback));
        Assert.AreEqual(block.GetHashCode(), new Block { UserId = 1 }.GetHashCode());
        Assert.AreEqual("square", squareForum.Fname);
        Assert.IsTrue(squareForum.Equals(new SquareForum { Fid = 1 }));
        Assert.IsFalse(squareForum.Equals(squareForumOther));
        Assert.AreEqual(squareForum.GetHashCode(), new SquareForum { Fid = 1 }.GetHashCode());
        Assert.AreEqual("user", blacklisted.LogName);
        Assert.IsTrue(blacklisted.Equals(new BawuBlacklistUser { UserId = 1 }));
        Assert.IsFalse(blacklisted.Equals(blacklistedOther));
        Assert.AreEqual(blacklisted.GetHashCode(), new BawuBlacklistUser { UserId = 1 }.GetHashCode());
        Assert.AreEqual("old", lastReplyerUser.NickName);
        Assert.AreEqual("old", lastReplyerUser.ShowName);
        Assert.AreEqual("user", lastReplyerUser.LogName);
        Assert.AreEqual("old2/portrait2", lastReplyerUserFallback.LogName);
        Assert.AreEqual(lastReplyerUser.GetHashCode(), new LastReplyerUser { UserId = 1 }.GetHashCode());
        Assert.AreEqual("old3", lastReplyer.NickName);
        Assert.AreEqual("old3", lastReplyer.ShowName);
        Assert.AreEqual("user3", lastReplyer.LogName);
        Assert.IsTrue(lastReplyer.Equals(new LastReplyer { UserId = 3 }));
        Assert.IsFalse(lastReplyer.Equals(lastReplyerOther));
        Assert.AreEqual(lastReplyer.GetHashCode(), new LastReplyer { UserId = 3 }.GetHashCode());
        Assert.AreEqual(lastReplyerThread.GetHashCode(), new LastReplyerThread { Pid = 3 }.GetHashCode());
        Assert.AreEqual("nick", recoverUser.NickName);
        Assert.AreEqual(1, frags[1].Index);
        Assert.AreEqual(0, content.Frags.Count);
        Assert.IsTrue(comments.HasMore == false);
        Assert.IsFalse(posts.HasMore);
    }

    [TestMethod]
    public async Task TinyModelDefaultsAndApiWrappers_CoverConstructorAndInitializerPaths()
    {
        var fragItem = new FragItem();
        var mappedFragItem = FragItemMapper.FromTbData(new PbContent
        {
            Item = new PbContent.Types.Item { ItemName = "badge" }
        });
        var forum = new Forum();
        var forumDetail = new ForumDetail();
        var userPost = new UserPost { Contents = new Content() };
        var atsCore = new RecordingHttpCore { AppFormResponse = "{\"error_code\":0,\"error_msg\":\"\"}" };
        var blacklistCore = new RecordingHttpCore { AppFormResponse = "{\"error_code\":0,\"error_msg\":\"\"}" };

        var ats = await new GetAts(atsCore).RequestAsync(1);
        var blacklist = await new GetBlacklist(blacklistCore).RequestAsync();

        Assert.AreEqual(string.Empty, fragItem.Text);
        Assert.AreEqual("FragItem", fragItem.GetFragType());
        Assert.AreEqual(0, fragItem.ToDict().Count);
        Assert.AreEqual("badge", mappedFragItem.Text);
        Assert.AreEqual(0L, forum.Fid);
        Assert.AreEqual(string.Empty, forum.Fname);
        Assert.AreEqual("Fid: 0", forum.ToString().Split(',')[0]);
        Assert.AreEqual(0UL, forumDetail.Fid);
        Assert.AreEqual(string.Empty, forumDetail.OriginAvatar);
        Assert.AreEqual(0, userPost.CreateTime);
        Assert.AreEqual(string.Empty, userPost.Contents.Text);
        Assert.AreEqual("CreateTime: 0", userPost.ToString().Split(',')[^1].Trim());
        Assert.AreEqual("/c/u/feed/atme", atsCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("1", atsCore.GetAppFormValue("pn"));
        Assert.AreEqual("/c/u/user/userBlackPage", blacklistCore.LastAppFormUri?.AbsolutePath);
        Assert.IsNotNull(ats);
        Assert.IsNotNull(blacklist);
    }

    [TestMethod]
    public void ContainerAndWebSocketCoreDefaults_CoverRemainingLowLineTypes()
    {
        var tabs = new TabMap([
            new KeyValuePair<string, int>("全部", 1),
            new KeyValuePair<string, int>("热门", 2)
        ]);
        var emptyTabs = new TabMap();
        var dislikeForums = new DislikeForums(
            [new DislikeForum { Fid = 3, Fname = "forum-a", MemberNum = 4, PostNum = 5, ThreadNum = 6, IsFollowed = true }],
            new DislikeForumsPage { CurrentPage = 1, HasMore = true, HasPrevious = false });
        var selfFollowForumsV1 = new SelfFollowForumsV1(
            [new SelfFollowForumV1 { Fid = 7, Fname = "forum-b", Level = 8 }],
            new SelfFollowForumsV1Page { CurrentPage = 2, TotalPage = 3, HasMore = true, HasPrevious = true });
        var image = new FragImage
        {
            Src = "https://imgsrc.baidu.com/forum/pic/item/0123456789abcdef0123456789abcdef.jpg",
            BigSrc = "https://example.com/big.jpg",
            OriginSrc = "https://example.com/origin.jpg",
            OriginSize = 64,
            ShowWidth = 320,
            ShowHeight = 240,
            Hash = "0123456789abcdef0123456789abcdef"
        };
        var emptyImage = new FragImage();
        var imageHashRegex = (System.Text.RegularExpressions.Regex)typeof(FragImage)
            .GetField("ImageHashExp", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        var core = new WebsocketCore();
        var account = new Account(new string('b', 192), new string('s', 64));
        var wsConnection = new RecordingWebSocketConnection();
        var wsCore = new WebsocketCore(null, new TiebaWebSocketFrameCodec(), new TiebaWebSocketHandshakeBuilder(),
            new RecordingWebSocketConnectionFactory(wsConnection),
            new TiebaWebSocketOptions(new Uri("ws://unit.test"), TimeSpan.FromMilliseconds(1)),
            new ImmediateDelayStrategy());

        core.SetAccount(account);
        var packed = core.PackWsBytes("ping"u8.ToArray(), 7, 9, false);
        var (parsedPayload, parsedCmd, parsedReqId) = core.ParseWsBytes(packed);
        var sendTask = wsCore.SendAsync(new WSReq
        {
            Cmd = 301001,
            ReqId = 1,
            Payload = new WSReq.Types.Payload { Data = ByteString.CopyFromUtf8("ping") }
        });
        sendTask.GetAwaiter().GetResult();
        wsCore.Dispose();

        var observeBackground = typeof(TiebaWebSocketEngine).GetMethod("ObserveBackgroundTaskAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var combineFailures = typeof(TiebaWebSocketEngine).GetMethod("CombineFailures",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var observeCanceled = observeBackground.Invoke(null,
            [Task.FromCanceled(cts.Token), cts.Token, true]);
        var observeFaulted = observeBackground.Invoke(null,
            [Task.FromException(new InvalidOperationException("boom")), CancellationToken.None, true]);
        var combinedFailure = (AggregateException)combineFailures.Invoke(null,
            [new InvalidOperationException("primary"), new InvalidOperationException("secondary")])!;

        Assert.AreEqual(2, tabs.Count);
        Assert.AreEqual(2, tabs.Map.Count);
        Assert.AreEqual(1, tabs["全部"]);
        Assert.IsTrue(tabs.ContainsKey("热门"));
        Assert.AreEqual(2, tabs.Keys.Count());
        Assert.AreEqual(2, tabs.Values.Count());
        Assert.AreEqual(2, tabs.ToList().Count);
        Assert.IsNotNull(((System.Collections.IEnumerable)tabs).GetEnumerator());
        Assert.IsFalse(emptyTabs.TryGetValue("missing", out _));
        Assert.AreEqual(1, dislikeForums.Count);
        Assert.IsTrue(dislikeForums.HasMore);
        Assert.AreEqual("forum-a", dislikeForums[0].Fname);
        Assert.AreEqual(1, selfFollowForumsV1.Count);
        Assert.IsTrue(selfFollowForumsV1.HasMore);
        Assert.AreEqual(8, selfFollowForumsV1[0].Level);
        Assert.AreEqual(string.Empty, emptyImage.Src);
        Assert.IsTrue(imageHashRegex.IsMatch("/0123456789abcdef0123456789abcdef."));
        Assert.AreEqual("FragImage", image.GetFragType());
        Assert.AreEqual(5, image.ToDict().Count);
        StringAssert.Contains(image.ToString(), "Hash: 0123456789abcdef0123456789abcdef");
        CollectionAssert.AreEqual("ping"u8.ToArray(), parsedPayload);
        Assert.AreEqual(7, parsedCmd);
        Assert.AreEqual(9, parsedReqId);
        Assert.AreEqual(account.Bduss, core.Account?.Bduss);
        Assert.IsNotNull(wsConnection.LastSentFrame);
        var sentFrame = new TiebaWebSocketFrameCodec().Parse(wsConnection.LastSentFrame!, null);
        Assert.AreEqual(301001, sentFrame.Cmd);
        CollectionAssert.AreEqual("ping"u8.ToArray(), sentFrame.Data);
        Assert.IsNull(observeCanceled);
        Assert.IsInstanceOfType<InvalidOperationException>(observeFaulted);
        Assert.AreEqual(2, combinedFailure.InnerExceptions.Count);
    }

    [TestMethod]
    public void ResidualValidationAndEqualityBranches_CloseHotspotTails()
    {
        var responseException = Throws<TieBaServerException>(() => ApiResponseValidator.CheckError(1, null));
        var parseException = Throws<TieBaServerException>(() => ApiResponseValidator.ParseJsonBody("{\"error_code\":2}"));
        var nullOptionsErrors = TiebaOptionsValidator.GetValidationErrors(null);
        var store = new MessageCursorStore();
        var validateBatchIds = typeof(ThreadProtocol).GetMethod("ValidateBatchIds", BindingFlags.NonPublic | BindingFlags.Static)!;
        var invalidBatch = Throws<TargetInvocationException>(() => validateBatchIds.Invoke(null,
            ["ids", (IReadOnlyList<long>)new long[] { 1, 0 }]));
        validateBatchIds.Invoke(null, ["ids", (IReadOnlyList<long>)new long[] { 1, 2 }]);

        store.Initialize([
            new WsMsgGroupInfo { GroupId = 88, GroupType = 6, LastMessageId = 5 }
        ]);

        var blankShare = new ShareThread
        {
            Content = new Content { Texts = [], Frags = [] },
            Title = string.Empty
        };
        var squareForum = new SquareForum { Fid = 1 };
        var block = new Block { UserId = 2 };
        var bawuBlacklistUser = new BawuBlacklistUser { UserId = 3 };
        var bawuUser = new BawuUser { UserId = 4 };
        var lastReplyerUser = new LastReplyerUser { UserId = 5 };

        Assert.AreEqual(1, responseException.Code);
        Assert.AreEqual(2, parseException.Code);
        StringAssert.Contains(responseException.Message, "Code: 1");
        CollectionAssert.AreEqual(new[] { "TiebaOptions configuration is required." }, nullOptionsErrors.ToArray());
        Assert.AreEqual(5L, store.GetLastMessageId(88));
        Assert.AreEqual(0L, store.GetLastMessageId(999));
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(invalidBatch.InnerException);
        StringAssert.Contains(blankShare.Text, "System.Collections.Generic.List");
        Assert.IsFalse(squareForum.Equals("not-a-forum"));
        Assert.IsFalse(block.Equals("not-a-block"));
        Assert.IsFalse(bawuBlacklistUser.Equals("not-a-blacklist-user"));
        Assert.AreEqual("4", bawuUser.LogName);
        Assert.AreEqual("5", lastReplyerUser.LogName);
    }

    [TestMethod]
    public async Task HttpExecutionPolicyAndAddPostPacking_CloseResidualHotspotBranches()
    {
        var retryPolicy = TiebaHttpExecutionPolicy.FromOptions(new TiebaOptions
        {
            RequestTimeout = TimeSpan.FromSeconds(1),
            MaxReadRetryAttempts = 1
        });
        var retryDescriptor = TiebaHttpRequestDescriptor.AppForm(new Uri("https://tiebac.baidu.com/retry"),
            [new KeyValuePair<string, string>("a", "1")]);
        var operationCanceledAttempts = 0;
        using var client = new HttpClient(new SwitchingHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)));
        var operationCanceledResponse = await retryPolicy.SendAsync(client,
            async ct =>
            {
                operationCanceledAttempts++;
                if (operationCanceledAttempts == 1)
                    throw new OperationCanceledException();

                return await TiebaHttpRequestFactory.CreateMessageAsync(retryDescriptor, ct);
            },
            allowRetry: true,
            requestKind: TiebaHttpRequestKind.AppForm,
            cancellationToken: CancellationToken.None);

        var packProto = typeof(AddPost).GetMethod("PackProto", BindingFlags.NonPublic | BindingFlags.Static)!;
        var parseBody = typeof(AddPost).GetMethod("ParseBody", BindingFlags.NonPublic | BindingFlags.Static)!;
        var authenticated = new Account(new string('b', 192), new string('s', 64))
        {
            ClientId = "client-id",
            Tbs = "tbs-1",
            ZId = "zid-1",
            C3Aid = "c3aid-1",
            SampleId = "sample-1",
            AndroidId = "0123456789abcdef"
        };
        var guest = new Account(new string('b', 192), new string('s', 64));
        var packedAuthenticated = (AddPostReqIdl)packProto.Invoke(null, [authenticated, "forum", 1UL, 2L, "show", "content"])!;
        var packedGuest = (AddPostReqIdl)packProto.Invoke(null, [guest, "forum", 1UL, 2L, string.Empty, "content"])!;
        var needVcode = new AddPostResIdl
        {
            Error = new Error { Errorno = 0, Errmsg = string.Empty },
            Data = new AddPostResIdl.Types.DataRes
            {
                Info = new AddPostResIdl.Types.DataRes.Types.PostAntiInfo { NeedVcode = "1" }
            }
        }.ToByteArray();
        var needVcodeException = Throws<TargetInvocationException>(() => parseBody.Invoke(null, [needVcode]));

        Assert.AreEqual(2, operationCanceledAttempts);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, operationCanceledResponse.StatusCode);
        Assert.AreEqual("client-id", packedAuthenticated.Data.Common.ClientId);
        Assert.AreEqual("tbs-1", packedAuthenticated.Data.Common.Tbs);
        Assert.AreEqual("zid-1", packedAuthenticated.Data.Common.ZId);
        Assert.AreEqual("c3aid-1", packedAuthenticated.Data.Common.C3Aid);
        Assert.AreEqual("sample-1", packedAuthenticated.Data.Common.SampleId);
        Assert.AreEqual("0123456789abcdef", packedAuthenticated.Data.Common.AndroidId);
        Assert.AreEqual(string.Empty, packedGuest.Data.Common.ClientId);
        Assert.AreEqual(string.Empty, packedGuest.Data.Common.Tbs);
        Assert.AreEqual(string.Empty, packedGuest.Data.Common.ZId);
        Assert.AreEqual(guest.C3Aid, packedGuest.Data.Common.C3Aid);
        Assert.AreEqual(string.Empty, packedGuest.Data.Common.SampleId);
        Assert.AreEqual("Need verify code", needVcodeException.InnerException?.Message);
    }

    private static ProfileResIdl CreateProfileResponse()
    {
        return new ProfileResIdl
        {
            Error = new global::Error { Errorno = 0, Errmsg = string.Empty },
            Data = new ProfileResIdl.Types.DataRes
            {
                User = new User
                {
                    Id = 42,
                    Portrait = "tb.1.profile?012345678901",
                    Name = "profile-user",
                    NameShow = "Profile User"
                }
            }
        };
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public Account? Account { get; private set; }
        public HttpClient HttpClient { get; } = new();
        public string AppFormResponse { get; set; } = "{\"error_code\":0,\"error_msg\":\"\"}";
        public string CustomResponse { get; set; } = "{}";
        public byte[] AppProtoResponse { get; set; } = [];
        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];
        public HttpRequestMessage? LastCustomRequest { get; private set; }
        public List<KeyValuePair<string, string>> LastCustomFormData { get; private set; } = [];
        public Uri? LastAppProtoUri { get; private set; }
        public byte[]? LastAppProtoRequestData { get; private set; }

        public void SetAccount(Account newAccount) => Account = newAccount;

        public async Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            LastCustomRequest = requestFactory();
            LastCustomFormData = await ReadFormDataAsync(LastCustomRequest, cancellationToken);
            return CustomResponse;
        }

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
            LastAppProtoRequestData = [.. data];
            return Task.FromResult(AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public string GetAppFormValue(string key) => LastAppFormData.Last(entry => entry.Key == key).Value;

        public string GetCustomFormValue(string key) => LastCustomFormData.Last(entry => entry.Key == key).Value;

        private static async Task<List<KeyValuePair<string, string>>> ReadFormDataAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is null)
                return [];

            var payload = await request.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
                return [];

            return payload.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(static part => part.Split('=', 2))
                .Select(static parts => new KeyValuePair<string, string>(
                    Uri.UnescapeDataString(parts[0].Replace('+', ' ')),
                    parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty))
                .ToList();
        }
    }

    private sealed class RecordingWebSocketConnection : ITiebaWebSocketConnection
    {
        public WebSocketState State { get; private set; } = WebSocketState.None;

        public byte[]? LastSentFrame { get; private set; }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            State = WebSocketState.Open;
            return Task.CompletedTask;
        }

        public Task SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastSentFrame = buffer.ToArray();
            return Task.CompletedTask;
        }

        public Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken)
        {
            var pending = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
            return pending.Task;
        }

        public Task CloseAsync(System.Net.WebSockets.WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            State = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public void Dispose() => State = WebSocketState.Closed;
    }

    private sealed class RecordingWebSocketConnectionFactory(RecordingWebSocketConnection connection)
        : ITiebaWebSocketConnectionFactory
    {
        public ITiebaWebSocketConnection CreateConnection() => connection;
    }

    private sealed class SwitchingHandler(params Func<int, HttpResponseMessage>[] behaviors) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var index = Math.Min(CallCount, behaviors.Length - 1);
            CallCount++;
            return Task.FromResult(behaviors[index](CallCount - 1));
        }
    }

    private sealed class ImmediateDelayStrategy : ITiebaWebSocketDelayStrategy
    {
        public Task DelayAsync(TimeSpan interval, CancellationToken cancellationToken) => Task.CompletedTask;
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

    private static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
    }

    private sealed class TailContainer : Containers<int>
    {
        public TailContainer() : base([])
        {
        }

        public void Push(int value) => Add(value);
    }
}
