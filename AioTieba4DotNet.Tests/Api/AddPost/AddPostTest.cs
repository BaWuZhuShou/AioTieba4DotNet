using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AddPostApi = AioTieba4DotNet.Api.AddPost.AddPost;
using GetTbsApi = AioTieba4DotNet.Api.GetTbs.GetTbs;
using AioTieba4DotNet.Api.Entities.Contents;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.AddPost;

[TestClass]
[TestSubject(typeof(AddPostApi))]
public class AddPostTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过回复测试");
            return;
        }

        var getTbs = new GetTbsApi(HttpCore);
        HttpCore.Account.Tbs = await getTbs.RequestAsync();
        
        var addPost = new AddPostApi(HttpCore);
        
        var contents = new List<IFrag>
        {
            new FragText { Text = "这是一条来自 AioTieba4DotNet 单元测试的回复。" }
        };

        try
        {
            // 使用一个测试贴进行回复
            var pid = await addPost.RequestAsync("测试吧", 11626, 8116540605, contents);
            Assert.IsGreaterThan(0, pid);
            Console.WriteLine($"回复成功，pid: {pid}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"回复失败: {ex.Message}");
        }
    }
}
