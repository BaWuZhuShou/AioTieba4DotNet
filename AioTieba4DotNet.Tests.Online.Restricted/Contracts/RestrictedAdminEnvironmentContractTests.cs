#nullable enable
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Restricted.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Environment)]
[TestCategory(OnlineTestFeatureCategories.Admin)]
[TestCategory(OnlineTestTierCategories.Restricted)]
[TestCategory(OnlineTestStageCategories.AdminRestricted)]
[TestCategory(OnlineTestCapabilityCategories.Admin)]
public sealed class RestrictedAdminEnvironmentContractTests
    : OnlineRestrictedExecutionTestBase
{
    [TestMethod]
    public void DefaultLocalEnvironmentStopsBeforeRestrictedAdminMutationAttempt()
    {
        var mutationAttempted = false;

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => ExecuteRestricted(
                "restricted admin environment contract probe",
                OnlineExecutionCapability.Admin,
                _ => mutationAttempted = true));

        Assert.IsFalse(mutationAttempted, "Restricted admin contract must deny execution before any mutation delegate runs.");
        Assert.Contains("explicit opt-in", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedOptIn, exception.Message);
    }
}
