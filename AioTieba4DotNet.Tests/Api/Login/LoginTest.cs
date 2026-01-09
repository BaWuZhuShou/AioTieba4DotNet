using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Login;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.Login.Login))]
public class LoginTest : TestBase
{
    [TestMethod]
    public async Task TestLogin()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过登录测试");
            return;
        }

        var loginApi = new AioTieba4DotNet.Api.Login.Login(HttpCore);
        var (user, tbs) = await loginApi.RequestAsync();
        Assert.IsNotNull(user);
        Console.WriteLine($"Logged in user: {user.UserName}, ID: {user.UserId}, TBS: {tbs}");
    }
}