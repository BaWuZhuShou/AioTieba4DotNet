using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Api.GetUInfoUserJson;

namespace AioTieba4DotNet.Tests.Api.GetUInfoUserJson;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.GetUInfoUserJson.GetUInfoUserJson))]
public class GetUInfoUserJsonTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getUInfoUserJson = new AioTieba4DotNet.Api.GetUInfoUserJson.GetUInfoUserJson(HttpCore);
        // 使用用户名进行查询
        var result = await getUInfoUserJson.RequestAsync("momoiebee");
        Assert.IsNotNull(result);
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
}
