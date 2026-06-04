#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[ExcludeFromCodeCoverage]
public static class ParityArtifactRetentionContract
{
    public const string LocalVerificationManifestRelativePath = ".sisyphus/evidence/local-verification.manifest.json";
    public const string LocalVerificationManifestSchemaRelativePath = ".sisyphus/evidence/local-verification.manifest.schema.json";
    public const string LegacyTruthFreezeEvidenceRelativePath = ".sisyphus/evidence/legacy-truth-freeze.json";
    public const string LegacyEvidenceSchemaRelativePath = ".sisyphus/evidence/legacy-evidence-schema.json";
    public const string LegacyGapLedgerEvidenceRelativePath = ".sisyphus/evidence/legacy-gap-ledger.json";
    public const string ConvergenceArtifactPath = ".sisyphus/evidence/legacy-convergence.json";

    public static readonly string[] RetainedArtifactPaths =
    [
        ParityTruthFreezeContract.EvidenceRelativePath,
        ParityGapLedgerContract.EvidenceRelativePath,
        LocalVerificationManifestRelativePath,
        LocalVerificationManifestSchemaRelativePath
    ];

    public static readonly string[] LegacyParityArtifactPaths =
    [
        LegacyTruthFreezeEvidenceRelativePath,
        LegacyEvidenceSchemaRelativePath,
        ".sisyphus/evidence/legacy-wire-parity.json",
        ".sisyphus/evidence/legacy-signature-parity.json",
        ".sisyphus/evidence/legacy-state-parity.json",
        ".sisyphus/evidence/legacy-shared-seams.json",
        ".sisyphus/evidence/legacy-forums-parity.json",
        ".sisyphus/evidence/legacy-threads-parity.json",
        ".sisyphus/evidence/legacy-users-parity.json",
        ".sisyphus/evidence/legacy-admins-parity.json",
        ".sisyphus/evidence/legacy-messages-parity.json",
        ".sisyphus/evidence/legacy-client-parity.json",
        LegacyGapLedgerEvidenceRelativePath
    ];

    public static readonly string[] ExcludedArtifactPaths =
    [
        ConvergenceArtifactPath,
        ..LegacyParityArtifactPaths
    ];

    public static readonly string[] ActiveParityEvidencePaths =
    [
        ParityTruthFreezeContract.EvidenceRelativePath,
        ParityGapLedgerContract.EvidenceRelativePath
    ];
}
