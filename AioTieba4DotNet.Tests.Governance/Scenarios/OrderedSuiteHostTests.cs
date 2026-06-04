#nullable enable
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Governance.Contracts;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Governance.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Scenarios;

[TestClass]
[DoNotParallelize]
public sealed class OrderedSuiteHostTests
{
    private readonly TestContext _testContext;

    public OrderedSuiteHostTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    [TestMethod]
    [TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
    [TestCategory(OnlineTestTierCategories.Safe)]
    public Task SafeOrderedSuiteRunsStages01Through06InExactOrder()
    {
        var host = new OrderedSuiteHost(_testContext);
        return host.ExecuteAsync(OnlineSuiteExecutionContract.SafeOrderedSuite, _testContext.CancellationToken);
    }

    [TestMethod]
    [TestCategory(OnlineTestSuiteCategories.RestrictedOrdered)]
    [TestCategory(OnlineTestTierCategories.Restricted)]
    public Task RestrictedOrderedSuiteRunsRestrictedStagesOnlyWhenExplicitlySelected()
    {
        var host = new OrderedSuiteHost(_testContext);
        return host.ExecuteAsync(OnlineSuiteExecutionContract.RestrictedOrderedSuite, _testContext.CancellationToken);
    }
}
