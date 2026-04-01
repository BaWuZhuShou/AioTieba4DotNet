using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Profile.GetUInfoProfile;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.UserSocial)]
[TestSubject(typeof(GetUInfoProfile<>))]
public sealed class GetUInfoProfileTest : TestBase
{
    [TestMethod]
    public async Task TestGetUInfoProfile()
    {
        var getUInfoProfile = new GetUInfoProfile<long>(HttpCore);
        var result = await getUInfoProfile.RequestAsync(1); // 百度官方账号 ID 为 1
        Assert.IsNotNull(result);
        Console.WriteLine($"User Profile: {result.UserName}");
    }
}
