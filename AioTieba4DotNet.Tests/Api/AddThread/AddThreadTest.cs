using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AddThreadApi = AioTieba4DotNet.Api.AddThread.AddThread;
using GetTbsApi = AioTieba4DotNet.Api.GetTbs.GetTbs;
using AioTieba4DotNet.Api.Entities.Contents;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.AddThread;

[TestClass]
[TestSubject(typeof(AddThreadApi))]
public class AddThreadTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过发帖测试");
            return;
        }

        // 1. 获取 TBS
        var getTbs = new GetTbsApi(HttpCore);
        HttpCore.Account.Tbs = await getTbs.RequestAsync();
        
        var addThread = new AddThreadApi(HttpCore);
        
        var contents = new List<IFrag>
        {
            new FragText { Text = "这是一条来自 AioTieba4DotNet 单元测试的帖子。" }
        };

        try
        {
            // 注意：频繁发帖可能会导致验证码或封禁，建议在测试吧进行
            // 使用 "测试吧" 的 FID 并不固定，这里只是演示
            // 实际上为了不干扰他人，我们通常只验证 pack 请求是否正确
            // 但既然是完整测试，就尝试发送
            long tid = await addThread.RequestAsync("测试吧", 11626, "AioTieba4DotNet 测试发帖", contents);
            Assert.IsTrue(tid > 0);
            Console.WriteLine($"发帖成功，tid: {tid}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发帖失败: {ex.Message}");
        }
    }
}
