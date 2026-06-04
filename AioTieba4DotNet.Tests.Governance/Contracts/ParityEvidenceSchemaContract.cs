#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using AioTieba4DotNet.Tests.Platform.Support;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[ExcludeFromCodeCoverage]
public static class ParityEvidenceSchemaContract
{
    public const string GeneratedAtUtcFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public const string MatchStatus = "match";
    public const string RequiresRemediationStatus = "requires-remediation";
    public const string UpstreamGapStatus = "upstream-gap";
    public const string BlockedByVerificationStatus = "blocked-by-verification";
    public const string IntentionalDivergenceApprovedStatus = "intentional-divergence-approved";

    private const string LedgerTableHeaderLine = "| Audit unit | Upstream anchor | .NET anchor | Transport kind | Auth prerequisites | Proof artifact | Status | Verification command | Notes |";
    private const string LedgerTableDelimiterLine = "| --- | --- | --- | --- | --- | --- | --- | --- | --- |";

    public static readonly string[] AllowedStatuses =
    [
        MatchStatus,
        RequiresRemediationStatus,
        UpstreamGapStatus,
        BlockedByVerificationStatus,
        IntentionalDivergenceApprovedStatus
    ];

    public static readonly ParityLedgerRow[] Rows = LoadRows();

    public static string GetLedgerPath()
    {
        return Path.Combine(RepositoryPaths.FindRepositoryRoot(), "docs", "related", "parity.md");
    }

    public static bool IsAllowedStatus(string status)
    {
        return AllowedStatuses.Contains(status, StringComparer.Ordinal);
    }

    public static ParityLedgerRow ValidateRow(ParityLedgerRow row, string sourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        EnsureRequiredValue("auditUnit", row.AuditUnit, sourceName);
        EnsureRequiredValue("upstreamAnchor", row.UpstreamAnchor, sourceName);
        EnsureRequiredValue("dotnetAnchor", row.DotNetAnchor, sourceName);
        EnsureRequiredValue("transportKind", row.TransportKind, sourceName);
        EnsureRequiredValue("authPrerequisites", row.AuthPrerequisites, sourceName);
        EnsureRequiredValue("proofArtifact", row.ProofArtifact, sourceName);
        EnsureRequiredValue("status", row.Status, sourceName);
        EnsureRequiredValue("verificationCommand", row.VerificationCommand, sourceName);
        EnsureRequiredValue("notes", row.Notes, sourceName);

        if (!IsAllowedStatus(row.Status))
        {
            throw new InvalidOperationException(
                $"{sourceName} must use one of the canonical parity statuses: {string.Join(", ", AllowedStatuses)}. Found '{row.Status}'.");
        }

        return row;
    }

    private static ParityLedgerRow[] LoadRows()
    {
        var ledgerPath = GetLedgerPath();
        if (!File.Exists(ledgerPath))
            throw new FileNotFoundException($"Parity ledger not found at '{ledgerPath}'.", ledgerPath);

        var lines = File.ReadAllLines(ledgerPath);
        var rows = new List<ParityLedgerRow>();

        for (var index = 0; index < lines.Length; index++)
        {
            if (!string.Equals(lines[index].Trim(), LedgerTableHeaderLine, StringComparison.Ordinal))
                continue;

            if (index + 1 >= lines.Length || !string.Equals(lines[index + 1].Trim(), LedgerTableDelimiterLine, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Parity ledger table starting at line {index + 1} must be followed by the canonical delimiter '{LedgerTableDelimiterLine}'.");
            }

            index += 2;
            while (index < lines.Length && lines[index].TrimStart().StartsWith("|", StringComparison.Ordinal))
            {
                var row = ParseRow(lines[index], index + 1);
                rows.Add(ValidateRow(row, $"parity ledger row at line {row.LineNumber}"));
                index++;
            }

            index--;
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                $"Parity ledger '{ledgerPath}' did not yield any canonical parity audit rows using the active table header.");
        }

        return [.. rows];
    }

    private static ParityLedgerRow ParseRow(string line, int lineNumber)
    {
        var columns = line.Split('|');
        if (columns.Length != 11)
        {
            throw new InvalidOperationException(
                $"Parity ledger row at line {lineNumber} must contain exactly 9 columns, but parsed {columns.Length - 2} data columns from '{line}'.");
        }

        var cells = columns
            .Skip(1)
            .Take(9)
            .Select(static cell => cell.Trim())
            .ToArray();

        for (var index = 0; index < cells.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(cells[index]))
            {
                throw new InvalidOperationException(
                    $"Parity ledger row at line {lineNumber} contains an empty value in column {index + 1}.");
            }
        }

        return new ParityLedgerRow(
            lineNumber,
            cells[0],
            cells[1],
            cells[2],
            cells[3],
            cells[4],
            cells[5],
            cells[6],
            cells[7],
            cells[8]);
    }

    private static void EnsureRequiredValue(string propertyName, string actualValue, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(actualValue))
        {
            throw new InvalidOperationException(
                $"{sourceName} must contain a non-empty '{propertyName}' value.");
        }
    }
}

[ExcludeFromCodeCoverage]
public readonly record struct ParityLedgerRow(
    int LineNumber,
    string AuditUnit,
    string UpstreamAnchor,
    string DotNetAnchor,
    string TransportKind,
    string AuthPrerequisites,
    string ProofArtifact,
    string Status,
    string VerificationCommand,
    string Notes);
