#nullable enable
using System.Linq;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Suite.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.RestrictedIsolation)]
public sealed class RestrictedIsolationContractTests
{
    [TestMethod]
    public void DefaultSafeSelection_ExcludesRestrictedTierAndSuiteCategories()
    {
        CollectionAssert.AreEqual(new[] { OnlineTestTierCategories.Safe }, OnlineSuiteExecutionContract.DefaultTierCategories);
        CollectionAssert.AreEqual(new[] { OnlineTestSuiteCategories.SafeOrdered }, OnlineSuiteExecutionContract.DefaultSuiteCategories);

        Assert.DoesNotContain(OnlineTestTierCategories.Restricted, OnlineSuiteExecutionContract.DefaultTierCategories);
        Assert.DoesNotContain(OnlineTestSuiteCategories.RestrictedOrdered, OnlineSuiteExecutionContract.DefaultSuiteCategories);
    }

    [TestMethod]
    public void DefaultSafeSelection_ExcludesRestrictedFeaturesAndStagesFromTheRunnableBaseline()
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
