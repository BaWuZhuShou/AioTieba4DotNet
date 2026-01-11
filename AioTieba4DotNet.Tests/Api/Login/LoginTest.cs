using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Login;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.Login.Login))]
public class LoginTest : TestBase
{
    [TestMethod]
    public async Task TestLoginAsync()
    {
        EnsureAuthenticated();

        var loginApi = new AioTieba4DotNet.Api.Login.Login(HttpCore);
        var (user, tbs) = await loginApi.RequestAsync();
        Assert.IsNotNull(user);
        Assert.IsFalse(string.IsNullOrEmpty(tbs));
    }
}
