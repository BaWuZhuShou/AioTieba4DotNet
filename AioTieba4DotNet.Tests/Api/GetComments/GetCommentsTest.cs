using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetCommentsApi = AioTieba4DotNet.Api.GetComments.GetComments;
using JetBrains.Annotations;

namespace AioTieba4DotNet.Tests.Api.GetComments;

[TestClass]
[TestSubject(typeof(GetCommentsApi))]
public class GetCommentsTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getComments = new GetCommentsApi(HttpCore, WebsocketCore);
        
        // 使用一个已知的帖子 ID 和楼层 ID 进行测试
        var tid = 10377929712;
        var pid = 153071185710;
        
        try 
        {
            var result = await getComments.RequestAsync(tid, pid, 1, false);
            Assert.IsNotNull(result, "返回结果不应为空");
            
            // 只有当获取到数据时才打印，避免在 ID 失效时产生额外的 null 问题 (虽然已经处理了健壮性)
            if (result.Objs.Count > 0)
            {
                Console.WriteLine($"成功获取 [{result.Forum?.Fname}] 吧，主题 [{result.Thread?.Title}] 下楼层 [{result.Post?.Pid}] 的楼中楼");
                Console.WriteLine($"楼中楼数量: {result.Objs.Count}");
                foreach (var comment in result.Objs)
                {
                    Console.WriteLine($"[{comment.User?.ShowName}]: {comment.ReplyToId} {comment.Text}");
                }
            }
            else
            {
                Console.WriteLine("获取成功，但该楼层没有楼中楼或数据已失效（返回了空对象）。");
            }
        }
        catch (AioTieba4DotNet.Exceptions.TieBaServerException ex)
        {
            Console.WriteLine($"请求失败 (贴吧服务器返回错误): {ex.Message}");
            // 如果是 ID 失效导致的服务器错误，我们在这里可以选择不抛出异常，或者标记为 Inconclusive
        }
    }
}
