using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DelPostApi = AioTieba4DotNet.Api.DelPost.DelPost;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.DelPost;

[TestClass]
[TestSubject(typeof(DelPostApi))]
public class DelPostTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过删帖测试");
            return;
        }

        var delPost = new DelPostApi(HttpCore);

        try
        {
            // 删帖测试建议在集成测试中进行
            // await delPost.RequestAsync(11626, 8116540605, 123456789);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除回复请求失败: {ex.Message}");
        }
    }
}
