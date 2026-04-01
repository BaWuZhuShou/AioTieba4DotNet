using System.Threading.Tasks;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.ForumExtensions)]
[TestSubject(typeof(TiebaClient))]
public sealed class TiebaClientForumExtensionsIntegrationTests : TestBase
{
    [TestMethod]
    public async Task GetFollowForumsAsync_KnownUser_ReturnsForumEntries()
    {
        EnsureAuthenticated();

        var forums = await Client.Forums.GetFollowForumsAsync(4954297652);

        Assert.IsNotEmpty(forums);
        Assert.IsGreaterThan(0UL, forums[0].Fid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(forums[0].Fname));
        Assert.IsPositive(forums[0].Level);
        Assert.IsPositive(forums[0].Exp);
    }

    [TestMethod]
    public async Task GetSelfFollowForumsAsync_AuthenticatedAccount_ReturnsDeterministicShape()
    {
        EnsureAuthenticated();

        var forums = await Client.Forums.GetSelfFollowForumsAsync(1, 50);

        Assert.IsNotNull(forums);
        if (forums.Count > 0)
        {
            Assert.IsGreaterThan(0UL, forums[0].Fid);
            Assert.IsFalse(string.IsNullOrWhiteSpace(forums[0].Fname));
        }
    }

    [TestMethod]
    public async Task GetSelfFollowForumsV1Async_AuthenticatedAccount_ReturnsLegacyPageShape()
    {
        EnsureAuthenticated();

        SelfFollowForumsV1 forums;
        try
        {
            forums = await Client.Forums.GetSelfFollowForumsV1Async(1, 20);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping legacy self-follow forum integration path in this environment: {exception.Message}");
            return;
        }

        Assert.IsNotNull(forums);
        Assert.IsGreaterThanOrEqualTo(1, forums.Page.CurrentPage);
        Assert.IsGreaterThanOrEqualTo(1, forums.Page.TotalPage);
    }

    [TestMethod]
    public async Task GetDislikeForumsAsync_AuthenticatedAccount_ReturnsDistinctPageShape()
    {
        EnsureAuthenticated();

        var forums = await Client.Forums.GetDislikeForumsAsync(1, 20);

        Assert.IsNotNull(forums);
        Assert.IsGreaterThanOrEqualTo(0, forums.Page.CurrentPage);
        if (forums.Count > 0)
        {
            Assert.IsGreaterThan(0UL, forums[0].Fid);
            Assert.IsFalse(string.IsNullOrWhiteSpace(forums[0].Fname));
            Assert.IsGreaterThanOrEqualTo(0, forums[0].MemberNum);
        }
    }

