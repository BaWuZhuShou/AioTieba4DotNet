#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AioTieba4DotNet.Tests.Infrastructure.Support;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[ExcludeFromCodeCoverage]
public static class PublicApiCoverageMatrixContract
{
    public const string OfflineTargetLane = "offline contract/unit";
    public const string SafeTargetLane = "safe";
    public const string RestrictedTargetLane = "restricted";

    public const string SafeOnlineBehavioralDisposition = "Safe online behavioral";
    public const string RestrictedOnlineBehavioralDisposition = "Restricted online behavioral";
    public const string OfflineContractUnitDisposition = "Offline contract/unit";
    public const string DeferredWithRationaleDisposition = "Deferred with rationale";

    private const string TableHeaderLine = "| API/member | Source file | Current coverage | Target lane | Disposition | Rationale | Required assets | Verification command | Notes |";
    private const string TableDelimiterLine = "| --- | --- | --- | --- | --- | --- | --- | --- | --- |";

    private static readonly Regex ApiClaimPattern = new(
        @"(?<lane>safe|restricted)-api\((?<category>Api:[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*)\)",
        RegexOptions.CultureInvariant);

    private static readonly Regex OnlineModuleSignaturePattern = new(
        @"^`?(?<contract>I[A-Za-z]+Module)\.(?<method>[A-Z][A-Za-z0-9]*)\(",
        RegexOptions.CultureInvariant);

    public static readonly string[] AllowedTargetLanes =
    [
        OfflineTargetLane,
        SafeTargetLane,
        RestrictedTargetLane
    ];

    public static readonly string[] AllowedDispositionValues =
    [
        SafeOnlineBehavioralDisposition,
        RestrictedOnlineBehavioralDisposition,
        OfflineContractUnitDisposition,
        DeferredWithRationaleDisposition
    ];

    public static readonly PublicApiCoverageMatrixRow[] Rows = LoadRows();

    public static readonly string[] AllowedFirstClassApiCategories = BuildAllowedFirstClassApiCategories();

    public static readonly string[] ClaimedFirstClassApiCategories = BuildClaimedFirstClassApiCategories();

    public static string GetMatrixPath()
    {
        return Path.Combine(RepositoryPaths.FindRepositoryRoot(), "docs", "related", "public-api-coverage-matrix.md");
    }

    public static bool IsAllowedTargetLane(string targetLane)
    {
        return AllowedTargetLanes.Contains(targetLane, StringComparer.Ordinal);
    }

    public static bool IsAllowedDisposition(string disposition)
    {
        return AllowedDispositionValues.Contains(disposition, StringComparer.Ordinal);
    }

    public static bool IsAllowedDispositionForLane(PublicApiCoverageMatrixRow row)
    {
        return row.TargetLane switch
        {
            OfflineTargetLane => string.Equals(row.Disposition, OfflineContractUnitDisposition, StringComparison.Ordinal),
            SafeTargetLane => string.Equals(row.Disposition, SafeOnlineBehavioralDisposition, StringComparison.Ordinal)
                || string.Equals(row.Disposition, DeferredWithRationaleDisposition, StringComparison.Ordinal),
            RestrictedTargetLane => string.Equals(row.Disposition, RestrictedOnlineBehavioralDisposition, StringComparison.Ordinal)
                || string.Equals(row.Disposition, DeferredWithRationaleDisposition, StringComparison.Ordinal),
            _ => false
        };
    }

    public static bool IsDirectOnlineFirstClassApiEligible(PublicApiCoverageMatrixRow row)
    {
        if (!row.HasDirectCoverageEvidence)
            return false;

        return row.TargetLane switch
        {
            SafeTargetLane => string.Equals(row.Disposition, SafeOnlineBehavioralDisposition, StringComparison.Ordinal),
            RestrictedTargetLane => string.Equals(row.Disposition, RestrictedOnlineBehavioralDisposition, StringComparison.Ordinal),
            _ => false
        };
    }

    public static string CreateExpectedFirstClassApiCategory(PublicApiCoverageMatrixRow row)
    {
        if (!TryCreateExpectedFirstClassApiCategory(row, out var category))
            throw new InvalidOperationException(
                $"Matrix row at line {row.LineNumber} has API/member '{row.ApiMember}' that cannot be mapped to a first-class API category.");

        return category;
    }

