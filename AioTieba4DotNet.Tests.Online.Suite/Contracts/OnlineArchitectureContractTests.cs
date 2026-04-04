#nullable enable
using System;
using System.IO;
using System.Linq;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Suite.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class OnlineArchitectureContractTests
{
    [TestMethod]
    public void SolutionMembership_ContainsCurrentProjectShells()
    {
        var solutionText = File.ReadAllText(RepositoryPaths.GetSolutionPath());

        foreach (var projectName in OnlineTestProjectTopology.ProjectNames)
        {
            Assert.IsTrue(
                Directory.Exists(RepositoryPaths.GetProjectDirectory(projectName)),
                $"Expected project directory '{projectName}' to exist for the current online test topology.");
            Assert.IsTrue(
                File.Exists(RepositoryPaths.GetProjectFilePath(projectName)),
                $"Expected project file '{projectName}.csproj' to exist for the current online test topology.");
            Assert.Contains(
                $"\"{projectName}\", \"{projectName}\\{projectName}.csproj\"",
                solutionText);
        }
    }

    [TestMethod]
    public void ProjectShells_ContainMinimalSource_And_NewReferencesOnly()
    {
        var infrastructureReference =
            $"..\\{OnlineTestProjectTopology.Infrastructure}\\{OnlineTestProjectTopology.Infrastructure}.csproj";

        foreach (var projectName in OnlineTestProjectTopology.ProjectNames)
        {
            var sourceFiles = Directory.EnumerateFiles(
                    RepositoryPaths.GetProjectDirectory(projectName),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Where(static path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase))
                .Where(static path => !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
                .Where(static path => !path.Contains("\\TestResults\\", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.IsNotEmpty(sourceFiles, $"Project '{projectName}' should keep at least one source file in its shell.");
        }

        foreach (var projectName in OnlineTestProjectTopology.ProjectNames.Except(new[] { OnlineTestProjectTopology.Infrastructure }))
            Assert.Contains(infrastructureReference, File.ReadAllText(RepositoryPaths.GetProjectFilePath(projectName)));

        Assert.DoesNotContain(
            "<ProjectReference Include=",
            File.ReadAllText(RepositoryPaths.GetProjectFilePath(OnlineTestProjectTopology.Infrastructure)));
    }

    [TestMethod]
    public void MetadataVocabulary_MatchesCurrentTopologyContract()
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
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Safe)]
    public void DefaultExecution_RemainsSafeOnly()
    {
        CollectionAssert.AreEqual(new[] { OnlineTestTierCategories.Safe }, OnlineSuiteExecutionContract.DefaultTierCategories);
        CollectionAssert.AreEqual(new[] { OnlineTestSuiteCategories.SafeOrdered }, OnlineSuiteExecutionContract.DefaultSuiteCategories);
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
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestFeatureCategories.ForumFoundation,
                OnlineTestFeatureCategories.ForumExtensions,
                OnlineTestFeatureCategories.ThreadRead,
                OnlineTestFeatureCategories.UserSocial,
                OnlineTestFeatureCategories.Messaging,
                OnlineTestFeatureCategories.ThreadWrite
            },
            OnlineSuiteExecutionContract.FeatureMatrix
                .Where(static entry => entry.TierCategory == OnlineTestTierCategories.Safe)
                .Select(static entry => entry.FeatureCategory)
                .ToArray());

        Assert.DoesNotContain(OnlineTestTierCategories.Restricted, OnlineSuiteExecutionContract.DefaultTierCategories);
        Assert.DoesNotContain(
            OnlineTestSuiteCategories.RestrictedOrdered,
            OnlineSuiteExecutionContract.DefaultSuiteCategories);
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Restricted)]
    public void RestrictedExecution_RemainsExplicitOptIn()
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
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestStageCategories.ModerationRestricted,
                OnlineTestStageCategories.AdminRestricted
            },
            OnlineSuiteExecutionContract.RestrictedOrderedStageCategories);

        Assert.HasCount(2, restrictedEntries);
        Assert.IsTrue(
            restrictedEntries.All(static entry => entry.CapabilityCategory is not null),
            "Restricted feature entries must remain explicit opt-in cases with a declared capability.");
        Assert.DoesNotContain(OnlineTestTierCategories.Restricted, OnlineSuiteExecutionContract.DefaultTierCategories);
        Assert.DoesNotContain(
            OnlineTestStageCategories.ModerationRestricted,
            OnlineSuiteExecutionContract.SafeOrderedStageCategories);
        Assert.DoesNotContain(
            OnlineTestStageCategories.AdminRestricted,
            OnlineSuiteExecutionContract.SafeOrderedStageCategories);
    }

}
