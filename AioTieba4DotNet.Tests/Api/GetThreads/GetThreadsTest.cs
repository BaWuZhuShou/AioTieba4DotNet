using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Api.GetThreads;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.GetThreads;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.GetThreads.GetThreads))]
public class GetThreadsTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getThreads = new AioTieba4DotNet.Api.GetThreads.GetThreads(HttpCore, WebsocketCore);
        
        // 调用接口获取“地下城与勇士”吧的主题帖
        var result = await getThreads.RequestWsAsync("DNF吧", 1, 30, 5, 0);
        
        // 验证结果
        Assert.IsNotNull(result, "返回结果不应为空");
        Assert.IsNotNull(result.Objs, "主题列表不应为空");
        
        Console.WriteLine($"成功获取 [{result.Forum?.Fname}] 吧的主题列表");
        Console.WriteLine($"当前页帖子数: {result.Objs.Count}");

        if (result.Objs.Count > 0)
        {
            var thread = result.Objs.First();
            Console.WriteLine($"首条帖子标题: {thread.Title}");
            string authorName = thread.User?.ShowName ?? "未知";
            Console.WriteLine($"首条帖子作者: {authorName}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(thread.Title), "帖子标题不应为空");
        }
    }
}