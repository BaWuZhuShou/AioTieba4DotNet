using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetTbsApi = AioTieba4DotNet.Api.GetTbs.GetTbs;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.GetTbs;

[TestClass]
[TestSubject(typeof(GetTbsApi))]
public class GetTbsTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getTbs = new GetTbsApi(HttpCore);
        var tbs = await getTbs.RequestAsync();
        // 匿名访问也会返回 TBS
        Assert.IsNotNull(tbs);
        Console.WriteLine($"TBS: {tbs}");
    }
}
