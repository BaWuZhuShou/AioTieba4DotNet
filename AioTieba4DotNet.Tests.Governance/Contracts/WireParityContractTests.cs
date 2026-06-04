#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
[TestCategory(OnlineTestParityCategories.Wire)]
public sealed class WireParityContractTests
{
    [TestMethod]
    public async Task UnifiedWireParityHarnessCapturesCanonicalizedWireDiffsAgainstFrozenUpstreamTruth()
    {
        var imageUri = new Uri("https://imgsrc.baidu.com/forum/pic/item/demo.jpg?timestamp=1710000000&cursor=abc123");
        var getImagesActual = RetainedTransportParitySupport.ObserveRequest(await RetainedTransportParitySupport.CaptureGetImagesRequestAsync(imageUri));
        var signForumsActual = RetainedTransportParitySupport.ObserveRequest(await RetainedTransportParitySupport.CaptureSignForumsRequestAsync());
        var selfFollowActual = RetainedTransportParitySupport.ObserveRequest(await RetainedTransportParitySupport.CaptureSelfFollowForumsRequestAsync());
        var lastReplyersActual = RetainedTransportParitySupport.ObserveRequest(await RetainedTransportParitySupport.CaptureLastReplyersRequestAsync());
        var squareForumsActual = RetainedTransportParitySupport.ObserveRequest(await RetainedTransportParitySupport.CaptureSquareForumsRequestAsync());
        var lastReplyersFallbackActual = RetainedTransportParitySupport.CreateExpectedFallbackPath(await RetainedTransportParitySupport.CaptureFallbackPathAsync("get_last_replyers"));
        var squareForumsFallbackActual = RetainedTransportParitySupport.CreateExpectedFallbackPath(await RetainedTransportParitySupport.CaptureFallbackPathAsync("get_square_forums"));
        var transportExceptionActual = RetainedTransportParitySupport.ObserveErrorNormalization(RetainedTransportParitySupport.CaptureTransportNormalizationException());
        var timeoutExceptionActual = RetainedTransportParitySupport.ObserveErrorNormalization(RetainedTransportParitySupport.CaptureTimeoutNormalizationException());

        var rows = new[]
        {
            CreateRow(
                "forums.get_images.request",
                RetainedTransportParitySupport.Compare(RetainedTransportParitySupport.CreateExpectedGetImagesUpstream(imageUri), getImagesActual)),
            CreateRow(
                "forums.sign_forums.request",
                RetainedTransportParitySupport.Compare(
                    RetainedTransportParitySupport.CreateExpectedWebFormUpstream(
                        "/c/c/forum/msign",
                        [new("_client_version", AioTieba4DotNet.Internal.Const.MainVersion), new("subapp_type", "hybrid")],
                        includeCookies: true),
                    signForumsActual)),
            CreateRow(
                "forums.get_self_follow_forums.request",
                RetainedTransportParitySupport.Compare(
                    RetainedTransportParitySupport.CreateExpectedWebFormUpstream(
                        "/c/f/forum/forumGuide",
                        [new("tbs", "sample-tbs"), new("sort_type", "3"), new("call_from", "3"), new("page_no", "2"), new("res_num", "20")],
                        includeCookies: true),
                    selfFollowActual)),
            CreateRow(
                "forums.get_last_replyers.http-request",
                RetainedTransportParitySupport.Compare(RetainedTransportParitySupport.CreateExpectedAppProtoUpstream("/c/f/frs/page", "cmd=301001"), lastReplyersActual)),
            CreateRow(
                "forums.get_square_forums.http-request",
                RetainedTransportParitySupport.Compare(RetainedTransportParitySupport.CreateExpectedAppProtoUpstream("/c/f/forum/getForumSquare", "cmd=309653"), squareForumsActual)),
            CreateRow(
                "forums.get_last_replyers.fallback-path",
                RetainedTransportParitySupport.Compare(RetainedTransportParitySupport.CreateExpectedFallbackPath(["ws.connect"]), lastReplyersFallbackActual)),
            CreateRow(
                "forums.get_square_forums.fallback-path",
                RetainedTransportParitySupport.Compare(RetainedTransportParitySupport.CreateExpectedFallbackPath(["ws.connect"]), squareForumsFallbackActual)),
            CreateRow(
                "transport.http.error-normalization.transport",
                RetainedTransportParitySupport.Compare(RetainedTransportParitySupport.CreateExpectedUpstreamStatusError(0, "network down"), transportExceptionActual)),
            CreateRow(
                "transport.http.error-normalization.timeout",
                RetainedTransportParitySupport.Compare(RetainedTransportParitySupport.CreateExpectedUpstreamTimeoutError("http://example.com"), timeoutExceptionActual))
        };

        CollectionAssert.AreEqual(new[] { "timestamp", "nonce", "token", "cursor" }, RetainedTransportParitySupport.MaskRules.Select(static rule => rule.MaskId).ToArray());
        Assert.IsTrue(rows.Any(static row => !row.Comparison.Match), "Expected the retained wire parity slice to surface at least one readable drift anchor.");
        Assert.IsTrue(rows.Any(static row => row.Comparison.AppliedMaskIds.Contains("timestamp", StringComparer.Ordinal)), "Expected at least one wire comparison to record timestamp masking.");
        Assert.IsTrue(rows.Any(static row => row.Comparison.Diffs.Any(static diff => string.Equals(diff.Path, "headers", StringComparison.Ordinal))), "Expected the retained wire parity slice to surface header drift explicitly.");
        Assert.IsTrue(rows.Any(static row => row.Comparison.Diffs.Any(static diff => string.Equals(diff.Path, "fallbackTransportPath", StringComparison.Ordinal))), "Expected the retained wire parity slice to surface fallback transport drift explicitly.");
    }

