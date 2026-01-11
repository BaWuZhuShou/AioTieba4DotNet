using System.Threading.Tasks;
using AioTieba4DotNet.Enums;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetThreads;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.GetThreads.GetThreads))]
public class GetThreadsTest : TestBase
{
    [TestMethod]
    public async Task TestRequestAsync()
    {
        var getThreads =
            new AioTieba4DotNet.Api.GetThreads.GetThreads(HttpCore, WebsocketCore, TiebaRequestMode.Websocket);

        // 调用接口获取“地下城与勇士”吧的主题帖
        var result = await getThreads.RequestAsync("DNF", 1, 30, 5, 0);

        // 验证结果
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Objs);
        Assert.IsNotNull(result.Forum);

        if (result.Objs.Count > 0)
        {
            var thread = result.Objs[0];
            Assert.IsFalse(string.IsNullOrWhiteSpace(thread.Title));
        }
    }
}
