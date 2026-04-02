using System;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DelThreadApi = AioTieba4DotNet.Api.DelThread.DelThread;

namespace AioTieba4DotNet.Tests.Api.DelThread;

[TestClass]
[TestSubject(typeof(DelThreadApi))]
public sealed class DelThreadTest : TestBase
{
    [TestMethod]
    [TestCategory(TestCategoryNames.Live)]
    [TestCategory(TestCategoryNames.ThreadWriteModeration)]
    public void OwnedThreadFixture_IsRequired_ForLiveDelThread()
    {
        var ownedThreadId = RequireOwnedThreadFixture(nameof(OwnedThreadFixture_IsRequired_ForLiveDelThread));
        Assert.IsGreaterThan(0L, ownedThreadId);
        Console.WriteLine(
            $"safeForumQuery={ConfiguredSafeForumQuery}, canonicalFname={ConfiguredCanonicalSafeForumName}, ownedTid={ownedThreadId}, api={typeof(DelThreadApi).Name}");
    }
}
