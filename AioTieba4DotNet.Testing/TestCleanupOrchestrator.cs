#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AioTieba4DotNet.Testing;

public enum TestCleanupObjectRelation
{
    Created,
    MutationTarget,
    OwnedFixture
}

public enum TestCleanupActionStatus
{
    Succeeded,
    Failed
}

public sealed record TestCleanupObjectRecord(
    string StageName,
    string ObjectType,
    string ObjectId,
    TestCleanupObjectRelation Relation,
    string Description);

public sealed record TestCleanupActionResult(
    string StageName,
    string Description,
    string CompensationOutcome,
    TestCleanupActionStatus Status,
    string? ErrorMessage);

public sealed record TestCleanupExecutionReport(
    IReadOnlyList<TestCleanupObjectRecord> RecordedObjects,
    IReadOnlyList<TestCleanupActionResult> CompensationResults)
{
    public bool Succeeded => CompensationResults.All(static result => result.Status != TestCleanupActionStatus.Failed);

    public IReadOnlyList<string> ToDisplayLines()
    {
        if (RecordedObjects.Count == 0 && CompensationResults.Count == 0)
            return [];

        List<string> lines = ["cleanup summary:"];
        lines.AddRange(RecordedObjects.Select(static record =>
            $"  object[{record.Relation}]: stage={record.StageName}, type={record.ObjectType}, id={record.ObjectId}, description={record.Description}"));
        lines.AddRange(CompensationResults.Select(static result =>
        {
            var suffix = string.IsNullOrWhiteSpace(result.ErrorMessage) ? string.Empty : $" :: {result.ErrorMessage}";
            return $"  compensation[{result.Status}]: stage={result.StageName}, description={result.Description}, outcome={result.CompensationOutcome}{suffix}";
        }));

        return lines;
    }
}

public sealed class TestCleanupOrchestrator : IAsyncDisposable
{
    private readonly Stack<RegisteredCleanupAction> _cleanupActions = [];
    private readonly List<TestCleanupObjectRecord> _recordedObjects = [];
    private bool _executed;
    private TestCleanupExecutionReport? _lastExecutionReport;

    public void Register(string description, Func<CancellationToken, ValueTask> cleanupAction)
        => Register(TestCategoryNames.Cleanup, description, description, cleanupAction);

    public void Register(string stageName, string description, string compensationOutcome,
        Func<CancellationToken, ValueTask> cleanupAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stageName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(compensationOutcome);
        ArgumentNullException.ThrowIfNull(cleanupAction);

        _cleanupActions.Push(new RegisteredCleanupAction(stageName, description, compensationOutcome, cleanupAction));
    }

    public IReadOnlyList<string> DescribePendingActions()
        => _cleanupActions
            .Select(static action => $"{action.StageName}: {action.Description} => {action.CompensationOutcome}")
            .ToArray();

    public void RecordCreatedObject(string stageName, string objectType, long objectId, string description)
        => RecordCreatedObject(stageName, objectType, objectId.ToString(CultureInfo.InvariantCulture), description);

    public void RecordCreatedObject(string stageName, string objectType, string objectId, string description)
        => RecordObject(stageName, objectType, objectId, TestCleanupObjectRelation.Created, description);

    public void RecordObject(string stageName, string objectType, string objectId, TestCleanupObjectRelation relation,
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stageName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectType);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        _recordedObjects.Add(new TestCleanupObjectRecord(stageName, objectType, objectId, relation, description));
    }

    public TestCleanupExecutionReport? GetLastExecutionReport()
        => _lastExecutionReport;

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_executed) return;

        _executed = true;
        List<TestCleanupActionResult> compensationResults = [];
        List<Exception> failures = [];

        while (_cleanupActions.Count > 0)
        {
            var action = _cleanupActions.Pop();

            try
            {
                await action.CleanupAction(cancellationToken);
                compensationResults.Add(new TestCleanupActionResult(
                    action.StageName,
                    action.Description,
                    action.CompensationOutcome,
                    TestCleanupActionStatus.Succeeded,
                    null));
            }
            catch (Exception exception)
            {
                compensationResults.Add(new TestCleanupActionResult(
                    action.StageName,
                    action.Description,
                    action.CompensationOutcome,
                    TestCleanupActionStatus.Failed,
                    exception.Message));

                failures.Add(new InvalidOperationException(
                    $"Cleanup compensation '{action.Description}' for stage '{action.StageName}' failed.",
                    exception));
            }
        }

        _lastExecutionReport = new TestCleanupExecutionReport(
            _recordedObjects.ToArray(),
            compensationResults.ToArray());

        if (failures.Count > 0)
        {
            throw new AggregateException(
                "One or more cleanup compensations failed after all registered cleanup work was attempted.",
                failures);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ExecuteAsync();
        GC.SuppressFinalize(this);
    }

    private sealed record RegisteredCleanupAction(
        string StageName,
        string Description,
        string CompensationOutcome,
        Func<CancellationToken, ValueTask> CleanupAction);
}
