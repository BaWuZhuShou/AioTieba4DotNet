using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.ThreadRead)]
[TestSubject(typeof(TiebaClient))]
public sealed class TiebaClientThreadReadIntegrationTests : TestBase
{
    [TestMethod]
    public async Task GetPostsAndCommentsAsync_SafeForumFirstThread_ReturnsStableReadShape()
    {
        var forum = await RequireSafeForumFixtureAsync(nameof(GetPostsAndCommentsAsync_SafeForumFirstThread_ReturnsStableReadShape));
        using var client = CreateClient(TiebaTransportMode.Http);
        var threads = await client.Threads.GetThreadsAsync(forum.ResolvedName, 1, 20);
        if (threads.Objs.Count == 0)
            Assert.Inconclusive($"Skipping thread-read scenario: forum '{forum.ResolvedName}' returned no threads.");

        var thread = threads.Objs[0];
        var posts = await client.Threads.GetPostsAsync(thread.Tid, 1, 10, withComments: true, commentRn: 2);

        Assert.IsNotNull(posts);
        Assert.AreEqual(thread.Tid, posts.Thread.Tid);
        Assert.AreEqual((long)forum.Fid, posts.Forum.Fid);
        Assert.IsGreaterThanOrEqualTo(0, posts.Page.TotalCount);

        if (posts.Objs.Count == 0)
            Assert.Inconclusive($"Skipping comment-read scenario: thread '{thread.Tid}' returned no posts.");

        var comments = await client.Threads.GetCommentsAsync(thread.Tid, posts.Objs[0].Pid);

        Assert.IsNotNull(comments);
        Assert.AreEqual(thread.Tid, comments.Thread.Tid);
        Assert.AreEqual(posts.Objs[0].Pid, comments.Post.Pid);
        Assert.IsGreaterThanOrEqualTo(0, comments.Page.TotalCount);
    }

    [TestMethod]
    public async Task GetTabMapAsync_SafeForum_ReturnsStandaloneMapAndPreservesThreadTabDictionary()
    {
        EnsureAuthenticated();
        var forum = await RequireSafeForumFixtureAsync(nameof(GetTabMapAsync_SafeForum_ReturnsStandaloneMapAndPreservesThreadTabDictionary));
        using var client = CreateClient(TiebaTransportMode.Http);

        var tabMap = await client.Threads.GetTabMapAsync(forum.ResolvedName);
        var threads = await client.Threads.GetThreadsAsync(forum.ResolvedName, 1, 20);

        Assert.IsNotNull(tabMap);
        Assert.IsNotNull(threads.TabDictionary);
        foreach (var pair in threads.TabDictionary)
        {
            Assert.IsTrue(tabMap.TryGetValue(pair.Key, out var tabId), $"Standalone tab map did not contain '{pair.Key}'.");
            Assert.AreEqual(pair.Value, tabId);
        }
    }

    [TestMethod]
    public async Task GetRecoversAsync_SafeForum_ReturnsRecoverPageShape_WhenModeratorAccessAllows()
    {
        EnsureAuthenticated();
        var forum = await RequireSafeForumFixtureAsync(nameof(GetRecoversAsync_SafeForum_ReturnsRecoverPageShape_WhenModeratorAccessAllows));

        try
        {
            var recovers = await Client.Threads.GetRecoversAsync(forum.ResolvedName, 1, 10);

            Assert.IsNotNull(recovers);
            Assert.IsGreaterThanOrEqualTo(1, recovers.Page.CurrentPage);
            Assert.IsGreaterThanOrEqualTo(0, recovers.Page.PageSize);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping moderator-only recover list path: {exception.Message}");
        }
    }

    [TestMethod]
    public async Task GetRecoverInfoAsync_SafeForumFirstRecover_ReturnsRecoverBody_WhenRecoverExists()
    {
        EnsureAuthenticated();
        var forum = await RequireSafeForumFixtureAsync(nameof(GetRecoverInfoAsync_SafeForumFirstRecover_ReturnsRecoverBody_WhenRecoverExists));

        try
        {
            var recovers = await Client.Threads.GetRecoversAsync(forum.ResolvedName, 1, 10);
            if (recovers.Count == 0)
                Assert.Inconclusive($"Skipping recover-detail scenario: forum '{forum.ResolvedName}' returned no recover entries.");

            var recover = recovers[0];
            var info = await Client.Threads.GetRecoverInfoAsync(forum.ResolvedName, recover.Tid, recover.Pid);

            Assert.IsNotNull(info);
            Assert.AreEqual(recover.Tid, info.Tid);
            Assert.AreEqual(recover.Pid, info.Pid);
            Assert.IsNotNull(info.Content);
            Assert.IsFalse(string.IsNullOrWhiteSpace(info.User.ShowName));
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping moderator-only recover detail path: {exception.Message}");
        }
    }
}
