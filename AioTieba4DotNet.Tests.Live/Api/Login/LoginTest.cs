using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Login;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.MessagingClient)]
[TestSubject(typeof(AioTieba4DotNet.Api.Login.Login))]
public sealed class LoginTest : TestBase
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
