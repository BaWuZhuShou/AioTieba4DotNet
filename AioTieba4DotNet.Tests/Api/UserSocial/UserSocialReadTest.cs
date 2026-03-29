using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.UserSocial;

[TestClass]
public class UserSocialReadTest : TestBase
{
    [TestMethod]
    [TestCategory("Live")]
    public async Task GetSelfInfoAsync_ReturnsConfiguredAccountSnapshot()
    {
        EnsureAuthenticated();

        UserSocialOrInconclusive(() => Console.WriteLine("self-info endpoints are account-gated in this environment."));
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetSelfInfoAsync());

        Assert.IsTrue(result.UserId > 0);
        Console.WriteLine($"self userId={result.UserId}, userName={result.UserName}, tiebaUid={result.TiebaUid}");
    }

    [TestMethod]
    [TestCategory("Live")]
    public async Task GetFansAsync_ReadsSelfFansPage()
    {
        EnsureAuthenticated();

        var self = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetSelfInfoAsync());
        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetFansAsync(self.UserId, 1));

        Assert.IsNotNull(result);
        Console.WriteLine($"self userId={self.UserId}, fansCount={result.Count}, currentPage={result.Page.CurrentPage}");
    }

    [TestMethod]
    [TestCategory("Live")]
    public async Task GetAtsAsync_ReadsAtInboxPage()
    {
        EnsureAuthenticated();

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetAtsAsync(1));

        Assert.IsNotNull(result);
        Console.WriteLine($"atsCount={result.Count}, currentPage={result.Page.CurrentPage}, hasMore={result.Page.HasMore}");
    }

    [TestMethod]
    [TestCategory("Live")]
    public async Task GetRepliesAsync_ReadsReplyInboxPage()
    {
        EnsureAuthenticated();

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetRepliesAsync(1));

        Assert.IsNotNull(result);
        Console.WriteLine($"replyCount={result.Count}, currentPage={result.Page.CurrentPage}, hasMore={result.Page.HasMore}");
    }

    [TestMethod]
    [TestCategory("Live")]
    public async Task GetBlacklistAsync_ReadsBlacklistPage()
    {
        EnsureAuthenticated();

        var result = await RunUserSocialOrInconclusiveAsync(() => Client.Users.GetBlacklistAsync());

        Assert.IsNotNull(result);
        Console.WriteLine($"blacklistCount={result.Count}");
    }

    private static async Task<T> RunUserSocialOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (exception.Code == 1 || exception.Code == 1130032)
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
