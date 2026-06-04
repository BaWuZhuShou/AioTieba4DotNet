#nullable enable
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Tiers.Safe.Features.UserSocial.Scenarios;

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
    public Task GetProfileAsyncKnownUserIdReturnsConsistentIdentity()
    {
        return ExecuteSafeAsync(
            "user social profile by user id sample",
            async scope =>
            {
                var operationName = nameof(GetProfileAsyncKnownUserIdReturnsConsistentIdentity);
                using var client = CreateClient(scope);
                var target = RequireDedicatedTargetUser(scope, operationName);
                var profile = await client.Users.GetProfileAsync((int)target.UserId);

                Assert.IsNotNull(profile);
                Assert.AreEqual(target.UserId, profile.UserId);
                Assert.AreEqual(target.Portrait, profile.Portrait);
                Assert.IsNotNull(profile.VImage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetProfileAsync)]
    public Task GetProfileAsyncKnownPortraitReturnsConsistentIdentity()
    {
        return ExecuteSafeAsync(
            "user social profile by portrait sample",
            async scope =>
            {
                var operationName = nameof(GetProfileAsyncKnownPortraitReturnsConsistentIdentity);
                using var client = CreateClient(scope);
                var target = RequireDedicatedTargetUser(scope, operationName);
                var profile = await client.Users.GetProfileAsync(target.Portrait);

                Assert.IsNotNull(profile);
                Assert.AreEqual(target.UserId, profile.UserId);
                Assert.AreEqual(target.Portrait, profile.Portrait);
                Assert.IsNotNull(profile.VImage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetUserInfoJsonAsync)]
    public Task GetUserInfoJsonAsyncKnownUserNameReturnsConsistentIdentity()
    {
        return ExecuteSafeAsync(
            "user social user-info-json sample",
            async scope =>
            {
                var operationName = nameof(GetUserInfoJsonAsyncKnownUserNameReturnsConsistentIdentity);
                using var client = CreateClient(scope);
                var target = RequireDedicatedTargetUser(scope, operationName);
                var userName = RequireTargetUserName(scope, operationName);
                var userInfoJson = await client.Users.GetUserInfoJsonAsync(userName);

                Assert.IsNotNull(userInfoJson);
                Assert.AreEqual(target.UserId, userInfoJson.UserId);
                Assert.AreEqual(target.Portrait, userInfoJson.Portrait);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetFollowsAsync)]
    public Task GetFollowsAsyncKnownUserIdReturnsPageShape()
    {
        return ExecuteSafeAsync(
            "user social follows sample",
            async scope =>
            {
                var operationName = nameof(GetFollowsAsyncKnownUserIdReturnsPageShape);
                using var client = CreateClient(scope);
                var target = RequireDedicatedTargetUser(scope, operationName);
                var follows = await client.Users.GetFollowsAsync(target.UserId, 1);

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
    [TestCategory(OnlineTestApiCategories.UsersGetUserInfoAppAsync)]
    public Task GetUserInfoAppAsyncKnownUserIdReturnsCompatibleShapeOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "user social app identity sample",
            async scope =>
            {
                var targetUserId = RequireTargetUserId(
                    scope,
                    nameof(GetUserInfoAppAsyncKnownUserIdReturnsCompatibleShapeOrExplicitSkip));
                using var client = CreateClient(scope);
                var userInfoApp = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserInfoAppAsync(targetUserId));

                Assert.IsNotNull(userInfoApp);
                Assert.AreEqual(targetUserId, userInfoApp.UserId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(userInfoApp.Portrait));

                if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.TargetPortrait))
                    Assert.AreEqual(scope.Safe.Assets.TargetPortrait, userInfoApp.Portrait);
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
    public Task GetTbsAsyncAuthenticatedAccountReturnsNonEmptyToken()
    {
        return ExecuteSafeAsync(
            "user social get-tbs sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var tbs = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetTbsAsync());

                Assert.IsFalse(string.IsNullOrWhiteSpace(tbs));
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersLoginAsync)]
    public Task LoginAsyncSafeCredentialsReturnAuthenticatedSession()
    {
        return ExecuteSafeAsync(
            "user social login sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var login = await RunUserSocialOrInconclusiveAsync(() => client.Users.LoginAsync());

                Assert.IsNotNull(login);
                Assert.IsNotNull(login.User);
                Assert.IsFalse(string.IsNullOrWhiteSpace(login.Tbs));
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoInitNicknameAsync)]
    public Task GetSelfInfoInitNicknameAsyncSafeCredentialsReturnConsistentIdentity()
    {
        return ExecuteSafeAsync(
            "user social self-info init nickname sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var initNickname = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoInitNicknameAsync());

                Assert.IsNotNull(initNickname);
                Assert.IsPositive(initNickname.TiebaUid);
                Assert.IsFalse(string.IsNullOrWhiteSpace(initNickname.UserName) && string.IsNullOrWhiteSpace(initNickname.NickName));
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoMoIndexAsync)]
    public Task GetSelfInfoMoIndexAsyncSafeCredentialsReturnConsistentIdentity()
    {
        return ExecuteSafeAsync(
            "user social self-info mo-index sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var moIndex = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoMoIndexAsync());

                Assert.IsNotNull(moIndex);
                Assert.IsPositive(moIndex.UserId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(moIndex.UserName));
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoAsync)]
    public Task GetSelfInfoAsyncSafeCredentialsReturnConsistentIdentity()
    {
        return ExecuteSafeAsync(
            "user social self-info sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var self = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoAsync());

                Assert.IsNotNull(self);
                Assert.IsPositive(self.UserId);
                Assert.IsPositive(self.TiebaUid);
                Assert.IsFalse(string.IsNullOrWhiteSpace(self.UserName));
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetFansAsync)]
    public Task GetFansAsyncDedicatedTargetUserReturnsPageShape()
    {
        return ExecuteSafeAsync(
            "user social fans sample",
            async scope =>
            {
                var operationName = nameof(GetFansAsyncDedicatedTargetUserReturnsPageShape);
                using var client = CreateClient(scope);
                var targetUserId = RequireTargetUserId(scope, operationName);
                var fans = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetFansAsync(targetUserId, 1));

                Assert.IsNotNull(fans);
                Assert.IsNotNull(fans.Page);
                Assert.IsGreaterThanOrEqualTo(1, fans.Page.CurrentPage);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetBlacklistAsync)]
    public Task GetBlacklistAsyncAuthenticatedAccountReturnsCollectionShape()
    {
        return ExecuteSafeAsync(
            "user social blacklist sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var blacklist = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetBlacklistAsync());

                Assert.IsNotNull(blacklist);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetBlacklistOldAsync)]
    public Task GetBlacklistOldAsyncAuthenticatedAccountReturnsPageShape()
    {
        return ExecuteSafeAsync(
            "user social old blacklist sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var blacklistOld = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetBlacklistOldAsync(1, 20));

                Assert.IsNotNull(blacklistOld);
                Assert.IsNotNull(blacklistOld.Page);
                Assert.IsGreaterThanOrEqualTo(1, blacklistOld.Page.CurrentPage);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersFollowAsync)]
    public Task FollowAsyncDedicatedTargetUserUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "user social follow lifecycle",
            async scope =>
            {
                var operationName = nameof(FollowAsyncDedicatedTargetUserUsesCompensationAudit);
                using var client = CreateClient(scope);
                var target = RequireDedicatedTargetUser(scope, operationName);

                var followed = await RunUserSocialOrInconclusiveAsync(() => client.Users.FollowAsync(target.Portrait));
                if (!followed)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target portrait '{target.Portrait}' did not accept the follow mutation. Reconfigure the disposable target so it starts unfollowed and can truthfully prove Users.FollowAsync in this environment.");
                }

                RegisterFollowCompensation(scope, client, target, $"temporary follow of dedicated target portrait '{target.Portrait}'");

                await scope.Compensation.ExecuteAsync();
                AssertSingleUserAudit(scope, target.Portrait, "user unfollowed", "user-follow");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersUnfollowAsync)]
    public Task UnfollowAsyncDedicatedTargetUserRestoresSetupFollowAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "user social unfollow lifecycle",
            async scope =>
            {
                var operationName = nameof(UnfollowAsyncDedicatedTargetUserRestoresSetupFollowAndPublishesCompensationAudit);
                using var client = CreateClient(scope);
                var target = RequireDedicatedTargetUser(scope, operationName);

                var setupFollowed = await RunUserSocialOrInconclusiveAsync(() => client.Users.FollowAsync(target.Portrait));
                if (!setupFollowed)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target portrait '{target.Portrait}' did not accept the setup follow mutation required before Users.UnfollowAsync can be proved in this environment.");
                }

                RegisterFollowCompensation(scope, client, target, $"setup follow of dedicated target portrait '{target.Portrait}' before Users.UnfollowAsync coverage");

                var unfollowed = await RunUserSocialOrInconclusiveAsync(() => client.Users.UnfollowAsync(target.Portrait));
                if (!unfollowed)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target portrait '{target.Portrait}' did not accept the unfollow mutation after the setup follow state was established.");
                }

                await scope.Compensation.ExecuteAsync();
                AssertSingleUserAudit(scope, target.Portrait, "user unfollowed", "user-follow");
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
                var target = RequireDedicatedTargetUser(scope, operationName);

                var blacklisted = await RunUserSocialOrInconclusiveAsync(() => client.Users.SetBlacklistAsync(target.UserId, BlacklistType.All));
                Assert.IsTrue(blacklisted,
                    $"Expected the dedicated target user '{target.UserId}' to accept a temporary current-blacklist mutation.");

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
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersAddBlacklistOldAsync)]
    public Task AddBlacklistOldAsyncDedicatedTargetUserUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "user social old blacklist add lifecycle",
            async scope =>
            {
                var operationName = nameof(AddBlacklistOldAsyncDedicatedTargetUserUsesCompensationAudit);
                using var client = CreateClient(scope);
                var target = RequireDedicatedTargetUser(scope, operationName);

                var added = await RunUserSocialOrInconclusiveAsync(() => client.Users.AddBlacklistOldAsync(target.UserId));
                if (!added)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target user '{target.UserId}' did not accept the _old blacklist add mutation. Reconfigure the disposable target so it starts outside the _old blacklist and can truthfully prove Users.AddBlacklistOldAsync in this environment.");
                }

                RegisterBlacklistOldCompensation(scope, client, target.UserId, $"temporary _old blacklist mutation for dedicated target user '{target.UserId}'");

                await scope.Compensation.ExecuteAsync();
                AssertSingleUserAudit(scope, target.UserId.ToString(CultureInfo.InvariantCulture), "old blacklist cleared", "user-blacklist-old");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersRemoveBlacklistOldAsync)]
    public Task RemoveBlacklistOldAsyncDedicatedTargetUserRestoresSetupAddAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "user social old blacklist remove lifecycle",
            async scope =>
            {
                var operationName = nameof(RemoveBlacklistOldAsyncDedicatedTargetUserRestoresSetupAddAndPublishesCompensationAudit);
                using var client = CreateClient(scope);
                var target = RequireDedicatedTargetUser(scope, operationName);

                var setupAdded = await RunUserSocialOrInconclusiveAsync(() => client.Users.AddBlacklistOldAsync(target.UserId));
                if (!setupAdded)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target user '{target.UserId}' did not accept the setup _old blacklist add mutation required before Users.RemoveBlacklistOldAsync can be proved in this environment.");
                }

                RegisterBlacklistOldCompensation(scope, client, target.UserId, $"setup _old blacklist add for dedicated target user '{target.UserId}' before Users.RemoveBlacklistOldAsync coverage");

                var removed = await RunUserSocialOrInconclusiveAsync(() => client.Users.RemoveBlacklistOldAsync(target.UserId));
                if (!removed)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated target user '{target.UserId}' did not accept the _old blacklist remove mutation after the setup add state was established.");
                }

                await scope.Compensation.ExecuteAsync();
                AssertSingleUserAudit(scope, target.UserId.ToString(CultureInfo.InvariantCulture), "old blacklist cleared", "user-blacklist-old");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetSelfInfoAsync)]
    public Task GetSelfInfoAsyncAuthenticatedAccountContentProbeReturns32BitUserId()
    {
        return ExecuteSafeAsync(
            "user social content self-info sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var self = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetSelfInfoAsync());

                Assert.IsTrue(
                    self.UserId is >= 1 and <= int.MaxValue,
                    $"Expected authenticated self user id to fit the public 32-bit user-id contract, but got '{self.UserId}'.");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetUserByTiebaUidAsync)]
    public Task GetUserByTiebaUidAsyncAuthenticatedAccountReturnsMappedIdentity()
    {
        return ExecuteSafeAsync(
            "user social user-by-uid sample",
            async scope =>
            {
                var operationName = nameof(GetUserByTiebaUidAsyncAuthenticatedAccountReturnsMappedIdentity);
                using var client = CreateClient(scope);
                var targetTiebaUid = RequireTargetTiebaUid(scope, operationName);
                var mappedUser = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserByTiebaUidAsync(targetTiebaUid));

                Assert.IsNotNull(mappedUser);
                Assert.AreEqual(targetTiebaUid, mappedUser.TiebaUid);
                Assert.IsPositive(mappedUser.UserId);

                if (scope.Safe.Assets.TargetUserId is > 0)
                {
                    Assert.AreEqual(scope.Safe.Assets.TargetUserId.Value, mappedUser.UserId);
                }
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetThreadsAsync)]
    public Task GetThreadsAsyncDedicatedTargetUserReturnsMappedThreadPage()
    {
        return ExecuteSafeAsync(
            "user social user threads sample",
            async scope =>
            {
                var operationName = nameof(GetThreadsAsyncDedicatedTargetUserReturnsMappedThreadPage);
                using var client = CreateClient(scope);
                var targetUserId = RequireTargetUserId(scope, operationName);
                var threads = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetThreadsAsync(targetUserId, 1, true));

                Assert.IsNotNull(threads);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetPostsAsync)]
    public Task GetPostsAsyncDedicatedTargetUserReturnsMappedPostPage()
    {
        return ExecuteSafeAsync(
            "user social user posts sample",
            async scope =>
            {
                var operationName = nameof(GetPostsAsyncDedicatedTargetUserReturnsMappedPostPage);
                using var client = CreateClient(scope);
                var targetUserId = RequireTargetUserId(scope, operationName);
                var posts = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetPostsAsync(targetUserId, 1, 20));

                Assert.IsNotNull(posts);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetUserForumInfoAsync)]
    public Task GetUserForumInfoAsyncDedicatedForumNameAndPortraitReturnsForumScopedProfile()
    {
        return ExecuteSafeAsync(
            "user social forum-info by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetUserForumInfoAsyncDedicatedForumNameAndPortraitReturnsForumScopedProfile);
                var forumName = RequireDedicatedForumSelector(scope, operationName);
                var portrait = RequireTargetPortrait(scope, operationName);
                var userForumInfo = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserForumInfoAsync(forumName, portrait));

                Assert.IsNotNull(userForumInfo);
                AssertForumNameMatchesConfiguredSelector(scope, forumName, userForumInfo.Fname);
                Assert.IsFalse(string.IsNullOrWhiteSpace(userForumInfo.User.Portrait));
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetUserForumInfoAsync)]
    public Task GetUserForumInfoAsyncDedicatedForumFidAndPortraitReturnsForumScopedProfileOrExplicitNumericAssetGate()
    {
        return ExecuteSafeAsync(
            "user social forum-info by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetUserForumInfoAsyncDedicatedForumFidAndPortraitReturnsForumScopedProfileOrExplicitNumericAssetGate);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var portrait = RequireTargetPortrait(scope, operationName);
                var userForumInfo = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetUserForumInfoAsync(forumId, portrait));

                Assert.IsNotNull(userForumInfo);
                Assert.IsFalse(string.IsNullOrWhiteSpace(userForumInfo.Fname));
                Assert.IsFalse(string.IsNullOrWhiteSpace(userForumInfo.User.Portrait));
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetRankUsersAsync)]
    public Task GetRankUsersAsyncDedicatedForumReturnsPageShape()
    {
        return ExecuteSafeAsync(
            "user social rank users sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRankUsersAsyncDedicatedForumReturnsPageShape);
                var forumName = RequireDedicatedForumSelector(scope, operationName);
                var rankUsers = await RunUserSocialOrInconclusiveAsync(() => client.Users.GetRankUsersAsync(forumName, 1));

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

    private static DedicatedTargetUser RequireDedicatedTargetUser(
        OnlineExecutionScope scope,
        string operationName)
    {
        var targetUserId = RequireTargetUserId(scope, operationName);
        var portrait = RequireTargetPortrait(scope, operationName);
        var userName = string.IsNullOrWhiteSpace(scope.Safe.Assets.TargetUserName)
            ? string.Empty
            : scope.Safe.Assets.TargetUserName;

        return new DedicatedTargetUser(targetUserId, userName, portrait);
    }

    private static string RequireDedicatedForumSelector(
        OnlineExecutionScope scope,
        string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumName))
            return scope.Safe.Assets.ForumName;

        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumQuery))
            return scope.Safe.Assets.ForumQuery;

        Assert.Inconclusive(
            $"Skipping {operationName}: dedicated user-social forum context is required. Set {OnlineTestEnvironmentVariables.SafeAssetsForumName} or {OnlineTestEnvironmentVariables.SafeAssetsForumQuery}.");
        return string.Empty;
    }

    private static ulong RequireDedicatedForumId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.ForumId is > 0)
            return (ulong)scope.Safe.Assets.ForumId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: the Users.GetUserForumInfoAsync(fid, portrait) overload requires a numeric dedicated forum asset. Set {OnlineTestEnvironmentVariables.SafeAssetsForumId} for this overload-specific coverage.");
        return default;
    }

    private static void AssertForumNameMatchesConfiguredSelector(
        OnlineExecutionScope scope,
        string configuredSelector,
        string actualForumName)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(actualForumName));

        if (TryResolveCanonicalForumName(scope.Safe.Assets.ForumName) is { } expectedForumName)
        {
            Assert.AreEqual(expectedForumName, actualForumName);
        }
    }

    private static string? TryResolveCanonicalForumName(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        return ulong.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out _)
            ? null
            : candidate;
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

    private static long RequireTargetTiebaUid(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.TargetTiebaUid is > 0)
            return scope.Safe.Assets.TargetTiebaUid.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: dedicated user-social tieba uid coverage requires an explicit tieba uid fixture. Set {OnlineTestEnvironmentVariables.SafeAssetsTargetTiebaUid} so Users.GetUserByTiebaUidAsync stays attributable to a single API call.");
        return default;
    }

    private static string RequireTargetUserName(
        OnlineExecutionScope scope,
        string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.TargetUserName))
            return scope.Safe.Assets.TargetUserName;

        Assert.Inconclusive(
            $"Skipping {operationName}: dedicated user-social user-name coverage requires an explicit target user name. Set {OnlineTestEnvironmentVariables.SafeAssetsTargetUserName} so JSON-profile coverage stays attributable to a single API call.");
        return string.Empty;
    }

    private static void AssertSingleUserAudit(
        OnlineExecutionScope scope,
        string auditMarker,
        string expectedOutcome,
        string expectedArtifactType)
    {
        var audit = scope.Compensation.GetLastAudit();
        Assert.IsNotNull(audit);
        Assert.IsTrue(audit.Succeeded,
            "Expected the UserSocial safe scenario to reconcile the dedicated user mutation.");
        Assert.HasCount(1, audit.RecordedArtifacts);
        Assert.HasCount(1, audit.CompensationResults);
        Assert.IsEmpty(audit.UnreconciledArtifacts);
        Assert.AreEqual(expectedOutcome, audit.CompensationResults[0].CompensationOutcome);
        Assert.AreEqual(expectedArtifactType, audit.RecordedArtifacts[0].ArtifactType);

        var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains(auditMarker, auditDisplay);
        Assert.Contains("unreconciled: none", auditDisplay);
    }

    private static void RegisterBlacklistOldCompensation(
        OnlineExecutionScope scope,
        TiebaClient client,
        long userId,
        string description)
    {
        var blacklistOldArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.UserSocial,
            "user-blacklist-old",
            userId.ToString(CultureInfo.InvariantCulture),
            description);
        scope.Compensation.Register(
            blacklistOldArtifact,
            "clear dedicated target old blacklist",
            "old blacklist cleared",
            cancellationToken => EnsureOldBlacklistRemovedAsync(client, userId, cancellationToken));
    }

    private static void RegisterFollowCompensation(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedTargetUser target,
        string description)
    {
        var followedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.UserSocial,
            "user-follow",
            target.UserId.ToString(CultureInfo.InvariantCulture),
            description);
        scope.Compensation.Register(
            followedArtifact,
            "undo dedicated user follow",
            "user unfollowed",
            cancellationToken => EnsureTargetUserUnfollowedAsync(client, target, cancellationToken));
    }

    private static void RegisterRefollowCompensation(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedTargetUser target,
        string description)
    {
        var followedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.UserSocial,
            "user-follow",
            target.UserId.ToString(CultureInfo.InvariantCulture),
            description);
        scope.Compensation.Register(
            followedArtifact,
            "restore dedicated user follow",
            "user followed",
            cancellationToken => FollowTargetUserAsync(client, target, cancellationToken));
    }

    private static void RegisterBlacklistOldRestoreCompensation(
        OnlineExecutionScope scope,
        TiebaClient client,
        long userId,
        string description)
    {
        var blacklistOldArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.UserSocial,
            "user-blacklist-old",
            userId.ToString(CultureInfo.InvariantCulture),
            description);
        scope.Compensation.Register(
            blacklistOldArtifact,
            "restore dedicated target old blacklist",
            "old blacklist restored",
            cancellationToken => AddOldBlacklistAsync(client, userId, cancellationToken));
    }

    private static async ValueTask FollowTargetUserAsync(
        TiebaClient client,
        DedicatedTargetUser target,
        CancellationToken cancellationToken)
    {
        var followed = await RunUserSocialCompensationAsync(
            cancellationToken => client.Users.FollowAsync(target.Portrait, cancellationToken),
            cancellationToken);
        if (!followed)
        {
            throw new InvalidOperationException(
                $"Expected the dedicated target portrait '{target.Portrait}' to be followed during compensation.");
        }
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

    private static async ValueTask AddOldBlacklistAsync(
        TiebaClient client,
        long userId,
        CancellationToken cancellationToken)
    {
        var added = await client.Users.AddBlacklistOldAsync(userId, cancellationToken);
        if (!added)
        {
            throw new InvalidOperationException(
                $"Expected the dedicated target user '{userId}' _old blacklist mutation to be restored during compensation.");
        }
    }

    private static async ValueTask EnsureTargetUserUnfollowedAsync(
        TiebaClient client,
        DedicatedTargetUser target,
        CancellationToken cancellationToken)
    {
        bool unfollowed;
        try
        {
            unfollowed = await client.Users.UnfollowAsync(target.Portrait, cancellationToken);
        }
        catch (TieBaServerException exception) when (exception.Code == 2260001)
        {
            return;
        }

        if (!unfollowed)
        {
            return;
        }
    }

    private static async ValueTask ClearCurrentBlacklistAsync(
        TiebaClient client,
        long userId,
        CancellationToken cancellationToken)
    {
        var cleared = await RunUserSocialCompensationAsync(
            ct => client.Users.SetBlacklistAsync(userId, BlacklistType.None, ct),
            cancellationToken);
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
        var removed = await RunUserSocialCompensationAsync(
            ct => client.Users.RemoveBlacklistOldAsync(userId, ct),
            cancellationToken);
        if (!removed)
        {
            throw new InvalidOperationException(
                $"Expected the dedicated target user '{userId}' _old blacklist mutation to be cleared during compensation.");
        }
    }

    private static async ValueTask EnsureOldBlacklistRemovedAsync(
        TiebaClient client,
        long userId,
        CancellationToken cancellationToken)
    {
        _ = await RunUserSocialCompensationAsync(
            ct => client.Users.RemoveBlacklistOldAsync(userId, ct),
            cancellationToken);
    }

    private static async Task<T> RunUserSocialOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (exception.Code is 1 or 1130032 or 110000 or 110004 or 1990043 or 2260001 or 220034)
        {
            Assert.Inconclusive($"Skipping user-social scenario in this environment: {exception.Message}");
            throw;
        }
    }

    private static async Task<bool> RunUserSocialCompensationAsync(
        Func<CancellationToken, Task<bool>> action,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (TieBaServerException exception) when (exception.Code is 2260001 or 220034 && attempt < maxAttempts - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken);
            }
        }

        return await action(cancellationToken);
    }

    private sealed record DedicatedTargetUser(long UserId, string UserName, string Portrait);
}
