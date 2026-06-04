#nullable enable
using System;
using System.Linq;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
[TestCategory(OnlineTestParityCategories.Gaps)]
public sealed class ParityGapLedgerContractTests
{
    [TestMethod]
    public void GapLedgerEnumeratesEveryDeferredMatrixRowAsCanonicalRows()
    {
        ParityGapLedgerContract.ValidateMatrixAlignment();
        ParityGapLedgerContract.ValidateMarkdownLedger();
    }

    [TestMethod]
    public void GapLedgerArtifactContainsCanonicalFieldsAndRequiredBlockerMetadata()
    {
        Assert.AreEqual(
            ".sisyphus/evidence/parity-gap-ledger.json",
            ParityGapLedgerContract.EvidenceRelativePath);
        Assert.AreEqual(
            "dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter \"TestCategory=Parity:Gaps\"",
            ParityGapLedgerContract.VerificationCommand);

        ParityGapLedgerContract.ValidateArtifact();
    }

    [TestMethod]
    public void GapLedgerRejectsHiddenOrProseOnlyRows()
    {
        var markdownAuditUnits = ParityEvidenceSchemaContract.Rows
            .Where(row => string.Equals(row.ProofArtifact.Trim('`'), ParityGapLedgerContract.EvidenceRelativePath, StringComparison.Ordinal))
            .Select(row => row.AuditUnit.Trim('`'))
            .ToArray();
        var artifactAuditUnits = ParityGapLedgerContract.Artifact.Rows
            .Select(static row => row.AuditUnit)
            .ToArray();

        var missingMarkdown = markdownAuditUnits.Skip(1).ToArray();
        var ex1 = Assert.ThrowsExactly<InvalidOperationException>(
            () => ParityGapLedgerContract.ValidateGapCoverage(missingMarkdown, artifactAuditUnits));
        StringAssert.Contains(ex1.Message, "Markdown gap rows");

        var missingArtifact = artifactAuditUnits.Skip(1).ToArray();
        var ex2 = Assert.ThrowsExactly<InvalidOperationException>(
            () => ParityGapLedgerContract.ValidateGapCoverage(markdownAuditUnits, missingArtifact));
        StringAssert.Contains(ex2.Message, "Gap artifact rows");
    }
}
