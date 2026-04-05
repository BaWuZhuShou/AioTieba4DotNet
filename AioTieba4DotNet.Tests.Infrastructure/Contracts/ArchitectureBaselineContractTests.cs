#nullable enable
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class ArchitectureBaselineContractTests
{
    [TestMethod]
    public void TargetTopologyUsesExpectedFourProjectRoots()
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
    public void MetadataTaxonomyUsesExpectedFeatureTierStageCapabilityAndApiVocabulary()
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
    public void PublicApiCoverageMatrixUsesPlanCompliantDispositionAndTruthfulApiClaims()
    {
        Assert.IsTrue(
            PublicApiCoverageMatrixContract.Rows.All(static row => PublicApiCoverageMatrixContract.IsAllowedTargetLane(row.TargetLane)),
            "Every matrix row must declare one of the canonical target lanes: offline contract/unit, safe, or restricted.");
        Assert.IsTrue(
            PublicApiCoverageMatrixContract.Rows.All(static row => PublicApiCoverageMatrixContract.IsAllowedDisposition(row.Disposition)),
            "Every matrix row must declare one of the plan-compliant disposition values.");
        Assert.IsTrue(
            PublicApiCoverageMatrixContract.Rows.All(static row => PublicApiCoverageMatrixContract.IsAllowedDispositionForLane(row)),
            "Matrix dispositions must stay compatible with their target lane so deferred, offline, safe, and restricted rows remain explicit.");

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
                $"Matrix row '{row.ApiMember}' at line {row.LineNumber} must not advertise filterable Api:* claims when coverage is '{row.CurrentCoverage}' and disposition is '{row.Disposition}'.");
        }

        CollectionAssert.AreEquivalent(
            PublicApiCoverageMatrixContract.AllowedFirstClassApiCategories,
            PublicApiCoverageMatrixContract.ClaimedFirstClassApiCategories,
            "Matrix-backed direct online eligibility and the matrix's safe-api/restricted-api claims must stay in sync.");
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
    public void DefaultExecutionUsesSafeOnlyTierAndSafeOrderedSuite()
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
    public void RestrictedExecutionRemainsExplicitAndCapabilityBacked()
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
