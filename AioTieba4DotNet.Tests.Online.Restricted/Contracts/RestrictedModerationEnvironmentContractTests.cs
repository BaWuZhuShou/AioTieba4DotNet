#nullable enable
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Restricted.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Environment)]
[TestCategory(OnlineTestFeatureCategories.Moderation)]
[TestCategory(OnlineTestTierCategories.Restricted)]
[TestCategory(OnlineTestStageCategories.ModerationRestricted)]
[TestCategory(OnlineTestCapabilityCategories.Moderation)]
public sealed class RestrictedModerationEnvironmentContractTests
    : OnlineRestrictedExecutionTestBase
{
    [TestMethod]
    public void DefaultLocalEnvironmentStopsBeforeRestrictedModerationMutationAttempt()
    {
        var mutationAttempted = false;

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => ExecuteRestricted(
                "restricted moderation environment contract probe",
                OnlineExecutionCapability.Moderation,
                _ => mutationAttempted = true));

        Assert.IsFalse(mutationAttempted, "Restricted moderation contract must deny execution before any mutation delegate runs.");
        Assert.Contains("explicit opt-in", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedOptIn, exception.Message);
    }
}
