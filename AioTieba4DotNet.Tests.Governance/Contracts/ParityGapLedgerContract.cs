#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using AioTieba4DotNet.Tests.Platform.Support;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[ExcludeFromCodeCoverage]
public static class ParityGapLedgerContract
{
    public const string EvidenceRelativePath = ".sisyphus/evidence/parity-gap-ledger.json";
    public const string ArtifactKind = "gap-ledger";
    public const string VerificationCommand =
        "dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter \"TestCategory=Parity:Gaps\"";

    public static readonly GapLedgerExpectation[] ExpectedRows =
    [
        new(
            "public-api.forums.sign-forum",
            "`IForumModule.SignAsync(string fname, CancellationToken cancellationToken = default)`",
            "aiotieba.api.sign_forum",
            "`IForumModule.SignAsync(string fname)`",
            "http-app-form",
            "safe auth + signable forum",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "safe-one-way-daily-action",
            "medium",
            "Provision a disposable sign fixture or approve a one-way daily-action policy with explicit account rotation.",
            "Single-forum sign remains implemented, but the current safe lane has no truthful undo path for a daily action."),
        new(
            "public-api.forums.sign-forums",
            "`IForumModule.SignForumsAsync(CancellationToken cancellationToken = default)`",
            "aiotieba.api.sign_forums",
            "`IForumModule.SignForumsAsync()`",
            "http-web-form",
            "safe auth + follow set",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "safe-one-way-daily-action",
            "medium",
            "Provision a disposable batch-sign fixture or approve a one-way daily-action policy with explicit account rotation.",
            "Batch sign remains implemented, but the current safe lane has no truthful compensation model for the daily action."),
        new(
            "public-api.forums.sign-growth",
            "`IForumModule.SignGrowthAsync(CancellationToken cancellationToken = default)`",
            "aiotieba.api.sign_growth",
            "`IForumModule.SignGrowthAsync()`",
            "http-web-form",
            "safe auth",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "safe-one-way-daily-action",
            "medium",
            "Provision a disposable growth-task fixture or approve a one-way daily-action policy with explicit account rotation.",
            "Growth sign remains implemented, but the current safe lane has no truthful compensation model for the daily action."),
        new(
            "public-api.threads.move",
            "`IThreadModule.MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0, CancellationToken cancellationToken = default)`",
            "aiotieba.api.move",
            "`IThreadModule.MoveAsync(string fname, long tid, int toTabId, int fromTabId = 0)`",
            "http-app-form",
            "restricted moderation forum + thread + alternate tab ids",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "restricted-live-proof-no-alternate-tab",
            "medium",
            "Rerun the restricted move scenario only after the dedicated moderation forum exposes a truthful reversible tab target different from the thread's current tab.",
            "Move remains implemented, but the current moderation forum exposes no alternate tab and the dedicated thread already starts in tab `0`, so bounded direct coverage has no truthful reversible target."),
        new(
            "public-api.users.remove-fan",
            "`IUserModule.RemoveFanAsync(long userId, CancellationToken cancellationToken = default)`",
            "aiotieba.api.remove_fan",
            "`IUserModule.RemoveFanAsync(long userId)`",
            "http-app-form",
            "safe auth + dedicated reciprocal fan fixture",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "safe-no-reciprocal-fan-fixture",
            "medium",
            "Add a disposable reciprocal fan fixture or approve a one-way social-mutation policy before claiming direct coverage.",
            "Fan removal remains implemented, but the current safe lane has no public re-add path or dedicated reciprocal fixture model."),
        new(
            "public-api.users.set-nickname",
            "`IUserModule.SetNicknameAsync(string nickName, CancellationToken cancellationToken = default)`",
            "aiotieba.api.set_nickname_old",
            "`IUserModule.SetNicknameAsync(string nickName)`",
            "http-web-form",
            "safe auth + disposable profile fixture",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "safe-no-profile-restore-contract",
            "medium",
            "Define a restoreable profile fixture and safe nickname-change budget before promoting direct coverage.",
            "Nickname writes remain implemented, but the current safe lane has no approved restore contract for uniqueness or cooldown-sensitive profile state."),
        new(
            "public-api.users.set-profile",
            "`IUserModule.SetProfileAsync(string nickName, string sign, Gender gender, CancellationToken cancellationToken = default)`",
            "aiotieba.api.set_profile",
            "`IUserModule.SetProfileAsync(string nickName, string sign, Gender gender)`",
            "http-web-form",
            "safe auth + disposable profile fixture",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "safe-no-profile-restore-contract",
            "medium",
            "Define a restoreable profile fixture and safe profile-change budget before promoting direct coverage.",
            "Profile writes remain implemented, but the current safe lane has no approved restore contract for cooldown or review-sensitive profile state."),
        new(
            "public-api.messages.send-chatroom-message",
            "`IMessagesModule.SendChatroomMessageAsync(long chatroomId, ulong forumId, string text, IReadOnlyList<long>? atUserIds = null, int robotCode = -1, CancellationToken cancellationToken = default)`",
            "aiotieba.api.send_chatroom_msg",
            "`IMessagesModule.SendChatroomMessageAsync(long chatroomId, ulong forumId, string text, IReadOnlyList<long>? atUserIds = null, int robotCode = -1)`",
            "blcp-chatroom-send",
            "messaging capability + dedicated chatroom id + forum id",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "safe-no-chatroom-compensation-model",
            "medium",
            "Approve a dedicated chatroom fixture with reversible acknowledgement rules before promoting direct coverage.",
            "Chatroom send remains implemented and parity-frozen offline, but the current safe lane has no approved compensation or acknowledgement model for outward group-message mutations."),
        new(
            "public-api.messages.set-message-read",
            "`IMessagesModule.SetMessageReadAsync(WsMessage message, CancellationToken cancellationToken = default)`",
            "aiotieba.api.set_msg_readed",
            "`IMessagesModule.SetMessageReadAsync(WsMessage message)`",
            "websocket-private-group-state-mutation",
            "messaging capability + dedicated unread message fixture",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "safe-no-unread-reset-path",
            "medium",
            "Add a fixture with a truthful unread reset path before promoting direct coverage.",
            "Read-state mutation remains implemented and parity-frozen offline, but the current safe lane has no public unread reset path for reversible inbox-state proof."),
        new(
            "public-api.admins.add-bawu",
            "`IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType, CancellationToken cancellationToken = default)`",
            "aiotieba.api.add_bawu",
            "`IAdminModule.AddBawuAsync(string fname, string userName, BawuType bawuType)`",
            "http-web-form",
            "restricted admin capability + forum + target user",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "restricted-no-runnable-direct-scenario",
            "medium",
            "Add a restricted fixture with a truthful reversible bawu roster target before promoting direct coverage.",
            "Bawu assignment remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family."),
        new(
            "public-api.admins.del-bawu",
            "`IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType, CancellationToken cancellationToken = default)`",
            "aiotieba.api.del_bawu",
            "`IAdminModule.DelBawuAsync(string fname, string portrait, BawuType bawuType)`",
            "http-web-form",
            "restricted admin capability + forum + target portrait",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "restricted-no-runnable-direct-scenario",
            "medium",
            "Add a restricted fixture with a truthful reversible bawu roster target before promoting direct coverage.",
            "Bawu removal remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family."),
        new(
            "public-api.admins.add-bawu-blacklist",
            "`IAdminModule.AddBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default)`",
            "aiotieba.api.add_bawu_blacklist",
            "`IAdminModule.AddBawuBlacklistAsync(string fname, long userId)`",
            "http-web-form",
            "restricted admin capability + forum + target user id",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "restricted-no-runnable-direct-scenario",
            "medium",
            "Add a restricted fixture with a truthful reversible bawu-blacklist target before promoting direct coverage.",
            "Bawu-blacklist add remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family."),
        new(
            "public-api.admins.del-bawu-blacklist",
            "`IAdminModule.DelBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default)`",
            "aiotieba.api.del_bawu_blacklist",
            "`IAdminModule.DelBawuBlacklistAsync(string fname, long userId)`",
            "http-web-form",
            "restricted admin capability + forum + target user id",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "restricted-no-runnable-direct-scenario",
            "medium",
            "Add a restricted fixture with a truthful reversible bawu-blacklist target before promoting direct coverage.",
            "Bawu-blacklist remove remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family."),
        new(
            "public-api.admins.set-bawu-perm",
            "`IAdminModule.SetBawuPermAsync(string fname, string portrait, BawuPermType permissions, CancellationToken cancellationToken = default)`",
            "aiotieba.api.set_bawu_perm",
            "`IAdminModule.SetBawuPermAsync(string fname, string portrait, BawuPermType permissions)`",
            "http-web-form",
            "restricted admin capability + forum + target portrait",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "restricted-no-runnable-direct-scenario",
            "medium",
            "Add a restricted fixture with a truthful reversible permission target before promoting direct coverage.",
            "Bawu permission writes remain implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family."),
        new(
            "public-api.admins.handle-unblock-appeals",
            "`IAdminModule.HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false, CancellationToken cancellationToken = default)`",
            "aiotieba.api.handle_unblock_appeals",
            "`IAdminModule.HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false)`",
            "http-web-form",
            "restricted admin capability + forum + appeal fixtures",
            ParityEvidenceSchemaContract.BlockedByVerificationStatus,
            "restricted-no-runnable-direct-scenario",
            "medium",
            "Add a restricted fixture with disposable appeals before promoting direct coverage.",
            "Appeal handling remains implemented and parity-frozen offline, but the current restricted suite has no approved direct reversible scenario for this write family."),
    ];

