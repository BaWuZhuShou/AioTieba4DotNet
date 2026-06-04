#nullable enable
using System.Linq;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.RestrictedIsolation)]
public sealed class RestrictedIsolationContractTests
{
    [TestMethod]
    public void DefaultSafeSelectionExcludesRestrictedTierAndSuiteCategories()
    {
        CollectionAssert.AreEqual(new[] { OnlineTestTierCategories.Safe }, OnlineSuiteExecutionContract.DefaultTierCategories);
        CollectionAssert.AreEqual(new[] { OnlineTestSuiteCategories.SafeOrdered }, OnlineSuiteExecutionContract.DefaultSuiteCategories);

        Assert.DoesNotContain(OnlineTestTierCategories.Restricted, OnlineSuiteExecutionContract.DefaultTierCategories);
        Assert.DoesNotContain(OnlineTestSuiteCategories.RestrictedOrdered, OnlineSuiteExecutionContract.DefaultSuiteCategories);
    }

    [TestMethod]
    public void DefaultSafeSelectionExcludesRestrictedFeaturesAndStagesFromTheRunnableBaseline()
    {
        var defaultFeatures = OnlineSuiteExecutionContract.FeatureMatrix
            .Where(static entry => entry.TierCategory == OnlineTestTierCategories.Safe)
            .Select(static entry => entry.FeatureCategory)
            .ToArray();

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
            defaultFeatures);

        Assert.DoesNotContain(OnlineTestFeatureCategories.Moderation, defaultFeatures);
        Assert.DoesNotContain(OnlineTestFeatureCategories.Admin, defaultFeatures);
        Assert.DoesNotContain(OnlineTestStageCategories.ModerationRestricted, OnlineSuiteExecutionContract.SafeOrderedStageCategories);
        Assert.DoesNotContain(OnlineTestStageCategories.AdminRestricted, OnlineSuiteExecutionContract.SafeOrderedStageCategories);
    }
}
