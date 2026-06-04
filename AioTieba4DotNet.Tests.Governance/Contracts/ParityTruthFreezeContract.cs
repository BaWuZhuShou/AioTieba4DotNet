#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AioTieba4DotNet.Tests.Platform.Support;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[ExcludeFromCodeCoverage]
public static class ParityTruthFreezeContract
{
    public const string EvidenceRelativePath = ".sisyphus/evidence/parity-truth-freeze.json";
    public const string RepositoryLocalSnapshotRelativePath = "aiotieba/";
    public const string CanonicalUpstreamRepositoryId = "lumina37/aiotieba";
    public const string CanonicalUpstreamRepositoryUrl = "https://github.com/lumina37/aiotieba";
    public const string CanonicalPreferredTag = "v4.6.4";
    public const string CanonicalUpstreamSha = "04f8e431f87507a6228b42061c70d298b34317ff";
    public const string CanonicalComparisonSource = "https://github.com/lumina37/aiotieba/tree/04f8e431f87507a6228b42061c70d298b34317ff";
    public const string CanonicalSourcePathPolicy = "Authoritative parity truth is the frozen lumina37/aiotieba tuple above. Treat repository-local aiotieba/ as reference-only unless explicit snapshot metadata matches repo id, canonical repo URL, preferred tag, and upstream SHA exactly; missing, mixed, or stale metadata must fail closed.";
    public const string GeneratedAtUtcFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public static string GetEvidencePath()
    {
        return Path.Combine(RepositoryPaths.FindRepositoryRoot(), EvidenceRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public static ParityTruthFreezeEvidence LoadEvidence()
    {
        var evidencePath = GetEvidencePath();
        if (!File.Exists(evidencePath))
            throw new FileNotFoundException($"Parity truth-freeze evidence not found at '{evidencePath}'.", evidencePath);

        using var document = JsonDocument.Parse(File.ReadAllText(evidencePath));
        return ValidateEvidence(ParseEvidence(document.RootElement, evidencePath));
    }

    public static ParityTruthFreezeEvidence ValidateEvidence(ParityTruthFreezeEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        ValidateFrozenTruthReference(
            evidence.UpstreamSha,
            evidence.ComparisonSource,
            evidence.SourcePathPolicy,
            $"truth-freeze evidence '{EvidenceRelativePath}'");
        EnsureExactMatch("repoId", evidence.RepoId, CanonicalUpstreamRepositoryId, $"truth-freeze evidence '{EvidenceRelativePath}'");
        EnsureExactMatch("canonicalRepoUrl", evidence.CanonicalRepoUrl, CanonicalUpstreamRepositoryUrl, $"truth-freeze evidence '{EvidenceRelativePath}'");
        EnsureExactMatch("preferredTag", evidence.PreferredTag, CanonicalPreferredTag, $"truth-freeze evidence '{EvidenceRelativePath}'");

        if (!DateTimeOffset.TryParseExact(
                evidence.GeneratedAtUtc,
                GeneratedAtUtcFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _))
        {
            throw new InvalidOperationException(
                $"truth-freeze evidence '{EvidenceRelativePath}' must use generatedAtUtc in '{GeneratedAtUtcFormat}' format.");
        }

        return evidence;
    }

    public static void ValidateFrozenTruthReference(
        string upstreamSha,
        string comparisonSource,
        string sourcePathPolicy,
        string sourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        EnsureExactMatch("upstreamSha", upstreamSha, CanonicalUpstreamSha, sourceName);
        EnsureExactMatch("comparisonSource", comparisonSource, CanonicalComparisonSource, sourceName);
        EnsureExactMatch("sourcePathPolicy", sourcePathPolicy, CanonicalSourcePathPolicy, sourceName);
    }

    public static ParityTruthFreezeLocalSnapshotMetadata ValidateTrustedLocalSnapshot(
        ParityTruthFreezeLocalSnapshotMetadata? metadata)
    {
        if (metadata is null)
        {
            throw new InvalidOperationException(
                $"Repository-local {RepositoryLocalSnapshotRelativePath} cannot be trusted as parity truth without explicit snapshot metadata. Missing metadata must fail closed.");
        }

        EnsureExactMatch("snapshotPath", metadata.SnapshotPath, RepositoryLocalSnapshotRelativePath, "repository-local aiotieba snapshot metadata");
        EnsureExactMatch("repoId", metadata.RepoId, CanonicalUpstreamRepositoryId, "repository-local aiotieba snapshot metadata");
        EnsureExactMatch("canonicalRepoUrl", metadata.CanonicalRepoUrl, CanonicalUpstreamRepositoryUrl, "repository-local aiotieba snapshot metadata");
        EnsureExactMatch("preferredTag", metadata.PreferredTag, CanonicalPreferredTag, "repository-local aiotieba snapshot metadata");
        EnsureExactMatch("upstreamSha", metadata.UpstreamSha, CanonicalUpstreamSha, "repository-local aiotieba snapshot metadata");
        return metadata;
    }

    private static ParityTruthFreezeEvidence ParseEvidence(JsonElement root, string sourceName)
    {
        return new ParityTruthFreezeEvidence(
            GetRequiredString(root, "repoId", sourceName),
            GetRequiredString(root, "canonicalRepoUrl", sourceName),
            GetRequiredString(root, "preferredTag", sourceName),
            GetRequiredString(root, "upstreamSha", sourceName),
            GetRequiredString(root, "comparisonSource", sourceName),
            GetRequiredString(root, "sourcePathPolicy", sourceName),
            GetRequiredString(root, "generatedAtUtc", sourceName));
    }

    private static string GetRequiredString(JsonElement root, string propertyName, string sourceName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
            throw new InvalidOperationException($"{sourceName} must contain a non-empty '{propertyName}' string.");

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"{sourceName} must store '{propertyName}' as a string, but found {property.ValueKind}.");
        }

        var value = property.GetString()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{sourceName} must contain a non-empty '{propertyName}' string.");

        return value;
    }

    private static void EnsureExactMatch(string propertyName, string actualValue, string expectedValue, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(actualValue))
        {
            throw new InvalidOperationException(
                $"{sourceName} must contain a non-empty '{propertyName}' string.");
        }

        if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{sourceName} must pin {propertyName} to '{expectedValue}', but found '{actualValue}'. Mixed or stale truth-source metadata must fail closed.");
        }
    }
}

[ExcludeFromCodeCoverage]
public sealed record ParityTruthFreezeEvidence(
    string RepoId,
    string CanonicalRepoUrl,
    string PreferredTag,
    string UpstreamSha,
    string ComparisonSource,
    string SourcePathPolicy,
    string GeneratedAtUtc);

[ExcludeFromCodeCoverage]
public sealed record ParityTruthFreezeLocalSnapshotMetadata(
    string SnapshotPath,
    string RepoId,
    string CanonicalRepoUrl,
    string PreferredTag,
    string UpstreamSha);
