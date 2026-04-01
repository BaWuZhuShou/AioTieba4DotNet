using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetComments;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ThreadRead)]
public sealed class GetCommentsTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        using var client = CreateClient(TiebaTransportMode.Http);
        var sample = await FindSampleCommentSourceAsync(client);
        var tid = sample.ThreadSample.Tid;
        var pid = sample.Pid;
        var threadModule = ((ITiebaClient)client).Threads;

        try
        {
            var result = await threadModule.GetCommentsAsync(tid, pid, 1, false);
            Assert.IsNotNull(result, "返回结果不应为空");
            Assert.AreEqual(tid, result.Thread.Tid);
            Assert.AreEqual(pid, result.Post.Pid);

            if (result.Objs.Count > 0)
            {
                Console.WriteLine(
                    $"safeForumQuery={sample.ThreadSample.Forum.Query}, canonicalFname={result.Forum?.Fname}, tid={tid}, pid={pid}, commentsPn=1, returnedComments={result.Objs.Count}");
                foreach (var comment in result.Objs)
                    Console.WriteLine($"[{comment.User?.ShowName}]: {comment.ReplyToId} {comment.Text}");
            }
            else
            {
                Console.WriteLine($"tid={tid}, pid={pid} returned no comments.");
            }
        }
        catch (TieBaServerException ex)
        {
            Console.WriteLine($"请求失败 (贴吧服务器返回错误): {ex.Message}");
            Assert.Inconclusive(
                $"Comments API sample expired for safe forum source tid={tid}, pid={pid}: {ex.Message}");
        }
    }

    private async Task<CommentSourceSample> FindSampleCommentSourceAsync(ITiebaClient client)
    {
        var forum = RequireConfiguredSafeForumReadFixture(nameof(TestRequest));
        var threadModule = client.Threads;
        var sample = await ThreadReadSampleDiscovery.RequireCommentSourceSampleAsync(
            threadModule,
            forum,
            nameof(TestRequest),
            5,
            10,
            5,
            20,
            2,
            ThreadSortType.Reply,
            PostSortType.Hot);
        Console.WriteLine(
            $"discovered safe sample from forum={sample.ThreadSample.Forum.ResolvedName}, tid={sample.ThreadSample.Tid}, pid={sample.Pid}, threadPage={sample.ThreadSample.ThreadsPageNumber}, replyNum={sample.ReplyCount}, previewComments={sample.PreviewCommentCount}");
        return sample;
    }
}
