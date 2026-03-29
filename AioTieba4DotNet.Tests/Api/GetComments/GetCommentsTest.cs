using System;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Exceptions;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetComments;

[TestClass]
[TestCategory("Live")]
public class GetCommentsTest : TestBase
{
    private const string SafeForumQuery = "lol欧服吧";
    private const string CanonicalSafeForumName = "lol欧服";

    [TestMethod]
    public async Task TestRequest()
    {
        var (tid, pid) = await FindSampleCommentSourceAsync();
        var threadModule = ((global::AioTieba4DotNet.ITiebaClient)Client).Threads;

        try
        {
            var result = await threadModule.GetCommentsAsync(tid, pid, 1, false);
            Assert.IsNotNull(result, "返回结果不应为空");
            Assert.AreEqual(tid, result.Thread.Tid);
            Assert.AreEqual(pid, result.Post.Pid);

            if (result.Objs.Count > 0)
            {
                Console.WriteLine(
                    $"safeForumQuery={SafeForumQuery}, canonicalFname={result.Forum?.Fname}, tid={tid}, pid={pid}, commentsPn=1, returnedComments={result.Objs.Count}");
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
            Assert.Inconclusive($"Comments API sample expired for safe forum source tid={tid}, pid={pid}: {ex.Message}");
        }
    }

    private async Task<(long Tid, long Pid)> FindSampleCommentSourceAsync()
    {
        var threadModule = ((global::AioTieba4DotNet.ITiebaClient)Client).Threads;
        var threads = await threadModule.GetThreadsAsync(CanonicalSafeForumName, 1, 10, ThreadSortType.Reply);

        foreach (var thread in threads.Objs.Take(5))
        {
            var posts = await threadModule.GetPostsAsync(
                thread.Tid,
                pn: 1,
                rn: 20,
                sort: PostSortType.Hot,
                onlyThreadAuthor: false,
                withComments: true,
                commentRn: 2,
                commentSortByAgree: true);
            var candidate = posts.Objs.FirstOrDefault(post => post.ReplyNum > 0 || post.Comments.Count > 0);
            if (candidate != null)
            {
                Console.WriteLine(
                    $"discovered safe sample from forum={CanonicalSafeForumName}, tid={thread.Tid}, pid={candidate.Pid}, replyNum={candidate.ReplyNum}, previewComments={candidate.Comments.Count}");
                return (thread.Tid, candidate.Pid);
            }
        }

        Assert.Inconclusive($"No post with comments was found in the first safe-forum sample window for {CanonicalSafeForumName}.");
        return default;
    }
}
