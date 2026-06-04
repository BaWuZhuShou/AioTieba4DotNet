#nullable enable
using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
public sealed class OnlineCleanupFailureContractTests
{
    [TestMethod]
    [TestCategory(OnlineTestContractCategories.CleanupFailure)]
    public async Task CompensationLifecycleSurfacesUnreconciledArtifactsWhenCompensationFails()
    {
        var probe = new CompensationAuditProbe();

        probe.ExecuteSafe("cleanup failure contract probe", scope =>
        {
            var createdArtifact = scope.Compensation.RecordCreatedArtifact(
                OnlineTestStageCategories.ThreadWrite,
                "thread",
                4040L,
                "unreconciled contract thread artifact");
            scope.Compensation.Register(
                createdArtifact,
                "delete unreconciled contract thread artifact",
                "thread deleted",
                _ => ValueTask.FromException(new InvalidOperationException(
                    "Simulated compensation failure for the cleanup failure contract.")));
        });

        var exception = await Assert.ThrowsExactlyAsync<OnlineCompensationAuditException>(
            async () => await probe.DisposeAsync());

        var audit = probe.PublishedAudit;
        Assert.IsNotNull(audit);
        Assert.AreSame(audit, exception.Audit);
        Assert.IsFalse(audit.Succeeded, "A failed compensation action should publish a failed CompensationAudit.");
        Assert.HasCount(1, audit.RecordedArtifacts);
        Assert.HasCount(1, audit.CompensationResults);
        Assert.HasCount(1, audit.UnreconciledArtifacts);
        Assert.AreEqual(OnlineCompensationActionStatus.Failed, audit.CompensationResults[0].Status);

        var auditDisplay = string.Join(Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains("compensation[Failed]", auditDisplay);
        Assert.Contains("unreconciled:", auditDisplay);
        Assert.Contains("detected 1 unreconciled artifact(s)", exception.Message);
        var compensationFailure = Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        Assert.IsNotNull(compensationFailure.InnerException);
        Assert.Contains("Simulated compensation failure", compensationFailure.InnerException.Message);
    }

    [TestMethod]
    [TestCategory(OnlineTestContractCategories.CleanupFailure)]
    [TestCategory(OnlineTestContractCategories.ThreadWriteCleanupFailure)]
    public async Task CompensationLifecycleSurfacesThreadWriteDisposableReplyWhenReplyCleanupFails()
    {
        var probe = new CompensationAuditProbe();

        probe.ExecuteSafe("thread-write cleanup failure contract probe", scope =>
        {
            var disposableReplyArtifact = scope.Compensation.RecordCreatedArtifact(
                OnlineTestStageCategories.ThreadWrite,
                "post",
                6060L,
                "thread-write unreconciled disposable reply asset");
            scope.Compensation.Register(
                disposableReplyArtifact,
                "delete thread-write unreconciled disposable reply asset",
                "reply deleted",
                _ => ValueTask.FromException(new InvalidOperationException(
                    "Simulated cleanup failure for the thread-write disposable reply asset.")));
        });

        var exception = await Assert.ThrowsExactlyAsync<OnlineCompensationAuditException>(
            async () => await probe.DisposeAsync());

        var audit = probe.PublishedAudit;
        Assert.IsNotNull(audit);
        Assert.AreSame(audit, exception.Audit);
        Assert.IsFalse(audit.Succeeded, "A failed thread-write cleanup action should publish a failed CompensationAudit.");
        Assert.HasCount(1, audit.RecordedArtifacts);
        Assert.HasCount(1, audit.CompensationResults);
        Assert.HasCount(1, audit.UnreconciledArtifacts);
        Assert.AreEqual(OnlineCompensationActionStatus.Failed, audit.CompensationResults[0].Status);
        Assert.AreEqual(OnlineTestStageCategories.ThreadWrite, audit.CompensationResults[0].StageCategory);

        var auditDisplay = string.Join(Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains("compensation[Failed]", auditDisplay);
        Assert.Contains("thread-write unreconciled disposable reply asset", auditDisplay);
        Assert.Contains("detected 1 unreconciled artifact(s)", exception.Message);
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
