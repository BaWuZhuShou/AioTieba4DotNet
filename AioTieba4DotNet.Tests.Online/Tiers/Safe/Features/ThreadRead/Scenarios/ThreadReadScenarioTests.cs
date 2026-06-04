#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Tiers.Safe.Features.ThreadRead.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.ThreadRead)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.ThreadRead)]
[TestSubject(typeof(TiebaClient))]
public sealed class ThreadReadScenarioTests : OnlineSafeExecutionTestBase
{
    private const int ThreadDiscoveryPageSize = 10;
    private const int PostDiscoveryPageSize = 30;

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetThreadsAsync)]
    public Task GetThreadsAsyncStableForumSelectorReturnsStableMetadata()
    {
        return ExecuteSafeAsync(
            "thread read threads by selector sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var fixture = RequireDedicatedForumFixture(
                    scope,
                    nameof(GetThreadsAsyncStableForumSelectorReturnsStableMetadata));
                var threads = await client.Threads.GetThreadsAsync(fixture.ForumSelector, 1, ThreadDiscoveryPageSize, ThreadSortType.Reply);

                AssertThreadsShape(fixture, threads);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetThreadsAsync)]
    public Task GetThreadsAsyncStableForumFidReturnsStableMetadata()
    {
        return ExecuteSafeAsync(
            "thread read threads by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetThreadsAsyncStableForumFidReturnsStableMetadata);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var threads = await client.Threads.GetThreadsAsync(forumId, 1, ThreadDiscoveryPageSize, ThreadSortType.Reply);

                AssertThreadsShape(fixture with { ForumId = forumId }, threads);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetTabMapAsync)]
    public Task GetTabMapAsyncStableForumNameReturnsStableEntries()
    {
        return ExecuteSafeAsync(
            "thread read tab-map by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumName = RequireDedicatedForumName(scope, nameof(GetTabMapAsyncStableForumNameReturnsStableEntries));
                var tabMap = await client.Threads.GetTabMapAsync(forumName);

                AssertTabMapShape(tabMap);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetTabMapAsync)]
    public Task GetTabMapAsyncStableForumFidReturnsStableEntries()
    {
        return ExecuteSafeAsync(
            "thread read tab-map by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumId = RequireDedicatedForumId(scope, nameof(GetTabMapAsyncStableForumFidReturnsStableEntries));
                var tabMap = await client.Threads.GetTabMapAsync(forumId);

                AssertTabMapShape(tabMap);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetPostsAsync)]
    public Task GetPostsAsyncDedicatedOwnedThreadReturnsPostPageWithPreviewComments()
    {
        return ExecuteSafeAsync(
            "thread read posts sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetPostsAsyncDedicatedOwnedThreadReturnsPostPageWithPreviewComments);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var threadId = RequireOwnedThreadId(scope, operationName);
                Posts posts;
                try
                {
                    posts = await client.Threads.GetPostsAsync(
                        threadId,
                        1,
                        10,
                        PostSortType.Hot,
                        withComments: true,
                        commentRn: 2,
                        commentSortByAgree: true);
                }
                catch (TieBaServerException exception) when (exception.Code == 4)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated owned thread fixture '{threadId}' is no longer visible to Threads.GetPostsAsync. Refresh {OnlineTestEnvironmentVariables.SafeAssetsOwnedThreadId} before rerunning thread-read post coverage.");
                    return;
                }

                Assert.IsNotNull(posts);
                Assert.IsNotNull(posts.Page);
                Assert.IsNotNull(posts.Thread);
                Assert.IsNotNull(posts.Forum);
                Assert.AreEqual(threadId, posts.Thread.Tid);
                Assert.IsPositive(posts.Forum.Fid);
                Assert.IsFalse(string.IsNullOrWhiteSpace(posts.Forum.Fname));

                if (fixture.ForumId is { } forumId)
                    Assert.AreEqual((long)forumId, posts.Forum.Fid);

                if (!string.IsNullOrWhiteSpace(fixture.ForumName))
                    Assert.AreEqual(fixture.ForumName, posts.Forum.Fname);

                Assert.IsNotNull(posts.Objs);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetCommentsAsync)]
    public Task GetCommentsAsyncDedicatedOwnedPostReturnsCommentPageShape()
    {
        return ExecuteSafeAsync(
            "thread read comments sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetCommentsAsyncDedicatedOwnedPostReturnsCommentPageShape);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var threadId = RequireOwnedThreadId(scope, operationName);
                var postId = RequireOwnedReplyId(scope, operationName);
                Comments comments;
                try
                {
                    comments = await client.Threads.GetCommentsAsync(threadId, postId, 1);
                }
                catch (TieBaServerException exception) when (exception.Code == 4)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated owned thread/reply fixture ('{threadId}' / '{postId}') is no longer visible to Threads.GetCommentsAsync. Refresh {OnlineTestEnvironmentVariables.SafeAssetsOwnedThreadId} and {OnlineTestEnvironmentVariables.SafeAssetsOwnedReplyId} before rerunning thread-read comment coverage.");
                    return;
                }

                Assert.IsNotNull(comments);
                Assert.IsNotNull(comments.Page);
                Assert.IsNotNull(comments.Thread);
                Assert.IsNotNull(comments.Post);
                Assert.IsNotNull(comments.Forum);
                Assert.AreEqual(threadId, comments.Thread.Tid);
                Assert.AreEqual(postId, comments.Post.Pid);
                Assert.IsPositive(comments.Forum.Fid);
                Assert.IsFalse(string.IsNullOrWhiteSpace(comments.Forum.Fname));

                if (fixture.ForumId is { } forumId)
                    Assert.AreEqual((long)forumId, comments.Forum.Fid);

                if (!string.IsNullOrWhiteSpace(fixture.ForumName))
                    Assert.AreEqual(fixture.ForumName, comments.Forum.Fname);

                Assert.IsNotNull(comments.Objs);
            });
    }

    private static void AssertTabMapShape(IReadOnlyDictionary<string, int> tabMap)
    {
        Assert.IsNotNull(tabMap);
        foreach (var pair in tabMap)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(pair.Key));
            Assert.IsGreaterThanOrEqualTo(0, pair.Value);
        }
    }

    private static void AssertThreadsShape(DedicatedForumFixture fixture, Threads threads)
    {
        Assert.IsNotNull(threads);
        Assert.IsNotNull(threads.Page);
        Assert.IsNotNull(threads.Forum);
        Assert.IsNotNull(threads.TabDictionary);
        Assert.IsPositive(threads.Forum.Fid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(threads.Forum.Fname));

        if (fixture.ForumId is { } forumId)
            Assert.AreEqual((long)forumId, threads.Forum.Fid);

        if (!string.IsNullOrWhiteSpace(fixture.ForumName))
            Assert.AreEqual(fixture.ForumName, threads.Forum.Fname);

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

    private static DedicatedForumFixture RequireDedicatedForumFixture(
        OnlineExecutionScope scope,
        string operationName)
    {
        var forumSelector = !string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumQuery)
            ? scope.Safe.Assets.ForumQuery
            : scope.Safe.Assets.ForumName;

        if (string.IsNullOrWhiteSpace(forumSelector))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: thread-read coverage requires an explicit dedicated safe forum asset. Set {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} instead of relying on a public fallback.");
        }

        return new DedicatedForumFixture(
            forumSelector,
            TryResolveCanonicalForumName(scope.Safe.Assets.ForumName),
            scope.Safe.Assets.ForumId is > 0 ? (ulong)scope.Safe.Assets.ForumId.Value : null);
    }

    private static string RequireDedicatedForumName(
        OnlineExecutionScope scope,
        string operationName)
    {
        if (TryResolveCanonicalForumName(scope.Safe.Assets.ForumName) is { } forumName)
            return forumName;

        Assert.Inconclusive(
            $"Skipping {operationName}: this thread-read path requires a canonical dedicated forum name. Set {OnlineTestEnvironmentVariables.SafeAssetsForumName} to the forum display name rather than a numeric selector before running the scenario.");
        return string.Empty;
    }

    private static ulong RequireDedicatedForumId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.ForumId is > 0)
            return (ulong)scope.Safe.Assets.ForumId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: this thread-read fid-overload path requires a dedicated numeric forum id. Set {OnlineTestEnvironmentVariables.SafeAssetsForumId} before running the scenario.");
        return default;
    }

    private static long RequireOwnedThreadId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.OwnedThreadId is > 0)
            return scope.Safe.Assets.OwnedThreadId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: thread-read post/comment coverage requires a dedicated owned thread id. Set {OnlineTestEnvironmentVariables.SafeAssetsOwnedThreadId} before running the scenario.");
        return default;
    }

    private static long RequireOwnedReplyId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.OwnedReplyId is > 0)
            return scope.Safe.Assets.OwnedReplyId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: thread-read comment coverage requires a dedicated owned reply id. Set {OnlineTestEnvironmentVariables.SafeAssetsOwnedReplyId} before running the scenario.");
        return default;
    }

    private static string? TryResolveCanonicalForumName(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        return ulong.TryParse(candidate, out _)
            ? null
            : candidate;
    }

    private sealed record DedicatedForumFixture(string ForumSelector, string? ForumName, ulong? ForumId);
}
