#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AioTieba4DotNet.Testing;

public enum TestLaneStageStatus
{
    Succeeded,
    Failed,
    Skipped
}

public sealed record TestLaneStagePlan(
    string Name,
    string Description,
    string Lane,
    string? TestCategoryFilter,
    bool IsCleanupWave)
{
    public bool RunsTests => !string.IsNullOrWhiteSpace(TestCategoryFilter);
}

public sealed record TestLaneExecutionPlan(
    string Lane,
    IReadOnlyList<TestLaneStagePlan> Stages,
    IReadOnlyList<string> RequestedStageFilter)
{
    public IReadOnlyList<string> ToDryRunLines()
    {
        List<string> lines = [];

        for (var index = 0; index < Stages.Count; index++)
        {
            var stage = Stages[index];
            lines.Add($"{index + 1}. {stage.Name} [{Lane}] - {stage.Description}");
            lines.Add(stage.IsCleanupWave
                ? $"    {Lane} => cleanup compensations / recorded object ledger"
                : $"    {Lane} => {stage.TestCategoryFilter}");
        }

        return lines;
    }
}

public sealed record TestLaneStageExecutionResult(
    string StageName,
    TestLaneStageStatus Status,
    string Message);

public sealed record TestLaneExecutionResult(
    TestLaneExecutionPlan Plan,
    IReadOnlyList<TestLaneStageExecutionResult> StageResults,
    Exception? Failure)
{
    public bool Succeeded => Failure is null && StageResults.All(static result => result.Status != TestLaneStageStatus.Failed);
}

public sealed class TestLaneOrchestrator
{
    public const string IntegrationLane = "integration";
    public const string LiveLane = "live";

    private readonly TestSequencingManifest _manifest;

    public TestLaneOrchestrator(TestSequencingManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        manifest.ValidateExpectedBusinessOrder();
        _manifest = manifest;
    }

    public static TestLaneOrchestrator LoadDefault()
        => new(TestSequencingManifest.LoadDefault());

    public IReadOnlyList<string> DescribeManifestDryRun()
    {
        List<string> lines = [];

        for (var index = 0; index < _manifest.Stages.Count; index++)
        {
            var stage = _manifest.Stages[index];
            var lanes = string.Join(", ", stage.Lanes);
            lines.Add($"{index + 1}. {stage.Name} [{lanes}] - {stage.Description}");

            foreach (var lane in stage.Lanes)
            {
                var normalizedLane = NormalizeLane(lane);
                lines.Add(IsCleanupStage(stage.Name)
                    ? $"    {normalizedLane} => cleanup compensations / recorded object ledger"
                    : $"    {normalizedLane} => {BuildCategoryFilter(normalizedLane, stage.Name)}");
            }
        }

        return lines;
    }

    public TestLaneExecutionPlan CreateExecutionPlan(string lane, IEnumerable<string>? stageFilter = null)
    {
        var normalizedLane = NormalizeLane(lane);
        var requestedStageFilter = NormalizeStageFilter(stageFilter);
        ValidateStageFilter(requestedStageFilter, normalizedLane);

        var requestedStageNames = requestedStageFilter.ToHashSet(StringComparer.Ordinal);
        var stages = _manifest
            .GetStagesForLane(normalizedLane)
            .Where(stage => requestedStageNames.Count == 0 || requestedStageNames.Contains(stage.Name))
            .Select(stage => new TestLaneStagePlan(
                stage.Name,
                stage.Description,
                normalizedLane,
                IsCleanupStage(stage.Name) ? null : BuildCategoryFilter(normalizedLane, stage.Name),
                IsCleanupStage(stage.Name)))
            .ToArray();

        return new TestLaneExecutionPlan(normalizedLane, stages, requestedStageFilter);
    }

    public async Task<TestLaneExecutionResult> RunAsync(string lane,
        Func<TestLaneStagePlan, CancellationToken, Task> executeStageAsync,
        TestCleanupOrchestrator? cleanupOrchestrator = null,
        IEnumerable<string>? stageFilter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executeStageAsync);

        var plan = CreateExecutionPlan(lane, stageFilter);
        List<TestLaneStageExecutionResult> stageResults = [];
        Exception? failure = null;

