using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetForumDetailApi = AioTieba4DotNet.Api.GetForumDetail.GetForumDetail;

namespace AioTieba4DotNet.Tests.Api.GetForumDetail;

[TestClass]
[TestSubject(typeof(GetForumDetailApi))]
public class GetForumDetailTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getForumDetail = new GetForumDetailApi(HttpCore);
        var result = await getForumDetail.RequestAsync(1815379); // DNF吧的FID
        Assert.IsNotNull(result);
        Console.WriteLine($"Forum Name: {result.Fname}");
    }
}