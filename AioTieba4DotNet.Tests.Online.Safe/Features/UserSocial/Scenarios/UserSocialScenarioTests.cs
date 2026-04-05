#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
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
    private const int FollowScanMaxPages = 5;
    private const int BlacklistOldScanPageSize = 20;
    private const int BlacklistOldScanMaxPages = 5;

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetProfileAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetUserInfoAppAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetUserInfoJsonAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetFollowsAsync)]
    public Task PublicIdentityReadsKnownUserIdReturnConsistentCrossEndpointIdentity()
    {
        return ExecuteSafeAsync(
            "user social public identity sample",
            async scope =>
            {
                var operationName = nameof(PublicIdentityReadsKnownUserIdReturnConsistentCrossEndpointIdentity);
                using var client = CreateClient(scope);
                var target = await ResolveDedicatedTargetUserAsync(scope, client, operationName);

                var profileByUserId = await client.Users.GetProfileAsync((int)target.UserId);
                var profileByPortrait = await client.Users.GetProfileAsync(target.Portrait);
                var userInfoJson = await client.Users.GetUserInfoJsonAsync(RequireTargetUserName(scope, target, operationName));
                var follows = await client.Users.GetFollowsAsync(target.UserId, 1);

                Assert.IsNotNull(profileByUserId);
                Assert.IsNotNull(profileByPortrait);
                Assert.IsNotNull(userInfoJson);
                Assert.AreEqual(target.UserId, profileByUserId.UserId);
                Assert.AreEqual(target.UserId, profileByPortrait.UserId);
                Assert.AreEqual(target.UserId, userInfoJson.UserId);
                Assert.AreEqual(target.Portrait, profileByUserId.Portrait);
                Assert.AreEqual(target.Portrait, profileByPortrait.Portrait);
                Assert.AreEqual(target.Portrait, userInfoJson.Portrait);
                Assert.IsNotNull(profileByUserId.VImage);
                Assert.IsNotNull(profileByPortrait.VImage);

                Assert.IsNotNull(follows);
                Assert.IsNotNull(follows.Page);
                Assert.IsGreaterThanOrEqualTo(1, follows.Page.CurrentPage);
                Assert.IsGreaterThanOrEqualTo(0, follows.Count);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetUserInfoWebAsync)]
    public Task GetUserInfoWebAsyncKnownUserIdReturnsCompatibleShapeOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "user social web identity sample",
            async scope =>
            {
                var targetUserId = RequireTargetUserId(
                    scope,
                    nameof(GetUserInfoWebAsyncKnownUserIdReturnsCompatibleShapeOrExplicitSkip));
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
    public Task GetHomepageAsyncKnownUserIdReturnsHomepageSnapshot()
    {
        return ExecuteSafeAsync(
            "user social homepage sample",
            async scope =>
            {
                var targetUserId = RequireTargetUserId(
                    scope,
                    nameof(GetHomepageAsyncKnownUserIdReturnsHomepageSnapshot));
                using var client = CreateClient(scope);
                var homepage = await client.Users.GetHomepageAsync(targetUserId, 1);

                Assert.IsNotNull(homepage);
                Assert.IsNotNull(homepage.User);
                Assert.AreEqual(targetUserId, homepage.User.UserId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(homepage.User.Portrait));
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetTbsAsync)]
    [TestCategory(OnlineTestApiCategories.UsersLoginAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoInitNicknameAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoMoIndexAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetFansAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetBlacklistAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetBlacklistOldAsync)]
    public Task AuthenticatedAccountSessionAndCollectionReadsSafeCredentialsReturnConsistentIdentityAndCollectionShapes()
    {
        return ExecuteSafeAsync(
            "user social authenticated collections",
            async scope =>
            {
                using var client = CreateClient(scope);
                var tbs = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetTbsAsync());
                var login = await RunUserSocialOrInconclusiveAsync(() => client.Users.LoginAsync());
                var initNickname = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoInitNicknameAsync());
                var moIndex = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoMoIndexAsync());
                var self = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoAsync());
                var fans = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetFansAsync(self.UserId, 1));
                var blacklist = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetBlacklistAsync());
                var blacklistOld = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetBlacklistOldAsync(1, 20));

                Assert.IsFalse(string.IsNullOrWhiteSpace(tbs));
                Assert.IsNotNull(login);
                Assert.IsNotNull(login.User);
                Assert.IsFalse(string.IsNullOrWhiteSpace(login.Tbs));

                Assert.IsNotNull(initNickname);
                Assert.IsNotNull(moIndex);
                Assert.IsNotNull(self);
                Assert.IsPositive(self.UserId);
                Assert.IsPositive(self.TiebaUid);
                Assert.IsFalse(string.IsNullOrWhiteSpace(self.UserName));
                Assert.AreEqual(self.UserId, login.User.UserId);
                Assert.AreEqual(self.UserId, initNickname.UserId);
                Assert.AreEqual(self.UserId, moIndex.UserId);
                Assert.AreEqual(self.TiebaUid, login.User.TiebaUid);
                Assert.AreEqual(self.TiebaUid, initNickname.TiebaUid);
                Assert.AreEqual(self.TiebaUid, moIndex.TiebaUid);

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
    [TestCategory(OnlineTestApiCategories.UsersFollowAsync)]
    [TestCategory(OnlineTestApiCategories.UsersUnfollowAsync)]
    public Task FollowAndUnfollowAsyncDedicatedTargetUserUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "user social follow lifecycle",
            async scope =>
            {
                var operationName = nameof(FollowAndUnfollowAsyncDedicatedTargetUserUsesCompensationAudit);
                using var client = CreateClient(scope);
                var self = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoAsync());
                var target = await ResolveDedicatedTargetUserAsync(scope, client, operationName);
                EnsureDedicatedTargetIsNotSelf(self, target, operationName);

                if (await IsUserFollowedAsync(client, self.UserId, target))
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target portrait '{target.Portrait}' is already followed by the safe account. Use a disposable target user that starts unfollowed so the scenario can prove both follow/unfollow overloads truthfully.");
                }

                var followed = await RunUserSocialOrInconclusiveAsync(() => client.Users.FollowAsync(target.Portrait));
                Assert.IsTrue(followed,
                    $"Expected the dedicated target portrait '{target.Portrait}' to accept a temporary safe follow mutation.");
                Assert.IsTrue(await IsUserFollowedAsync(client, self.UserId, target),
                    "Expected the dedicated target user to appear in the authenticated self-follow listing after the follow mutation.");

                var unfollowed = await RunUserSocialOrInconclusiveAsync(() => client.Users.UnfollowAsync(target.Portrait));
                Assert.IsTrue(unfollowed,
                    $"Expected the dedicated target portrait '{target.Portrait}' to accept a temporary safe unfollow mutation.");
                Assert.IsFalse(await IsUserFollowedAsync(client, self.UserId, target),
                    "Expected the dedicated target user to disappear from the authenticated self-follow listing after the unfollow mutation.");

                var followedAgain = await RunUserSocialOrInconclusiveAsync(() => client.Users.FollowAsync(target.Portrait));
                Assert.IsTrue(followedAgain,
                    $"Expected the dedicated target portrait '{target.Portrait}' to accept a second temporary safe follow mutation for compensation coverage.");
                Assert.IsTrue(await IsUserFollowedAsync(client, self.UserId, target),
                    "Expected the dedicated target user to reappear in the authenticated self-follow listing before compensation runs.");

                var followedArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.UserSocial,
                    "user-follow",
                    target.UserId.ToString(CultureInfo.InvariantCulture),
                    $"temporary follow of dedicated target portrait '{target.Portrait}'");
                scope.Compensation.Register(
                    followedArtifact,
                    "undo dedicated user follow",
                    "user unfollowed",
                    cancellationToken => UnfollowTargetUserAsync(client, target, cancellationToken));

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded,
                    "Expected the UserSocial safe scenario to reconcile the dedicated user follow mutation.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("user unfollowed", audit.CompensationResults[0].CompensationOutcome);

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(target.Portrait, auditDisplay);
                Assert.Contains("unreconciled: none", auditDisplay);
                Assert.IsFalse(await IsUserFollowedAsync(client, self.UserId, target),
                    "Expected the dedicated target user follow mutation to be undone once compensation completed.");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersSetBlacklistAsync)]
    public Task SetBlacklistAsyncDedicatedTargetUserUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "user social current blacklist lifecycle",
            async scope =>
            {
                var operationName = nameof(SetBlacklistAsyncDedicatedTargetUserUsesCompensationAudit);
                using var client = CreateClient(scope);
                var self = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoAsync());
                var target = await ResolveDedicatedTargetUserAsync(scope, client, operationName);
                EnsureDedicatedTargetIsNotSelf(self, target, operationName);

                if (await FindBlacklistEntryAsync(client, target.UserId) is not null)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target user '{target.UserId}' is already present in the current blacklist. Use a disposable target user that starts unblocked so the reversible blacklist path stays truthful.");
                }

                var blacklisted = await RunUserSocialOrInconclusiveAsync(() => client.Users.SetBlacklistAsync(target.UserId, BlacklistType.All));
                Assert.IsTrue(blacklisted,
                    $"Expected the dedicated target user '{target.UserId}' to accept a temporary current-blacklist mutation.");
                var currentEntry = await FindBlacklistEntryAsync(client, target.UserId);
                Assert.IsNotNull(currentEntry);
                Assert.IsTrue(currentEntry.BlockFollow);
                Assert.IsTrue(currentEntry.BlockInteract);
                Assert.IsTrue(currentEntry.BlockChat);

                var cleared = await RunUserSocialOrInconclusiveAsync(() => client.Users.SetBlacklistAsync(target.UserId, BlacklistType.None));
                Assert.IsTrue(cleared,
                    $"Expected the dedicated target user '{target.UserId}' current-blacklist mutation to be reversible through BlacklistType.None.");
                Assert.IsNull(await FindBlacklistEntryAsync(client, target.UserId),
                    "Expected the dedicated target user to disappear from the current blacklist after the explicit reset.");

                var blacklistedAgain = await RunUserSocialOrInconclusiveAsync(() => client.Users.SetBlacklistAsync(target.UserId, BlacklistType.All));
                Assert.IsTrue(blacklistedAgain,
                    $"Expected the dedicated target user '{target.UserId}' to accept a second current-blacklist mutation for compensation coverage.");
                Assert.IsNotNull(await FindBlacklistEntryAsync(client, target.UserId));

                var blacklistArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.UserSocial,
                    "user-blacklist",
                    target.UserId.ToString(CultureInfo.InvariantCulture),
                    $"temporary current-blacklist mutation for dedicated target user '{target.UserId}'");
                scope.Compensation.Register(
                    blacklistArtifact,
                    "clear dedicated target current blacklist",
                    "current blacklist cleared",
                    cancellationToken => ClearCurrentBlacklistAsync(client, target.UserId, cancellationToken));

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded,
                    "Expected the UserSocial safe scenario to reconcile the current blacklist mutation.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("current blacklist cleared", audit.CompensationResults[0].CompensationOutcome);

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(target.UserId.ToString(CultureInfo.InvariantCulture), auditDisplay);
                Assert.Contains("unreconciled: none", auditDisplay);
                Assert.IsNull(await FindBlacklistEntryAsync(client, target.UserId),
                    "Expected the dedicated target user current-blacklist mutation to be undone once compensation completed.");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersAddBlacklistOldAsync)]
    [TestCategory(OnlineTestApiCategories.UsersRemoveBlacklistOldAsync)]
    public Task BlacklistOldLifecycleAsyncDedicatedTargetUserUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "user social old blacklist lifecycle",
            async scope =>
            {
                var operationName = nameof(BlacklistOldLifecycleAsyncDedicatedTargetUserUsesCompensationAudit);
                using var client = CreateClient(scope);
                var self = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoAsync());
                var target = await ResolveDedicatedTargetUserAsync(scope, client, operationName);
                EnsureDedicatedTargetIsNotSelf(self, target, operationName);

                if (await FindBlacklistOldEntryAsync(client, target.UserId) is not null)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target user '{target.UserId}' is already present in the _old blacklist. Use a disposable target user that starts outside the _old list so the reversible add/remove path stays truthful.");
                }

                var added = await RunUserSocialOrInconclusiveAsync(() => client.Users.AddBlacklistOldAsync(target.UserId));
                Assert.IsTrue(added,
                    $"Expected the dedicated target user '{target.UserId}' to accept a temporary _old blacklist add mutation.");
                Assert.IsNotNull(await FindBlacklistOldEntryAsync(client, target.UserId),
                    "Expected the dedicated target user to appear in the _old blacklist after the add mutation.");

                var removed = await RunUserSocialOrInconclusiveAsync(() => client.Users.RemoveBlacklistOldAsync(target.UserId));
                Assert.IsTrue(removed,
                    $"Expected the dedicated target user '{target.UserId}' _old blacklist mutation to be reversible.");
                Assert.IsNull(await FindBlacklistOldEntryAsync(client, target.UserId),
                    "Expected the dedicated target user to disappear from the _old blacklist after the remove mutation.");

                var addedAgain = await RunUserSocialOrInconclusiveAsync(() => client.Users.AddBlacklistOldAsync(target.UserId));
                Assert.IsTrue(addedAgain,
                    $"Expected the dedicated target user '{target.UserId}' to accept a second _old blacklist add mutation for compensation coverage.");
                Assert.IsNotNull(await FindBlacklistOldEntryAsync(client, target.UserId));

                var blacklistOldArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.UserSocial,
                    "user-blacklist-old",
                    target.UserId.ToString(CultureInfo.InvariantCulture),
                    $"temporary _old blacklist mutation for dedicated target user '{target.UserId}'");
                scope.Compensation.Register(
                    blacklistOldArtifact,
                    "clear dedicated target old blacklist",
                    "old blacklist cleared",
                    cancellationToken => RemoveOldBlacklistAsync(client, target.UserId, cancellationToken));

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded,
                    "Expected the UserSocial safe scenario to reconcile the _old blacklist mutation.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("old blacklist cleared", audit.CompensationResults[0].CompensationOutcome);

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(target.UserId.ToString(CultureInfo.InvariantCulture), auditDisplay);
                Assert.Contains("unreconciled: none", auditDisplay);
                Assert.IsNull(await FindBlacklistOldEntryAsync(client, target.UserId),
                    "Expected the dedicated target user _old blacklist mutation to be undone once compensation completed.");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetUserByTiebaUidAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetThreadsAsync)]
    [TestCategory(OnlineTestApiCategories.UsersGetPostsAsync)]
    public Task AuthenticatedAccountContentReadsSafeCredentialsReturnMappedThreadsAndPosts()
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
    public Task GetUserForumInfoAndRankUsersAsyncDedicatedForumAndPortraitReturnsForumScopedResults()
    {
        return ExecuteSafeAsync(
            "user social dedicated forum profile sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetUserForumInfoAndRankUsersAsyncDedicatedForumAndPortraitReturnsForumScopedResults);
                var forumName = await ResolveDedicatedForumNameAsync(scope, client, operationName);
                var forum = await client.Forums.GetForumAsync(forumName);
                var portrait = RequireTargetPortrait(scope, operationName);

                Assert.IsNotNull(forum);
                Assert.IsPositive(forum.Fid);
                Assert.AreEqual(forumName, forum.Fname);

                var userForumInfoByFname = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserForumInfoAsync(forumName, portrait));
                var userForumInfoByFid = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserForumInfoAsync((ulong)forum.Fid, portrait));
                var rankUsers = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetRankUsersAsync(forumName, 1));

                Assert.IsNotNull(userForumInfoByFname);
                Assert.IsNotNull(userForumInfoByFid);
                Assert.AreEqual(forumName, userForumInfoByFname.Fname);
                Assert.AreEqual(forumName, userForumInfoByFid.Fname);
                Assert.IsFalse(string.IsNullOrWhiteSpace(userForumInfoByFname.User.Portrait));
                Assert.AreEqual(userForumInfoByFname.User.Portrait, userForumInfoByFid.User.Portrait);

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

    private static async Task<DedicatedTargetUser> ResolveDedicatedTargetUserAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        var targetUserId = RequireTargetUserId(scope, operationName);
        var userInfoApp = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserInfoAppAsync(targetUserId));

        var portrait = !string.IsNullOrWhiteSpace(scope.Safe.Assets.TargetPortrait)
            ? scope.Safe.Assets.TargetPortrait
            : userInfoApp.Portrait;
        if (string.IsNullOrWhiteSpace(portrait))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: the dedicated target user '{targetUserId}' did not expose a usable portrait. Set {OnlineTestEnvironmentVariables.SafeAssetsTargetPortrait} or choose a target user id that resolves to a stable portrait.");
        }

        return new DedicatedTargetUser(targetUserId, userInfoApp.UserName, portrait);
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

    private static string RequireTargetUserName(
        OnlineExecutionScope scope,
        DedicatedTargetUser target,
        string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.TargetUserName))
            return scope.Safe.Assets.TargetUserName;

        if (!string.IsNullOrWhiteSpace(target.AppUserName))
            return target.AppUserName;

        Assert.Inconclusive(
            $"Skipping {operationName}: the dedicated target user '{target.UserId}' did not expose a stable user name for JSON profile coverage. Set {OnlineTestEnvironmentVariables.SafeAssetsTargetUserName} or choose a target user id that resolves to a user name.");
        return string.Empty;
    }

    private static void EnsureDedicatedTargetIsNotSelf(UserInfo self, DedicatedTargetUser target, string operationName)
    {
        if (self.UserId != target.UserId)
            return;

        Assert.Inconclusive(
            $"Skipping {operationName}: the dedicated target user id '{target.UserId}' resolves to the safe account itself. Configure a separate disposable target user for reversible social mutations.");
    }

    private static async Task<bool> IsUserFollowedAsync(TiebaClient client, long ownerUserId, DedicatedTargetUser target)
    {
        for (var page = 1; page <= FollowScanMaxPages; page++)
        {
            var follows = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetFollowsAsync(ownerUserId, page));
            if (follows.Any(user => user.UserId == target.UserId
                                    || string.Equals(user.Portrait, target.Portrait, StringComparison.Ordinal)))
            {
                return true;
            }

            if (!follows.Page.HasMore || follows.Page.CurrentPage >= follows.Page.TotalPage)
                return false;
        }

        return false;
    }

    private static async Task<BlacklistUser?> FindBlacklistEntryAsync(TiebaClient client, long userId)
    {
        var blacklist = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetBlacklistAsync());
        return blacklist.FirstOrDefault(entry => entry.UserId == userId);
    }

    private static async Task<BlacklistOldUser?> FindBlacklistOldEntryAsync(TiebaClient client, long userId)
    {
        for (var page = 1; page <= BlacklistOldScanMaxPages; page++)
        {
            var blacklistOld = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetBlacklistOldAsync(page, BlacklistOldScanPageSize));
            var entry = blacklistOld.FirstOrDefault(candidate => candidate.UserId == userId);
            if (entry is not null)
                return entry;

            if (!blacklistOld.Page.HasMore || blacklistOld.Page.CurrentPage >= blacklistOld.Page.TotalPage)
                return null;
        }

        return null;
    }

    private static async ValueTask UnfollowTargetUserAsync(
        TiebaClient client,
        DedicatedTargetUser target,
        CancellationToken cancellationToken)
    {
        var unfollowed = await client.Users.UnfollowAsync(target.Portrait, cancellationToken);
        if (!unfollowed)
        {
            throw new InvalidOperationException(
                $"Expected the dedicated target portrait '{target.Portrait}' to be unfollowed during compensation.");
        }
    }

    private static async ValueTask ClearCurrentBlacklistAsync(
        TiebaClient client,
        long userId,
        CancellationToken cancellationToken)
    {
        var cleared = await client.Users.SetBlacklistAsync(userId, BlacklistType.None, cancellationToken);
        if (!cleared)
        {
            throw new InvalidOperationException(
                $"Expected the dedicated target user '{userId}' current blacklist mutation to be cleared during compensation.");
        }
    }

    private static async ValueTask RemoveOldBlacklistAsync(
        TiebaClient client,
        long userId,
        CancellationToken cancellationToken)
    {
        var removed = await client.Users.RemoveBlacklistOldAsync(userId, cancellationToken);
        if (!removed)
        {
            throw new InvalidOperationException(
                $"Expected the dedicated target user '{userId}' _old blacklist mutation to be cleared during compensation.");
        }
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

    private sealed record DedicatedTargetUser(long UserId, string AppUserName, string Portrait);
}
