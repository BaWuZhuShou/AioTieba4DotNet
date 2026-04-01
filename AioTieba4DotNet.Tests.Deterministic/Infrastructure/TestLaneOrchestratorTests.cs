#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure;

[TestClass]
public sealed class TestLaneOrchestratorTests
{
    [TestMethod]
    public void CreateExecutionPlan_IntegrationLane_UsesManifestBusinessOrder()
    {
        var orchestrator = TestLaneOrchestrator.LoadDefault();

        var plan = orchestrator.CreateExecutionPlan(TestLaneOrchestrator.IntegrationLane);

        CollectionAssert.AreEqual(
            new[]
            {
                TestCategoryNames.ForumFoundation,
                TestCategoryNames.ForumExtensions,
                TestCategoryNames.ThreadRead,
                TestCategoryNames.UserSocial,
                TestCategoryNames.MessagingClient
            },
            plan.Stages.Select(static stage => stage.Name).ToArray());
    }

    [TestMethod]
    public void CreateExecutionPlan_FilteredLiveStages_PreservesBusinessOrder_AndKeepsCleanupSynthetic()
    {
        var orchestrator = TestLaneOrchestrator.LoadDefault();

        var plan = orchestrator.CreateExecutionPlan(
            TestLaneOrchestrator.LiveLane,
            new[]
            {
                TestCategoryNames.Cleanup,
                TestCategoryNames.MessagingClient,
                TestCategoryNames.ForumExtensions
            });

        CollectionAssert.AreEqual(
            new[]
            {
                TestCategoryNames.ForumExtensions,
                TestCategoryNames.MessagingClient,
                TestCategoryNames.Cleanup
            },
            plan.Stages.Select(static stage => stage.Name).ToArray());
        Assert.IsFalse(plan.Stages[0].IsCleanupWave);
        Assert.IsTrue(plan.Stages[^1].IsCleanupWave);
        Assert.IsNull(plan.Stages[^1].TestCategoryFilter);
    }

    [TestMethod]
    public async Task RunAsync_ControlledMidWaveFailure_StillExecutesCleanupWave()
    {
        var orchestrator = TestLaneOrchestrator.LoadDefault();
        var cleanup = new TestCleanupOrchestrator();
        var cleanupExecuted = false;

        cleanup.RecordCreatedObject(TestCategoryNames.ThreadWriteModeration, "thread", 123456, "synthetic created thread");
        cleanup.Register(
            TestCategoryNames.ThreadWriteModeration,
            "delete synthetic thread",
            "delete created thread 123456",
            _ =>
            {
                cleanupExecuted = true;
                return ValueTask.CompletedTask;
            });

        var result = await orchestrator.RunAsync(
            TestLaneOrchestrator.LiveLane,
            async (stage, _) =>
            {
                await Task.Yield();
                if (stage.Name == TestCategoryNames.ThreadRead)
                    throw new InvalidOperationException("controlled mid-wave failure");
            },
            cleanup,
            new[]
            {
                TestCategoryNames.ForumExtensions,
                TestCategoryNames.ThreadRead,
                TestCategoryNames.Cleanup
            });

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(cleanupExecuted);
        CollectionAssert.AreEqual(
            new[]
            {
                TestCategoryNames.ForumExtensions,
                TestCategoryNames.ThreadRead,
                TestCategoryNames.Cleanup
            },
            result.StageResults.Select(static result => result.StageName).ToArray());
        Assert.AreEqual(TestLaneStageStatus.Succeeded, result.StageResults[0].Status);
        Assert.AreEqual(TestLaneStageStatus.Failed, result.StageResults[1].Status);
        Assert.AreEqual(TestLaneStageStatus.Succeeded, result.StageResults[2].Status);

        var report = cleanup.GetLastExecutionReport();
        Assert.IsNotNull(report);
        Assert.HasCount(1, report.RecordedObjects);
        Assert.HasCount(1, report.CompensationResults);
        Assert.AreEqual(TestCleanupActionStatus.Succeeded, report.CompensationResults[0].Status);
    }

    [TestMethod]
    public async Task CleanupOrchestrator_FailedCompensation_ContinuesRemainingActions_AndCapturesResults()
    {
        var cleanup = new TestCleanupOrchestrator();
        List<string> executionOrder = [];

        cleanup.RecordCreatedObject(TestCategoryNames.ThreadWriteModeration, "thread", 456, "synthetic created thread");
        cleanup.RecordCreatedObject(TestCategoryNames.ThreadWriteModeration, "reply", 789, "synthetic created reply");
        cleanup.Register(
            TestCategoryNames.ThreadWriteModeration,
            "delete synthetic reply",
            "delete created reply 789",
            _ =>
            {
                executionOrder.Add("reply");
                return ValueTask.FromException(new InvalidOperationException("reply cleanup failed"));
            });
        cleanup.Register(
            TestCategoryNames.ThreadWriteModeration,
            "delete synthetic thread",
            "delete created thread 456",
            _ =>
            {
                executionOrder.Add("thread");
                return ValueTask.CompletedTask;
            });

        var exception = await Assert.ThrowsExactlyAsync<AggregateException>(async () => await cleanup.ExecuteAsync());

        CollectionAssert.AreEqual(new[] { "thread", "reply" }, executionOrder.ToArray());
        Assert.IsTrue(exception.Flatten().InnerExceptions.Any(static inner =>
            inner.InnerException?.Message.Contains("reply cleanup failed", StringComparison.Ordinal) == true));

        var report = cleanup.GetLastExecutionReport();
        Assert.IsNotNull(report);
        Assert.HasCount(2, report.RecordedObjects);
        Assert.HasCount(2, report.CompensationResults);
        Assert.AreEqual(TestCleanupActionStatus.Succeeded, report.CompensationResults[0].Status);
        Assert.AreEqual(TestCleanupActionStatus.Failed, report.CompensationResults[1].Status);
    }
}
