#nullable enable
using System.IO;
using System.Linq;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class ParityArtifactRetentionContractTests
{
    [TestMethod]
    public void ActiveParityArtifactSurfaceRequiresOnlyFinalRetainedArtifacts()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                ".sisyphus/evidence/parity-truth-freeze.json",
                ".sisyphus/evidence/parity-gap-ledger.json",
                ".sisyphus/evidence/local-verification.manifest.json",
                ".sisyphus/evidence/local-verification.manifest.schema.json"
            },
            ParityArtifactRetentionContract.RetainedArtifactPaths);
        CollectionAssert.AreEqual(
            new[]
            {
                ".sisyphus/evidence/parity-truth-freeze.json",
                ".sisyphus/evidence/parity-gap-ledger.json"
            },
            ParityArtifactRetentionContract.ActiveParityEvidencePaths);
    }

    [TestMethod]
    public void RetiredParityArtifactsRemainOutOfActiveValidationSurface()
    {
        CollectionAssert.Contains(ParityArtifactRetentionContract.ExcludedArtifactPaths, ParityArtifactRetentionContract.ConvergenceArtifactPath);

        foreach (var retiredPath in ParityArtifactRetentionContract.ExcludedArtifactPaths)
        {
            CollectionAssert.DoesNotContain(ParityArtifactRetentionContract.RetainedArtifactPaths, retiredPath);
            CollectionAssert.DoesNotContain(ParityArtifactRetentionContract.ActiveParityEvidencePaths, retiredPath);
        }

        CollectionAssert.AreEquivalent(
            ParityArtifactRetentionContract.LegacyParityArtifactPaths,
            ParityArtifactRetentionContract.ExcludedArtifactPaths
                .Except([ParityArtifactRetentionContract.ConvergenceArtifactPath])
                .ToArray());
    }

    [TestMethod]
    public void FinalRetainedArtifactsExistWithoutRequiringRetiredParityArtifacts()
    {
        foreach (var relativePath in ParityArtifactRetentionContract.RetainedArtifactPaths)
        {
            var fullPath = Path.Combine(RepositoryPaths.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(fullPath), $"Retained artifact '{relativePath}' must exist.");
        }

        var verifyLocalPs1 = File.ReadAllText(Path.Combine(RepositoryPaths.FindRepositoryRoot(), "scripts", "verify-local.ps1"));
        var verifyLocalSh = File.ReadAllText(Path.Combine(RepositoryPaths.FindRepositoryRoot(), "scripts", "verify-local.sh"));

        foreach (var retiredPath in ParityArtifactRetentionContract.ExcludedArtifactPaths)
        {
            Assert.IsFalse(verifyLocalPs1.Contains(retiredPath), $"verify-local.ps1 must not require retired artifact '{retiredPath}'.");
            Assert.IsFalse(verifyLocalSh.Contains(retiredPath), $"verify-local.sh must not require retired artifact '{retiredPath}'.");
        }

        foreach (var retainedPath in ParityArtifactRetentionContract.RetainedArtifactPaths)
        {
            Assert.IsTrue(verifyLocalPs1.Contains(retainedPath), $"verify-local.ps1 must validate retained artifact '{retainedPath}'.");
            Assert.IsTrue(verifyLocalSh.Contains(retainedPath), $"verify-local.sh must validate retained artifact '{retainedPath}'.");
        }
    }
}
