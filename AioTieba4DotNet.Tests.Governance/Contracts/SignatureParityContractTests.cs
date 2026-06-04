#nullable enable
using System.Collections.Generic;
using System.Linq;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
[TestCategory(OnlineTestParityCategories.Signature)]
public sealed class SignatureParityContractTests
{
    [TestMethod]
    public void UnifiedSignatureParityHarnessCapturesMd5InputOrderAndDynamicMaskingRules()
    {
        var orderedFields = new List<KeyValuePair<string, string>>
        {
            new("k1", "v1"),
            new("ts", "1710000000"),
            new("nonce", "abc"),
            new("token", "secret-token"),
            new("cursor", "cursor-a")
        };
        var signedFields = AioTieba4DotNet.Transport.Http.TiebaHttpRequestSigner.Sign(orderedFields);

        var actual = RetainedTransportParitySupport.ObserveSignature(signedFields);
        var expected = RetainedTransportParitySupport.ObserveSignature(
        [
            new("k1", "v1"),
            new("ts", "9999999999"),
            new("nonce", "zzz"),
            new("token", "another-token"),
            new("cursor", "cursor-b"),
            new("sign", signedFields[^1].Value)
        ]);

        var orderedReference = RetainedTransportParitySupport.ObserveSignature(
        [
            new("k1", "v1"),
            new("ts", "1710000000"),
            new("nonce", "abc"),
            new("token", "secret-token"),
            new("cursor", "cursor-a"),
            new("sign", signedFields[^1].Value)
        ]);

        var rows = new[]
        {
            CreateRow("signer.md5-input-order", RetainedTransportParitySupport.Compare(orderedReference, actual)),
            CreateRow("signer.dynamic-field-masking", RetainedTransportParitySupport.Compare(expected, actual))
        };

        Assert.IsTrue(rows.All(static row => row.Comparison.Match), "Expected the retained signature parity reference cases to match after canonical masking.");
        Assert.IsTrue(rows.Any(static row => row.Comparison.AppliedMaskIds.Contains("timestamp", System.StringComparer.Ordinal)), "Expected the retained signature slice to record timestamp masking explicitly.");
        Assert.IsTrue(rows.Any(static row => row.Comparison.AppliedMaskIds.Contains("token", System.StringComparer.Ordinal)), "Expected the retained signature slice to record token masking explicitly.");
    }

    [TestMethod]
    public void SignatureParityDetectsFieldOrderDrift()
    {
        var actual = RetainedTransportParitySupport.ObserveSignature(
        [
            new("kw", "forum"),
            new("pn", "1"),
            new("rn", "30"),
            new("sign", "ignored")
        ]);
        var reordered = RetainedTransportParitySupport.ObserveSignature(
        [
            new("pn", "1"),
            new("kw", "forum"),
            new("rn", "30"),
            new("sign", "ignored")
        ]);

        var comparison = RetainedTransportParitySupport.Compare(reordered, actual);

        Assert.IsFalse(comparison.Match, "Expected the signature parity contract to detect field-order drift.");
        Assert.IsTrue(comparison.Diffs.Any(static diff => string.Equals(diff.Path, "form.keys", System.StringComparison.Ordinal)), comparison.ToFailureMessage("forced-field-order-drift"));
    }

    private static SignatureParityRow CreateRow(string auditUnit, TransportParityComparison comparison)
    {
        return new SignatureParityRow(auditUnit, comparison);
    }

    private sealed record SignatureParityRow(string AuditUnit, TransportParityComparison Comparison);
}
