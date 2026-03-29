using System;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetThreadPosts;

[TestClass]
[TestCategory("Live")]
public class GetThreadPostsTest : TestBase
{
    private const string SafeForumQuery = "lol欧服吧";
    private const string CanonicalSafeForumName = "lol欧服";

    [TestMethod]
    public async Task TestRequest()
    {
        var threadModule = ((global::AioTieba4DotNet.ITiebaClient)Client).Threads;
        var threads = await threadModule.GetThreadsAsync(CanonicalSafeForumName, 1, 10, ThreadSortType.Reply);
        Assert.IsNotNull(threads.Objs);
        Assert.IsNotEmpty(threads.Objs);

        var tid = threads.Objs.First().Tid;
        Console.WriteLine(
            $"safeForumQuery={SafeForumQuery}, canonicalFname={threads.Forum.Fname}, sample tid={tid}, threadsPn=1, threadsRn=10, threadsSort={ThreadSortType.Reply}");
        var result = await threadModule.GetPostsAsync(
            tid,
            pn: 1,
            rn: 10,
            sort: PostSortType.Hot,
            onlyThreadAuthor: false,
            withComments: true,
            commentRn: 2,
            commentSortByAgree: true);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Objs);
        Assert.IsTrue(result.Objs.All(post => post.Comments.Count <= 2));
        Console.WriteLine(
            $"posts tid={tid}, pn=1, rn=10, sort={PostSortType.Hot}, withComments=true, commentRn=2, commentSortByAgree=true, returnedPosts={result.Objs.Count}");

        foreach (var post in result.Objs.Take(5))
            Console.WriteLine(
                $"sample pid={post.Pid}, floor={post.Floor}, previewComments={post.Comments.Count}, replyNum={post.ReplyNum}, text={post.Text} (by {post.User?.ShowName})");
    }
}
