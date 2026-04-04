#nullable enable
using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Safe.Features.UserSocial.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.UserSocial)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.UserSocial)]
[TestSubject(typeof(TiebaClient))]
public sealed class UserSocialScenarioTests : OnlineSafeExecutionTestBase
{
    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetProfileAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetUserInfoAppAsync)]
    public Task GetProfileAndUserInfoAppAsync_KnownUserId_ReturnsConsistentPublicIdentity()
    {
        return ExecuteSafeAsync(
            "user social public identity sample",
            async scope =>
            {
                var targetUserId = RequireTargetUserId(
                    scope,
                    nameof(GetProfileAndUserInfoAppAsync_KnownUserId_ReturnsConsistentPublicIdentity));
                using var client = CreateClient(scope);

                var profile = await client.Users.GetProfileAsync(targetUserId);
                var userInfoApp = await client.Users.GetUserInfoAppAsync(targetUserId);

                Assert.IsNotNull(profile);
                Assert.IsNotNull(userInfoApp);
                Assert.AreEqual(targetUserId, profile.UserId);
                Assert.AreEqual(targetUserId, userInfoApp.UserId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(profile.Portrait));
                Assert.IsFalse(string.IsNullOrWhiteSpace(userInfoApp.Portrait));
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetUserInfoWebAsync)]
    public Task GetUserInfoWebAsync_KnownUserId_ReturnsCompatibleShapeOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "user social web identity sample",
            async scope =>
            {
                var targetUserId = RequireTargetUserId(
                    scope,
                    nameof(GetUserInfoWebAsync_KnownUserId_ReturnsCompatibleShapeOrExplicitSkip));
                using var client = CreateClient(scope);
                var userInfoWeb = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserInfoWebAsync(targetUserId));

                Assert.IsNotNull(userInfoWeb);
                Assert.AreEqual(targetUserId, userInfoWeb.UserId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(userInfoWeb.Portrait));
                Assert.DoesNotContain("?", userInfoWeb.Portrait);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetHomepageAsync)]
    public Task GetHomepageAsync_KnownUserId_ReturnsHomepageSnapshot()
    {
        return ExecuteSafeAsync(
            "user social homepage sample",
            async scope =>
            {
                var targetUserId = RequireTargetUserId(
                    scope,
                    nameof(GetHomepageAsync_KnownUserId_ReturnsHomepageSnapshot));
                using var client = CreateClient(scope);
                var homepage = await client.Users.GetHomepageAsync(targetUserId, 1);

                Assert.IsNotNull(homepage);
                Assert.IsNotNull(homepage.User);
                Assert.AreEqual(targetUserId, homepage.User.UserId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(homepage.User.Portrait));
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetFansAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetBlacklistAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetBlacklistOldAsync)]
    public Task AuthenticatedAccountSocialReads_SafeCredentials_ReturnCollectionShapes()
    {
        return ExecuteSafeAsync(
            "user social authenticated collections",
            async scope =>
            {
                using var client = CreateClient(scope);
                var self = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoAsync());
                var fans = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetFansAsync(self.UserId, 1));
                var blacklist = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetBlacklistAsync());
                var blacklistOld = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetBlacklistOldAsync(1, 20));

                Assert.IsNotNull(self);
                Assert.IsPositive(self.UserId);
                Assert.IsPositive(self.TiebaUid);
                Assert.IsFalse(string.IsNullOrWhiteSpace(self.UserName));

                Assert.IsNotNull(fans);
                Assert.IsNotNull(fans.Page);
                Assert.IsGreaterThanOrEqualTo(1, fans.Page.CurrentPage);

                Assert.IsNotNull(blacklist);
                Assert.IsNotNull(blacklistOld);
                Assert.IsNotNull(blacklistOld.Page);
                Assert.IsGreaterThanOrEqualTo(1, blacklistOld.Page.CurrentPage);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetUserByTiebaUidAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetThreadsAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetPostsAsync)]
    public Task AuthenticatedAccountContentReads_SafeCredentials_ReturnMappedThreadsAndPosts()
    {
        return ExecuteSafeAsync(
            "user social authenticated content sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var self = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoAsync());
                Assert.IsInRange(self.UserId, 1L, int.MaxValue);

                var userId = (int)self.UserId;
                var mappedUser = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserByTiebaUidAsync(self.TiebaUid));
                var threads = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetThreadsAsync(userId, 1, true));
                var posts = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetPostsAsync(userId, 1, 20));

                Assert.IsNotNull(mappedUser);
                Assert.AreEqual(self.TiebaUid, mappedUser.TiebaUid);
                Assert.IsPositive(mappedUser.UserId);

                Assert.IsNotNull(threads);
                Assert.IsNotNull(posts);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetUserForumInfoAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetRankUsersAsync)]
    public Task GetUserForumInfoAndRankUsersAsync_DedicatedForumAndPortrait_ReturnsForumScopedResults()
    {
        return ExecuteSafeAsync(
            "user social dedicated forum profile sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumName = await ResolveDedicatedForumNameAsync(scope, client,
                    nameof(GetUserForumInfoAndRankUsersAsync_DedicatedForumAndPortrait_ReturnsForumScopedResults));
                var portrait = RequireTargetPortrait(scope,
                    nameof(GetUserForumInfoAndRankUsersAsync_DedicatedForumAndPortrait_ReturnsForumScopedResults));

                var userForumInfo = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserForumInfoAsync(forumName, portrait));
                var rankUsers = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetRankUsersAsync(forumName, 1));

                Assert.IsNotNull(userForumInfo);
                Assert.AreEqual(forumName, userForumInfo.Fname);
                Assert.IsFalse(string.IsNullOrWhiteSpace(userForumInfo.User.Portrait));

                Assert.IsNotNull(rankUsers);
                Assert.IsNotNull(rankUsers.Page);
                Assert.IsGreaterThanOrEqualTo(1, rankUsers.Page.CurrentPage);
            },
            OnlineExecutionCapability.Authenticated);
    }

    private static TiebaClient CreateClient(OnlineExecutionScope scope)
    {
        var options = new TiebaOptions
        {
            Bduss = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Bduss : null,
            Stoken = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Stoken : null,
            TransportMode = TiebaTransportMode.Http
        };

        return new TiebaClient(options);
    }

    private static async Task<string> ResolveDedicatedForumNameAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumName))
            return scope.Safe.Assets.ForumName;

        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumQuery))
        {
            var forum = await client.Forums.GetForumAsync(scope.Safe.Assets.ForumQuery);
            Assert.IsNotNull(forum);
            Assert.IsFalse(string.IsNullOrWhiteSpace(forum.Fname));
            return forum.Fname;
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: dedicated user-social forum context is required. Set {OnlineTestEnvironmentVariables.SafeAssetsForumName} or {OnlineTestEnvironmentVariables.SafeAssetsForumQuery}.");
        return string.Empty;
    }

    private static string RequireTargetPortrait(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.TargetPortrait))
            return scope.Safe.Assets.TargetPortrait;

        Assert.Inconclusive(
            $"Skipping {operationName}: dedicated user-social portrait fixture is required. Set {OnlineTestEnvironmentVariables.SafeAssetsTargetPortrait}.");
        return string.Empty;
    }

    private static int RequireTargetUserId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.TargetUserId is > 0 and <= int.MaxValue)
            return checked((int)scope.Safe.Assets.TargetUserId.Value);

        if (scope.Safe.Assets.TargetUserId is > int.MaxValue)
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: dedicated user-social target user id must fit the v3 public API surface. Reconfigure {OnlineTestEnvironmentVariables.SafeAssetsTargetUserId} with a positive 32-bit user id.");
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: dedicated user-social target user id is required. Set {OnlineTestEnvironmentVariables.SafeAssetsTargetUserId} instead of relying on a hardcoded public identity.");
        return default;
    }

    private static async Task<T> RunUserSocialOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (exception.Code is 1 or 1130032 or 110000 or 110004)
        {
            Assert.Inconclusive($"Skipping user-social read in this environment: {exception.Message}");
            throw;
        }
    }
}
