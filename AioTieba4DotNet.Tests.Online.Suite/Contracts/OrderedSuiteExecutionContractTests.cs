#nullable enable
using System.Linq;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Suite.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class OrderedSuiteExecutionContractTests
{
    [TestMethod]
    public void SafeOrderedExecutionPlan_MapsStages01Through06ToSafeScenarioProject()
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
            suite.Stages.All(static stage => stage.ProjectName == OnlineTestProjectTopology.Safe),
            "Safe ordered suite stages should execute only from the safe scenario project.");
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
    public void RestrictedOrderedExecutionPlan_RemainsExplicitAndRestrictedOnly()
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
            suite.Stages.All(static stage => stage.ProjectName == OnlineTestProjectTopology.Restricted),
            "Restricted ordered suite stages should execute only from the restricted scenario project.");
        Assert.DoesNotContain(OnlineTestSuiteCategories.RestrictedOrdered, OnlineSuiteExecutionContract.DefaultSuiteCategories);
        Assert.DoesNotContain(OnlineTestTierCategories.Restricted, OnlineSuiteExecutionContract.DefaultTierCategories);
    }
}
