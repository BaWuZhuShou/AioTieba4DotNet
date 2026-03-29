using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Enums;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetThreads;

[TestClass]
[TestCategory("Live")]
[TestSubject(typeof(AioTieba4DotNet.Api.GetThreads.GetThreads))]
public class GetThreadsTest : TestBase
{
    private const string SafeForumQuery = "lol欧服吧";
    private const string CanonicalSafeForumName = "lol欧服";

    [TestMethod]
    public async Task TestRequestAsync()
    {
        var threads = ((global::AioTieba4DotNet.ITiebaClient)Client).Threads;
        var result = await threads.GetThreadsAsync(CanonicalSafeForumName, 1, 10, ThreadSortType.Reply);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Objs);
        Assert.IsNotNull(result.Forum);
        Assert.AreEqual(CanonicalSafeForumName, result.Forum.Fname);

        Console.WriteLine(
            $"safeForumQuery={SafeForumQuery}, canonicalFname={result.Forum.Fname}, pn=1, rn=10, sort={ThreadSortType.Reply}, returnedThreads={result.Objs.Count}");

        if (result.Objs.Count > 0)
        {
            var thread = result.Objs[0];
            Assert.IsFalse(string.IsNullOrWhiteSpace(thread.Title));
            Console.WriteLine($"sample tid={thread.Tid}, pid={thread.Pid}, title={thread.Title}");
        }
    }
}
