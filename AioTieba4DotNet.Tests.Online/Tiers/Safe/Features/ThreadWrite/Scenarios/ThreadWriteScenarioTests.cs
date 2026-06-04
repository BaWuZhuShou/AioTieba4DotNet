#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Tiers.Safe.Features.ThreadWrite.Scenarios;

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
    public Task AddPostAsyncDedicatedRootThreadCreatesDisposableReplyAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "thread write add reply lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(AddPostAsyncDedicatedRootThreadCreatesDisposableReplyAndPublishesCompensationAudit);
                var context = ResolveDisposableRootThread(scope, operationName);
                var replyMarker = CreateReplyMarker();

                // Irreducible exception: Threads.AddPostAsync does not expose the created pid, so the safe scenario
                // must perform one bounded Threads.GetPostsAsync lookup to register truthful deletion compensation.
                var createdPost = await CreateDisposableReplyAsync(client, context, replyMarker);

                RegisterReplyDeletionCompensation(scope, client, context, replyMarker, createdPost.Pid, "disposable thread-write reply");

                await scope.Compensation.ExecuteAsync();
                AssertReplyAudit(scope, replyMarker, "reply deleted", expectedArtifactTypes: ["post"]);

                var deletedPost = await FindDisposableReplyAsync(client, context.ThreadId, replyMarker);
                Assert.IsNull(deletedPost, "Expected the disposable reply to be deleted once compensation completed.");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsAgreeAsync)]
    public Task AgreeAsyncDedicatedRootThreadRootPostUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "thread write agree lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(AgreeAsyncDedicatedRootThreadRootPostUsesCompensationAudit);
                var context = ResolveDisposableRootThread(scope, operationName);

                var agreeSucceeded = await client.Threads.AgreeAsync(context.ThreadId, context.RootPostId);
                if (!agreeSucceeded)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated root post '{context.RootPostId}' did not accept the agree mutation. Reconfigure the owned root thread so its root post can truthfully prove Threads.AgreeAsync in this environment.");
                }

                RegisterReplyMutationCompensation(
                    scope,
                    "post-agree",
                    $"{context.ThreadId}:{context.RootPostId}",
                    $"temporary agree on dedicated thread-write root post '{context.RootPostId}'",
                    "undo dedicated thread-write root-post agree",
                    "post unagreed",
                    cancellationToken => EnsureReplyUnagreedAsync(client, context.ThreadId, context.RootPostId, cancellationToken));

                await scope.Compensation.ExecuteAsync();
                AssertReplyAudit(scope, $"{context.ThreadId}:{context.RootPostId}", ["post unagreed"], expectedArtifactTypes: ["post-agree"]);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsUnagreeAsync)]
    public Task UnagreeAsyncDedicatedRootThreadRootPostRestoresSetupAgreeAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "thread write unagree lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(UnagreeAsyncDedicatedRootThreadRootPostRestoresSetupAgreeAndPublishesCompensationAudit);
                var context = ResolveDisposableRootThread(scope, operationName);
                var unagreeSucceeded = await client.Threads.UnagreeAsync(context.ThreadId, context.RootPostId);
                if (!unagreeSucceeded)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated root post '{context.RootPostId}' did not accept the unagree mutation. Reconfigure the owned root thread so its root post starts agreed by the safe account and can truthfully prove Threads.UnagreeAsync in this environment.");
                }

                RegisterReplyMutationCompensation(
                    scope,
                    "post-agree",
                    $"{context.ThreadId}:{context.RootPostId}",
                    $"temporary unagree on dedicated thread-write root post '{context.RootPostId}'",
                    "restore dedicated thread-write root-post agree",
                    "post re-agreed",
                    cancellationToken => EnsurePostAgreedAsync(client, context.ThreadId, context.RootPostId, cancellationToken));

                await scope.Compensation.ExecuteAsync();
                AssertReplyAudit(scope, $"{context.ThreadId}:{context.RootPostId}", ["post re-agreed"], expectedArtifactTypes: ["post-agree"]);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDisagreeAsync)]
    public Task DisagreeAsyncDedicatedRootThreadRootPostUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "thread write disagree lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(DisagreeAsyncDedicatedRootThreadRootPostUsesCompensationAudit);
                var context = ResolveDisposableRootThread(scope, operationName);

                var disagreeSucceeded = await client.Threads.DisagreeAsync(context.ThreadId, context.RootPostId);
                if (!disagreeSucceeded)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated root post '{context.RootPostId}' did not accept the disagree mutation. Reconfigure the owned root thread so its root post can truthfully prove Threads.DisagreeAsync in this environment.");
                }

                RegisterReplyMutationCompensation(
                    scope,
                    "post-disagree",
                    $"{context.ThreadId}:{context.RootPostId}",
                    $"temporary disagree on dedicated thread-write root post '{context.RootPostId}'",
                    "undo dedicated thread-write root-post disagree",
                    "post disagree reverted",
                    cancellationToken => EnsureReplyUndisagreedAsync(client, context.ThreadId, context.RootPostId, cancellationToken));

                await scope.Compensation.ExecuteAsync();
                AssertReplyAudit(scope, $"{context.ThreadId}:{context.RootPostId}", ["post disagree reverted"], expectedArtifactTypes: ["post-disagree"]);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsUndisagreeAsync)]
    public Task UndisagreeAsyncDedicatedRootThreadRootPostRestoresSetupDisagreeAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "thread write undisagree lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(UndisagreeAsyncDedicatedRootThreadRootPostRestoresSetupDisagreeAndPublishesCompensationAudit);
                var context = ResolveDisposableRootThread(scope, operationName);
                var undisagreeSucceeded = await client.Threads.UndisagreeAsync(context.ThreadId, context.RootPostId, false);
                if (!undisagreeSucceeded)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated root post '{context.RootPostId}' did not accept the undisagree mutation. Reconfigure the owned root thread so its root post starts disagreed by the safe account and can truthfully prove Threads.UndisagreeAsync in this environment.");
                }

                RegisterReplyMutationCompensation(
                    scope,
                    "post-disagree",
                    $"{context.ThreadId}:{context.RootPostId}",
                    $"temporary undisagree on dedicated thread-write root post '{context.RootPostId}'",
                    "restore dedicated thread-write root-post disagree",
                    "post re-disagreed",
                    cancellationToken => EnsurePostDisagreedAsync(client, context.ThreadId, context.RootPostId, cancellationToken));

                await scope.Compensation.ExecuteAsync();
                AssertReplyAudit(scope, $"{context.ThreadId}:{context.RootPostId}", ["post re-disagreed"], expectedArtifactTypes: ["post-disagree"]);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDelPostAsync)]
    public Task DelPostAsyncDedicatedRootThreadDeletesDisposableReplyAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "thread write delete reply lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(DelPostAsyncDedicatedRootThreadDeletesDisposableReplyAndPublishesCompensationAudit);
                var context = ResolveDisposableRootThread(scope, operationName);
                var replyMarker = CreateReplyMarker();

                // Smallest remaining exception: Threads.DelPostAsync needs a concrete reply id, and the safe fixture
                // contract does not provide a disposable, restorable reply asset. Creating one disposable reply first
                // is the narrowest truthful setup that preserves repeatable cleanup.
                var createdPost = await CreateDisposableReplyAsync(client, context, replyMarker);

                RegisterReplyDeletionCompensation(scope, client, context, replyMarker, createdPost.Pid, "disposable thread-write reply");

                var deleted = await client.Threads.DelPostAsync(context.ForumName, context.ThreadId, createdPost.Pid);
                Assert.IsTrue(deleted, "Expected the dedicated disposable reply to accept a direct delete mutation.");

                await scope.Compensation.ExecuteAsync();
                AssertReplyAudit(scope, replyMarker, "reply deleted", expectedArtifactTypes: ["post"]);

                var deletedPost = await FindDisposableReplyAsync(client, context.ThreadId, replyMarker);
                Assert.IsNull(deletedPost, "Expected the disposable reply to be deleted once the direct delete mutation completed.");
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

    private static void AssertReplyAudit(
        OnlineExecutionScope scope,
        string replyMarker,
        string expectedOutcome,
        string[] expectedArtifactTypes)
    {
        AssertReplyAudit(scope, replyMarker, [expectedOutcome], expectedArtifactTypes);
    }

    private static void AssertReplyAudit(
        OnlineExecutionScope scope,
        string replyMarker,
        string[] expectedOutcomes,
        string[] expectedArtifactTypes)
    {
        var audit = scope.Compensation.GetLastAudit();
        Assert.IsNotNull(audit);
        Assert.IsTrue(audit.Succeeded, "Expected the ThreadWrite safe scenario to reconcile the disposable reply artifacts.");
        Assert.HasCount(expectedArtifactTypes.Length, audit.RecordedArtifacts);
        Assert.HasCount(expectedOutcomes.Length, audit.CompensationResults);
        Assert.IsEmpty(audit.UnreconciledArtifacts);

        for (var index = 0; index < expectedOutcomes.Length; index++)
        {
            Assert.AreEqual(expectedOutcomes[index], audit.CompensationResults[index].CompensationOutcome);
        }

        foreach (var artifactType in expectedArtifactTypes)
        {
            Assert.IsTrue(
                audit.RecordedArtifacts.Any(artifact => artifact.ArtifactType == artifactType),
                $"Expected the compensation audit to record the '{artifactType}' artifact.");
        }

        var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains(replyMarker, auditDisplay);
        Assert.Contains("unreconciled: none", auditDisplay);
    }

    private static void RegisterReplyDeletionCompensation(
        OnlineExecutionScope scope,
        TiebaClient client,
        DisposableRootThreadContext context,
        string replyMarker,
        long postId,
        string descriptionPrefix)
    {
        var createdArtifact = scope.Compensation.RecordCreatedArtifact(
            OnlineTestStageCategories.ThreadWrite,
            "post",
            postId,
            $"{descriptionPrefix} '{replyMarker}'");
        scope.Compensation.Register(
            createdArtifact,
            "delete disposable thread-write reply",
            "reply deleted",
            cancellationToken => EnsureReplyDeletedAsync(client, context, replyMarker, cancellationToken));
    }

    private static void RegisterReplyMutationCompensation(
        OnlineExecutionScope scope,
        string artifactType,
        string artifactId,
        string description,
        string compensationDescription,
        string compensationOutcome,
        Func<CancellationToken, ValueTask> compensation)
    {
        var mutatedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.ThreadWrite,
            artifactType,
            artifactId,
            description);
        scope.Compensation.Register(
            mutatedArtifact,
            compensationDescription,
            compensationOutcome,
            compensation);
    }

    private static DisposableRootThreadContext ResolveDisposableRootThread(
        OnlineExecutionScope scope,
        string operationName)
    {
        if (scope.Safe.Assets.OwnedThreadId is not > 0)
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: safe thread-write execution requires a dedicated root thread asset that is managed outside ordered suite prewarming. Set {OnlineTestEnvironmentVariables.SafeAssetsOwnedThreadId} to a disposable root thread for the safe account.");
        }

        if (scope.Safe.Assets.OwnedRootPostId is not > 0)
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: safe thread-write root-post coverage requires a dedicated owned root post id. Set {OnlineTestEnvironmentVariables.SafeAssetsOwnedRootPostId} so root-post mutations do not derive prerequisites through Threads.GetPostsAsync.");
        }

        if (string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumName))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: safe thread-write mutations require the canonical dedicated forum name. Set {OnlineTestEnvironmentVariables.SafeAssetsForumName} so thread-write calls can target the disposable root thread without forum discovery reads.");
        }

        return new DisposableRootThreadContext(
            scope.Safe.Assets.ForumName,
            scope.Safe.Assets.OwnedThreadId!.Value,
            scope.Safe.Assets.OwnedRootPostId!.Value);
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

    private static async ValueTask EnsureReplyDeletedAsync(
        TiebaClient client,
        DisposableRootThreadContext context,
        string replyMarker,
        CancellationToken cancellationToken)
    {
        var existingReply = await FindDisposableReplyAsync(client, context.ThreadId, replyMarker);
        if (existingReply is null)
            return;

        var deleted = await client.Threads.DelPostAsync(context.ForumName, context.ThreadId, existingReply.Pid, cancellationToken);
        if (!deleted)
            throw new InvalidOperationException($"Expected to delete disposable thread-write reply {existingReply.Pid} from thread {context.ThreadId}.");
    }

    private static async ValueTask EnsureReplyUnagreedAsync(
        TiebaClient client,
        long threadId,
        long postId,
        CancellationToken cancellationToken)
    {
        _ = await client.Threads.UnagreeAsync(threadId, postId, false, cancellationToken);
    }

    private static async ValueTask EnsurePostAgreedAsync(
        TiebaClient client,
        long threadId,
        long postId,
        CancellationToken cancellationToken)
    {
        var agreed = await client.Threads.AgreeAsync(threadId, postId);
        if (!agreed)
        {
            throw new InvalidOperationException($"Expected to re-agree post {postId} in thread {threadId} during compensation.");
        }
    }

    private static async ValueTask EnsureReplyUndisagreedAsync(
        TiebaClient client,
        long threadId,
        long postId,
        CancellationToken cancellationToken)
    {
        _ = await client.Threads.UndisagreeAsync(threadId, postId, false, cancellationToken);
    }

    private static async ValueTask EnsurePostDisagreedAsync(
        TiebaClient client,
        long threadId,
        long postId,
        CancellationToken cancellationToken)
    {
        var disagreed = await client.Threads.DisagreeAsync(threadId, postId);
        if (!disagreed)
        {
            throw new InvalidOperationException($"Expected to re-disagree post {postId} in thread {threadId} during compensation.");
        }
    }

    private static string CreateReplyMarker()
    {
        return $"safethreadwrite{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}";
    }

    private sealed record DisposableRootThreadContext(string ForumName, long ThreadId, long RootPostId);
}
