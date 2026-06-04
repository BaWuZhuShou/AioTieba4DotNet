#nullable enable
using System;
using System.Linq;
using System.Reflection;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Governance.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class OrderedSuiteExecutionContractTests
{
    [TestMethod]
    public void SafeOrderedExecutionPlanMapsStages01Through06ToOnlineScenarioProject()
    {
        var suite = OnlineSuiteExecutionContract.SafeOrderedSuite;

        Assert.AreEqual(OnlineTestSuiteCategories.SafeOrdered, suite.SuiteCategory);
        Assert.AreEqual(OnlineTestTierCategories.Safe, suite.TierCategory);
        Assert.IsFalse(suite.RequiresExplicitSelection);
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
            suite.OrderedStageCategories.ToArray());
        Assert.IsTrue(
            suite.Stages.All(static stage => stage.ProjectName == OnlineTestProjectTopology.Online),
            "Safe ordered suite stages should execute only from the unified online scenario project.");
        CollectionAssert.AreEqual(
            new[]
            {
                $"TestCategory={OnlineTestSuiteCategories.SafeOrdered}&TestCategory={OnlineTestTierCategories.Safe}&TestCategory={OnlineTestStageCategories.ForumFoundation}",
                $"TestCategory={OnlineTestSuiteCategories.SafeOrdered}&TestCategory={OnlineTestTierCategories.Safe}&TestCategory={OnlineTestStageCategories.ForumExtensions}",
                $"TestCategory={OnlineTestSuiteCategories.SafeOrdered}&TestCategory={OnlineTestTierCategories.Safe}&TestCategory={OnlineTestStageCategories.ThreadRead}",
                $"TestCategory={OnlineTestSuiteCategories.SafeOrdered}&TestCategory={OnlineTestTierCategories.Safe}&TestCategory={OnlineTestStageCategories.UserSocial}",
                $"TestCategory={OnlineTestSuiteCategories.SafeOrdered}&TestCategory={OnlineTestTierCategories.Safe}&TestCategory={OnlineTestStageCategories.Messaging}",
                $"TestCategory={OnlineTestSuiteCategories.SafeOrdered}&TestCategory={OnlineTestTierCategories.Safe}&TestCategory={OnlineTestStageCategories.ThreadWrite}"
            },
            suite.Stages.Select(static stage => stage.TestCategoryFilter).ToArray());
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Restricted)]
    public void RestrictedOrderedExecutionPlanRemainsExplicitAndRestrictedOnly()
    {
        var suite = OnlineSuiteExecutionContract.RestrictedOrderedSuite;

        Assert.AreEqual(OnlineTestSuiteCategories.RestrictedOrdered, suite.SuiteCategory);
        Assert.AreEqual(OnlineTestTierCategories.Restricted, suite.TierCategory);
        Assert.IsTrue(suite.RequiresExplicitSelection);
        CollectionAssert.AreEqual(
            new[]
            {
                OnlineTestStageCategories.ModerationRestricted,
                OnlineTestStageCategories.AdminRestricted
            },
            suite.OrderedStageCategories.ToArray());
        Assert.IsTrue(
            suite.Stages.All(static stage => stage.ProjectName == OnlineTestProjectTopology.Online),
            "Restricted ordered suite stages should execute only from the unified online scenario project.");
        Assert.DoesNotContain(OnlineTestSuiteCategories.RestrictedOrdered, OnlineSuiteExecutionContract.DefaultSuiteCategories);
        Assert.DoesNotContain(OnlineTestTierCategories.Restricted, OnlineSuiteExecutionContract.DefaultTierCategories);
    }

    [TestMethod]
    public void OrderedSuiteLookupRejectsUnknownSuiteCategory()
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => OnlineSuiteExecutionContract.GetOrderedSuite("Suite:UnknownOrdered"));

        Assert.Contains("Unknown ordered suite category", exception.Message);
    }

    [TestMethod]
    public void OrderedSuiteHostCompensationParsingCountsCleanFailedAndUnreconciledSignals()
    {
        var method = typeof(OrderedSuiteHost).GetMethod("BuildCompensationAggregate", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "Ordered suite host should keep the compensation parser as an explicit private host concern.");

        var aggregate = method.Invoke(null,
        [
            new[]
            {
                $"{OnlineSuiteExecutionContract.CompensationAudit} summary:",
                "  unreconciled: none",
                $"{OnlineSuiteExecutionContract.CompensationAudit} summary:",
                "  compensation[Failed]: stage=Stage:06-ThreadWrite, description=delete disposable reply, outcome=reply deleted :: simulated failure",
                "  unreconciled: stage=Stage:06-ThreadWrite, type=post, id=6060, relation=Created, description=thread-write unreconciled disposable reply asset"
            }
        ]) as OrderedSuiteCompensationAggregate;

        Assert.IsNotNull(aggregate);
        Assert.AreEqual(2, aggregate.AuditSummaryCount);
        Assert.AreEqual(1, aggregate.CleanAuditSummaryCount);
        Assert.AreEqual(1, aggregate.FailedCompensationCount);
        Assert.AreEqual(1, aggregate.UnreconciledArtifactCount);

        var summaryLine = Assert.ContainsSingle(aggregate.ToDisplayLines());
        Assert.Contains("failedCompensations=1", summaryLine);
        Assert.Contains("unreconciledArtifacts=1", summaryLine);
    }
}
