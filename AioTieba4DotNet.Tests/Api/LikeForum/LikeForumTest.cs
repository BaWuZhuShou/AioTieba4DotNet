using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnlikeForumApi = AioTieba4DotNet.Api.UnlikeForum.UnlikeForum;

namespace AioTieba4DotNet.Tests.Api.LikeForum;

[TestClass]
[TestSubject(typeof(UnlikeForumApi))]
public class UnlikeForumTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过取消关注吧测试");
            return;
        }

        var unlikeForum = new UnlikeForumApi(HttpCore);

        try
        {
            // 取消关注 "测试吧"
            var success = await unlikeForum.RequestAsync(73);
            Assert.IsTrue(success);
            Console.WriteLine("取消关注成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"取消关注吧请求失败: {ex.Message}");
        }
    }
}
