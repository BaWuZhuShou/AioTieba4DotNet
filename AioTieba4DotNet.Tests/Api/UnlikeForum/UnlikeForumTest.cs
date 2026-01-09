using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LikeForumApi = AioTieba4DotNet.Api.LikeForum.LikeForum;
using GetTbsApi = AioTieba4DotNet.Api.GetTbs.GetTbs;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.LikeForum;

[TestClass]
[TestSubject(typeof(LikeForumApi))]
public class LikeForumTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过关注吧测试");
            return;
        }

        var getTbs = new GetTbsApi(HttpCore);
        HttpCore!.Account!.Tbs = await getTbs.RequestAsync();
        
        var likeForum = new LikeForumApi(HttpCore);

        try
        {
            // 关注 "测试吧"
            var success = await likeForum.RequestAsync(73);
            Assert.IsTrue(success);
            Console.WriteLine("关注成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"关注吧请求失败: {ex.Message}");
        }
    }
}
