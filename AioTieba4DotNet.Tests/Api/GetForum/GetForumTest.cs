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
        var result = await getForum.RequestAsync(new AioTieba4DotNet.Api.GetForum.GetFormParams("测试"));
        Console.WriteLine(result);
        Assert.AreEqual("测试", result.Fname);
    }
}