    public static bool TryCreateExpectedFirstClassApiCategory(PublicApiCoverageMatrixRow row, [NotNullWhen(true)] out string? category)
    {
        var signatureMatch = OnlineModuleSignaturePattern.Match(row.ApiMember);
        if (!signatureMatch.Success)
        {
            category = null;
            return false;
        }

        string? moduleName = signatureMatch.Groups["contract"].Value switch
        {
            "IClientModule" => "Client",
            "IForumModule" => "Forums",
            "IThreadModule" => "Threads",
            "IUserModule" => "Users",
            "IMessagesModule" => "Messages",
            "IAdminModule" => "Admins",
            _ => null
        };

        if (moduleName is null)
        {
            category = null;
            return false;
        }

        category = OnlineTestApiCategories.CreateFirstClassCategory(moduleName, signatureMatch.Groups["method"].Value);
        return true;
    }

    public static string[] GetClaimedFirstClassApiCategories(PublicApiCoverageMatrixRow row)
    {
        var parsedCategories = ToDistinctArray(
            ApiClaimPattern.Matches(row.VerificationCommand)
                .Cast<Match>()
                .Select(static match => match.Groups["category"].Value));

        if (!TryCreateExpectedFirstClassApiCategory(row, out var expectedCategory))
            return [];

        return ToDistinctArray(parsedCategories.Where(category => string.Equals(category, expectedCategory, StringComparison.Ordinal)));
    }

    private static PublicApiCoverageMatrixRow[] LoadRows()
    {
        var matrixPath = GetMatrixPath();
        if (!File.Exists(matrixPath))
            throw new FileNotFoundException($"Public API coverage matrix not found at '{matrixPath}'.", matrixPath);

        var lines = File.ReadAllLines(matrixPath);
        var rows = new List<PublicApiCoverageMatrixRow>();

        for (var index = 0; index < lines.Length; index++)
        {
            if (!string.Equals(lines[index].Trim(), TableHeaderLine, StringComparison.Ordinal))
                continue;

            if (index + 1 >= lines.Length || !string.Equals(lines[index + 1].Trim(), TableDelimiterLine, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Matrix table starting at line {index + 1} must be followed by the canonical delimiter '{TableDelimiterLine}'.");

            index += 2;
            while (index < lines.Length && lines[index].TrimStart().StartsWith("|", StringComparison.Ordinal))
            {
                rows.Add(ParseRow(lines[index], index + 1));
                index++;
            }

            index--;
        }

        if (rows.Count == 0)
            throw new InvalidOperationException(
                $"Matrix '{matrixPath}' did not yield any data rows using the canonical table header at line 1 or later.");

        return [.. rows];
    }

    private static PublicApiCoverageMatrixRow ParseRow(string line, int lineNumber)
    {
        var columns = line.Split('|');
        if (columns.Length != 11)
            throw new InvalidOperationException(
                $"Matrix row at line {lineNumber} must contain exactly 9 columns, but parsed {columns.Length - 2} data columns from '{line}'.");

        var cells = columns
            .Skip(1)
            .Take(9)
            .Select(static cell => cell.Trim())
            .ToArray();

        for (var index = 0; index < cells.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(cells[index]))
                throw new InvalidOperationException(
                    $"Matrix row at line {lineNumber} contains an empty value in column {index + 1}.");
        }

        return new PublicApiCoverageMatrixRow(
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

    private static string[] BuildAllowedFirstClassApiCategories()
    {
        var categories = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in Rows)
        {
            if (!IsDirectOnlineFirstClassApiEligible(row))
                continue;

            var category = CreateExpectedFirstClassApiCategory(row);
            if (seen.Add(category))
                categories.Add(category);
        }

        return [.. categories];
    }

    private static string[] BuildClaimedFirstClassApiCategories()
    {
        var categories = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in Rows)
        {
            foreach (var category in GetClaimedFirstClassApiCategories(row))
            {
                if (seen.Add(category))
                    categories.Add(category);
            }
        }

        return [.. categories];
    }

    private static string[] ToDistinctArray(IEnumerable<string> values)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var value in values)
        {
            if (seen.Add(value))
                result.Add(value);
        }

        return [.. result];
    }
}

[ExcludeFromCodeCoverage]
public readonly record struct PublicApiCoverageMatrixRow(
    int LineNumber,
    string ApiMember,
    string SourceFile,
    string CurrentCoverage,
    string TargetLane,
    string Disposition,
    string Rationale,
    string RequiredAssets,
    string VerificationCommand,
    string Notes)
{
    public bool HasDirectCoverageEvidence => CurrentCoverage.StartsWith("Direct ", StringComparison.Ordinal);
}
