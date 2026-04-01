using System;
using System.Threading.Tasks;
using AioTieba4DotNet;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.UserSocial;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.UserSocial)]
public sealed class UserSocialReadTest : TestBase
{
    [TestMethod]
    public async Task GetSelfInfoAsync_ReturnsConfiguredAccountSnapshot()
    {
        EnsureAuthenticated();

        UserSocialOrInconclusive(() => Console.WriteLine("self-info endpoints are account-gated in this environment."));
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetSelfInfoAsync());

        Assert.IsGreaterThan(0, result.UserId);
        Console.WriteLine($"self userId={result.UserId}, userName={result.UserName}, tiebaUid={result.TiebaUid}");
    }

    [TestMethod]
    public async Task GetFansAsync_ReadsSelfFansPage()
    {
        EnsureAuthenticated();

        var self = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetSelfInfoAsync());
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetFansAsync(self.UserId, 1));

        Assert.IsNotNull(result);
        Console.WriteLine($"self userId={self.UserId}, fansCount={result.Count}, currentPage={result.Page.CurrentPage}");
    }

    [TestMethod]
    public async Task GetAtsAsync_ReadsAtInboxPage()
    {
        EnsureAuthenticated();

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Messages.GetAtsAsync(1));

        Assert.IsNotNull(result);
        Console.WriteLine($"atsCount={result.Count}, currentPage={result.Page.CurrentPage}, hasMore={result.Page.HasMore}");
    }

    [TestMethod]
    public async Task GetRepliesAsync_ReadsReplyInboxPage()
    {
        EnsureAuthenticated();

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Messages.GetRepliesAsync(1));

        Assert.IsNotNull(result);
        Console.WriteLine($"replyCount={result.Count}, currentPage={result.Page.CurrentPage}, hasMore={result.Page.HasMore}");
    }

    [TestMethod]
    public async Task GetBlacklistAsync_ReadsBlacklistUsersPage()
    {
        EnsureAuthenticated();

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetBlacklistAsync());

        Assert.IsNotNull(result);
        Console.WriteLine($"blacklistUsersCount={result.Count}");
    }

    [TestMethod]
    public async Task GetBlacklistOldAsync_ReadsOldBlacklistPage()
    {
        EnsureAuthenticated();

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetBlacklistOldAsync(1, 20));
        var page = result.GetType().GetProperty("Page")?.GetValue(result);
        var currentPage = page?.GetType().GetProperty("CurrentPage")?.GetValue(page);

        Assert.IsNotNull(result);
        Console.WriteLine($"blacklistOldUsersCount={result.Count}, currentPage={currentPage}");
    }

    [TestMethod]
    public async Task GetUserInfoAppAsync_KnownUser_ReturnsAppShape()
    {
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetUserInfoAppAsync(1));

        Assert.IsGreaterThan(0L, result.UserId);
        Console.WriteLine($"basicInfoApp userId={result.UserId}, portrait={result.Portrait}");
    }

    [TestMethod]
    public async Task GetUserInfoWebAsync_KnownUser_ReturnsCompatibleShape()
    {
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetUserInfoWebAsync(1));

        Assert.IsGreaterThan(0L, result.UserId);
        Assert.IsFalse(result.Portrait.Contains('?', StringComparison.Ordinal));
        Console.WriteLine($"basicInfoWeb userId={result.UserId}, portrait={result.Portrait}");
    }

    [TestMethod]
    public async Task GetHomepageAsync_KnownUser_ReturnsHomepageShape()
    {
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetHomepageAsync(1, 1));

        Assert.IsNotNull(result.User);
        Console.WriteLine($"homepageUserId={result.User.UserId}, threadCount={result.Count}");
    }

    [TestMethod]
    public async Task GetUserByTiebaUidAsync_SelfAccount_ReturnsMappedUser()
    {
        EnsureAuthenticated();

        var self = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetSelfInfoAsync());
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetUserByTiebaUidAsync(self.TiebaUid));

        Assert.AreEqual(self.TiebaUid, result.TiebaUid);
        Console.WriteLine($"tiebaUid={result.TiebaUid}, mappedUserId={result.UserId}");
    }

    [TestMethod]
    public async Task GetUserForumInfoAsync_SafeFixtures_ReturnsForumScopedProfile()
    {
        var forum = await RequireSafeForumFixtureAsync(nameof(GetUserForumInfoAsync_SafeFixtures_ReturnsForumScopedProfile));
        var portrait = RequireSafeTargetPortraitFixture(nameof(GetUserForumInfoAsync_SafeFixtures_ReturnsForumScopedProfile));

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetUserForumInfoAsync(forum.ResolvedName, portrait));

        Assert.IsNotNull(result);
        Console.WriteLine($"forum={result.Fname}, portrait={result.User.Portrait}, level={result.Level}");
    }

    [TestMethod]
    public async Task GetRankUsersAsync_SafeForum_ReturnsRankPage()
    {
        var forum = await RequireSafeForumFixtureAsync(nameof(GetRankUsersAsync_SafeForum_ReturnsRankPage));

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetRankUsersAsync(forum.ResolvedName, 1));

        Assert.IsNotNull(result);
        Console.WriteLine($"forum={forum.ResolvedName}, rankCount={result.Count}, currentPage={result.Page.CurrentPage}");
    }

    [TestMethod]
    public async Task GetThreadsAsync_ReadsSelfUserThreadPage()
    {
        EnsureAuthenticated();

        var self = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetSelfInfoAsync());
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetThreadsAsync((int)self.UserId, 1, true));

        Assert.IsNotNull(result);
        Console.WriteLine($"selfUserId={self.UserId}, threadCount={result.Count}");
    }

    [TestMethod]
    public async Task GetPostsAsync_ReadsSelfUserPostPage()
    {
        EnsureAuthenticated();

        var self = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetSelfInfoAsync());
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetPostsAsync((int)self.UserId, 1, 20));

        Assert.IsNotNull(result);
        Console.WriteLine($"selfUserId={self.UserId}, postCount={result.Count}");
    }

    private static async Task<T> RunUserSocialOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (exception.Code is 1 or 1130032 or 110000 or 110004)
        {
            Assert.Inconclusive($"Skipping live user-social read in this environment: {exception.Message}");
            throw;
        }
    }

    private static void UserSocialOrInconclusive(Action action)
    {
        action();
    }
}
