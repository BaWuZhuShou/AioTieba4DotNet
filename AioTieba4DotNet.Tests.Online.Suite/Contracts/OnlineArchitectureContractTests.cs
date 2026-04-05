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
    public void SolutionMembershipContainsCurrentProjectShells()
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
    public void ProjectShellsContainMinimalSourceAndNewReferencesOnly()
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
    public void MetadataVocabularyMatchesCurrentTopologyContract()
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
        CollectionAssert.AreEquivalent(PublicApiCoverageMatrixContract.AllowedFirstClassApiCategories, OnlineTestApiCategories.All);
        Assert.IsTrue(
            OnlineTestApiCategories.All.All(OnlineTestApiCategories.IsWellFormedFirstClassCategory),
            $"Every first-class API category must follow the explicit '{OnlineTestApiCategories.FirstClassNamingRule}' rule.");
        CollectionAssert.AreEquivalent(
            PublicApiCoverageMatrixContract.AllowedFirstClassApiCategories,
            PublicApiCoverageMatrixContract.ClaimedFirstClassApiCategories,
            "The canonical matrix and the filterable first-class Api:* claims must stay in sync.");
    }

    [TestMethod]
    public void PublicApiCoverageMatrixRejectsIndirectOfflineDeferredAndUncoveredApiClaims()
    {
        foreach (var row in PublicApiCoverageMatrixContract.Rows)
        {
            var claimedCategories = PublicApiCoverageMatrixContract.GetClaimedFirstClassApiCategories(row);

            if (PublicApiCoverageMatrixContract.IsDirectOnlineFirstClassApiEligible(row))
            {
                CollectionAssert.AreEqual(
                    new[] { PublicApiCoverageMatrixContract.CreateExpectedFirstClassApiCategory(row) },
                    claimedCategories,
                    $"Matrix row '{row.ApiMember}' at line {row.LineNumber} must advertise exactly one truthful first-class Api:* category.");
                continue;
            }

            Assert.IsEmpty(
                claimedCategories,
                $"Matrix row '{row.ApiMember}' at line {row.LineNumber} must not advertise a filterable Api:* claim because it is not a direct online first-class coverage row.");
        }
    }

    [TestMethod]
    public void FirstClassApiCategoriesResolveToDiscoverableRunnableScenarioTests()
    {
        CollectionAssert.AreEquivalent(
            PublicApiCoverageMatrixContract.AllowedFirstClassApiCategories,
            DiscoverableOnlineTestApiCategoryContract.DiscoverableFirstClassApiCategories,
            "Matrix-backed first-class Api:* categories must map exactly to discoverable Safe/Restricted test usage with no missing or unexpected filter surface.");

        foreach (var category in PublicApiCoverageMatrixContract.AllowedFirstClassApiCategories)
        {
            var tests = DiscoverableOnlineTestApiCategoryContract.GetTestsForCategory(category);
            Assert.IsTrue(
                tests.Length > 0,
                $"First-class Api:* category '{category}' must resolve to at least one discoverable runnable test in the Safe or Restricted scenario projects.");
        }
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Safe)]
    public void DefaultExecutionRemainsSafeOnly()
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
    public void RestrictedExecutionRemainsExplicitOptIn()
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
