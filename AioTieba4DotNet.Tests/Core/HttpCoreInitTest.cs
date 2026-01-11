using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
public class HttpCoreInitTest : TestBase
{
    [TestMethod]
    public async Task TestAutoInitTbs()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过测试");
            return;
        }

        // TestBase 构造函数中已经初始化了 HttpCore 和 Account
        // 此时 HttpCore.SetAccount 应该已经触发了后台获取 TBS

        // 稍微等待一下后台任务，虽然不是必须的，因为 GetTbsAsync 会等待
        await Task.Delay(100);

        // 获取当前 HttpCore 实例
        var httpCore = HttpCore;

        // 如果后台任务成功，Tbs 应该已经被设置
        // 即使没有完成，调用 GetTbsAsync 应该能正常返回
        var tbs = await httpCore.GetTbsAsync();

        Assert.IsFalse(string.IsNullOrEmpty(tbs), "TBS 应该被成功获取");
        Assert.AreEqual(tbs, httpCore.Account?.Tbs, "获取的 TBS 应该存储在 Account 中");
    }
}
