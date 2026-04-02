using System;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.ThreadWrite;

[TestClass]
public sealed class ThreadWriteSafetyTest : TestBase
{
    [TestMethod]
    [TestCategory(TestCategoryNames.Live)]
    [TestCategory(TestCategoryNames.ThreadWriteModeration)]
    public void OwnedThreadFixture_IsExplicitlyRequired_ForLiveThreadMutations()
    {
        var ownedThreadId =
            RequireOwnedThreadFixture(nameof(OwnedThreadFixture_IsExplicitlyRequired_ForLiveThreadMutations));

        Assert.IsGreaterThan(0L, ownedThreadId);
        Console.WriteLine(
            $"safeForumQuery={ConfiguredSafeForumQuery}, canonicalFname={ConfiguredCanonicalSafeForumName}, ownedTid={ownedThreadId}");
    }

    [TestMethod]
    [TestCategory(TestCategoryNames.Live)]
    [TestCategory(TestCategoryNames.ThreadWriteModeration)]
    public void OwnedReplyFixture_IsExplicitlyRequired_ForLiveReplyMutations()
    {
        var ownedReplyId =
            RequireOwnedReplyFixture(nameof(OwnedReplyFixture_IsExplicitlyRequired_ForLiveReplyMutations));

        Assert.IsGreaterThan(0L, ownedReplyId);
        Console.WriteLine(
            $"safeForumQuery={ConfiguredSafeForumQuery}, canonicalFname={ConfiguredCanonicalSafeForumName}, ownedPid={ownedReplyId}");
    }
}