        foreach (var stage in plan.Stages)
        {
            if (stage.IsCleanupWave)
            {
                if (cleanupOrchestrator is null)
                {
                    stageResults.Add(new TestLaneStageExecutionResult(
                        stage.Name,
                        TestLaneStageStatus.Skipped,
                        "No cleanup orchestrator was supplied for the cleanup wave."));
                    continue;
                }

                try
                {
                    await cleanupOrchestrator.ExecuteAsync(cancellationToken);
                    stageResults.Add(new TestLaneStageExecutionResult(
                        stage.Name,
                        TestLaneStageStatus.Succeeded,
                        SummarizeCleanup(cleanupOrchestrator.GetLastExecutionReport())));
                }
                catch (Exception exception)
                {
                    stageResults.Add(new TestLaneStageExecutionResult(
                        stage.Name,
                        TestLaneStageStatus.Failed,
                        exception.Message));
                    failure = failure is null ? exception : new AggregateException(failure, exception);
                }

                continue;
            }

            if (failure is not null)
            {
                stageResults.Add(new TestLaneStageExecutionResult(
                    stage.Name,
                    TestLaneStageStatus.Skipped,
                    "Skipped because an earlier wave failed."));
                continue;
            }

            try
            {
                await executeStageAsync(stage, cancellationToken);
                stageResults.Add(new TestLaneStageExecutionResult(
                    stage.Name,
                    TestLaneStageStatus.Succeeded,
                    stage.TestCategoryFilter ?? string.Empty));
            }
            catch (Exception exception)
            {
                failure = exception;
                stageResults.Add(new TestLaneStageExecutionResult(
                    stage.Name,
                    TestLaneStageStatus.Failed,
                    exception.Message));
            }
        }

        return new TestLaneExecutionResult(plan, stageResults, failure);
    }

    private void ValidateStageFilter(IReadOnlyList<string> stageFilter, string lane)
    {
        if (stageFilter.Count == 0)
            return;

        var knownStages = _manifest.GetStageNames().ToHashSet(StringComparer.Ordinal);
        var unknownStages = stageFilter.Where(stage => !knownStages.Contains(stage)).ToArray();
        if (unknownStages.Length > 0)
        {
            throw new InvalidOperationException(
                $"Unknown stage filter(s): {string.Join(", ", unknownStages)}.");
        }

        var laneStages = _manifest.GetStagesForLane(lane)
            .Select(static stage => stage.Name)
            .ToHashSet(StringComparer.Ordinal);
        var outOfLaneStages = stageFilter.Where(stage => !laneStages.Contains(stage)).ToArray();
        if (outOfLaneStages.Length > 0)
        {
            throw new InvalidOperationException(
                $"Stage filter(s) are not available for lane '{lane}': {string.Join(", ", outOfLaneStages)}.");
        }
    }

    private static IReadOnlyList<string> NormalizeStageFilter(IEnumerable<string>? stageFilter)
    {
        if (stageFilter is null)
            return [];

        return stageFilter
            .Where(static stage => !string.IsNullOrWhiteSpace(stage))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizeLane(string lane)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lane);

        return lane.ToLowerInvariant() switch
        {
            IntegrationLane => IntegrationLane,
            LiveLane => LiveLane,
            _ => throw new ArgumentOutOfRangeException(nameof(lane), lane,
                "Lane must be 'integration' or 'live'.")
        };
    }

    private static bool IsCleanupStage(string stageName)
        => string.Equals(stageName, TestCategoryNames.Cleanup, StringComparison.Ordinal);

    private static string BuildCategoryFilter(string lane, string stageName)
    {
        var laneCategory = lane switch
        {
            IntegrationLane => TestCategoryNames.Integration,
            LiveLane => TestCategoryNames.Live,
            _ => throw new ArgumentOutOfRangeException(nameof(lane), lane,
                "Lane must be 'integration' or 'live'.")
        };

        return $"TestCategory={laneCategory}&TestCategory={stageName}";
    }

    private static string SummarizeCleanup(TestCleanupExecutionReport? report)
    {
        if (report is null)
            return "No cleanup report was recorded.";

        var failureCount = report.CompensationResults.Count(static result => result.Status == TestCleanupActionStatus.Failed);
        return $"objects={report.RecordedObjects.Count}, compensations={report.CompensationResults.Count}, failures={failureCount}";
    }
}
