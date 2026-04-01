using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.MessagingClient)]
public sealed class HttpCoreInitTest : TestBase
{
    [TestMethod]
    public async Task TestExplicitSessionTbsInitialization()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过测试");
            return;
        }

        var tbs = await ((TiebaClient)Client).Users.GetTbsAsync();

        Assert.IsFalse(string.IsNullOrEmpty(tbs), "TBS 应该被成功获取");
    }
}
