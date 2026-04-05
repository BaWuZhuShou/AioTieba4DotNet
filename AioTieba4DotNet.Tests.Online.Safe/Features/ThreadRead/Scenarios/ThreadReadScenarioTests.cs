#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Safe.Features.ThreadRead.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.ThreadRead)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.ThreadRead)]
[TestSubject(typeof(TiebaClient))]
public sealed class ThreadReadScenarioTests : OnlineSafeExecutionTestBase
{
    private const int ThreadDiscoveryPageSize = 10;
    private const int ThreadCandidateCount = 5;
    private const int CommentDiscoveryThreadPages = 2;
    private const int CommentDiscoveryPostPages = 3;
    private const int PostDiscoveryPageSize = 30;

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetThreadsAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsGetTabMapAsync)]
    public Task GetThreadsAndTabMapAsyncStableForumReturnStableMetadataForNameAndFidOverloads()
    {
        return ExecuteSafeAsync(
            "thread read threads and tab-map sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(GetThreadsAndTabMapAsyncStableForumReturnStableMetadataForNameAndFidOverloads));
                var threadsByName = await client.Threads.GetThreadsAsync(context.ForumSelector, 1, ThreadDiscoveryPageSize,
                    ThreadSortType.Reply);
                var threadsByFid = await client.Threads.GetThreadsAsync(context.ForumId, 1, ThreadDiscoveryPageSize,
                    ThreadSortType.Reply);
                var tabMapByName = await client.Threads.GetTabMapAsync(context.ForumName);
                var tabMapByFid = await client.Threads.GetTabMapAsync(context.ForumId);

                AssertThreadsShape(context, threadsByName);
                AssertThreadsShape(context, threadsByFid);

                Assert.IsNotNull(tabMapByName);
                Assert.IsNotNull(tabMapByFid);
                Assert.AreEqual(tabMapByName.Count, tabMapByFid.Count);
                foreach (var pair in tabMapByName)
                {
                    Assert.IsTrue(tabMapByFid.TryGetValue(pair.Key, out var otherValue));
                    Assert.AreEqual(pair.Value, otherValue);
                }
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetPostsAsync)]
    public Task GetPostsAsyncStableForumFirstThreadReturnsPostPageWithPreviewComments()
    {
        return ExecuteSafeAsync(
            "thread read posts sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(GetPostsAsyncStableForumFirstThreadReturnsPostPageWithPreviewComments));
                var threads = await client.Threads.GetThreadsAsync(context.ForumSelector, 1, ThreadDiscoveryPageSize,
                    ThreadSortType.Reply);

                Assert.IsNotEmpty(threads.Objs);
                var thread = threads.Objs[0];
                var posts = await client.Threads.GetPostsAsync(
                    thread.Tid,
                    1,
                    10,
                    PostSortType.Hot,
                    withComments: true,
                    commentRn: 2,
                    commentSortByAgree: true);

                Assert.IsNotNull(posts);
                Assert.IsNotNull(posts.Page);
                Assert.IsNotNull(posts.Thread);
                Assert.IsNotNull(posts.Forum);
                Assert.AreEqual(thread.Tid, posts.Thread.Tid);
                Assert.AreEqual((long)context.ForumId, posts.Forum.Fid);
                Assert.AreEqual(context.ForumName, posts.Forum.Fname);
                Assert.IsNotNull(posts.Objs);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetCommentsAsync)]
    public Task GetCommentsAsyncStableForumFirstPostReturnsCommentPageShape()
    {
        return ExecuteSafeAsync(
            "thread read comments sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(GetCommentsAsyncStableForumFirstPostReturnsCommentPageShape));
                var sample = await RequireCommentedPostSampleAsync(
                    client,
                    context,
                    nameof(GetCommentsAsyncStableForumFirstPostReturnsCommentPageShape));
                var comments = await client.Threads.GetCommentsAsync(sample.ThreadId, sample.PostId, 1);

                Assert.IsNotNull(comments);
                Assert.IsNotNull(comments.Page);
                Assert.IsNotNull(comments.Thread);
                Assert.IsNotNull(comments.Post);
                Assert.AreEqual(sample.ThreadId, comments.Thread.Tid);
                Assert.AreEqual(sample.PostId, comments.Post.Pid);
                Assert.AreEqual((long)context.ForumId, comments.Forum.Fid);
                Assert.AreEqual(context.ForumName, comments.Forum.Fname);
                Assert.IsNotNull(comments.Objs);
            });
    }

    private static void AssertThreadsShape(DedicatedForumContext context, Threads threads)
    {
        Assert.IsNotNull(threads);
        Assert.IsNotNull(threads.Page);
        Assert.IsNotNull(threads.Forum);
        Assert.IsNotNull(threads.TabDictionary);
        Assert.AreEqual((long)context.ForumId, threads.Forum.Fid);
        Assert.AreEqual(context.ForumName, threads.Forum.Fname);
        Assert.IsNotNull(threads.Objs);
    }

    private static TiebaClient CreateClient(OnlineExecutionScope scope)
    {
        var options = new TiebaOptions
        {
            Bduss = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Bduss : null,
            Stoken = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Stoken : null,
            TransportMode = TiebaTransportMode.Http
        };

        return new TiebaClient(options);
    }

    private static async Task<DedicatedForumContext> ResolveDedicatedForumContextAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        ArgumentNullException.ThrowIfNull(client);

        var forumSelector = !string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumQuery)
            ? scope.Safe.Assets.ForumQuery
            : scope.Safe.Assets.ForumName;

        if (string.IsNullOrWhiteSpace(forumSelector))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: thread-read coverage requires an explicit dedicated safe forum asset. Set {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} instead of relying on a public fallback.");
        }

        var forum = await client.Forums.GetForumAsync(forumSelector);
        Assert.IsNotNull(forum);
        Assert.IsPositive(forum.Fid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(forum.Fname));
        return new DedicatedForumContext(forumSelector, forum.Fname, checked((ulong)forum.Fid));
    }

    private static async Task<CommentedPostSample> RequireCommentedPostSampleAsync(
        TiebaClient client,
        DedicatedForumContext context,
        string operationName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(context);

        for (var threadPage = 1; threadPage <= CommentDiscoveryThreadPages; threadPage++)
        {
            var threads = await client.Threads.GetThreadsAsync(context.ForumSelector, threadPage, ThreadDiscoveryPageSize,
                ThreadSortType.Reply);

            foreach (var thread in threads.Objs.Take(ThreadCandidateCount))
            {
                for (var postPage = 1; postPage <= CommentDiscoveryPostPages; postPage++)
                {
                    var posts = await client.Threads.GetPostsAsync(
                        thread.Tid,
                        postPage,
                        PostDiscoveryPageSize,
                        PostSortType.Hot,
                        withComments: true,
                        commentRn: 2,
                        commentSortByAgree: true);
                    var post = posts.Objs.FirstOrDefault(static candidate => candidate.Comments.Count > 0 || candidate.ReplyNum > 0);
                    if (post is not null)
                        return new CommentedPostSample(thread.Tid, post.Pid);
                }

                if (!threads.HasMore)
                    break;
            }

            if (!threads.HasMore)
                break;
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: the dedicated safe forum '{context.ForumName}' did not expose a thread with visible comments in the bounded discovery window. Point {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} at a safe forum that includes comment coverage.");
        return default;
    }

    private sealed record DedicatedForumContext(string ForumSelector, string ForumName, ulong ForumId);

    private sealed record CommentedPostSample(long ThreadId, long PostId);
}
