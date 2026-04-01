using System;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetThreadPosts;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ThreadRead)]
public sealed class GetThreadPostsTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        using var client = CreateClient(TiebaTransportMode.Http);
        var threadModule = ((ITiebaClient)client).Threads;
        var forum = RequireConfiguredSafeForumReadFixture(nameof(TestRequest));
        var sample = await ThreadReadSampleDiscovery.RequireThreadSampleAsync(
            threadModule,
            forum,
            nameof(TestRequest),
            maxThreadPages: 5,
            threadPageSize: 10,
            threadSort: ThreadSortType.Reply);
        var tid = sample.Tid;
        Console.WriteLine(
            $"safeForumQuery={sample.Forum.Query}, resolvedFname={sample.Forum.ResolvedName}, sample tid={tid}, threadsPn={sample.ThreadsPageNumber}, threadsRn={sample.ThreadsPageSize}, threadsSort={sample.ThreadsSort}");
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