    [TestMethod]
    public async Task SearchExactAsync_SafeForum_ReturnsStablePageShape()
    {
        var forum = await RequireSafeForumFixtureAsync(nameof(SearchExactAsync_SafeForum_ReturnsStablePageShape));

        var searches = await Client.Forums.SearchExactAsync(forum.ResolvedName, "吧", 1, 20);

        Assert.IsNotNull(searches);
        Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalCount);
        Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalPage);
    }

    [TestMethod]
    public async Task GetPortraitAsync_SafeTargetPortrait_ReturnsImageShape()
    {
        var portrait = RequireSafeTargetPortraitFixture(nameof(GetPortraitAsync_SafeTargetPortrait_ReturnsImageShape));

        var image = await Client.Forums.GetPortraitAsync(portrait, ForumImageSize.Small);

        Assert.IsNotNull(image);
        Assert.IsFalse(image.IsEmpty);
        Assert.IsGreaterThan(0, image.Width);
        Assert.IsGreaterThan(0, image.Height);
    }

    [TestMethod]
    public async Task GetLastReplyersAsync_SafeForum_ReturnsStableThreadPage()
    {
        var forum = await RequireSafeForumFixtureAsync(nameof(GetLastReplyersAsync_SafeForum_ReturnsStableThreadPage));

        var threads = await Client.Forums.GetLastReplyersAsync(forum.ResolvedName, 1, 20);

        Assert.IsNotNull(threads);
        Assert.IsNotNull(threads.Forum);
        Assert.AreEqual(forum.Fid, (ulong)threads.Forum.Fid);
        Assert.IsGreaterThanOrEqualTo(0, threads.Page.TotalCount);
    }

    [TestMethod]
    public async Task GetMemberUsersAsync_SafeForum_ReturnsStableMemberPage()
    {
        EnsureAuthenticated();
        var forum = await RequireSafeForumFixtureAsync(nameof(GetMemberUsersAsync_SafeForum_ReturnsStableMemberPage));

        var members = await Client.Forums.GetMemberUsersAsync(forum.ResolvedName, 1);

        Assert.IsNotNull(members);
        Assert.IsGreaterThanOrEqualTo(1, members.Page.CurrentPage);
        Assert.IsGreaterThanOrEqualTo(1, members.Page.TotalPage);
    }

    [TestMethod]
    public async Task GetRankForumsAsync_SafeForum_ReturnsStableRankPage()
    {
        var forum = await RequireSafeForumFixtureAsync(nameof(GetRankForumsAsync_SafeForum_ReturnsStableRankPage));

        var ranks = await Client.Forums.GetRankForumsAsync(forum.ResolvedName, 1, ForumRankType.Weekly);

        Assert.IsNotNull(ranks);
        Assert.IsGreaterThanOrEqualTo(1, ranks.Page.CurrentPage);
        Assert.IsGreaterThanOrEqualTo(1, ranks.Page.TotalPage);
    }

    [TestMethod]
    public async Task GetSquareForumsAsync_AuthenticatedAccount_ReturnsStableSquarePage()
    {
        EnsureAuthenticated();

        var forums = await Client.Forums.GetSquareForumsAsync("游戏", 1, 20);

        Assert.IsNotNull(forums);
        Assert.IsGreaterThanOrEqualTo(0, forums.Page.TotalCount);
        if (forums.Count > 0)
        {
            Assert.IsGreaterThan(0UL, forums[0].Fid);
            Assert.IsFalse(string.IsNullOrWhiteSpace(forums[0].Fname));
        }
    }

    [TestMethod]
    public async Task GetForumLevelAsync_SafeForum_ReturnsStableLevelShape()
    {
        EnsureAuthenticated();
        var forum = await RequireSafeForumFixtureAsync(nameof(GetForumLevelAsync_SafeForum_ReturnsStableLevelShape));

        var level = await Client.Forums.GetForumLevelAsync(forum.ResolvedName);

        Assert.IsNotNull(level);
        Assert.IsGreaterThanOrEqualTo(0, level.UserLevel);
        Assert.IsFalse(string.IsNullOrWhiteSpace(level.LevelName) && level.UserLevel > 0);
    }

    [TestMethod]
    public async Task GetRecomStatusAsync_SafeForum_ReturnsQuotaShape_WhenPrivilegeAllows()
    {
        EnsureAuthenticated();
        var forum = await RequireSafeForumFixtureAsync(nameof(GetRecomStatusAsync_SafeForum_ReturnsQuotaShape_WhenPrivilegeAllows));

        try
        {
            var status = await Client.Forums.GetRecomStatusAsync(forum.ResolvedName);

            Assert.IsNotNull(status);
            Assert.IsGreaterThanOrEqualTo(0, status.TotalRecommendNum);
            Assert.IsGreaterThanOrEqualTo(0, status.UsedRecommendNum);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping privilege-sensitive recom status integration path: {exception.Message}");
        }
    }

    [TestMethod]
    public async Task GetStatisticsAsync_SafeForum_ReturnsOrderedSeries_WhenPrivilegeAllows()
    {
        EnsureAuthenticated();
        var forum = await RequireSafeForumFixtureAsync(nameof(GetStatisticsAsync_SafeForum_ReturnsOrderedSeries_WhenPrivilegeAllows));

        try
        {
            var statistics = await Client.Forums.GetStatisticsAsync(forum.ResolvedName);

            Assert.IsNotNull(statistics);
            Assert.AreEqual(statistics.View.Count, statistics.Thread.Count);
            Assert.IsGreaterThanOrEqualTo(0, statistics.View.Count);
            Assert.IsGreaterThanOrEqualTo(0, statistics.Recommend.Count);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping privilege-sensitive statistics integration path: {exception.Message}");
        }
    }
}
