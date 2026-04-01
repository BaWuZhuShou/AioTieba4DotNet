using System;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DelPostApi = AioTieba4DotNet.Api.DelPost.DelPost;

namespace AioTieba4DotNet.Tests.Api.DelPost;

[TestClass]
[TestSubject(typeof(DelPostApi))]
public sealed class DelPostTest : TestBase
{
    [TestMethod]
    [TestCategory(TestCategoryNames.Live)]
    [TestCategory(TestCategoryNames.ThreadWriteModeration)]
    public void OwnedReplyFixture_IsRequired_ForLiveDelPost()
    {
        var ownedThreadId = RequireOwnedThreadFixture(nameof(OwnedReplyFixture_IsRequired_ForLiveDelPost));
        var ownedReplyId = RequireOwnedReplyFixture(nameof(OwnedReplyFixture_IsRequired_ForLiveDelPost));
        Console.WriteLine(
            $"safeForumQuery={ConfiguredSafeForumQuery}, canonicalFname={ConfiguredCanonicalSafeForumName}, ownedTid={ownedThreadId}, ownedPid={ownedReplyId}, api={typeof(DelPostApi).Name}");
    }
}
