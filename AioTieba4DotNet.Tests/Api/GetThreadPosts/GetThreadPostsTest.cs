using System;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetThreadPostsApi = AioTieba4DotNet.Api.GetThreadPosts.GetThreadPosts;
using GetThreadsApi = AioTieba4DotNet.Api.GetThreads.GetThreads;

namespace AioTieba4DotNet.Tests.Api.GetThreadPosts;

[TestClass]
public class GetThreadPostsTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getThreads = new GetThreadsApi(HttpCore, WebsocketCore, mode: TiebaRequestMode.Http);
        var threads = await getThreads.RequestAsync("地下城与勇士", 1, 10, 5, 0);
        Assert.IsNotNull(threads.Objs);
        Assert.IsNotEmpty(threads.Objs);

        var tid = threads.Objs.First().Tid;
        Console.WriteLine($"成功获取 {threads.Forum.Fname} 的贴子列表，共 {threads.Objs.Count} 条贴子");
        var getPosts = new GetThreadPostsApi(HttpCore, WebsocketCore);
        var result = await getPosts.RequestAsync(tid, 1, 30, 0, false, false, 0, false);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Objs);
        Console.WriteLine($"成功获取 tid={tid} 的回复列表，共 {result.Objs.Count} 条回复");

        foreach (var post in result.Objs.Take(5))
        {
            Console.WriteLine($"F{post.Floor}: {post.Text}  (by {post.User?.ShowName})");
        }
    }
}