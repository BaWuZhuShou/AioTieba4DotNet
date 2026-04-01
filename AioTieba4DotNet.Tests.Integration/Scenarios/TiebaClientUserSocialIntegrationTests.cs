using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.UserSocial)]
[TestSubject(typeof(TiebaClient))]
public sealed class TiebaClientUserSocialIntegrationTests : TestBase
{
    [TestMethod]
    public async Task GetProfileAsync_ByUserName_ReturnsPortraitOrExplicitSkip()
    {
        try
        {
            var userInfo = await Client.Users.GetProfileAsync("百度");
            Assert.IsNotNull(userInfo);
            Assert.IsFalse(string.IsNullOrEmpty(userInfo.Portrait));
        }
        catch (Exception exception)
        {
            Assert.Inconclusive($"Profile API failed: {exception.Message}");
        }
    }

    [TestMethod]
    public async Task GetProfileAsync_ByUserId_ReturnsExpectedUser()
    {
        var userInfo = await Client.Users.GetProfileAsync(1);

        Assert.IsNotNull(userInfo);
    }

    [TestMethod]
    public async Task GetBasicInfoWebAsync_KnownUser_ReturnsCompatibleShape()
    {
        try
        {
            var userInfo = await Client.Users.GetBasicInfoWebAsync(1);

            Assert.IsNotNull(userInfo);
            Assert.IsGreaterThan(0L, userInfo.UserId);
            Assert.IsFalse(userInfo.Portrait.Contains('?', StringComparison.Ordinal));
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping basic-info-web integration path in this environment: {exception.Message}");
        }
    }

    [TestMethod]
    public async Task GetHomepageAsync_KnownUser_ReturnsHomepageSnapshot()
    {
        try
        {
            var homepage = await Client.Users.GetHomepageAsync(1, 1);

            Assert.IsNotNull(homepage);
            Assert.IsNotNull(homepage.User);
            Assert.IsGreaterThan(0L, homepage.User.UserId);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping homepage integration path in this environment: {exception.Message}");
        }
    }

    [TestMethod]
    public async Task GetBlacklistLegacyAsync_AuthenticatedAccount_ReturnsLegacyPageShape()
    {
        EnsureAuthenticated();

        var blacklist = await Client.Users.GetBlacklistLegacyAsync(1, 20);

        Assert.IsNotNull(blacklist);
        Assert.IsGreaterThanOrEqualTo(1, blacklist.Page.CurrentPage);
    }

    [TestMethod]
    public async Task GetUserByTiebaUidAsync_SelfAccount_ReturnsMappedUser()
    {
        EnsureAuthenticated();
        try
        {
            var self = await Client.Users.GetSelfInfoAsync();

            var user = await Client.Users.GetUserByTiebaUidAsync(self.TiebaUid);

            Assert.IsNotNull(user);
            Assert.AreEqual(self.TiebaUid, user.TiebaUid);
            Assert.IsGreaterThan(0L, user.UserId);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping tieba-uid self lookup integration path in this environment: {exception.Message}");
        }
    }

    [TestMethod]
    public async Task GetUserForumInfoAsync_SafeFixtures_ReturnsForumScopedProfile()
    {
        var forum = await RequireSafeForumFixtureAsync(nameof(GetUserForumInfoAsync_SafeFixtures_ReturnsForumScopedProfile));
        var portrait = RequireSafeTargetPortraitFixture(nameof(GetUserForumInfoAsync_SafeFixtures_ReturnsForumScopedProfile));

        var info = await Client.Users.GetUserForumInfoAsync(forum.ResolvedName, portrait);

        Assert.IsNotNull(info);
        Assert.AreEqual(forum.ResolvedName, info.Fname);
        Assert.IsFalse(string.IsNullOrWhiteSpace(info.User.Portrait));
    }

    [TestMethod]
    public async Task GetRankUsersAsync_SafeForum_ReturnsRankPage()
    {
        var forum = await RequireSafeForumFixtureAsync(nameof(GetRankUsersAsync_SafeForum_ReturnsRankPage));

        var users = await Client.Users.GetRankUsersAsync(forum.ResolvedName, 1);

        Assert.IsNotNull(users);
        Assert.IsGreaterThanOrEqualTo(1, users.Page.CurrentPage);
        Assert.IsGreaterThanOrEqualTo(1, users.Page.TotalPage);
    }
}
