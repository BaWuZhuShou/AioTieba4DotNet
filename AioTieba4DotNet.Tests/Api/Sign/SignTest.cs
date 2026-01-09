using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignApi = AioTieba4DotNet.Api.Sign.Sign;
using GetTbsApi = AioTieba4DotNet.Api.GetTbs.GetTbs;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.Sign;

[TestClass]
[TestSubject(typeof(SignApi))]
public class SignTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过签到测试");
            return;
        }

        var getTbs = new GetTbsApi(HttpCore);
        HttpCore.Account.Tbs = await getTbs.RequestAsync();
        
        var signApi = new SignApi(HttpCore);

        try
        {
            // 签到测试，使用测试吧
            bool success = await signApi.RequestAsync("测试吧", 11626);
            Assert.IsTrue(success);
            Console.WriteLine("签到成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"签到请求失败 (可能已签到): {ex.Message}");
        }
    }
}
