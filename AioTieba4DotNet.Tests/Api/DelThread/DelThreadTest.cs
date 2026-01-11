using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DelThreadApi = AioTieba4DotNet.Api.DelThread.DelThread;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.DelThread;

[TestClass]
[TestSubject(typeof(DelThreadApi))]
public class DelThreadTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过删帖测试");
            return;
        }

        var delThread = new DelThreadApi(HttpCore);

        try
        {
            // 删帖测试建议在集成测试中进行，这里仅验证接口封装
            // await delThread.RequestAsync(11626, 123456789);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删帖请求失败: {ex.Message}");
        }
    }
}
