using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetForumApi = AioTieba4DotNet.Api.GetForum.GetForum;

namespace AioTieba4DotNet.Tests.Api.GetForum;

[TestClass]
[TestCategory("Integration")]
[TestSubject(typeof(GetForumApi))]
public class GetForumTest : TestBase
{
    private const string SafeForumQuery = "lol欧服吧";
    private const string CanonicalSafeForumName = "lol欧服";

    [TestMethod]
    public async Task TestRequest()
    {
        var getForum = new GetForumApi(HttpCore);
        var result = await getForum.RequestAsync(SafeForumQuery);
        Console.WriteLine(result);
        Assert.AreEqual(CanonicalSafeForumName, result.Fname);
    }
}
