using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AgreeApi = AioTieba4DotNet.Api.Agree.Agree;
using GetTbsApi = AioTieba4DotNet.Api.GetTbs.GetTbs;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.Agree;

[TestClass]
[TestSubject(typeof(AgreeApi))]
public class AgreeTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过写操作测试");
            return;
        }
        
        // 1. 获取 TBS
        var getTbs = new GetTbsApi(HttpCore);
        string tbs = await getTbs.RequestAsync();
        Assert.IsFalse(string.IsNullOrEmpty(tbs), "获取 TBS 失败");
        
        var agreeApi = new AgreeApi(HttpCore);
        
        // 使用一个已知的帖子 ID 进行测试 (建议使用自己的帖子或测试贴)
        long tid = 8116540605;
        
        // 点赞主题帖
        try 
        {
            bool success = await agreeApi.RequestAsync(tid, 0, false, false, false);
            Assert.IsTrue(success, "点赞失败");
            Console.WriteLine("点赞主题帖成功");
            
            // 取消点赞
            bool undoSuccess = await agreeApi.RequestAsync(tid, 0, false, false, true);
            Assert.IsTrue(undoSuccess, "取消点赞失败");
            Console.WriteLine("取消点赞主题帖成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"点赞操作失败: {ex.Message}");
        }
    }
}