    public static readonly GapLedgerArtifact Artifact = LoadArtifact();

    public static string GetEvidencePath()
    {
        return Path.Combine(RepositoryPaths.FindRepositoryRoot(), EvidenceRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public static void ValidateMatrixAlignment()
    {
        var expectedApiMembers = ExpectedRows
            .Select(static row => row.MatrixApiMember)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        var actualApiMembers = PublicApiCoverageMatrixContract.Rows
            .Where(static row => string.Equals(row.Disposition, PublicApiCoverageMatrixContract.DeferredWithRationaleDisposition, StringComparison.Ordinal))
            .Select(static row => row.ApiMember)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        EnsureExactSequence(
            actualApiMembers,
            expectedApiMembers,
            "Every matrix row with deferred disposition must map to one canonical retained gap-ledger audit unit, with no hidden extras.");
    }

    public static void ValidateMarkdownLedger()
    {
        var rows = ParityEvidenceSchemaContract.Rows
            .Where(row => string.Equals(NormalizeMarkdownCell(row.ProofArtifact), EvidenceRelativePath, StringComparison.Ordinal))
            .OrderBy(static row => row.AuditUnit, StringComparer.Ordinal)
            .ToArray();

        EnsureEquivalentSet(
            rows.Select(row => NormalizeMarkdownCell(row.AuditUnit)).ToArray(),
            ExpectedRows.Select(static row => row.AuditUnit).OrderBy(static value => value, StringComparer.Ordinal).ToArray(),
            "Retained markdown ledger rows must enumerate every expected unresolved audit unit exactly once.");

        foreach (var expected in ExpectedRows)
        {
            var row = rows.Single(candidate => string.Equals(NormalizeMarkdownCell(candidate.AuditUnit), expected.AuditUnit, StringComparison.Ordinal));

            AssertExactMatch(NormalizeMarkdownCell(row.UpstreamAnchor), NormalizeMarkdownCell(expected.UpstreamAnchor), expected.AuditUnit, "upstreamAnchor");
            AssertExactMatch(NormalizeMarkdownCell(row.DotNetAnchor), NormalizeMarkdownCell(expected.DotNetAnchor), expected.AuditUnit, "dotNetAnchor");
            AssertExactMatch(NormalizeMarkdownCell(row.TransportKind), NormalizeMarkdownCell(expected.TransportKind), expected.AuditUnit, "transportKind");
            AssertExactMatch(NormalizeMarkdownCell(row.AuthPrerequisites), NormalizeMarkdownCell(expected.AuthPrerequisites), expected.AuditUnit, "authPrerequisites");
            AssertExactMatch(NormalizeMarkdownCell(row.ProofArtifact), EvidenceRelativePath, expected.AuditUnit, "proofArtifact");
            AssertExactMatch(NormalizeMarkdownCell(row.Status), NormalizeMarkdownCell(expected.Status), expected.AuditUnit, "status");
            AssertExactMatch(NormalizeMarkdownCell(row.VerificationCommand), VerificationCommand, expected.AuditUnit, "verificationCommand");
            AssertExactMatch(row.Notes, expected.Notes, expected.AuditUnit, "notes");
        }
    }

    public static void ValidateArtifact()
    {
        ValidateArtifact(Artifact);
    }

    public static void ValidateArtifact(GapLedgerArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (artifact.SchemaVersion != 1)
            throw new InvalidOperationException($"Gap ledger artifact '{EvidenceRelativePath}' must pin schemaVersion to 1.");

        if (!string.Equals(artifact.ArtifactKind, ArtifactKind, StringComparison.Ordinal))
            throw new InvalidOperationException($"Gap ledger artifact '{EvidenceRelativePath}' must use artifactKind '{ArtifactKind}'.");

        if (!DateTimeOffset.TryParseExact(
                artifact.GeneratedAtUtc,
                ParityEvidenceSchemaContract.GeneratedAtUtcFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _))
        {
            throw new InvalidOperationException(
                $"Gap ledger artifact '{EvidenceRelativePath}' must use generatedAtUtc in '{ParityEvidenceSchemaContract.GeneratedAtUtcFormat}' format.");
        }

        ParityTruthFreezeContract.ValidateEvidence(artifact.FrozenTruthSource);

        if (!string.Equals(artifact.ProofArtifact, EvidenceRelativePath, StringComparison.Ordinal))
            throw new InvalidOperationException($"Gap ledger artifact '{EvidenceRelativePath}' must point proofArtifact back to itself.");

        if (!string.Equals(artifact.VerificationCommand, VerificationCommand, StringComparison.Ordinal))
            throw new InvalidOperationException($"Gap ledger artifact '{EvidenceRelativePath}' must advertise the canonical Parity:Gaps verification command.");

        var rows = artifact.Rows
            .OrderBy(static row => row.AuditUnit, StringComparer.Ordinal)
            .ToArray();

        EnsureEquivalentSet(
            rows.Select(static row => row.AuditUnit).ToArray(),
            ExpectedRows.Select(static row => row.AuditUnit).OrderBy(static value => value, StringComparer.Ordinal).ToArray(),
            "Gap ledger artifact must enumerate every expected unresolved audit unit exactly once.");

        foreach (var expected in ExpectedRows)
        {
            var row = rows.Single(candidate => string.Equals(candidate.AuditUnit, expected.AuditUnit, StringComparison.Ordinal));

            AssertRequired(row.Blocker, expected.AuditUnit, "blocker");
            AssertRequired(row.RiskLevel, expected.AuditUnit, "riskLevel");
            AssertRequired(row.NextAction, expected.AuditUnit, "nextAction");

            AssertExactMatch(row.UpstreamAnchor, NormalizeMarkdownCell(expected.UpstreamAnchor), expected.AuditUnit, "upstreamAnchor");
            AssertExactMatch(row.DotNetAnchor, NormalizeMarkdownCell(expected.DotNetAnchor), expected.AuditUnit, "dotNetAnchor");
            AssertExactMatch(row.TransportKind, NormalizeMarkdownCell(expected.TransportKind), expected.AuditUnit, "transportKind");
            AssertExactMatch(row.AuthPrerequisites, NormalizeMarkdownCell(expected.AuthPrerequisites), expected.AuditUnit, "authPrerequisites");
            AssertExactMatch(row.ProofArtifact, EvidenceRelativePath, expected.AuditUnit, "proofArtifact");
            AssertExactMatch(row.Status, NormalizeMarkdownCell(expected.Status), expected.AuditUnit, "status");
            AssertExactMatch(row.VerificationCommand, VerificationCommand, expected.AuditUnit, "verificationCommand");
            AssertExactMatch(row.Blocker, expected.Blocker, expected.AuditUnit, "blocker");
            AssertExactMatch(row.RiskLevel, expected.RiskLevel, expected.AuditUnit, "riskLevel");
            AssertExactMatch(row.NextAction, expected.NextAction, expected.AuditUnit, "nextAction");
            AssertExactMatch(row.Notes, expected.Notes, expected.AuditUnit, "notes");
        }
    }

    public static void ValidateGapCoverage(IReadOnlyCollection<string> markdownAuditUnits, IReadOnlyCollection<string> artifactAuditUnits)
    {
        ArgumentNullException.ThrowIfNull(markdownAuditUnits);
        ArgumentNullException.ThrowIfNull(artifactAuditUnits);

        var expectedAuditUnits = ExpectedRows
            .Select(static row => row.AuditUnit)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        EnsureExactSequence(
            markdownAuditUnits.OrderBy(static value => value, StringComparer.Ordinal).ToArray(),
            expectedAuditUnits,
            "Markdown gap rows must not hide unresolved units in prose or omit canonical rows.");
        EnsureExactSequence(
            artifactAuditUnits.OrderBy(static value => value, StringComparer.Ordinal).ToArray(),
            expectedAuditUnits,
            "Gap artifact rows must not hide unresolved units in prose or omit machine-readable rows.");
    }

    private static GapLedgerArtifact LoadArtifact()
    {
        var evidencePath = GetEvidencePath();
        if (!File.Exists(evidencePath))
            throw new FileNotFoundException($"Gap ledger artifact not found at '{evidencePath}'.", evidencePath);

        using var document = JsonDocument.Parse(File.ReadAllText(evidencePath));
        var root = document.RootElement;

        return new GapLedgerArtifact(
            GetRequiredInt32(root, "schemaVersion", evidencePath),
            GetRequiredString(root, "artifactKind", evidencePath),
            GetRequiredString(root, "generatedAtUtc", evidencePath),
            new ParityTruthFreezeEvidence(
                GetRequiredString(root.GetProperty("frozenTruthSource"), "repoId", evidencePath),
                GetRequiredString(root.GetProperty("frozenTruthSource"), "canonicalRepoUrl", evidencePath),
                GetRequiredString(root.GetProperty("frozenTruthSource"), "preferredTag", evidencePath),
                GetRequiredString(root.GetProperty("frozenTruthSource"), "upstreamSha", evidencePath),
                GetRequiredString(root.GetProperty("frozenTruthSource"), "comparisonSource", evidencePath),
                GetRequiredString(root.GetProperty("frozenTruthSource"), "sourcePathPolicy", evidencePath),
                GetRequiredString(root.GetProperty("frozenTruthSource"), "generatedAtUtc", evidencePath)),
            GetRequiredString(root, "proofArtifact", evidencePath),
            GetRequiredString(root, "verificationCommand", evidencePath),
            root.GetProperty("rows")
                .EnumerateArray()
                .Select(element => new GapLedgerArtifactRow(
                    GetRequiredString(element, "auditUnit", evidencePath),
                    GetRequiredString(element, "upstreamAnchor", evidencePath),
                    GetRequiredString(element, "dotNetAnchor", evidencePath),
                    GetRequiredString(element, "transportKind", evidencePath),
                    GetRequiredString(element, "authPrerequisites", evidencePath),
                    GetRequiredString(element, "proofArtifact", evidencePath),
                    GetRequiredString(element, "status", evidencePath),
                    GetRequiredString(element, "verificationCommand", evidencePath),
                    GetRequiredString(element, "blocker", evidencePath),
                    GetRequiredString(element, "riskLevel", evidencePath),
                    GetRequiredString(element, "nextAction", evidencePath),
                    GetRequiredString(element, "notes", evidencePath)))
                .ToArray());
    }

    private static void AssertRequired(string value, string auditUnit, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Gap row '{auditUnit}' must provide a non-empty '{fieldName}' value.");
    }

    private static void AssertExactMatch(string actual, string expected, string auditUnit, string fieldName)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Gap row '{auditUnit}' must keep '{fieldName}' exactly aligned. Expected '{expected}', actual '{actual}'.");
        }
    }

