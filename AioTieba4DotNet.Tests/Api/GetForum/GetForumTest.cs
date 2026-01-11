using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetForumApi = AioTieba4DotNet.Api.GetForum.GetForum;

namespace AioTieba4DotNet.Tests.Api.GetForum;

[TestClass]
[TestSubject(typeof(GetForumApi))]
public class GetForumTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getForum = new GetForumApi(HttpCore);
        var fname = "地下城与勇士";
        var result = await getForum.RequestAsync(fname);
        Console.WriteLine(result);
        Assert.AreEqual(fname, result.Fname);
    }
}
