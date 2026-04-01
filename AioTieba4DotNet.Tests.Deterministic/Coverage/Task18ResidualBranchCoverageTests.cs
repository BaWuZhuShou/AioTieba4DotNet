#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.AddBlacklistOld;
using AioTieba4DotNet.Api.DelBlacklistOld;
using AioTieba4DotNet.Api.GetBawuPostlogs;
using AioTieba4DotNet.Api.GetBawuUserlogs;
using AioTieba4DotNet.Api.GetSelfFollowForumsV1;
using AioTieba4DotNet.Api.GetUserForumInfo;
using AioTieba4DotNet.Api.LikeForum;
using AioTieba4DotNet.Api.Recommend;
using AioTieba4DotNet.Api.SignForums;
using AioTieba4DotNet.Api.UnlikeForum;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Coverage;

[TestClass]
public sealed class Task18ResidualBranchCoverageTests
{
    [TestMethod]
    public async Task ResidualJsonApiBranches_CloseFallbackPaths()
    {
        var httpCore = new RoutingHttpCore();
        httpCore.EnqueueAppFormResponse("/c/c/user/userMuteAdd", "{\"errorno\":0,\"errmsg\":\"\"}");
        httpCore.EnqueueAppFormResponse("/c/c/user/userMuteDel", "{\"errorno\":0,\"errmsg\":\"\"}");
        httpCore.EnqueueAppFormResponse("/c/c/forum/like", "{\"error_code\":0,\"error_msg\":\"\"}");
        httpCore.EnqueueAppFormResponse("/c/c/forum/unlike", "{\"error_code\":0,\"error_msg\":\"\"}");
        httpCore.EnqueueCustomResponse("{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":0}}" );
        httpCore.EnqueueAppFormResponse("/c/c/bawu/pushRecomToPersonalized", "{\"error_code\":0,\"error_msg\":\"\"}");
        httpCore.EnqueueAppFormResponse("/c/f/forum/getUserForumLevelInfo", "{\"error_code\":15,\"error_msg\":\"direct-msg\"}");
        httpCore.EnqueueWebGetResponse("/mg/o/getForumHome", "{\"errno\":0,\"errmsg\":\"\"}");
        httpCore.EnqueueWebGetResponse("/bawu2/platform/listPostLog", string.Empty);
        httpCore.EnqueueWebGetResponse("/bawu2/platform/listUserLog", string.Empty);

        Assert.IsTrue(await new AddBlacklistOld(httpCore).RequestAsync(1));
        Assert.IsTrue(await new DelBlacklistOld(httpCore).RequestAsync(2));
        Assert.IsTrue(await new LikeForum(httpCore).RequestAsync(3));
        Assert.IsTrue(await new UnlikeForum(httpCore).RequestAsync(4));
        Assert.IsTrue(await new SignForums(httpCore).RequestAsync());

