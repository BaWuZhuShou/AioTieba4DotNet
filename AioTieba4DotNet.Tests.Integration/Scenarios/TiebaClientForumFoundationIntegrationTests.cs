using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.ForumFoundation)]
[TestSubject(typeof(TiebaClient))]
public sealed class TiebaClientForumFoundationIntegrationTests : TestBase
{
    [TestMethod]
    public async Task GetThreadsAsync_ByForumName_ReturnsExpectedForum()
    {
        using var client = CreateClient(TiebaTransportMode.Http);
        var threads = await client.Threads.GetThreadsAsync("DNF");

        Assert.IsNotNull(threads);
        Assert.IsNotNull(threads.Forum);
        Assert.AreEqual("地下城与勇士", threads.Forum.Fname);
    }

    [TestMethod]
    public async Task GetThreadsAsync_ByForumId_ReturnsExpectedForum()
    {
        using var client = CreateClient(TiebaTransportMode.Http);
        var threads = await client.Threads.GetThreadsAsync(81570);

        Assert.IsNotNull(threads);
        Assert.IsNotNull(threads.Forum);
        Assert.AreEqual(81570L, threads.Forum.Fid);
    }

    [TestMethod]
    public async Task GetFnameAsync_ByForumId_ReturnsExpectedForumName()
    {
        var forumName = await Client.Forums.GetFnameAsync(81570);

        Assert.AreEqual("地下城与勇士", forumName);
    }
}
