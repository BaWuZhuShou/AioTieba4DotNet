#nullable enable
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
[TestCategory(OnlineTestParityCategories.State)]
public sealed class StateParityContractTests
{
    [TestMethod]
    public async Task SessionParityCapturesRollbackAndMutationOrder()
    {
        var rows = (await RetainedSessionStateParitySupport.CaptureCanonicalRowsAsync()).ToArray();

        Assert.AreEqual(10, rows.Length, "Expected the retained session parity slice to keep six success/fallback rows plus four rollback rows.");

        foreach (var row in rows)
            Assert.IsTrue(row.Comparison.Match, row.Comparison.ToFailureMessage(row.AuditUnit));

        AssertMutationOrder(rows, "session.tbs.load",
        [
            "loader.observed: state.tbsState Pending -> Initializing",
            "after: state.tbsState Initializing -> Ready",
            "after: state.tbs <null> -> loaded-tbs",
            "after: account.tbs <null> -> loaded-tbs"
        ]);
        AssertMutationOrder(rows, "session.zid.init",
        [
            "executor.observed: state.zIdState Pending -> Initializing",
            "after-apply: state.zIdState Initializing -> Ready",
            "after-apply: state.zId <null> -> zid-001",
            "after-apply: account.zId <null> -> zid-001"
        ]);
        AssertMutationOrder(rows, "session.client-sync",
        [
            "executor.observed: state.clientState Pending -> Initializing",
            "after-apply: state.clientState Initializing -> Ready",
            "after-apply: state.clientId <null> -> client-001",
            "after-apply: state.sampleId <null> -> sample-001",
            "after-apply: account.clientId <null> -> client-001",
            "after-apply: account.sampleId <null> -> sample-001"
        ]);
        AssertMutationOrder(rows, "dispatcher.websocket-fallback.session-mutation",
        [
            "ws.connect.observed: state.webSocketState Pending -> Initializing",
            "http.execute.observed: state.webSocketState Initializing -> Pending",
            "after-apply: state.clientState Pending -> Ready",
            "after-apply: state.clientId <null> -> fallback-client-id",
            "after-apply: state.sampleId <null> -> fallback-sample-id",
            "after-apply: account.clientId <null> -> fallback-client-id",
            "after-apply: account.sampleId <null> -> fallback-sample-id"
        ]);

        var fallbackRow = rows.Single(static row => row.AuditUnit == "dispatcher.websocket-fallback.session-mutation");
        CollectionAssert.AreEqual(
            new[] { "ws.connect", "http.execute", "apply-mutation" },
            fallbackRow.Comparison.Actual.TransportPath.ToArray(),
            "Expected websocket fallback to restore websocket state before the HTTP path mutates the session.");

        Assert.IsTrue(rows.Count(static row => row.Comparison.Actual.RollbackRestoredPriorState) >= 4,
            "Expected the retained state parity slice to include rollback rows for TBS, ZId, client sync, and websocket warmup.");
    }

    [TestMethod]
    public async Task SessionParityRestoresPriorStateOnFailure()
    {
        var rollbackRows = (await RetainedSessionStateParitySupport.CaptureCanonicalRowsAsync())
            .Where(static row => row.AuditUnit.EndsWith(".rollback", System.StringComparison.Ordinal))
            .ToArray();

        Assert.AreEqual(4, rollbackRows.Length,
            "Expected rollback coverage for TBS refresh, ZId initialization, client sync, and websocket warmup.");

        foreach (var row in rollbackRows)
        {
            Assert.IsTrue(row.Comparison.Match, row.Comparison.ToFailureMessage(row.AuditUnit));
            Assert.IsTrue(row.Comparison.Actual.RollbackRestoredPriorState,
                $"Expected '{row.AuditUnit}' to restore the prior session state exactly.");
            Assert.IsTrue(row.Comparison.Actual.RollbackRestoredPriorAccount,
                $"Expected '{row.AuditUnit}' to restore the prior account values exactly.");
            Assert.AreEqual(row.Comparison.Actual.BeforeState, row.Comparison.Actual.AfterState,
                $"Expected '{row.AuditUnit}' to leave the session state identical before and after the failure.");
            Assert.AreEqual(row.Comparison.Actual.BeforeAccount, row.Comparison.Actual.AfterAccount,
                $"Expected '{row.AuditUnit}' to leave the mirrored account values identical before and after the failure.");
        }

        AssertMutationOrder(rollbackRows, "session.tbs.refresh.rollback",
        [
            "loader.observed: state.tbsState Ready -> Initializing",
            "after: state.tbsState Initializing -> Ready"
        ]);
        AssertMutationOrder(rollbackRows, "session.zid.init.rollback",
        [
            "executor.observed: state.zIdState Ready -> Initializing",
            "after: state.zIdState Initializing -> Ready"
        ]);
        AssertMutationOrder(rollbackRows, "session.client-sync.rollback",
        [
            "executor.observed: state.clientState Ready -> Initializing",
            "after: state.clientState Initializing -> Ready"
        ]);
        AssertMutationOrder(rollbackRows, "session.websocket.warmup.rollback",
        [
            "connect.observed: state.webSocketState Pending -> Initializing",
            "after: state.webSocketState Initializing -> Pending"
        ]);
    }

    private static void AssertMutationOrder(SessionStateParityRow[] rows, string auditUnit, string[] expected)
    {
        var row = rows.Single(item => item.AuditUnit == auditUnit);
        CollectionAssert.AreEqual(expected, row.Comparison.Actual.MutationOrder.ToArray(),
            $"Expected '{auditUnit}' to keep the frozen mutation order.");
    }
}
