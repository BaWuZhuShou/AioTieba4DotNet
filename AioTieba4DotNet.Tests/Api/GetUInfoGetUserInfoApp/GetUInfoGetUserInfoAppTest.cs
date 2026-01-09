using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetUInfoGetUserInfoApp;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.GetUInfoGetUserInfoApp))]
public class GetUInfoGetUserInfoAppTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getUInfoGetUserInfoApp = new AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.GetUInfoGetUserInfoApp(HttpCore);
        var result = await getUInfoGetUserInfoApp.RequestAsync(1); // 百度官方账号 ID 为 1
        Assert.IsNotNull(result);
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
}