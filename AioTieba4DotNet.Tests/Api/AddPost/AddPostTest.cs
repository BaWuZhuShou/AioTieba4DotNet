using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AddPostApi = AioTieba4DotNet.Api.AddPost.AddPost;
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

        var addPost = new AddPostApi(HttpCore, WebsocketCore);

        var content = "这是一条来自 AioTieba4DotNet 单元测试的回复。";

        try
        {
            // 使用一个测试贴进行回复
            var success = await addPost.RequestAsync("", 1, 1, content);
            Assert.IsTrue(success);
            Console.WriteLine($"回复成功: {success}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"回复失败: {ex.Message}");
        }
    }
}
