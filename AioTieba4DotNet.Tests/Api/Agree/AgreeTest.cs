using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AgreeApi = AioTieba4DotNet.Api.Agree.Agree;

namespace AioTieba4DotNet.Tests.Api.Agree;

[TestClass]
[TestSubject(typeof(AgreeApi))]
public class AgreeTest : TestBase
{
    [TestMethod]
    public async Task TestRequestAsync()
    {
        EnsureAuthenticated();

        var agreeApi = new AgreeApi(HttpCore);

        // 使用一个已知的帖子 ID 进行测试 (建议使用自己的帖子或测试贴)
        const long tid = 8116540605;

        // 点赞主题帖
        var success = await agreeApi.RequestAsync(tid, 0, false, false, false);
        Assert.IsTrue(success, "点赞失败");

        // 取消点赞
        var undoSuccess = await agreeApi.RequestAsync(tid, 0, false, false, true);
        Assert.IsTrue(undoSuccess, "取消点赞失败");
    }
}
