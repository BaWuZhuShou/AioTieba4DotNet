using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AioTieba4DotNet.Tests.Api.GetUInfoGetUserInfoApp;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.UserSocial)]
[TestSubject(typeof(AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.GetUInfoGetUserInfoApp))]
public sealed class GetUInfoGetUserInfoAppTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getUInfoGetUserInfoApp = new AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.GetUInfoGetUserInfoApp(HttpCore);
        var result = await getUInfoGetUserInfoApp.RequestAsync(1); // 百度官方账号 ID 为 1
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }
}
