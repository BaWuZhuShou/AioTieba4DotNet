using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AioTieba4DotNet.Tests.Api.GetUInfoUserJson;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.UserSocial)]
[TestSubject(typeof(AioTieba4DotNet.Api.GetUInfoUserJson.GetUInfoUserJson))]
public sealed class GetUInfoUserJsonTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getUInfoUserJson = new AioTieba4DotNet.Api.GetUInfoUserJson.GetUInfoUserJson(HttpCore);
        // 使用用户名进行查询
        var result = await getUInfoUserJson.RequestAsync("momoiebee");
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }
}