    [TestMethod]
    public async Task WireParityDetectsSchemeDrift()
    {
        var imageUri = new Uri("http://imgsrc.baidu.com/forum/pic/item/demo.jpg");
        var actual = RetainedTransportParitySupport.ObserveRequest(await RetainedTransportParitySupport.CaptureGetImagesRequestAsync(imageUri));
        var forcedSchemeDrift = RetainedTransportParitySupport.CreateExpectedGetImagesUpstream(new Uri("https://imgsrc.baidu.com/forum/pic/item/demo.jpg"));

        var comparison = RetainedTransportParitySupport.Compare(forcedSchemeDrift, actual);

        Assert.IsFalse(comparison.Match, "Expected the wire parity contract to detect a forced scheme drift.");
        Assert.IsTrue(comparison.Diffs.Any(static diff => string.Equals(diff.Path, "scheme", StringComparison.Ordinal)), comparison.ToFailureMessage("forced-scheme-drift"));
    }

    [TestMethod]
    public void WireParityLeavesUnmaskedDynamicFieldsVisible()
    {
        var expected = new TransportParityObservation(
            "http",
            "GET",
            "tieba.baidu.com",
            "/c/f/demo",
            [new TransportParityField("timestamp_ms", "111", null)],
            [],
            [],
            [],
            [],
            [],
            TransportErrorNormalization.None,
            []);
        var actual = expected with
        {
            QueryFields = [new TransportParityField("timestamp_ms", "222", null)]
        };

        var comparison = RetainedTransportParitySupport.Compare(expected, actual);

        Assert.IsFalse(comparison.Match, "Expected unmasked dynamic fields to stay visible as diffs.");
        Assert.IsTrue(comparison.Diffs.Any(static diff => string.Equals(diff.Path, "query.fields", StringComparison.Ordinal)), comparison.ToFailureMessage("unmasked-dynamic-field"));
    }

    private static WireParityRow CreateRow(string auditUnit, TransportParityComparison comparison)
    {
        return new WireParityRow(auditUnit, comparison);
    }

    private sealed record WireParityRow(string AuditUnit, TransportParityComparison Comparison);
}
