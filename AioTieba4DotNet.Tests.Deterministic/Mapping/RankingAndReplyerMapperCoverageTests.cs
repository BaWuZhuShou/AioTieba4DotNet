#nullable enable
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class RankingAndReplyerMapperCoverageTests
{
    [TestMethod]
    public void RankUsersMapper_HandlesVipRowsAndPaginationFallbacks()
    {
        var mapped = RankUsersMapper.FromHtml("""
            <ul class="p_rank_pager" data-field='{&quot;cur_page&quot;:2,&quot;total_num&quot;:4}'></ul>
            <tr class="drl_list_item">
              <td>1</td>
              <td><span class="drl_item_vip"></span>Alice</td>
              <td><span class="bg_lv12"></span></td>
              <td>345</td>
            </tr>
            """);
        var fallback = RankUsersMapper.FromHtml("""
            <tr class="drl_list_item_self"><td>1</td><td>Bob</td><td>invalid-level</td><td>not-a-number</td></tr>
            """);

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual("Alice", mapped[0].UserName);
        Assert.IsTrue(mapped[0].IsVip);
        Assert.AreEqual(12, mapped[0].Level);
        Assert.AreEqual(345, mapped[0].Exp);
        Assert.AreEqual(2, mapped.Page.CurrentPage);
        Assert.AreEqual(4, mapped.Page.TotalPage);
        Assert.IsTrue(mapped.Page.HasMore);
        Assert.IsTrue(mapped.Page.HasPrevious);
        Assert.AreEqual(1, fallback.Count);
        Assert.AreEqual(0, fallback[0].Level);
        Assert.AreEqual(0, fallback[0].Exp);
        Assert.AreEqual(1, fallback.Page.CurrentPage);
        Assert.IsFalse(fallback.Page.HasMore);
        Assert.ThrowsExactly<System.ArgumentNullException>(() => RankUsersMapper.FromHtml(null!));
    }

    [TestMethod]
    public void RankForumsMapper_HandlesBaWuDetectionAndPaginationFallbacks()
    {
        var mapped = RankForumsMapper.FromHtml("""
            <div class="pagination"><span>2</span><a href="?pn=5"></a></div>
            <tr class="j_rank_row">
              <td>1</td>
              <td>Forum &amp; Name</td>
              <td>123</td>
              <td>456</td>
              <td class="has_bawu">yes</td>
            </tr>
            """);
        var fallback = RankForumsMapper.FromHtml("""
            <tr class="j_rank_row">
              <td>1</td>
              <td>Second</td>
              <td>bad</td>
              <td>bad</td>
              <td class="no_bawu">no_bawu</td>
            </tr>
            """);

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual("Forum & Name", mapped[0].Fname);
        Assert.AreEqual(123, mapped[0].SignNum);
        Assert.AreEqual(456, mapped[0].MemberNum);
        Assert.IsTrue(mapped[0].HasBaWu);
        Assert.AreEqual(2, mapped.Page.CurrentPage);
        Assert.AreEqual(5, mapped.Page.TotalPage);
        Assert.IsTrue(mapped.Page.HasMore);
        Assert.IsTrue(mapped.Page.HasPrevious);
        Assert.AreEqual(1, fallback.Count);
        Assert.IsFalse(fallback[0].HasBaWu);
        Assert.AreEqual(0, fallback[0].SignNum);
        Assert.AreEqual(0, fallback[0].MemberNum);
        Assert.AreEqual(1, fallback.Page.CurrentPage);
        Assert.IsFalse(fallback.Page.HasMore);
        Assert.ThrowsExactly<System.ArgumentNullException>(() => RankForumsMapper.FromHtml(null!));
    }

    [TestMethod]
    public void ForumAndListMappers_CoverNullEmptyAndFallbackBranches()
    {
        var rankUsers = RankUsersMapper.FromHtml("""
            <tr class="drl_list_item_self"><td>1</td><td>Short</td></tr>
            """);
        var rankForums = RankForumsMapper.FromHtml("""
            <tr class="j_rank_row"><td>1</td><td>Short</td><td>bad</td><td>bad</td></tr>
            """);
        var detailWithBawu = ForumDetailMapper.FromTbData(new global::GetForumDetailResIdl.Types.DataRes
        {
            ForumInfo = new global::GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
            {
                ForumId = 7356044,
                ForumName = "lol欧服吧",
                Lv1Name = "游戏",
                Avatar = "small",
                AvatarOrigin = "origin",
                Slogan = "safe forum",
                MemberCount = 1,
                ThreadCount = 2
            },
            ElectionTab = new global::GetForumDetailResIdl.Types.DataRes.Types.ManagerElectionTab
            {
                NewStrategyText = "已有吧主"
            }
        });
        var detailWithoutBawu = ForumDetailMapper.FromTbData(new global::GetForumDetailResIdl.Types.DataRes
        {
            ForumInfo = new global::GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
            {
                ForumId = 1,
                ForumName = "forum",
                Lv1Name = "cat",
                Avatar = "avatar",
                AvatarOrigin = "origin",
                Slogan = "forum",
                MemberCount = 3,
                ThreadCount = 4
            },
            ElectionTab = new global::GetForumDetailResIdl.Types.DataRes.Types.ManagerElectionTab
            {
                NewStrategyText = "暂无吧主"
            }
        });
        var recomEmpty = RecomStatusMapper.FromTbData(new JObject());
        var recomValues = RecomStatusMapper.FromTbData(new JObject
        {
            ["total_recommend_num"] = 7,
            ["used_recommend_num"] = 3
        });
        var selfFollowEmpty = SelfFollowForumsMapper.FromTbData(new JObject());
        var selfFollowValues = SelfFollowForumsMapper.FromTbData(new JObject
        {
            ["like_forum"] = new JArray
            {
                new JObject
                {
                    ["forum_id"] = 7356044,
                    ["forum_name"] = "lol欧服吧",
                    ["level_id"] = 7,
                    ["is_sign"] = 1
                }
            },
            ["like_forum_has_more"] = 0
        });
        var selfFollowMore = SelfFollowForumsMapper.FromTbData(new JObject
        {
            ["like_forum_has_more"] = 1
        });
        var forumTNull = ForumTMapper.FromTbData((SimpleForum?)null);
        var forumTValues = ForumTMapper.FromTbData(new SimpleForum
        {
            Id = 7356044,
            Name = "lol欧服吧",
            FirstClass = "游戏",
            SecondClass = "网络游戏",
            MemberNum = 1,
            PostNum = 2
        });
        var forumTRich = ForumTMapper.FromTbData(new global::FrsPageResIdl.Types.DataRes
        {
            Forum = new global::FrsPageResIdl.Types.DataRes.Types.ForumInfo
            {
                Id = 7356045,
                Name = "lol欧服吧2",
                FirstClass = "游戏",
                SecondClass = "MOBA",
                MemberNum = 2,
                ThreadNum = 3,
                PostNum = 4,
                Managers =
                {
                    new global::FrsPageResIdl.Types.DataRes.Types.ForumInfo.Types.Manager()
                }
            },
            ForumRule = new global::FrsPageResIdl.Types.DataRes.Types.ForumRuleStatus
            {
                HasForumRule = 1
            }
        });
        var exact = new ExactSearch { Pid = 123, Text = "body" };
        var exactSearches = new ExactSearches([exact], new ExactSearchesPage
        {
            PageSize = 20,
            CurrentPage = 2,
            TotalPage = 3,
            TotalCount = 1,
            HasMore = true,
            HasPrevious = false
        });

        Assert.AreEqual(0, rankUsers.Count);
        Assert.AreEqual(1, rankUsers.Page.CurrentPage);
        Assert.AreEqual(0, rankForums.Count);
        Assert.AreEqual(1, rankForums.Page.CurrentPage);
        Assert.IsTrue(detailWithBawu.HasBaWu);
        Assert.IsFalse(detailWithoutBawu.HasBaWu);
        Assert.AreEqual(0, recomEmpty.TotalRecommendNum);
        Assert.AreEqual(0, recomEmpty.UsedRecommendNum);
        Assert.AreEqual(7, recomValues.TotalRecommendNum);
        Assert.AreEqual(3, recomValues.UsedRecommendNum);
        Assert.AreEqual(0, selfFollowEmpty.Count);
        Assert.IsFalse(selfFollowEmpty.HasMore);
        Assert.AreEqual(1, selfFollowValues.Count);
        Assert.IsFalse(selfFollowValues.HasMore);
        Assert.IsTrue(selfFollowMore.HasMore);
        Assert.AreEqual(0L, forumTNull.Fid);
        Assert.AreEqual("lol欧服吧", forumTValues.Fname);
        Assert.AreEqual("游戏", forumTValues.Category);
        Assert.IsTrue(forumTRich.HasBaWu);
        Assert.IsTrue(forumTRich.HasRule);
        Assert.IsFalse(exact.Equals(new object()));
        Assert.IsFalse(exact.Equals(null));
        Assert.IsFalse(exact.Equals(new ExactSearch { Pid = 456 }));
        Assert.AreEqual(1, exactSearches.Count);
        Assert.IsTrue(exactSearches.HasMore);
        Assert.ThrowsExactly<System.ArgumentNullException>(() => new ExactSearches([], null!));
    }

    [TestMethod]
    public void LastReplyersMapper_MapsForumThreadReplyersAndCurrentPageFallback()
    {
        var mapped = LastReplyersMapper.FromTbData(new FrsPageResIdl4lp.Types.DataRes
        {
            Forum = new FrsPageResIdl4lp.Types.DataRes.Types.ForumInfo
            {
                Id = 7356044,
                Name = "lol欧服"
            },
            Page = new Page
            {
                PageSize = 30,
                CurrentPage = 0,
                TotalPage = 5,
                TotalCount = 99,
                HasMore = 1,
                HasPrev = 0
            },
            ThreadList =
            {
                new ThreadInfo
                {
                    Id = 123456,
                    FirstPostId = 654321,
                    Title = "reply thread",
                    IsGood = 1,
                    IsTop = 1,
                    CreateTime = 100,
                    LastTimeInt = 200,
                    Author = new User { Id = 42, Portrait = "tb.1.author?012345678901", Name = "author", NameShow = "Author" },
                    LastReplyer = new User { Id = 43, Name = "replyer", NameShow = "Replyer" }
                },
                new ThreadInfo
                {
                    Id = 2,
                    FirstPostId = 3,
                    Title = "fallback thread"
                }
            }
        });

        Assert.AreEqual(2, mapped.Count);
        Assert.AreEqual(7356044UL, mapped[0].Fid);
        Assert.AreEqual("lol欧服", mapped[0].Fname);
        Assert.AreEqual("tb.1.author", mapped[0].User.Portrait);
        Assert.AreEqual("Author", mapped[0].User.ShowName);
        Assert.AreEqual("Replyer", mapped[0].LastReplyer.ShowName);
        Assert.IsTrue(mapped[0].IsGood);
        Assert.IsTrue(mapped[0].IsTop);
        Assert.AreEqual(1, mapped.Page.CurrentPage);
        Assert.AreEqual(5, mapped.Page.TotalPage);
        Assert.IsTrue(mapped.Page.HasMore);
        Assert.IsFalse(mapped.Page.HasPrevious);
        Assert.AreEqual(string.Empty, mapped[1].User.UserName);
        Assert.AreEqual(0L, mapped[1].LastReplyer.UserId);
    }

    [TestMethod]
    public void LastReplyersMapper_HandlesMissingForumAndPageDefaults()
    {
        var mapped = LastReplyersMapper.FromTbData(new FrsPageResIdl4lp.Types.DataRes
        {
            ThreadList =
            {
                new ThreadInfo
                {
                    Id = 1,
                    FirstPostId = 2,
                    Title = "fallback",
                    Author = new User { Portrait = "plain-portrait" },
                    LastReplyer = new User()
                }
            }
        });

        Assert.AreEqual(0UL, mapped[0].Fid);
        Assert.AreEqual(string.Empty, mapped[0].Fname);
        Assert.AreEqual("plain-portrait", mapped[0].User.Portrait);
        Assert.AreEqual(0, mapped.Page.PageSize);
        Assert.AreEqual(0, mapped.Page.CurrentPage);
        Assert.AreEqual(0, mapped.Page.TotalPage);
        Assert.AreEqual(0, mapped.Page.TotalCount);
    }
}
