#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AioTieba4DotNet.Tests.Platform.Execution;

public enum OnlineCompensationArtifactRelation
{
    Created,
    Mutated,
    OwnedFixture
}

public enum OnlineCompensationActionStatus
{
    Succeeded,
    Failed
}

public sealed record OnlineCompensationArtifact(
    string ArtifactKey,
    string StageCategory,
    string ArtifactType,
    string ArtifactId,
    OnlineCompensationArtifactRelation Relation,
    string Description);

public sealed record OnlineCompensationArtifactRegistration(
    string ArtifactKey,
    string StageCategory,
    string ArtifactType,
    string ArtifactId,
    OnlineCompensationArtifactRelation Relation,
    string Description);

public sealed record OnlineCompensationActionResult(
    string StageCategory,
    string Description,
    string CompensationOutcome,
    string? ArtifactKey,
    OnlineCompensationActionStatus Status,
    string? ErrorMessage);

public sealed record OnlineCompensationAudit(
    string AuditCategory,
    IReadOnlyList<OnlineCompensationArtifact> RecordedArtifacts,
    IReadOnlyList<OnlineCompensationActionResult> CompensationResults)
{
    public IReadOnlyList<OnlineCompensationArtifact> UnreconciledArtifacts =>
        RecordedArtifacts
            .Where(artifact => !GetSuccessfulArtifactKeys().Contains(artifact.ArtifactKey))
            .ToArray();

    public bool Succeeded =>
        UnreconciledArtifacts.Count == 0
        && CompensationResults.All(static result => result.Status != OnlineCompensationActionStatus.Failed);

    public IReadOnlyList<string> ToDisplayLines()
    {
        List<string> lines = [$"{AuditCategory} summary:"];

        if (RecordedArtifacts.Count == 0 && CompensationResults.Count == 0)
        {
            lines.Add("  no recorded artifacts or compensation actions.");
            return lines;
        }

        lines.AddRange(RecordedArtifacts.Select(static artifact =>
            $"  artifact[{artifact.Relation}]: stage={artifact.StageCategory}, type={artifact.ArtifactType}, id={artifact.ArtifactId}, description={artifact.Description}"));
        lines.AddRange(CompensationResults.Select(static result =>
        {
            var artifactSuffix = string.IsNullOrWhiteSpace(result.ArtifactKey)
                ? string.Empty
                : $", artifact={result.ArtifactKey}";
            var errorSuffix = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? string.Empty
                : $" :: {result.ErrorMessage}";

            return
                $"  compensation[{result.Status}]: stage={result.StageCategory}, description={result.Description}, outcome={result.CompensationOutcome}{artifactSuffix}{errorSuffix}";
        }));

        if (UnreconciledArtifacts.Count == 0)
        {
            lines.Add("  unreconciled: none");
            return lines;
        }

        lines.AddRange(UnreconciledArtifacts.Select(static artifact =>
            $"  unreconciled: stage={artifact.StageCategory}, type={artifact.ArtifactType}, id={artifact.ArtifactId}, relation={artifact.Relation}, description={artifact.Description}"));
        return lines;
    }

    private HashSet<string> GetSuccessfulArtifactKeys()
    {
        return CompensationResults
            .Where(static result => result.Status == OnlineCompensationActionStatus.Succeeded)
            .Select(static result => result.ArtifactKey)
            .Where(static artifactKey => !string.IsNullOrWhiteSpace(artifactKey))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}

public sealed class OnlineCompensationAuditException : InvalidOperationException
{
    public OnlineCompensationAuditException(OnlineCompensationAudit audit, Exception? innerException)
        : base(BuildMessage(audit), innerException)
    {
        Audit = audit;
    }

    public OnlineCompensationAudit Audit { get; }

    private static string BuildMessage(OnlineCompensationAudit audit)
    {
        ArgumentNullException.ThrowIfNull(audit);

        return string.Join(Environment.NewLine,
            new[]
            {
                $"{OnlineExecutionContracts.CompensationAudit} detected {audit.UnreconciledArtifacts.Count} unreconciled artifact(s)."
            }.Concat(audit.ToDisplayLines()));
    }
}

public sealed class OnlineTestCompensationScope : IAsyncDisposable
{
    private readonly Stack<RegisteredCompensationAction> _registeredCompensations = [];
    private readonly List<OnlineCompensationArtifact> _recordedArtifacts = [];
    private int _artifactSequence;
    private bool _executed;
    private OnlineCompensationAudit? _lastAudit;

    public OnlineCompensationArtifactRegistration RecordCreatedArtifact(
        string stageCategory,
        string artifactType,
        long artifactId,
        string description)
    {
        return RecordCreatedArtifact(stageCategory, artifactType,
            artifactId.ToString(CultureInfo.InvariantCulture), description);
    }

    public OnlineCompensationArtifactRegistration RecordCreatedArtifact(
        string stageCategory,
        string artifactType,
        string artifactId,
        string description)
    {
        return RecordArtifact(stageCategory, artifactType, artifactId, OnlineCompensationArtifactRelation.Created,
            description);
    }

    public OnlineCompensationArtifactRegistration RecordMutatedArtifact(
        string stageCategory,
        string artifactType,
        long artifactId,
        string description)
    {
        return RecordMutatedArtifact(stageCategory, artifactType,
            artifactId.ToString(CultureInfo.InvariantCulture), description);
    }

    public OnlineCompensationArtifactRegistration RecordMutatedArtifact(
        string stageCategory,
        string artifactType,
        string artifactId,
        string description)
    {
        return RecordArtifact(stageCategory, artifactType, artifactId, OnlineCompensationArtifactRelation.Mutated,
            description);
    }

    public OnlineCompensationArtifactRegistration RecordOwnedFixture(
        string stageCategory,
        string artifactType,
        long artifactId,
        string description)
    {
        return RecordOwnedFixture(stageCategory, artifactType,
            artifactId.ToString(CultureInfo.InvariantCulture), description);
    }

    public OnlineCompensationArtifactRegistration RecordOwnedFixture(
        string stageCategory,
        string artifactType,
        string artifactId,
        string description)
    {
        return RecordArtifact(stageCategory, artifactType, artifactId, OnlineCompensationArtifactRelation.OwnedFixture,
            description);
    }

    public void Register(
        string stageCategory,
        string description,
        string compensationOutcome,
        Func<CancellationToken, ValueTask> compensationAction)
    {
        RegisterCore(null, stageCategory, description, compensationOutcome, compensationAction);
    }

    public void Register(
        OnlineCompensationArtifactRegistration artifact,
        string description,
        string compensationOutcome,
        Func<CancellationToken, ValueTask> compensationAction)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        RegisterCore(artifact.ArtifactKey, artifact.StageCategory, description, compensationOutcome,
            compensationAction);
    }

    public OnlineCompensationAudit? GetLastAudit()
    {
        return _lastAudit;
    }

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_executed)
            return;

        _executed = true;
        List<OnlineCompensationActionResult> compensationResults = [];
        List<Exception> failures = [];

        while (_registeredCompensations.Count > 0)
        {
            var action = _registeredCompensations.Pop();

            try
            {
                await action.CompensationAction(cancellationToken);
                compensationResults.Add(new OnlineCompensationActionResult(
                    action.StageCategory,
                    action.Description,
                    action.CompensationOutcome,
                    action.ArtifactKey,
                    OnlineCompensationActionStatus.Succeeded,
                    null));
            }
            catch (Exception exception)
            {
                compensationResults.Add(new OnlineCompensationActionResult(
                    action.StageCategory,
                    action.Description,
                    action.CompensationOutcome,
                    action.ArtifactKey,
                    OnlineCompensationActionStatus.Failed,
                    exception.Message));

                failures.Add(new InvalidOperationException(
                    $"Compensation '{action.Description}' for stage '{action.StageCategory}' failed.",
                    exception));
            }
        }

        _lastAudit = new OnlineCompensationAudit(
            OnlineExecutionContracts.CompensationAudit,
            _recordedArtifacts.ToArray(),
            compensationResults.ToArray());

        if (_lastAudit.Succeeded)
            return;

        Exception? innerException = failures.Count switch
        {
            0 => null,
            1 => failures[0],
            _ => new AggregateException(
                "One or more compensation actions failed after all registered cleanup work was attempted.",
                failures)
        };
        throw new OnlineCompensationAuditException(_lastAudit, innerException);
    }

    public async ValueTask DisposeAsync()
    {
        await ExecuteAsync();
        GC.SuppressFinalize(this);
    }

    private OnlineCompensationArtifactRegistration RecordArtifact(
        string stageCategory,
        string artifactType,
        string artifactId,
        OnlineCompensationArtifactRelation relation,
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stageCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactType);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var artifactKey = CreateArtifactKey(stageCategory, artifactType, artifactId);
        var registration = new OnlineCompensationArtifactRegistration(
            artifactKey,
            stageCategory,
            artifactType,
            artifactId,
            relation,
            description);
        _recordedArtifacts.Add(new OnlineCompensationArtifact(
            registration.ArtifactKey,
            registration.StageCategory,
            registration.ArtifactType,
            registration.ArtifactId,
            registration.Relation,
            registration.Description));

        return registration;
    }

    private void RegisterCore(
        string? artifactKey,
        string stageCategory,
        string description,
        string compensationOutcome,
        Func<CancellationToken, ValueTask> compensationAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stageCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(compensationOutcome);
        ArgumentNullException.ThrowIfNull(compensationAction);

        _registeredCompensations.Push(new RegisteredCompensationAction(
            artifactKey,
            stageCategory,
            description,
            compensationOutcome,
            compensationAction));
    }

    private string CreateArtifactKey(string stageCategory, string artifactType, string artifactId)
    {
        var sequence = Interlocked.Increment(ref _artifactSequence);
        return string.Create(CultureInfo.InvariantCulture, $"{stageCategory}|{artifactType}|{artifactId}|{sequence}");
    }

    private sealed record RegisteredCompensationAction(
        string? ArtifactKey,
        string StageCategory,
        string Description,
        string CompensationOutcome,
        Func<CancellationToken, ValueTask> CompensationAction);
}
