using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.ThreadWrite;

[TestClass]
public class ThreadWriteSafetyTest : TestBase
{
    [TestMethod]
    [TestCategory("Live")]
    public void OwnedThreadFixture_IsExplicitlyRequired_ForLiveThreadMutations()
    {
        var ownedThreadId = RequireOwnedThreadFixture(nameof(OwnedThreadFixture_IsExplicitlyRequired_ForLiveThreadMutations));

        Console.WriteLine(
            $"safeForumQuery={ConfiguredSafeForumQuery}, canonicalFname={ConfiguredCanonicalSafeForumName}, ownedTid={ownedThreadId}");
    }

    [TestMethod]
    [TestCategory("Live")]
    public void OwnedReplyFixture_IsExplicitlyRequired_ForLiveReplyMutations()
    {
        var ownedReplyId = RequireOwnedReplyFixture(nameof(OwnedReplyFixture_IsExplicitlyRequired_ForLiveReplyMutations));

        Console.WriteLine(
            $"safeForumQuery={ConfiguredSafeForumQuery}, canonicalFname={ConfiguredCanonicalSafeForumName}, ownedPid={ownedReplyId}");
    }
}
