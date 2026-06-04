#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
public sealed class OnlineCleanupContractTests
{
    [TestMethod]
    [TestCategory(OnlineTestContractCategories.Cleanup)]
    public async Task CompensationLifecycleRegistersCreatedAndMutatedArtifactsAndPublishesCompensationAudit()
    {
        var executedCompensations = new List<string>();
        var probe = new CompensationAuditProbe();

        probe.ExecuteSafe("cleanup contract success probe", scope =>
        {
            var createdArtifact = scope.Compensation.RecordCreatedArtifact(
                OnlineTestStageCategories.ThreadWrite,
                "thread",
                1001L,
                "temporary owned thread for compensation contract coverage");
            var mutatedArtifact = scope.Compensation.RecordMutatedArtifact(
                OnlineTestStageCategories.UserSocial,
                "forum-follow",
                "forum:csharp",
                "temporary follow relation for compensation contract coverage");

            scope.Compensation.Register(
                createdArtifact,
                "delete temporary owned thread",
                "thread deleted",
                _ =>
                {
                    executedCompensations.Add("thread");
                    return ValueTask.CompletedTask;
                });
            scope.Compensation.Register(
                mutatedArtifact,
                "revert temporary follow relation",
                "forum unfollowed",
                _ =>
                {
                    executedCompensations.Add("follow");
                    return ValueTask.CompletedTask;
                });
        });

        await probe.DisposeAsync();

        var audit = probe.PublishedAudit;
        Assert.IsNotNull(audit);
        Assert.AreEqual(OnlineSuiteExecutionContract.CompensationAudit, audit.AuditCategory);
        Assert.IsTrue(audit.Succeeded, "Successful compensation work should produce a passing CompensationAudit.");
        Assert.HasCount(2, audit.RecordedArtifacts);
        Assert.HasCount(2, audit.CompensationResults);
        Assert.IsEmpty(audit.UnreconciledArtifacts);
        CollectionAssert.AreEquivalent(new[] { "thread", "follow" }, executedCompensations);
        Assert.IsTrue(
            audit.CompensationResults.All(static result => result.Status == OnlineCompensationActionStatus.Succeeded),
            "All registered compensation actions should be reported as succeeded.");

        var auditDisplay = string.Join(Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains(OnlineSuiteExecutionContract.CompensationAudit, auditDisplay);
        Assert.Contains("unreconciled: none", auditDisplay);
    }

    [TestMethod]
    [TestCategory(OnlineTestContractCategories.Cleanup)]
    public async Task CompensationLifecycleRemainsPerTestScopedWithoutSharedLedgerState()
    {
        var firstProbe = new CompensationAuditProbe();
        firstProbe.ExecuteSafe("first compensation scope probe", scope =>
        {
            var createdArtifact = scope.Compensation.RecordCreatedArtifact(
                OnlineTestStageCategories.ThreadWrite,
                "thread",
                2002L,
                "first per-test compensation artifact");
            scope.Compensation.Register(
                createdArtifact,
                "delete first per-test artifact",
                "thread deleted",
                _ => ValueTask.CompletedTask);
        });

        await firstProbe.DisposeAsync();

        var secondProbe = new CompensationAuditProbe();
        await secondProbe.DisposeAsync();

        var firstAudit = firstProbe.PublishedAudit;
        var secondAudit = secondProbe.PublishedAudit;

        Assert.IsNotNull(firstAudit);
        Assert.IsNotNull(secondAudit);
        Assert.HasCount(1, firstAudit.RecordedArtifacts);
        Assert.HasCount(1, firstAudit.CompensationResults);
        Assert.HasCount(0, secondAudit.RecordedArtifacts);
        Assert.HasCount(0, secondAudit.CompensationResults);
        Assert.IsEmpty(secondAudit.UnreconciledArtifacts);
        Assert.AreEqual(OnlineSuiteExecutionContract.CompensationAudit, secondAudit.AuditCategory);
    }

    private sealed class CompensationAuditProbe : OnlineSafeExecutionTestBase
    {
        public OnlineCompensationAudit? PublishedAudit => LastCompensationAudit;

        public new void ExecuteSafe(
            string operationName,
            Action<OnlineExecutionScope> action,
            OnlineExecutionCapability capability = OnlineExecutionCapability.None)
        {
            base.ExecuteSafe(operationName, action, capability);
        }
    }
}
