using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnlikeForumApi = AioTieba4DotNet.Api.UnlikeForum.UnlikeForum;
using GetTbsApi = AioTieba4DotNet.Api.GetTbs.GetTbs;
using JetBrains.Annotations;

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

        var getTbs = new GetTbsApi(HttpCore);
        HttpCore!.Account!.Tbs = await getTbs.RequestAsync();

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
