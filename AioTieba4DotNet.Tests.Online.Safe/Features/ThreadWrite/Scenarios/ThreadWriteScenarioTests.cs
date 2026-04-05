#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Safe.Features.ThreadWrite.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.ThreadWrite)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.ThreadWrite)]
[TestSubject(typeof(TiebaClient))]
public sealed class ThreadWriteScenarioTests : OnlineSafeExecutionTestBase
{
    private const int ReplyLookupPageSize = 30;
    private const int ReplyLookupHeadPages = 5;
    private const int ReplyLookupTailPages = 3;
    private const int PostLookupAttempts = 6;
    private static readonly TimeSpan PostLookupDelay = TimeSpan.FromSeconds(2);

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsAddPostAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsAgreeAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsDisagreeAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsDelPostAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsUnagreeAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsUndisagreeAsync)]
    public Task AddPostAgreeAndDisagreeAsyncDedicatedRootThreadUsesDisposableReplyAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "thread write disposable reply lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDisposableRootThreadAsync(
                    scope,
                    client,
                    nameof(AddPostAgreeAndDisagreeAsyncDedicatedRootThreadUsesDisposableReplyAndPublishesCompensationAudit));
                var replyMarker = CreateReplyMarker();

                var createdPost = await CreateDisposableReplyAsync(client, context, replyMarker);
                var createdArtifact = scope.Compensation.RecordCreatedArtifact(
                    OnlineTestStageCategories.ThreadWrite,
                    "post",
                    createdPost.Pid,
                    $"disposable thread-write reply '{replyMarker}'");
                scope.Compensation.Register(
                    createdArtifact,
                    "delete disposable thread-write reply",
                    "reply deleted",
                    cancellationToken => DeleteDisposableReplyAsync(client, context, createdPost.Pid, cancellationToken));

                var agreeSucceeded = await client.Threads.AgreeAsync(context.ThreadId, createdPost.Pid);
                Assert.IsTrue(agreeSucceeded, "Expected the dedicated disposable reply to accept a temporary agree mutation.");
                var unagreeSucceeded = await client.Threads.UnagreeAsync(context.ThreadId, createdPost.Pid);
                Assert.IsTrue(unagreeSucceeded,
                    "Expected the dedicated disposable reply to accept a direct unagree call after the temporary agree mutation.");

                var disagreeSucceeded = await client.Threads.DisagreeAsync(context.ThreadId, createdPost.Pid);
                Assert.IsTrue(disagreeSucceeded,
                    "Expected the dedicated disposable reply to accept a temporary disagree mutation once the agree mutation was undone.");
                var mutatedArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.ThreadWrite,
                    "post-disagree",
                    $"{context.ThreadId}:{createdPost.Pid}",
                    $"temporary disagree on disposable thread-write reply '{replyMarker}'");
                scope.Compensation.Register(
                    mutatedArtifact,
                    "undo disposable thread-write reply disagree",
                    "reply disagree reverted",
                    cancellationToken => UndoDisagreeAsync(client, context.ThreadId, createdPost.Pid, cancellationToken));

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded, "Expected the ThreadWrite safe scenario to reconcile all disposable reply artifacts.");
                Assert.HasCount(2, audit.RecordedArtifacts);
                Assert.HasCount(2, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("reply disagree reverted", audit.CompensationResults[0].CompensationOutcome);
                Assert.AreEqual("reply deleted", audit.CompensationResults[1].CompensationOutcome);
                Assert.IsTrue(
                    audit.RecordedArtifacts.Any(static artifact => artifact.ArtifactType == "post"),
                    "Expected the compensation audit to record the created disposable reply artifact.");
                Assert.IsTrue(
                    audit.RecordedArtifacts.Any(static artifact => artifact.ArtifactType == "post-disagree"),
                    "Expected the compensation audit to record the temporary disagree mutation against the disposable reply.");

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(replyMarker, auditDisplay);
                Assert.Contains("unreconciled: none", auditDisplay);

                var deletedPost = await FindDisposableReplyAsync(client, context.ThreadId, replyMarker);
                Assert.IsNull(deletedPost, "Expected the disposable reply to be deleted once compensation completed.");
            },
            OnlineExecutionCapability.Authenticated);
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

    private static async Task<DisposableRootThreadContext> ResolveDisposableRootThreadAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        if (scope.Safe.Assets.OwnedThreadId is not > 0)
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: safe thread-write execution requires a dedicated root thread asset that is managed outside ordered suite prewarming. Set {OnlineTestEnvironmentVariables.SafeAssetsOwnedThreadId} to a disposable root thread for the safe account.");
        }

        var threadId = scope.Safe.Assets.OwnedThreadId!.Value;
        var snapshot = await client.Threads.GetPostsAsync(threadId, 1, 1, PostSortType.Desc);

        Assert.IsNotNull(snapshot);
        Assert.IsNotNull(snapshot.Thread);
        Assert.IsNotNull(snapshot.Forum);
        Assert.AreEqual(threadId, snapshot.Thread.Tid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(snapshot.Forum.Fname));

        return new DisposableRootThreadContext(snapshot.Forum.Fname, threadId);
    }

    private static async Task<Post> CreateDisposableReplyAsync(
        TiebaClient client,
        DisposableRootThreadContext context,
        string replyMarker)
    {
        var addSucceeded = await client.Threads.AddPostAsync(context.ForumName, context.ThreadId, replyMarker);
        Assert.IsTrue(addSucceeded, "Expected the dedicated root thread to accept a disposable reply for safe ThreadWrite coverage.");

        var createdPost = await FindDisposableReplyAsync(client, context.ThreadId, replyMarker);
        Assert.IsNotNull(createdPost, "Expected to locate the disposable reply after it was created so cleanup could be registered immediately.");
        Assert.AreEqual(context.ThreadId, createdPost.Tid);
        Assert.Contains(replyMarker, createdPost.Text);
        return createdPost;
    }

    private static async Task<Post?> FindDisposableReplyAsync(TiebaClient client, long threadId, string replyMarker)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(replyMarker);

        for (var attempt = 0; attempt < PostLookupAttempts; attempt++)
        {
            var match = await FindDisposableReplyOnceAsync(client, threadId, replyMarker);
            if (match is not null)
                return match;

            if (attempt < PostLookupAttempts - 1)
                await Task.Delay(PostLookupDelay);
        }

        return null;
    }

    private static async Task<Post?> FindDisposableReplyOnceAsync(TiebaClient client, long threadId, string replyMarker)
    {
        var recentPosts = await client.Threads.GetPostsAsync(threadId, 1, ReplyLookupPageSize, PostSortType.Desc);
        var recentMatch = FindReplyByMarker(recentPosts, replyMarker);
        if (recentMatch is not null)
            return recentMatch;

        var headPageLimit = Math.Min(Math.Max(recentPosts.Page.TotalPage, 1), ReplyLookupHeadPages);
        for (var page = 2; page <= headPageLimit; page++)
        {
            var headPage = await client.Threads.GetPostsAsync(threadId, page, ReplyLookupPageSize, PostSortType.Desc);
            var headMatch = FindReplyByMarker(headPage, replyMarker);
            if (headMatch is not null)
                return headMatch;
        }

        var tailPageStart = Math.Max(1, recentPosts.Page.TotalPage - ReplyLookupTailPages + 1);
        for (var page = tailPageStart; page <= recentPosts.Page.TotalPage; page++)
        {
            if (page <= headPageLimit)
                continue;

            var tailPage = await client.Threads.GetPostsAsync(threadId, page, ReplyLookupPageSize, PostSortType.Asc);
            var tailMatch = FindReplyByMarker(tailPage, replyMarker);
            if (tailMatch is not null)
                return tailMatch;
        }

        return null;
    }

    private static Post? FindReplyByMarker(Posts posts, string replyMarker)
    {
        ArgumentNullException.ThrowIfNull(posts);

        return posts.Objs.FirstOrDefault(post => post.Text.Contains(replyMarker, StringComparison.Ordinal));
    }

    private static async ValueTask DeleteDisposableReplyAsync(
        TiebaClient client,
        DisposableRootThreadContext context,
        long postId,
        CancellationToken cancellationToken)
    {
        var deleted = await client.Threads.DelPostAsync(context.ForumName, context.ThreadId, postId, cancellationToken);
        if (!deleted)
            throw new InvalidOperationException($"Expected to delete disposable thread-write reply {postId} from thread {context.ThreadId}.");
    }

    private static async ValueTask UndoDisagreeAsync(
        TiebaClient client,
        long threadId,
        long postId,
        CancellationToken cancellationToken)
    {
        var undisagreed = await client.Threads.UndisagreeAsync(threadId, postId, false, cancellationToken);
        if (!undisagreed)
            throw new InvalidOperationException($"Expected to undo the temporary disagree mutation on disposable thread-write reply {postId}.");
    }

    private static string CreateReplyMarker()
    {
        return $"safethreadwrite{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}";
    }

    private sealed record DisposableRootThreadContext(string ForumName, long ThreadId);
}
