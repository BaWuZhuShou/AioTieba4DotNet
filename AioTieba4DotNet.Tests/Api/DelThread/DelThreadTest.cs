using System;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DelThreadApi = AioTieba4DotNet.Api.DelThread.DelThread;

namespace AioTieba4DotNet.Tests.Api.DelThread;

[TestClass]
[TestSubject(typeof(DelThreadApi))]
public class DelThreadTest : TestBase
{
    [TestMethod]
    [TestCategory("Live")]
    public void OwnedThreadFixture_IsRequired_ForLiveDelThread()
    {
        var ownedThreadId = RequireOwnedThreadFixture(nameof(OwnedThreadFixture_IsRequired_ForLiveDelThread));
        Console.WriteLine(
            $"safeForumQuery={ConfiguredSafeForumQuery}, canonicalFname={ConfiguredCanonicalSafeForumName}, ownedTid={ownedThreadId}, api={typeof(DelThreadApi).Name}");
    }
}
