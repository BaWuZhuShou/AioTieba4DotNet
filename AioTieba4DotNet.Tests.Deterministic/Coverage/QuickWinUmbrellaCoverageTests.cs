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
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Coverage;

[TestClass]
public sealed class QuickWinUmbrellaCoverageTests
{
    [TestMethod]
    public void TiebaHttpErrorNormalizer_CoversCancellationTiebaTimeoutAndTransportPaths()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var canceled = new OperationCanceledException(cts.Token);
        var tieba = new TiebaAuthenticationException("auth");
        var uri = new Uri("https://example.com/api");

        var normalizedCanceled =
            TiebaHttpErrorNormalizer.Normalize(canceled, TiebaHttpRequestKind.AppForm, uri, false, cts.Token);
        var normalizedTieba =
            TiebaHttpErrorNormalizer.Normalize(tieba, TiebaHttpRequestKind.WebGet, uri, false, default);
        var normalizedTimeout = TiebaHttpErrorNormalizer.Normalize(new OperationCanceledException(),
            TiebaHttpRequestKind.WebForm, uri, false, default);
        var normalizedTransport = TiebaHttpErrorNormalizer.Normalize(new InvalidOperationException("boom"),
            TiebaHttpRequestKind.AppProto, null, false, default);

        Assert.AreSame(canceled, normalizedCanceled);
        Assert.AreSame(tieba, normalizedTieba);
        Assert.IsInstanceOfType<TiebaTimeoutException>(normalizedTimeout);
        StringAssert.Contains(normalizedTimeout.Message, "WebForm");
        Assert.IsInstanceOfType<TiebaTransportException>(normalizedTransport);
        StringAssert.Contains(normalizedTransport.Message, "<unknown-uri>");
    }

    [TestMethod]
    public void RecoverInfoAndCommentsMappers_HandleNullAndMappedShapes()
    {
        var emptyRecoverInfo = RecoverInfoMapper.FromTbData(null);
        var mappedRecoverInfo = RecoverInfoMapper.FromTbData(JObject.Parse(
            """
            {
              "thread_info": {
                "title": "Recover detail title",
                "thread_id": 1001,
                "post_id": 2002,
                "content_detail": [
                  { "type": 1, "value": "第一段" },
                  { "type": 2, "value": "ignored" },
                  { "type": 1, "value": "第二段" }
                ],
                "all_pics": [
                  { "url": "https://imgsrc.baidu.com/forum/pic/item/1234567890abcdef1234567890abcdef.jpg", "width": 320, "height": 240 }
                ]
              },
              "user_info": {
                "user_name": "recover-author",
                "show_nickname": "Recover Author",
                "portrait": "tb.1.recover?from=test"
              }
            }
            """));
        var emptyComments = CommentsMapper.FromTbData(null);

        Assert.AreEqual(string.Empty, emptyRecoverInfo.Title);
        Assert.AreEqual(0, emptyRecoverInfo.Content.Frags.Count);
        Assert.AreEqual(string.Empty, emptyRecoverInfo.User.UserName);
        Assert.AreEqual("Recover detail title", mappedRecoverInfo.Title);
        Assert.AreEqual(1001L, mappedRecoverInfo.Tid);
        Assert.AreEqual(2002L, mappedRecoverInfo.Pid);
        Assert.AreEqual("recover-author", mappedRecoverInfo.User.UserName);
        Assert.AreEqual("tb.1.recover", mappedRecoverInfo.User.Portrait);
        Assert.AreEqual("Recover detail title\n第一段第二段", mappedRecoverInfo.Text);
        Assert.AreEqual(1, mappedRecoverInfo.Content.Images.Count);
        Assert.AreEqual(3, mappedRecoverInfo.Content.Frags.Count);
        Assert.AreEqual(0, emptyComments.Objs.Count);
        Assert.IsNotNull(emptyComments.Page);
        Assert.IsNotNull(emptyComments.Forum);
        Assert.IsNotNull(emptyComments.Thread);
        Assert.IsNotNull(emptyComments.Post);
    }

    [TestMethod]
    public void MemberUsersMapper_HandlesPaginationFallbackAndHtmlDecoding()
    {
        var withoutPagination = MemberUsersMapper.FromHtml("""
                                                           <div class="name_wrap"><a title="Alice &amp; Bob" href="/home/main?id=tb.1.user&amp;fr=home"><span class="level_7"></span></a></div>
                                                           """);
        var withPagination = MemberUsersMapper.FromHtml("""
                                                        <div class="tbui_pagination"><li class="active">2</li>(4)</div>
                                                        <div class="name_wrap"><a title="Charlie" href="/home/main?id=tb.1.charlie"><span class="level_not_a_number"></span></a></div>
                                                        """);

        Assert.AreEqual(1, withoutPagination.Page.CurrentPage);
        Assert.AreEqual(1, withoutPagination.Page.TotalPage);
        Assert.IsFalse(withoutPagination.Page.HasMore);
        Assert.AreEqual("Alice & Bob", withoutPagination[0].UserName);
        Assert.AreEqual("tb.1.user", withoutPagination[0].Portrait);
        Assert.AreEqual(7, withoutPagination[0].Level);
        Assert.AreEqual(2, withPagination.Page.CurrentPage);
        Assert.AreEqual(4, withPagination.Page.TotalPage);
        Assert.IsTrue(withPagination.Page.HasMore);
        Assert.IsTrue(withPagination.Page.HasPrevious);
    }

    [TestMethod]
    public void SmallBranchHeavyMappers_HandleMissingPayloadsAndFallbackPages()
    {
        var sparseForumStatistics = ForumStatisticsMapper.FromTbData(new JArray
        {
            new JObject
            {
                ["group"] = new JArray(new JObject(),
                    new JObject { ["values"] = new JArray(new JObject { ["value"] = 5 }, new JObject()) })
            },
            new JObject { ["group"] = new JArray(new JObject(), new JObject()) },
            new JObject { ["group"] = new JArray(new JObject(), JValue.CreateNull()) },
            JValue.CreateNull(),
            new JObject { ["group"] = new JArray(new JObject()) },
            new JObject { ["group"] = new JObject() }
        });
        var sparseForumStatisticsSingleGroup = ForumStatisticsMapper.FromTbData(new JArray
        {
            new JObject { ["group"] = new JArray(new JObject()) }
        });
        var emptyRecoverInfo = RecoverInfoMapper.FromTbData(new JObject());
        var sparseSelfFollowForums = SelfFollowForumsV1Mapper.FromTbData(new JObject
        {
            ["list"] = new JArray
            {
                new JObject { ["forum_id"] = 12, ["forum_name"] = "forum-a", ["level_id"] = 3 }, new JObject()
            },
            ["page"] = new JObject { ["cur_page"] = 1, ["total_page"] = 2 }
        });
        var emptySelfFollowForums = SelfFollowForumsV1Mapper.FromTbData(new JObject());
        var blacklistWithPerms = MapBlacklistUser(new JObject
        {
            ["id"] = 8,
            ["portrait"] = "tb.1.blacklist?012345678901",
            ["name"] = "blocked-user",
            ["name_show"] = "Blocked User",
            ["perm_list"] = new JObject { ["follow"] = 1, ["interact"] = 0, ["chat"] = 1 }
        });
        var blacklistFallback = MapBlacklistUser(new JObject());
        var squareForums = SquareForumsMapper.FromTbData(new GetForumSquareResIdl.Types.DataRes
        {
            ForumInfo =
            {
                new GetForumSquareResIdl.Types.DataRes.Types.RecommendForumInfo
                {
                    ForumId = 9,
                    ForumName = "forum-square",
                    MemberCount = 10,
                    ThreadCount = 11,
                    IsLike = 1
                }
            }
        });

        CollectionAssert.AreEqual(new[] { 5, 0 }, sparseForumStatistics.View as System.Collections.ICollection);
        Assert.AreEqual(0, sparseForumStatistics.Thread.Count);
        Assert.AreEqual(0, sparseForumStatistics.NewMember.Count);
        Assert.AreEqual(0, sparseForumStatistics.Post.Count);
        Assert.AreEqual(0, sparseForumStatistics.AvgTime.Count);
        Assert.AreEqual(0, sparseForumStatisticsSingleGroup.View.Count);
        Assert.AreEqual(string.Empty, emptyRecoverInfo.Title);
        Assert.AreEqual(0L, emptyRecoverInfo.Tid);
        Assert.AreEqual(0L, emptyRecoverInfo.Pid);
        Assert.AreEqual(string.Empty, emptyRecoverInfo.User.UserName);
        Assert.AreEqual(2, sparseSelfFollowForums.Count);
        Assert.AreEqual(12UL, sparseSelfFollowForums[0].Fid);
        Assert.AreEqual(string.Empty, sparseSelfFollowForums[1].Fname);
        Assert.IsTrue(sparseSelfFollowForums.Page.HasMore);
        Assert.IsFalse(sparseSelfFollowForums.Page.HasPrevious);
        Assert.AreEqual(0, emptySelfFollowForums.Count);
        Assert.IsFalse(emptySelfFollowForums.Page.HasMore);
        Assert.AreEqual(8L, blacklistWithPerms.UserId);
        Assert.AreEqual("tb.1.blacklist", blacklistWithPerms.Portrait);
        Assert.IsTrue(GetBlacklistUserFlag(blacklistWithPerms, "BlockFollow"));
        Assert.IsFalse(GetBlacklistUserFlag(blacklistWithPerms, "BlockInteract"));
        Assert.IsTrue(GetBlacklistUserFlag(blacklistWithPerms, "BlockChat"));
        Assert.IsFalse(GetBlacklistUserFlag(blacklistFallback, "BlockFollow"));
        Assert.AreEqual(1, squareForums.Count);
        Assert.AreEqual(0, squareForums.Page.PageSize);
        Assert.AreEqual(0, squareForums.Page.CurrentPage);
        Assert.IsTrue(squareForums[0].IsFollowed);
    }

    [TestMethod]
    public void GetFollowsForumMapperAndPageTMapper_HandleFallbackShapes()
    {
        var follows = typeof(global::AioTieba4DotNet.Api.GetFollows.GetFollows);
        var parseBody = follows.GetMethod("ParseBody", BindingFlags.NonPublic | BindingFlags.Static);
        var parsed = (Models.Shared.UserList)parseBody!.Invoke(null, ["{" + "\"error_code\":0,\"error_msg\":\"\"}"])!;

        var forum = ForumMapper.FromTbData(new Dictionary<string, object>
        {
            ["id"] = 1L,
            ["name"] = 2,
            ["first_class"] = 3,
            ["second_class"] = 4,
            ["avatar"] = 5,
            ["slogan"] = 6,
            ["member_num"] = 7,
            ["post_num"] = 8,
            ["thread_num"] = 9
        });
        var protoPage = PageTMapper.FromTbData((Page?)null);
        var jsonPage = PageTMapper.FromTbData(new JObject());

        Assert.AreEqual(0, parsed.Count);
        Assert.AreEqual(0, parsed.Page.CurrentPage);
        Assert.AreEqual(0, parsed.Page.TotalCount);
        Assert.IsFalse(parsed.Page.HasMore);
        Assert.AreEqual(string.Empty, forum.Fname);
        Assert.AreEqual(string.Empty, forum.Category);
        Assert.AreEqual(string.Empty, forum.Subcategory);
        Assert.AreEqual(string.Empty, forum.SmallAvatar);
        Assert.AreEqual(string.Empty, forum.Slogan);
        Assert.IsFalse(forum.HasBaWu);
        Assert.AreEqual(0, protoPage.PageSize);
        Assert.AreEqual(0, jsonPage.CurrentPage);
        Assert.IsFalse(jsonPage.HasMore);
        Assert.IsFalse(jsonPage.HasPrevious);
    }

    [TestMethod]
    public async Task LightweightMappersAndRecommend_HandleFallbackAndErrorBranches()
    {
        var userInfoPfFallback = UserInfoPfMapper.FromTbData(new ProfileResIdl.Types.DataRes
        {
            User = new User { Id = 123, Portrait = "tb.1.user?012345678901", Gender = 1 }
        });
        var userInfoGuInfoWeb = UserInfoGuInfoWebMapper.FromTbData(new JObject
        {
            ["uid"] = 456,
            ["uname"] = "456",
            ["portrait"] = "tb.1.web?foo=bar",
            ["show_nickname"] = JValue.CreateNull()
        });
        var selfFollowForums = SelfFollowForumsV1Mapper.FromTbData(new JObject
        {
            ["list"] = new JArray
            {
                new JObject { ["forum_id"] = 7, ["forum_name"] = "forum-a", ["level_id"] = 3 },
                new JObject(),
                new JValue("skip")
            },
            ["page"] = new JObject { ["cur_page"] = 2, ["total_page"] = 1 }
        });
        var selfFollowFallback = SelfFollowForumsV1Mapper.FromTbData(new JObject());
        var bawuPerm = BawuPermMapper.FromTbData(new JObject
        {
            ["perm_setting"] = new JObject
            {
                ["category_user"] = new JArray
                {
                    new JObject { ["switch"] = true, ["perm"] = 2 },
                    new JObject { ["switch"] = 0, ["perm"] = 3 },
                    new JObject { ["switch"] = " ", ["perm"] = 4 },
                    new JObject { ["switch"] = "0", ["perm"] = 5 }
                },
                ["category_thread"] = new JArray
                {
                    new JObject { ["switch"] = 1, ["perm"] = 4 },
                    new JObject { ["switch"] = "yes", ["perm"] = 3 },
                    new JObject { ["switch"] = "1", ["perm"] = 5 }
                }
            }
        });

        var recommendOkCore = new RecordingHttpCore("{\"data\":{\"is_push_success\":1}}");
        var recommend = new Recommend(recommendOkCore);
        var recommendResult = await recommend.RequestAsync(12, 34);

        var recommendFailCore = new RecordingHttpCore("{\"data\":{\"is_push_success\":0,\"msg\":\"nope\"}}");
        var recommendFail = new Recommend(recommendFailCore);

        Assert.AreEqual(123L, userInfoPfFallback.UserId);
        Assert.AreEqual("tb.1.user", userInfoPfFallback.Portrait);
        Assert.AreEqual(string.Empty, userInfoPfFallback.UserName);
        Assert.AreEqual(456L, userInfoGuInfoWeb.UserId);
        Assert.AreEqual("tb.1.web", userInfoGuInfoWeb.Portrait);
        Assert.AreEqual(string.Empty, userInfoGuInfoWeb.UserName);
        Assert.AreEqual(string.Empty, userInfoGuInfoWeb.NickNameNew);
        Assert.AreEqual(2, selfFollowForums.Count);
        Assert.AreEqual(7UL, selfFollowForums[0].Fid);
        Assert.AreEqual(3, selfFollowForums[0].Level);
        Assert.AreEqual(string.Empty, selfFollowForums[1].Fname);
        Assert.IsTrue(selfFollowForums.Page.HasPrevious);
        Assert.IsFalse(selfFollowForums.Page.HasMore);
        Assert.AreEqual(0, selfFollowFallback.Count);
        Assert.AreEqual(
            BawuPermType.RecoverAppeal | BawuPermType.Unblock | BawuPermType.UnblockAppeal | BawuPermType.Recover,
            bawuPerm.Permissions);
        Assert.IsTrue(recommendResult);
        var recommendUri = recommendOkCore.LastAppFormUri;
        Assert.IsNotNull(recommendUri);
        Assert.AreEqual("/c/c/bawu/pushRecomToPersonalized", recommendUri.AbsolutePath);
        Assert.AreEqual("12", recommendOkCore.GetAppFormValue("forum_id"));
        Assert.AreEqual("34", recommendOkCore.GetAppFormValue("thread_id"));

        var exception = await Assert.ThrowsAsync<TieBaServerException>(() => recommendFail.RequestAsync(12, 34));

        Assert.Contains("nope", exception.Message);
        Assert.AreEqual("12", recommendFailCore.GetAppFormValue("forum_id"));
        Assert.AreEqual("34", recommendFailCore.GetAppFormValue("thread_id"));
    }

    [TestMethod]
    public void SmallMapperFallbacks_CloseResidualQuickBranches()
    {
        var emptyUser = UserInfoGuInfoWebMapper.FromTbData(new JObject());
        var retainedUser = UserInfoGuInfoWebMapper.FromTbData(new JObject
        {
            ["uid"] = 789, ["uname"] = "user-name", ["portrait"] = "tb.1.raw", ["show_nickname"] = "Nick"
        });
        var emptyPanel = UserInfoPanelMapper.FromTbData(new JObject());
        var panel = UserInfoPanelMapper.FromTbData(new JObject
        {
            ["portrait"] = "tb.1.panel",
            ["name"] = "panel-user",
            ["show_nickname"] = "Panel Nick",
            ["name_show"] = "Panel Old",
            ["gender"] = "female",
            ["tb_age"] = "3.5",
            ["vipInfo"] = "not-an-object",
            ["post_num"] = "1.2万",
            ["followed_count"] = "7"
        });
        var emptyUserForum = UserInfoUfMapper.FromTbData(new JObject());
        var blacklistAll = MapBlacklistUser(new JObject
        {
            ["id"] = 9,
            ["portrait"] = "tb.1.black",
            ["name"] = "blocked-user",
            ["name_show"] = "Blocked User",
            ["perm_list"] = new JObject { ["follow"] = 1, ["interact"] = 1, ["chat"] = 1 }
        });
        var atMessage = AtMessageMapper.FromTbData(new JObject());
        var followSparse = FollowForumsMapper.FromTbData(new JObject { ["forum_list"] = new JObject() });
        var exactSearches = ExactSearchesMapper.FromTbData(new JObject
        {
            ["post_list"] = new JArray
            {
                new JObject
                {
                    ["content"] = "body",
                    ["title"] = "title",
                    ["fname"] = "forum",
                    ["tid"] = 1,
                    ["pid"] = 2,
                    ["is_floor"] = 0,
                    ["time"] = 3
                }
            },
            ["page"] = new JObject()
        });
        var exactSearchesWithAuthor = ExactSearchesMapper.FromTbData(new JObject
        {
            ["post_list"] = new JArray
            {
                new JObject
                {
                    ["content"] = "body-2",
                    ["title"] = "title-2",
                    ["fname"] = "forum-2",
                    ["tid"] = 11,
                    ["pid"] = 22,
                    ["time"] = 33,
                    ["author"] = new JObject { ["name_show"] = "Shown Name" }
                }
            }
        });
        var recoverInfoMissingThread = RecoverInfoMapper.FromTbData(new JObject { ["user_info"] = new JObject() });
        var recoverContentImageOnly = RecoverContentMapper.FromTbData(new JObject
        {
            ["content_detail"] = new JArray { new JObject { ["type"] = 2, ["value"] = "skip" } },
            ["all_pics"] = new JArray
            {
                new JObject { ["url"] = "https://example.com/plain.png", ["width"] = 1, ["height"] = 2 }
            }
        });
        var memberUsersFallbackLevel = MemberUsersMapper.FromHtml("""
                                                                  <div class="tbui_pagination"><li class="active">oops</li>(2)</div>
                                                                  <div class="name_wrap"><a title="Delta" href="/home/main?id=tb.1.delta&amp;fr=home"><span class="level_0oops"></span></a></div>
                                                                  """);
        var rankUsers = RankUsersMapper.FromHtml("""
                                                 <tr class="drl_list_item"><td>1</td><td>Alice</td><td><span class="level_missing"></span></td><td>5</td></tr>
                                                 <ul class="p_rank_pager" data-field='{"unused":1}'></ul>
                                                 """);
        var paginationNoPages = AdminHtmlParsing.ParseCommonPage("<div class='breadcrumbs'><em>4</em></div>");
        var paginationMissingActive = AdminHtmlParsing.ParseCommonPage("""
                                                                       <div class='breadcrumbs'><em>3</em></div>
                                                                       <div class='tbui_pagination'><li>1</li></div>
                                                                       """);
        var paginationMissingTotal = AdminHtmlParsing.ParseCommonPage("""
                                                                      <div class='breadcrumbs'><em>2</em></div>
                                                                      <div class='tbui_pagination'><li class='active'>2</li></div>
                                                                      """);
        var defaultDisabledPermissions = BawuPermMapper.FromTbData(new JObject
        {
            ["perm_setting"] = new JObject
            {
                ["category_user"] = new JArray { new JObject { ["switch"] = new JObject(), ["perm"] = 2 } }
            }
        });
        var sparseStats = ForumStatisticsMapper.FromTbData(new JArray
        {
            new JObject { ["group"] = new JArray(new JObject()) }
        });

        Assert.AreEqual(0L, emptyUser.UserId);
        Assert.AreEqual(string.Empty, emptyUser.UserName);
        Assert.AreEqual(string.Empty, emptyUser.Portrait);
        Assert.AreEqual(789L, retainedUser.UserId);
        Assert.AreEqual("user-name", retainedUser.UserName);
        Assert.AreEqual("tb.1.raw", retainedUser.Portrait);
        Assert.AreEqual("Nick", retainedUser.NickNameNew);
        Assert.AreEqual(string.Empty, emptyPanel.Portrait);
        Assert.AreEqual(0F, emptyPanel.Age);
        Assert.AreEqual(Gender.Female, panel.Gender);
        Assert.AreEqual(3.5f, panel.Age);
        Assert.IsFalse(panel.IsVip);
        Assert.AreEqual(12000, panel.PostNum);
        Assert.AreEqual(7, panel.FanNum);
        Assert.AreEqual(0L, emptyUserForum.UserId);
        Assert.AreEqual(string.Empty, emptyUserForum.Portrait);
        Assert.IsFalse(emptyUserForum.IsLike);
        Assert.IsTrue(GetBlacklistUserFlag(blacklistAll, "BlockFollow"));
        Assert.IsTrue(GetBlacklistUserFlag(blacklistAll, "BlockInteract"));
        Assert.IsTrue(GetBlacklistUserFlag(blacklistAll, "BlockChat"));
        Assert.IsNull(atMessage.Replyer);
        Assert.IsFalse(atMessage.IsFloor);
        Assert.IsFalse(atMessage.IsFirstPost);
        Assert.AreEqual(0, followSparse.Count);
        Assert.IsFalse(followSparse.HasMore);
        Assert.AreEqual(1, exactSearches.Count);
        Assert.AreEqual(string.Empty, exactSearches[0].ShowName);
        Assert.IsFalse(exactSearches[0].IsComment);
        Assert.AreEqual("Shown Name", exactSearchesWithAuthor[0].ShowName);
        Assert.AreEqual(string.Empty, recoverInfoMissingThread.Title);
        Assert.AreEqual(0L, recoverInfoMissingThread.Tid);
        Assert.AreEqual(string.Empty, recoverInfoMissingThread.User.UserName);
        Assert.AreEqual(0, recoverContentImageOnly.Texts.Count);
        Assert.AreEqual(1, recoverContentImageOnly.Images.Count);
        Assert.AreEqual(string.Empty, recoverContentImageOnly.Images[0].Hash);
        Assert.AreEqual(1, memberUsersFallbackLevel.Count);
        Assert.AreEqual(1, memberUsersFallbackLevel.Page.CurrentPage);
        Assert.AreEqual(2, memberUsersFallbackLevel.Page.TotalPage);
        Assert.AreEqual(0, memberUsersFallbackLevel[0].Level);
        Assert.AreEqual(1, rankUsers.Count);
        Assert.AreEqual(1, rankUsers.Page.CurrentPage);
        Assert.AreEqual(1, rankUsers.Page.TotalPage);
        Assert.IsFalse(rankUsers.Page.HasMore);
        Assert.IsFalse(rankUsers[0].IsVip);
        Assert.AreEqual(0, rankUsers[0].Level);
        Assert.AreEqual((1, 1, 4, false, false), paginationNoPages);
        Assert.AreEqual((1, 1, 3, false, false), paginationMissingActive);
        Assert.AreEqual((2, 2, 2, false, true), paginationMissingTotal);
        Assert.AreEqual(BawuPermType.None, defaultDisabledPermissions.Permissions);
        Assert.AreEqual(0, sparseStats.View.Count);
    }

    [TestMethod]
    public async Task LegacyApiWrapperBranches_CloseResidualQuickBranches()
    {
        var httpCore = new RoutingHttpCore();
        httpCore.EnqueueAppFormResponse("/c/c/user/userMuteAdd",
            "{\"error_code\":0,\"error_msg\":\"\",\"errorno\":0,\"errmsg\":\"\"}");
        httpCore.EnqueueAppFormResponse("/c/c/user/userMuteAdd", "{\"error_code\":11,\"error_msg\":\"add-failed\"}");
        httpCore.EnqueueAppFormResponse("/c/c/user/userMuteDel",
            "{\"error_code\":0,\"error_msg\":\"\",\"errorno\":12,\"errmsg\":\"del-failed\"}");
        httpCore.EnqueueAppFormResponse("/c/c/forum/like",
            "{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":0,\"errmsg\":\"\"}}");
        httpCore.EnqueueAppFormResponse("/c/c/forum/unlike",
            "{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":0,\"errmsg\":\"\"}}");
        httpCore.EnqueueAppFormResponse("/c/c/forum/like",
            "{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":5,\"errmsg\":\"like-failed\"}}");
        httpCore.EnqueueAppFormResponse("/c/c/forum/unlike",
            "{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":6,\"errmsg\":\"unlike-failed\"}}");
        httpCore.EnqueueCustomResponse(
            "{\"error_code\":0,\"error_msg\":\"\",\"error\":{\"errno\":9,\"errmsg\":\"nested-error\"}}");
        httpCore.EnqueueAppFormResponse("/c/c/bawu/pushRecomToPersonalized",
            "{\"error_code\":0,\"error_msg\":\"\",\"data\":{\"is_push_success\":0}}");
        httpCore.EnqueueAppFormResponse("/c/f/forum/getUserForumLevelInfo",
            "{\"error_code\":13,\"error\":\"fallback-error\"}");
        httpCore.EnqueueAppFormResponse("/c/f/forum/getUserForumLevelInfo",
            "{\"error_code\":14,\"errmsg\":\"errmsg-fallback\"}");
        httpCore.EnqueueWebGetResponse("/mg/o/getForumHome", """
                                                             {"errno":0,"errmsg":"","data":{"like_forum":{"list":[{"forum_id":12,"forum_name":"forum-a","level_id":3}],"page":{"cur_page":1,"total_page":1}}}}
                                                             """);
        httpCore.EnqueueWebGetResponse("/bawu2/platform/listPostLog", string.Empty);
        httpCore.EnqueueWebGetResponse("/bawu2/platform/listUserLog", string.Empty);

        Assert.IsTrue(await new AddBlacklistOld(httpCore).RequestAsync(42));
        var addException =
            await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new AddBlacklistOld(httpCore).RequestAsync(43));
        var delException =
            await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new DelBlacklistOld(httpCore).RequestAsync(44));
        Assert.IsTrue(await new LikeForum(httpCore).RequestAsync(10));
        Assert.IsTrue(await new UnlikeForum(httpCore).RequestAsync(10));
        var likeException =
            await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new LikeForum(httpCore).RequestAsync(11));
        var unlikeException =
            await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new UnlikeForum(httpCore).RequestAsync(11));
        var nestedException =
            await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new SignForums(httpCore).RequestAsync());
        var recommendException =
            await Assert.ThrowsExactlyAsync<TieBaServerException>(() => new Recommend(httpCore).RequestAsync(12, 34));
        var userForumError =
            await Assert.ThrowsExactlyAsync<TieBaServerException>(() =>
                new GetUserForumInfo(httpCore).RequestAsync(1, "tb.1.user"));
        var userForumErrmsg =
            await Assert.ThrowsExactlyAsync<TieBaServerException>(() =>
                new GetUserForumInfo(httpCore).RequestAsync(1, "tb.1.user"));
        var selfFollowForums = await new GetSelfFollowForumsV1(httpCore).RequestAsync(2, 30);
        var postLogs = await new GetBawuPostlogs(httpCore).RequestAsync("forum", 2, "operator", BawuSearchType.Operator,
            DateTimeOffset.UnixEpoch, null, 7);
        var userLogs = await new GetBawuUserlogs(httpCore).RequestAsync("forum", 3, "operator", BawuSearchType.Operator,
            DateTimeOffset.UnixEpoch, null, 0);

        Assert.AreEqual(11, addException.Code);
        Assert.AreEqual(12, delException.Code);
        Assert.AreEqual(5, likeException.Code);
        Assert.AreEqual(6, unlikeException.Code);
        Assert.AreEqual(9, nestedException.Code);
        StringAssert.Contains(recommendException.Message, "Recommend failed.");
        StringAssert.Contains(userForumError.Message, "fallback-error");
        StringAssert.Contains(userForumErrmsg.Message, "errmsg-fallback");
        Assert.AreEqual(1, selfFollowForums.Count);
        Assert.AreEqual(12UL, selfFollowForums[0].Fid);
        Assert.AreEqual("forum-a", selfFollowForums[0].Fname);
        Assert.AreEqual(3, selfFollowForums[0].Level);
        Assert.AreEqual("op_uname", httpCore.GetWebGetValueForPath("/bawu2/platform/listPostLog", "stype"));
        Assert.AreEqual("operator", httpCore.GetWebGetValueForPath("/bawu2/platform/listPostLog", "svalue"));
        Assert.IsTrue(long.Parse(httpCore.GetWebGetValueForPath("/bawu2/platform/listPostLog", "end")) > 0);
        Assert.AreEqual("0", httpCore.GetWebGetValueForPath("/bawu2/platform/listPostLog", "begin"));
        Assert.AreEqual("op_uname", httpCore.GetWebGetValueForPath("/bawu2/platform/listUserLog", "stype"));
        Assert.AreEqual("operator", httpCore.GetWebGetValueForPath("/bawu2/platform/listUserLog", "svalue"));
        Assert.IsTrue(long.Parse(httpCore.GetWebGetValueForPath("/bawu2/platform/listUserLog", "end")) > 0);
        Assert.AreEqual("0", httpCore.GetWebGetValueForPath("/bawu2/platform/listUserLog", "begin"));
        Assert.AreEqual(0, postLogs.Count);
        Assert.AreEqual(0, userLogs.Count);
    }

    private sealed class RecordingHttpCore(string response) : ITiebaHttpCore
    {
        public string Response { get; set; } = response;

        public Account? Account { get; private set; } = new(new string('b', 192));

        public HttpClient HttpClient { get; } = new();

        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];

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
            LastAppFormData = new List<KeyValuePair<string, string>>(data);
            return Task.FromResult(Response);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
            return LastAppFormData.FindLast(entry => entry.Key == key).Value;
        }
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

        public void SetAccount(Account account)
        {
            Account = account;
        }

        public void EnqueueAppFormResponse(string path, string response)
        {
            Enqueue(_appFormResponses, path, response);
        }

        public void EnqueueWebGetResponse(string path, string response)
        {
            Enqueue(_webGetResponses, path, response);
        }

        public void EnqueueCustomResponse(string response)
        {
            _customResponses.Enqueue(response);
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            LastCustomRequest = requestFactory();
            return Task.FromResult(_customResponses.Dequeue());
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            return Task.FromResult(Dequeue(_appFormResponses, uri.AbsolutePath));
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            LastWebGetUri = uri;
            _webGetParameters[uri.AbsolutePath] = [.. parameters];
            return Task.FromResult(Dequeue(_webGetResponses, uri.AbsolutePath));
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Uri? LastAppFormUri { get; private set; }

        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];

        public Uri? LastWebGetUri { get; private set; }

        public HttpRequestMessage? LastCustomRequest { get; private set; }

        public string GetWebGetValueForPath(string path, string key)
        {
            return _webGetParameters[path].Last(entry => entry.Key == key).Value;
        }

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

    private static BlacklistUser MapBlacklistUser(JObject data)
    {
        return (BlacklistUser)typeof(BlacklistUserMapper)
            .GetMethod("FromTbData", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [data])!;
    }

    private static bool GetBlacklistUserFlag(BlacklistUser user, string propertyName)
    {
        return (bool)typeof(BlacklistUser)
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!
            .GetValue(user)!;
    }
}