        var recommendException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new Recommend(httpCore).RequestAsync(5, 6));
        var userForumException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new GetUserForumInfo(httpCore).RequestAsync(7, "tb.1.user"));
        var selfFollowException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new GetSelfFollowForumsV1(httpCore).RequestAsync(8, 9));
        var postLogs = await new GetBawuPostlogs(httpCore).RequestAsync("forum", 2, string.Empty, BawuSearchType.Operator,
            DateTimeOffset.UnixEpoch, null, 0);
        var userLogs = await new GetBawuUserlogs(httpCore).RequestAsync("forum", 3, "operator", BawuSearchType.Operator,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), 0);

        StringAssert.Contains(recommendException.Message, "Recommend failed.");
        StringAssert.Contains(userForumException.Message, "direct-msg");
        StringAssert.Contains(selfFollowException.Message, "Unable to parse self follow forums v1 data.");
        Assert.AreEqual("0", httpCore.GetWebGetValueForPath("/bawu2/platform/listPostLog", "begin"));
        Assert.IsTrue(long.Parse(httpCore.GetWebGetValueForPath("/bawu2/platform/listPostLog", "end")) > 0);
        Assert.AreEqual("op_uname", httpCore.GetWebGetValueForPath("/bawu2/platform/listUserLog", "stype"));
        Assert.AreEqual((DateTimeOffset.UnixEpoch.AddDays(1).ToUnixTimeSeconds()).ToString(),
            httpCore.GetWebGetValueForPath("/bawu2/platform/listUserLog", "end"));
        Assert.AreEqual(0, postLogs.Count);
        Assert.AreEqual(0, userLogs.Count);
    }

    [TestMethod]
    public void ResidualMapperBranches_CloseFallbackPaths()
    {
        var profile = UserInfoPfMapper.FromTbData(new ProfileResIdl.Types.DataRes
        {
            User = new User
            {
                Id = 1,
                Portrait = string.Empty,
                NewGodData = new User.Types.NewGodInfo { Status = 0 },
                PrivSets = new User.Types.PrivSets { Like = 0, Reply = 0 }
            },
            AntiStat = new ProfileResIdl.Types.DataRes.Types.Anti { BlockStat = 1, HideStat = 1, DaysTofree = 0 }
        });
        var profileRich = UserInfoPfMapper.FromTbData(new ProfileResIdl.Types.DataRes
        {
            User = new User
            {
                Id = 11,
                Portrait = "tb.1.profile?012345678901",
                Name = "rich-user",
                NameShow = "Rich User",
                TiebaUid = "123",
                TbAge = "3.5",
                Gender = (int)Gender.Male,
                PrivSets = new User.Types.PrivSets { Like = 2, Reply = 6 },
                NewTshowIcon = { new User.Types.TshowInfo { Name = "vip" } },
                NewGodData = new User.Types.NewGodInfo { Status = 1 },
                UserGrowth = new User.Types.UserGrowth { LevelId = 7 },
                Iconinfo = { new User.Types.Icon { Name = "icon" } },
                IpAddress = "127.0.0.1"
            },
            AntiStat = new ProfileResIdl.Types.DataRes.Types.Anti { BlockStat = 1, HideStat = 1, DaysTofree = 31 },
            UserAgreeInfo = new ProfileResIdl.Types.DataRes.Types.UserAgreeInfo { TotalAgreeNum = 5 }
        });
        var profileNullBackingsProto = new ProfileResIdl.Types.DataRes
        {
            User = new User
            {
                Id = 13,
                Portrait = "tb.1.null-profile?012345678901",
                Name = "null-profile",
                NameShow = "Null Profile",
                Gender = (int)Gender.Female
            }
        };
        SetPrivateField(profileNullBackingsProto.User, "portrait_", null);
        SetPrivateField(profileNullBackingsProto.User, "tbAge_", null);
        SetPrivateField(profileNullBackingsProto.User, "tiebaUid_", null);
        SetPrivateField(profileNullBackingsProto.User, "privSets_", null);
        SetPrivateField(profileNullBackingsProto.User, "newGodData_", null);
        SetPrivateField(profileNullBackingsProto.User, "userGrowth_", null);
        var profileNullBackings = UserInfoPfMapper.FromTbData(profileNullBackingsProto);
        var threadUser = UserInfoTMapper.FromTbData(new User
        {
            Id = 2,
            Portrait = string.Empty,
            NewGodData = new User.Types.NewGodInfo { Status = 0 },
            PrivSets = new User.Types.PrivSets { Like = 0, Reply = 0 }
        });
        var threadUserNullBackingsProto = new User
        {
            Id = 14,
            Portrait = "tb.1.null-thread?012345678901",
            Name = "null-thread",
            NameShow = "Null Thread",
            Gender = (int)Gender.Female
        };
        SetPrivateField(threadUserNullBackingsProto, "portrait_", null);
        SetPrivateField(threadUserNullBackingsProto, "tbAge_", null);
        SetPrivateField(threadUserNullBackingsProto, "tiebaUid_", null);
        SetPrivateField(threadUserNullBackingsProto, "privSets_", null);
        SetPrivateField(threadUserNullBackingsProto, "newGodData_", null);
        SetPrivateField(threadUserNullBackingsProto, "userGrowth_", null);
        var threadUserNullBackings = UserInfoTMapper.FromTbData(threadUserNullBackingsProto);
        var threadUserRich = UserInfoTMapper.FromTbData(new User
        {
            Id = 12,
            Portrait = "tb.1.thread?012345678901",
            Name = "thread-user",
            NameShow = "Thread User",
            LevelId = 6,
            IsBawu = 1,
            PrivSets = new User.Types.PrivSets { Like = 2, Reply = 6 },
            NewTshowIcon = { new User.Types.TshowInfo { Name = "vip" } },
            NewGodData = new User.Types.NewGodInfo { Status = 1 },
            UserGrowth = new User.Types.UserGrowth { LevelId = 8 },
            Iconinfo = { new User.Types.Icon { Name = "icon" } },
            IpAddress = "10.0.0.1"
        });
        var uidUser = UserInfoTUidMapper.FromTbData(new User { Id = 3, Portrait = "tb.1.uid", TiebaUid = string.Empty, TbAge = string.Empty });
        var sharedUser = UserInfoMapper.FromTbData(new User { Id = 4, Portrait = "tb.1.shared", Name = "user", NameShow = "User" });
        var groupUser = UserInfoMapper.FromTbData(new GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.MsgInfo.Types.UserInfo
        {
            UserId = 5,
            Portrait = "tb.1.group",
            UserName = "group-user"
        });
        var blacklistFallback = BlacklistUserMapper.FromTbData(new JObject());
        var blacklistSparsePerms = BlacklistUserMapper.FromTbData(new JObject
        {
            ["perm_list"] = new JObject()
        });
        var recoverInfo = RecoverInfoMapper.FromTbData(new JObject { ["thread_info"] = new JObject() });
        var recoverContent = RecoverContentMapper.FromTbData(new JObject
        {
            ["content_detail"] = new JArray(new JObject { ["type"] = 2, ["value"] = "skip" }),
            ["all_pics"] = new JArray(new JObject())
        });
        var forumStats = ForumStatisticsMapper.FromTbData(new JArray { new JObject { ["group"] = new JArray(new JObject()) } });
        var thread = ThreadMapper.FromTbData(new ThreadInfo
        {
            IsShareThread = 1,
            OriginThreadInfo = new ThreadInfo.Types.OriginThreadInfo { Pid = 0 }
        });
        var shareOriginNullTid = new ThreadInfo.Types.OriginThreadInfo
        {
            Title = "shared-null-tid",
            Fid = 77,
            Fname = "share-forum",
            Pid = 88
        };
        SetPrivateField(shareOriginNullTid, "tid_", null);
        var shareNullTid = ShareThreadMapper.FromTbData(shareOriginNullTid);
        var tabMap = TabMapMapper.FromTbData(new SearchPostForumResIdl.Types.DataRes
        {
            ExactMatch = new SearchPostForumResIdl.Types.DataRes.Types.SearchForum()
        });
        var tabMapNull = TabMapMapper.FromTbData(null);
        var virtualImage = VirtualImagePfMapper.FromTbData(new User.Types.VirtualImageInfo
        {
            IssetVirtualImage = 0,
            PersonalState = new User.Types.VirtualImageInfo.Types.PersonalState { Text = "off" }
        });
        var virtualImageThreadNull = VirtualImagePfMapper.FromTbData((ThreadInfo)null!);
        var virtualImageThreadEmpty = VirtualImagePfMapper.FromTbData(new ThreadInfo
        {
            CustomFigure = new ThreadInfo.Types.CustomFigure { BackgroundValue = string.Empty },
            CustomState = new ThreadInfo.Types.CustomState { Content = string.Empty }
        });
        var virtualImageUserNull = VirtualImagePfMapper.FromTbData((User.Types.VirtualImageInfo)null!);
        var bawuUser = new BawuUser { UserId = 6 };
        var bawuUserRich = new BawuUser { UserId = 7, UserName = "user7", NickNameNew = "nick7", Portrait = "portrait7" };
        var bawuUserPortraitOnly = new BawuUser { UserId = 8, UserName = string.Empty, NickNameNew = "nick8", Portrait = "portrait8" };

        Assert.AreEqual(string.Empty, profile.Portrait);
        Assert.IsFalse(profile.IsGod);
        Assert.IsFalse(profile.IsBlocked);
        Assert.AreEqual(11L, profileRich.UserId);
        Assert.AreEqual("tb.1.profile", profileRich.Portrait);
        Assert.AreEqual("rich-user", profileRich.UserName);
        Assert.AreEqual("Rich User", profileRich.NickNameNew);
        Assert.AreEqual(123L, profileRich.TiebaUid);
        Assert.AreEqual(3.5f, profileRich.Age);
        Assert.IsTrue(profileRich.IsVip);
        Assert.IsTrue(profileRich.IsGod);
        Assert.IsTrue(profileRich.IsBlocked);
        Assert.AreEqual(PrivLike.Friend, profileRich.PrivLike);
        Assert.AreEqual(PrivReply.Follow, profileRich.PrivReply);
        Assert.AreEqual(13L, profileNullBackings.UserId);
        Assert.AreEqual(string.Empty, profileNullBackings.Portrait);
        Assert.AreEqual(0L, profileNullBackings.TiebaUid);
        Assert.AreEqual(0f, profileNullBackings.Age);
        Assert.AreEqual(PrivLike.Public, profileNullBackings.PrivLike);
        Assert.AreEqual(PrivReply.All, profileNullBackings.PrivReply);
        Assert.AreEqual(PrivLike.Public, threadUser!.PrivLike);
        Assert.AreEqual(PrivReply.All, threadUser.PrivReply);
        Assert.AreEqual(14L, threadUserNullBackings.UserId);
        Assert.AreEqual(string.Empty, threadUserNullBackings.Portrait);
        Assert.AreEqual(PrivLike.Public, threadUserNullBackings.PrivLike);
        Assert.AreEqual(PrivReply.All, threadUserNullBackings.PrivReply);
        Assert.AreEqual(12L, threadUserRich!.UserId);
        Assert.AreEqual("tb.1.thread", threadUserRich.Portrait);
        Assert.AreEqual("thread-user", threadUserRich.UserName);
        Assert.AreEqual("Thread User", threadUserRich.NickNameNew);
        Assert.AreEqual(6, threadUserRich.Level);
        Assert.IsTrue(threadUserRich.IsBawu);
        Assert.IsTrue(threadUserRich.IsVip);
        Assert.IsTrue(threadUserRich.IsGod);
        Assert.AreEqual(PrivLike.Friend, threadUserRich.PrivLike);
        Assert.AreEqual(PrivReply.Follow, threadUserRich.PrivReply);
        Assert.AreEqual("tb.1.uid", uidUser.Portrait);
        Assert.AreEqual(0L, uidUser.TiebaUid);
        Assert.AreEqual(4L, sharedUser!.UserId);
        Assert.AreEqual("tb.1.shared", sharedUser.Portrait);
        Assert.AreEqual(5L, groupUser.UserId);
        Assert.AreEqual("group-user", groupUser.UserName);
        Assert.IsFalse(blacklistFallback.BlockFollow);
        Assert.IsFalse(blacklistFallback.BlockInteract);
        Assert.IsFalse(blacklistFallback.BlockChat);
        Assert.IsFalse(blacklistSparsePerms.BlockFollow);
        Assert.IsFalse(blacklistSparsePerms.BlockInteract);
        Assert.IsFalse(blacklistSparsePerms.BlockChat);
        Assert.AreEqual(string.Empty, recoverInfo.Title);
        Assert.AreEqual(0L, recoverInfo.Pid);
        Assert.AreEqual(0, recoverContent.Texts.Count);
        Assert.AreEqual(1, recoverContent.Images.Count);
        Assert.AreEqual(string.Empty, recoverContent.Images[0].Hash);
        Assert.AreEqual(0, forumStats.View.Count);
        Assert.IsFalse(thread.IsShare);
        Assert.IsNull(thread.ShareOrigin);
        Assert.AreEqual("shared-null-tid", shareNullTid.Title);
        Assert.AreEqual(0L, shareNullTid.Tid);
        Assert.AreEqual(0, tabMap.Count);
        Assert.AreEqual(0, tabMapNull.Count);
        Assert.IsFalse(virtualImage.Enabled);
        Assert.IsTrue(virtualImageThreadNull.Enabled);
        Assert.IsFalse(virtualImageThreadEmpty.Enabled);
        Assert.IsFalse(virtualImageUserNull.Enabled);
        Assert.AreEqual("6", bawuUser.LogName);
        Assert.AreEqual("nick7", bawuUserRich.ShowName);
        Assert.AreEqual("user7", bawuUserRich.LogName);
        Assert.AreEqual("nick8", bawuUserPortraitOnly.ShowName);
        Assert.AreEqual("nick8/portrait8", bawuUserPortraitOnly.LogName);
    }

    [TestMethod]
    public void ForumImageMapper_ClosesResidualJpegFailureBranches()
    {
        var truncatedSegment = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF], "image/jpeg");
        var truncatedSof = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x07], "image/jpeg");

        Assert.IsTrue(truncatedSegment.IsEmpty);
        Assert.IsTrue(truncatedSof.IsEmpty);
    }

    [TestMethod]
    public async Task ResidualApiFallbackBranches_CloseNullErrorMessages_AndSparsePayloadShapes()
    {
        var httpCore = new RoutingHttpCore();
        httpCore.EnqueueAppFormResponse("/c/c/forum/like", "{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":325}}");
        httpCore.EnqueueAppFormResponse("/c/c/forum/unlike", "{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":326}}");
        httpCore.EnqueueCustomResponse("{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":327}}");
        httpCore.EnqueueAppFormResponse("/c/c/bawu/pushRecomToPersonalized", "{\"error_code\":0,\"error_msg\":\"\"}");
        httpCore.EnqueueAppFormResponse("/c/c/bawu/pushRecomToPersonalized", "{\"error_code\":0,\"error_msg\":\"\",\"data\":{}}");
        httpCore.EnqueueAppFormResponse("/c/f/forum/getUserForumLevelInfo",
            "{\"data\":{\"user_info\":{\"id\":8},\"forum_info\":{\"forum_name\":\"forum\"},\"user_forum_info\":{\"is_follow\":1}}}");
        httpCore.EnqueueAppFormResponse("/c/f/forum/getUserForumLevelInfo", "{\"error_code\":15}");
        httpCore.EnqueueWebGetResponse("/bawu2/platform/listPostLog", string.Empty);

        var likeException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new LikeForum(httpCore).RequestAsync(1));
        var unlikeException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new UnlikeForum(httpCore).RequestAsync(2));
        var signException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new SignForums(httpCore).RequestAsync());
        var recommendException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new Recommend(httpCore).RequestAsync(3, 4));
        var recommendMissingFlagException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new Recommend(httpCore).RequestAsync(4, 5));
        var sparseUserForum = await new GetUserForumInfo(httpCore).RequestAsync(5, "tb.1.user");
        var sparseUserForumException = await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new GetUserForumInfo(httpCore).RequestAsync(6, "tb.1.user"));
        var postLogsByUser = await new GetBawuPostlogs(httpCore).RequestAsync("forum", 7, "target-user", BawuSearchType.User,
            null, null, 0);

        Assert.AreEqual(325, likeException.Code);
        Assert.AreEqual(326, unlikeException.Code);
        Assert.AreEqual(327, signException.Code);
        Assert.AreEqual(1, recommendException.Code);
        StringAssert.Contains(recommendException.Message, "Recommend failed.");
        Assert.AreEqual(1, recommendMissingFlagException.Code);
        StringAssert.Contains(recommendMissingFlagException.Message, "Recommend failed.");
        Assert.AreEqual(0, postLogsByUser.Count);
        Assert.AreEqual("post_uname", httpCore.GetWebGetValueForPath("/bawu2/platform/listPostLog", "stype"));
        Assert.AreEqual(8L, sparseUserForum.User.UserId);
        Assert.AreEqual("forum", sparseUserForum.Fname);
        Assert.IsTrue(sparseUserForum.IsFollow);
        Assert.AreEqual(15, sparseUserForumException.Code);
    }

    [TestMethod]
    public void ResidualMapperBranches_CloseSparseEdgeShapes()
    {
        var zeroCountMissingActive = AdminHtmlParsing.ParseCommonPage("""
            <div class="breadcrumbs"><em>0</em></div>
            <div class="tbui_pagination"><li><a>1</a></li></div>
            """);
        var disabledObjectSwitchPermissions = BawuPermMapper.FromTbData(new JObject
        {
            ["perm_setting"] = new JObject
            {
                ["category_user"] = new JArray
                {
                    new JObject { ["switch"] = new JObject(), ["perm"] = 2 }
                }
            }
        });
        var isEnabled = typeof(BawuPermMapper).GetMethod("IsEnabled", BindingFlags.NonPublic | BindingFlags.Static)
                        ?? throw new InvalidOperationException("BawuPermMapper.IsEnabled not found.");
        var nullSwitchEnabled = (bool)isEnabled.Invoke(null, [null])!;
        var mapRow = typeof(BawuPostLogsMapper).GetMethod("MapRow", BindingFlags.NonPublic | BindingFlags.Static)
                     ?? throw new InvalidOperationException("BawuPostLogsMapper.MapRow not found.");
        var attributeTitleRow = (BawuPostLog?)mapRow.Invoke(null,
        [
            "<tr><td><div class=\"post_meta\"><a href=\"/home/main?id=tb.1.author\"></a><time>03-05 12:34</time></div><h1><a href=\"/p/123#456\" title=\"回复：Title From Attribute\">reply</a></h1><div>123456789012正文</div></td><td>删帖</td><td>operator-a</td><td>2024-05-06 07:08</td></tr>"
        ]);
        var blankAttributeTitleRow = (BawuPostLog?)mapRow.Invoke(null,
        [
            "<tr><td><div class=\"post_meta\"><a href=\"/home/main?id=tb.1.author\"></a><time>03-05 12:34</time></div><h1><a href=\"/p/789#1011\" title=\"   \">回复：Title From Inner</a></h1><div>123456789012正文</div></td><td>恢复</td><td>operator-b</td><td>2024-05-07 08:09</td></tr>"
        ]);
        var titleAttributeLogs = BawuPostLogsMapper.FromTbData("""
            <div class="breadcrumbs"><em>1</em></div>
            <div class="tbui_pagination"><li class="active"><a>1</a></li>(1)</div>
            <table>
              <tr>
                <td>
                  <div class="post_meta"><a href="/home/main?id=tb.1.author"></a><time>03-05 12:34</time></div>
                  <h1><a href="/p/123#456" title="回复：Title From Attribute">reply</a></h1>
                  <div>123456789012正文</div>
                </td>
                <td>删帖</td>
                <td>operator-a</td>
                <td>2024-05-06 07:08</td>
              </tr>
              <tr>
                <td>
                  <div class="post_meta"><a href="/home/main?id=tb.1.author"></a><time>03-05 12:34</time></div>
                  <h1><a href="/p/789#1011" title="   ">回复：Title From Inner</a></h1>
                  <div>123456789012正文</div>
                </td>
                <td>恢复</td>
                <td>operator-b</td>
                <td>2024-05-07 08:09</td>
              </tr>
            </table>
            """);
        var sparseStats = ForumStatisticsMapper.FromTbData(new JArray(new JObject
        {
            ["group"] = new JArray(new JObject(), new JObject())
        }));
        var threadUserProto = new User { Id = 21, Gender = (int)Gender.Male };
        SetPrivateField(threadUserProto, "portrait_", null);
        SetPrivateField(threadUserProto, "name_", null);
        SetPrivateField(threadUserProto, "nameShow_", null);
        SetPrivateField(threadUserProto, "iconinfo_", null);
        SetPrivateField(threadUserProto, "newGodData_", null);
        SetPrivateField(threadUserProto, "privSets_", null);
        SetPrivateField(threadUserProto, "userGrowth_", null);
        var threadUser = UserInfoTMapper.FromTbData(threadUserProto);
        var profileProto = new ProfileResIdl.Types.DataRes { User = new User { Id = 22, Gender = (int)Gender.Female } };
        SetPrivateField(profileProto.User, "portrait_", null);
        SetPrivateField(profileProto.User, "name_", null);
        SetPrivateField(profileProto.User, "nameShow_", null);
        SetPrivateField(profileProto.User, "tbAge_", null);
        SetPrivateField(profileProto.User, "tiebaUid_", null);
        SetPrivateField(profileProto.User, "intro_", null);
        SetPrivateField(profileProto.User, "ipAddress_", null);
        SetPrivateField(profileProto.User, "iconinfo_", null);
        SetPrivateField(profileProto.User, "newTshowIcon_", null);
        SetPrivateField(profileProto.User, "privSets_", null);
        SetPrivateField(profileProto.User, "newGodData_", null);
        SetPrivateField(profileProto.User, "userGrowth_", null);
        var profile = UserInfoPfMapper.FromTbData(profileProto);
        var tabMapProto = new SearchPostForumResIdl.Types.DataRes
        {
            ExactMatch = new SearchPostForumResIdl.Types.DataRes.Types.SearchForum()
        };
        SetPrivateField(tabMapProto.ExactMatch, "tabInfo_", null);
        var tabMap = TabMapMapper.FromTbData(tabMapProto);
        var sharedUserProto = new User { Id = 23, Name = "shared-user", NameShow = "Shared User" };
        SetPrivateField(sharedUserProto, "portrait_", null);
        var sharedUser = UserInfoMapper.FromTbData(sharedUserProto);
        var groupUserProto = new GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.MsgInfo.Types.UserInfo { UserId = 24, UserName = "group-user" };
        SetPrivateField(groupUserProto, "portrait_", null);
        var groupUser = UserInfoMapper.FromTbData(groupUserProto);
        var replyWithTextTarget = CommentMapper.FromTbData(new SubPostList
        {
            Id = 25,
            AuthorId = 26,
            Content =
            {
                new PbContent { Type = 0, Text = "回复 " },
                new PbContent { Type = 0, Text = "target-user" },
                new PbContent { Type = 0, Text = " :tail" }
            }
        });
        var postWithSignature = PostMapper.FromTbData(new global::Post
        {
            Signature = new global::Post.Types.SignatureData
            {
                Content =
                {
                    new global::Post.Types.SignatureData.Types.SignatureContent { Type = 0, Text = "sig" },
                    new global::Post.Types.SignatureData.Types.SignatureContent { Type = 2, Text = "ignored" }
                }
            }
        });
        var recoverContent = RecoverContentMapper.FromTbData(new JObject
        {
            ["content_detail"] = new JArray(new JObject()),
            ["all_pics"] = new JArray()
        });
        var shareWithoutOrigin = ThreadMapper.FromTbData(new ThreadInfo { IsShareThread = 1 });
        var panelWithoutStatus = UserInfoPanelMapper.FromTbData(new JObject { ["vipInfo"] = new JObject() });
        var bawuUser = new BawuUser { UserId = 27, UserName = "fallback-user" };

        Assert.AreEqual((0, 0, 0, false, false), zeroCountMissingActive);
        Assert.AreEqual(BawuPermType.None, disabledObjectSwitchPermissions.Permissions);
        Assert.IsFalse(nullSwitchEnabled);
        Assert.IsNotNull(attributeTitleRow);
        Assert.IsNotNull(blankAttributeTitleRow);
        Assert.AreEqual("reply", attributeTitleRow!.Title);
        Assert.AreEqual("Title From Inner", blankAttributeTitleRow!.Title);
        Assert.AreEqual(2, titleAttributeLogs.Count);
        Assert.AreEqual("reply", titleAttributeLogs[0].Title);
        Assert.AreEqual("Title From Inner", titleAttributeLogs[1].Title);
        Assert.AreEqual(0, sparseStats.View.Count);
        Assert.AreEqual(string.Empty, threadUser!.Portrait);
        Assert.AreEqual(22L, profile.UserId);
        Assert.AreEqual(string.Empty, profile.Portrait);
        Assert.AreEqual(string.Empty, profile.UserName);
        Assert.AreEqual(string.Empty, profile.NickNameNew);
        Assert.IsFalse(profile.IsVip);
        Assert.AreEqual(0, tabMap.Count);
        Assert.AreEqual(string.Empty, sharedUser!.Portrait);
        Assert.AreEqual(string.Empty, groupUser.Portrait);
        Assert.AreEqual(1, replyWithTextTarget.Content.Texts.Count);
        Assert.AreEqual("tail", replyWithTextTarget.Content.Texts[0].Text);
        Assert.AreEqual("sig", postWithSignature.Sign);
        Assert.AreEqual(0, recoverContent.Texts.Count);
        Assert.IsFalse(shareWithoutOrigin.IsShare);
        Assert.IsNull(shareWithoutOrigin.ShareOrigin);
        Assert.IsFalse(panelWithoutStatus.IsVip);
        Assert.AreEqual("fallback-user", bawuUser.ShowName);
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
        field!.SetValue(target, value);
    }

    private sealed class RoutingHttpCore : ITiebaHttpCore
    {
        private readonly Dictionary<string, Queue<string>> _appFormResponses = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Queue<string>> _webGetResponses = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<string> _customResponses = [];
        private readonly Dictionary<string, List<KeyValuePair<string, string>>> _webGetParameters =
            new(StringComparer.OrdinalIgnoreCase);

        public RoutingHttpCore()
        {
            Account = new Account(new string('b', 192), new string('s', 64)) { Tbs = "tbs-123" };
        }

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public void SetAccount(Account newAccount) => Account = newAccount;

        public void EnqueueAppFormResponse(string path, string response) => Enqueue(_appFormResponses, path, response);

        public void EnqueueWebGetResponse(string path, string response) => Enqueue(_webGetResponses, path, response);

        public void EnqueueCustomResponse(string response) => _customResponses.Enqueue(response);

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            _ = requestFactory();
            return Task.FromResult(_customResponses.Dequeue());
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => Task.FromResult(Dequeue(_appFormResponses, uri.AbsolutePath));

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            _webGetParameters[uri.AbsolutePath] = [.. parameters];
            return Task.FromResult(Dequeue(_webGetResponses, uri.AbsolutePath));
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public string GetWebGetValueForPath(string path, string key) =>
            _webGetParameters[path].Last(entry => entry.Key == key).Value;

        private static void Enqueue(Dictionary<string, Queue<string>> responses, string path, string response)
        {
            if (!responses.TryGetValue(path, out var queue))
            {
                queue = new Queue<string>();
                responses[path] = queue;
            }

            queue.Enqueue(response);
        }

        private static string Dequeue(Dictionary<string, Queue<string>> responses, string path)
        {
            if (!responses.TryGetValue(path, out var queue) || queue.Count == 0)
                throw new InvalidOperationException($"No queued response for '{path}'.");

            return queue.Dequeue();
        }
    }
}
