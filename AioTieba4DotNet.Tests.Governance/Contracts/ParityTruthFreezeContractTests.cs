#nullable enable
using System;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
[TestCategory(OnlineTestParityCategories.TruthFreeze)]
public sealed class ParityTruthFreezeContractTests
{
    [TestMethod]
    public void FrozenTruthSourceEvidenceMatchesCanonicalUpstreamTuple()
    {
        var evidence = ParityTruthFreezeContract.LoadEvidence();

        Assert.AreEqual(ParityTruthFreezeContract.EvidenceRelativePath, ".sisyphus/evidence/parity-truth-freeze.json");
        Assert.AreEqual(ParityTruthFreezeContract.CanonicalUpstreamRepositoryId, evidence.RepoId);
        Assert.AreEqual(ParityTruthFreezeContract.CanonicalUpstreamRepositoryUrl, evidence.CanonicalRepoUrl);
        Assert.AreEqual(ParityTruthFreezeContract.CanonicalPreferredTag, evidence.PreferredTag);
        Assert.AreEqual(ParityTruthFreezeContract.CanonicalUpstreamSha, evidence.UpstreamSha);
        Assert.AreEqual(ParityTruthFreezeContract.CanonicalComparisonSource, evidence.ComparisonSource);
        Assert.AreEqual(ParityTruthFreezeContract.CanonicalSourcePathPolicy, evidence.SourcePathPolicy);
        Assert.IsNotNull(evidence.GeneratedAtUtc);
    }

    [TestMethod]
    public void RepositoryLocalSnapshotRequiresExactCanonicalMetadataBeforeTrust()
    {
        var metadata = new ParityTruthFreezeLocalSnapshotMetadata(
            ParityTruthFreezeContract.RepositoryLocalSnapshotRelativePath,
            ParityTruthFreezeContract.CanonicalUpstreamRepositoryId,
            ParityTruthFreezeContract.CanonicalUpstreamRepositoryUrl,
            ParityTruthFreezeContract.CanonicalPreferredTag,
            ParityTruthFreezeContract.CanonicalUpstreamSha);

        var validatedMetadata = ParityTruthFreezeContract.ValidateTrustedLocalSnapshot(metadata);

        Assert.AreEqual(ParityTruthFreezeContract.RepositoryLocalSnapshotRelativePath, validatedMetadata.SnapshotPath);
        Assert.AreEqual(ParityTruthFreezeContract.CanonicalUpstreamSha, validatedMetadata.UpstreamSha);
    }

    [TestMethod]
    public void TruthFreezeRejectsMixedOrMissingSourceMetadata()
    {
        var missingComparisonSource = new ParityTruthFreezeEvidence(
            ParityTruthFreezeContract.CanonicalUpstreamRepositoryId,
            ParityTruthFreezeContract.CanonicalUpstreamRepositoryUrl,
            ParityTruthFreezeContract.CanonicalPreferredTag,
            ParityTruthFreezeContract.CanonicalUpstreamSha,
            string.Empty,
            ParityTruthFreezeContract.CanonicalSourcePathPolicy,
            "2026-04-05T00:00:00Z");
        var missingComparisonSourceException = Assert.ThrowsExactly<InvalidOperationException>(
            () => ParityTruthFreezeContract.ValidateEvidence(missingComparisonSource));
        Assert.Contains("comparisonSource", missingComparisonSourceException.Message);

        var missingLocalMetadataException = Assert.ThrowsExactly<InvalidOperationException>(
            () => ParityTruthFreezeContract.ValidateTrustedLocalSnapshot(null));
        Assert.Contains("Missing metadata must fail closed", missingLocalMetadataException.Message);
        Assert.Contains(ParityTruthFreezeContract.RepositoryLocalSnapshotRelativePath, missingLocalMetadataException.Message);

        var staleLocalMetadata = new ParityTruthFreezeLocalSnapshotMetadata(
            ParityTruthFreezeContract.RepositoryLocalSnapshotRelativePath,
            ParityTruthFreezeContract.CanonicalUpstreamRepositoryId,
            ParityTruthFreezeContract.CanonicalUpstreamRepositoryUrl,
            ParityTruthFreezeContract.CanonicalPreferredTag,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var staleLocalMetadataException = Assert.ThrowsExactly<InvalidOperationException>(
            () => ParityTruthFreezeContract.ValidateTrustedLocalSnapshot(staleLocalMetadata));
        Assert.Contains("Mixed or stale truth-source metadata must fail closed", staleLocalMetadataException.Message);
        Assert.Contains(ParityTruthFreezeContract.CanonicalUpstreamSha, staleLocalMetadataException.Message);
    }
}
