using System;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AddPostApi = AioTieba4DotNet.Api.AddPost.AddPost;

namespace AioTieba4DotNet.Tests.Api.AddPost;

[TestClass]
[TestCategory("Live")]
[TestSubject(typeof(AddPostApi))]
public class AddPostTest : TestBase
{
    [TestMethod]
    public void OwnedThreadFixture_IsRequired_ForLiveAddPost()
    {
        var ownedThreadId = RequireOwnedThreadFixture(nameof(OwnedThreadFixture_IsRequired_ForLiveAddPost));

        Console.WriteLine(
            $"safeForumQuery={ConfiguredSafeForumQuery}, canonicalFname={ConfiguredCanonicalSafeForumName}, ownedTid={ownedThreadId}, api={typeof(AddPostApi).Name}");
    }
}
