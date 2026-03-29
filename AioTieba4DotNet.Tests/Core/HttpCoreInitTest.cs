using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
[TestCategory("Live")]
public class HttpCoreInitTest : TestBase
{
    [TestMethod]
    public async Task TestExplicitSessionTbsInitialization()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过测试");
            return;
        }

        var tbs = await ((global::AioTieba4DotNet.TiebaClient)Client).Users.GetTbsAsync();

        Assert.IsFalse(string.IsNullOrEmpty(tbs), "TBS 应该被成功获取");
    }
}