    private static string NormalizeMarkdownCell(string value)
    {
        if (value.Length >= 2 && value.StartsWith('`') && value.EndsWith('`'))
            return value[1..^1];

        return value;
    }

    private static void EnsureExactSequence(IReadOnlyList<string> actual, IReadOnlyList<string> expected, string message)
    {
        if (actual.Count != expected.Count || !actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{message}{Environment.NewLine}Expected: {string.Join(", ", expected)}{Environment.NewLine}Actual: {string.Join(", ", actual)}");
        }
    }

    private static void EnsureEquivalentSet(IReadOnlyList<string> actual, IReadOnlyList<string> expected, string message)
    {
        var actualOrdered = actual.OrderBy(static value => value, StringComparer.Ordinal).ToArray();
        var expectedOrdered = expected.OrderBy(static value => value, StringComparer.Ordinal).ToArray();
        EnsureExactSequence(actualOrdered, expectedOrdered, message);
    }

    private static string GetRequiredString(JsonElement element, string propertyName, string sourceName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"{sourceName} must provide string property '{propertyName}'.");

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{sourceName} property '{propertyName}' must not be empty.");

        return value;
    }

    private static int GetRequiredInt32(JsonElement element, string propertyName, string sourceName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
            throw new InvalidOperationException($"{sourceName} must provide integer property '{propertyName}'.");

        return value;
    }
}

[ExcludeFromCodeCoverage]
public sealed record GapLedgerExpectation(
    string AuditUnit,
    string MatrixApiMember,
    string UpstreamAnchor,
    string DotNetAnchor,
    string TransportKind,
    string AuthPrerequisites,
    string Status,
    string Blocker,
    string RiskLevel,
    string NextAction,
    string Notes);

[ExcludeFromCodeCoverage]
public sealed record GapLedgerArtifact(
    int SchemaVersion,
    string ArtifactKind,
    string GeneratedAtUtc,
    ParityTruthFreezeEvidence FrozenTruthSource,
    string ProofArtifact,
    string VerificationCommand,
    IReadOnlyList<GapLedgerArtifactRow> Rows);

[ExcludeFromCodeCoverage]
public sealed record GapLedgerArtifactRow(
    string AuditUnit,
    string UpstreamAnchor,
    string DotNetAnchor,
    string TransportKind,
    string AuthPrerequisites,
    string ProofArtifact,
    string Status,
    string VerificationCommand,
    string Blocker,
    string RiskLevel,
    string NextAction,
    string Notes);
