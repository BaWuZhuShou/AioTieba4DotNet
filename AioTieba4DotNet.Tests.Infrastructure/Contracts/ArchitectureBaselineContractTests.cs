#nullable enable
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class ArchitectureBaselineContractTests
{
    [TestMethod]
    public void TargetTopology_UsesExpectedFourProjectRoots()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestProjectTopology.Infrastructure,
                OnlineTestProjectTopology.Safe,
                OnlineTestProjectTopology.Restricted,
                OnlineTestProjectTopology.Suite
            },
            OnlineTestProjectTopology.ProjectNames);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestProjectTopology.Infrastructure,
                OnlineTestProjectTopology.Suite
            },
            OnlineTestProjectTopology.ContractProjectNames);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestProjectTopology.Safe,
                OnlineTestProjectTopology.Restricted
            },
            OnlineTestProjectTopology.ScenarioProjectNames);
    }

    [TestMethod]
    public void MetadataTaxonomy_UsesExpectedFeatureTierStageCapabilityAndApiVocabulary()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestContractCategories.Architecture,
                OnlineTestContractCategories.Environment,
                OnlineTestContractCategories.Gating,
                OnlineTestContractCategories.ProjectLayout,
                OnlineTestContractCategories.Style,
                OnlineTestContractCategories.Cleanup,
                OnlineTestContractCategories.CleanupFailure,
                OnlineTestContractCategories.RestrictedIsolation,
                OnlineTestContractCategories.ThreadWriteCleanupFailure
            },
            OnlineTestContractCategories.All);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestFeatureCategories.ForumFoundation,
                OnlineTestFeatureCategories.ForumExtensions,
                OnlineTestFeatureCategories.ThreadRead,
                OnlineTestFeatureCategories.ThreadWrite,
                OnlineTestFeatureCategories.UserSocial,
                OnlineTestFeatureCategories.Messaging,
                OnlineTestFeatureCategories.Moderation,
                OnlineTestFeatureCategories.Admin
            },
            OnlineTestFeatureCategories.All);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestTierCategories.Safe,
                OnlineTestTierCategories.Restricted
            },
            OnlineTestTierCategories.All);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestStageCategories.ForumFoundation,
                OnlineTestStageCategories.ForumExtensions,
                OnlineTestStageCategories.ThreadRead,
                OnlineTestStageCategories.UserSocial,
                OnlineTestStageCategories.Messaging,
                OnlineTestStageCategories.ThreadWrite,
                OnlineTestStageCategories.ModerationRestricted,
                OnlineTestStageCategories.AdminRestricted
            },
            OnlineTestStageCategories.All);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestCapabilityCategories.Authenticated,
                OnlineTestCapabilityCategories.Messaging,
                OnlineTestCapabilityCategories.Moderation,
                OnlineTestCapabilityCategories.Admin
            },
            OnlineTestCapabilityCategories.All);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestApiCategories.ForumsGetForumAsync,
                OnlineTestApiCategories.ForumsGetDetailAsync,
                OnlineTestApiCategories.ForumsGetFnameAsync,
                OnlineTestApiCategories.ForumsSearchExactAsync,
                OnlineTestApiCategories.ForumsGetLastReplyersAsync,
                OnlineTestApiCategories.ForumsGetRankForumsAsync,
                OnlineTestApiCategories.ForumsGetSelfFollowForumsAsync,
                OnlineTestApiCategories.ForumsFollowAsync,
                OnlineTestApiCategories.ForumsUnfollowAsync,
                OnlineTestApiCategories.ThreadsGetThreadsAsync,
                OnlineTestApiCategories.ThreadsGetPostsAsync,
                OnlineTestApiCategories.ThreadsGetCommentsAsync,
                OnlineTestApiCategories.ThreadsAddPostAsync,
                OnlineTestApiCategories.ThreadsAgreeAsync,
                OnlineTestApiCategories.ThreadsDelPostAsync,
                OnlineTestApiCategories.ThreadsUnagreeAsync,
                OnlineTestApiCategories.ThreadsRecoverAsync,
                OnlineTestApiCategories.UsersGetProfileAsync,
                OnlineTestApiCategories.UsersGetUserInfoAppAsync,
                OnlineTestApiCategories.UsersGetUserInfoWebAsync,
                OnlineTestApiCategories.UsersGetHomepageAsync,
                OnlineTestApiCategories.UsersGetSelfInfoAsync,
                OnlineTestApiCategories.UsersGetFansAsync,
                OnlineTestApiCategories.UsersGetBlacklistAsync,
                OnlineTestApiCategories.UsersGetBlacklistOldAsync,
                OnlineTestApiCategories.UsersGetUserByTiebaUidAsync,
                OnlineTestApiCategories.UsersGetThreadsAsync,
                OnlineTestApiCategories.UsersGetPostsAsync,
                OnlineTestApiCategories.UsersGetUserForumInfoAsync,
                OnlineTestApiCategories.UsersGetRankUsersAsync,
                OnlineTestApiCategories.UsersGetPanelInfoAsync,
                OnlineTestApiCategories.MessagesGetAtsAsync,
                OnlineTestApiCategories.MessagesGetRepliesAsync,
                OnlineTestApiCategories.MessagesGetGroupMessagesAsync,
                OnlineTestApiCategories.MessagesSendMessageAsync,
                OnlineTestApiCategories.AdminsGetBawuInfoAsync,
                OnlineTestApiCategories.AdminsGetBlocksAsync,
                OnlineTestApiCategories.AdminsBlockAsync,
                OnlineTestApiCategories.AdminsUnblockAsync
            },
            OnlineTestApiCategories.All);
        CollectionAssert.AreEqual(
            new (string Feature, string Tier, string Stage, string? Capability)[]
            {
                (
                    OnlineTestFeatureCategories.ForumFoundation,
                    OnlineTestTierCategories.Safe,
                    OnlineTestStageCategories.ForumFoundation,
                    null),
                (
                    OnlineTestFeatureCategories.ForumExtensions,
                    OnlineTestTierCategories.Safe,
                    OnlineTestStageCategories.ForumExtensions,
                    OnlineTestCapabilityCategories.Authenticated),
                (
                    OnlineTestFeatureCategories.ThreadRead,
                    OnlineTestTierCategories.Safe,
                    OnlineTestStageCategories.ThreadRead,
                    null),
                (
                    OnlineTestFeatureCategories.UserSocial,
                    OnlineTestTierCategories.Safe,
                    OnlineTestStageCategories.UserSocial,
                    OnlineTestCapabilityCategories.Authenticated),
                (
                    OnlineTestFeatureCategories.Messaging,
                    OnlineTestTierCategories.Safe,
                    OnlineTestStageCategories.Messaging,
                    OnlineTestCapabilityCategories.Messaging),
                (
                    OnlineTestFeatureCategories.ThreadWrite,
                    OnlineTestTierCategories.Safe,
                    OnlineTestStageCategories.ThreadWrite,
                    OnlineTestCapabilityCategories.Authenticated),
                (
                    OnlineTestFeatureCategories.Moderation,
                    OnlineTestTierCategories.Restricted,
                    OnlineTestStageCategories.ModerationRestricted,
                    OnlineTestCapabilityCategories.Moderation),
                (
                    OnlineTestFeatureCategories.Admin,
                    OnlineTestTierCategories.Restricted,
                    OnlineTestStageCategories.AdminRestricted,
                    OnlineTestCapabilityCategories.Admin)
            },
            OnlineSuiteExecutionContract.FeatureMatrix
                .Select(static entry =>
                    (entry.FeatureCategory, entry.TierCategory, entry.StageCategory, entry.CapabilityCategory))
                .ToArray());
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Safe)]
    public void DefaultExecution_UsesSafeOnlyTierAndSafeOrderedSuite()
    {
        CollectionAssert.AreEqual(
            new[] { OnlineTestTierCategories.Safe },
            OnlineSuiteExecutionContract.DefaultTierCategories);
        CollectionAssert.AreEqual(
            new[] { OnlineTestSuiteCategories.SafeOrdered },
            OnlineSuiteExecutionContract.DefaultSuiteCategories);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestStageCategories.ForumFoundation,
                OnlineTestStageCategories.ForumExtensions,
                OnlineTestStageCategories.ThreadRead,
                OnlineTestStageCategories.UserSocial,
                OnlineTestStageCategories.Messaging,
                OnlineTestStageCategories.ThreadWrite
            },
            OnlineSuiteExecutionContract.SafeOrderedStageCategories);

        Assert.DoesNotContain(OnlineTestTierCategories.Restricted, OnlineSuiteExecutionContract.DefaultTierCategories);
        Assert.DoesNotContain(
            OnlineTestSuiteCategories.RestrictedOrdered,
            OnlineSuiteExecutionContract.DefaultSuiteCategories);
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Restricted)]
    public void RestrictedExecution_RemainsExplicitAndCapabilityBacked()
    {
        var restrictedEntries = OnlineSuiteExecutionContract.FeatureMatrix
            .Where(static entry => entry.TierCategory == OnlineTestTierCategories.Restricted)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestTierCategories.Restricted,
                OnlineTestSuiteCategories.RestrictedOrdered
            },
            OnlineSuiteExecutionContract.RestrictedOptInCategories);
        Assert.HasCount(2, restrictedEntries);
        Assert.IsTrue(
            restrictedEntries.All(static entry => entry.CapabilityCategory is not null),
            "Restricted feature entries must always declare an explicit capability category.");
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestStageCategories.ModerationRestricted,
                OnlineTestStageCategories.AdminRestricted
            },
            restrictedEntries.Select(static entry => entry.StageCategory).ToArray());
    }

}
