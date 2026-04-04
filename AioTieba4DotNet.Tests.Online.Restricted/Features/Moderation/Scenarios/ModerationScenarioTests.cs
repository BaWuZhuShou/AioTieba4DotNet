#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Restricted.Features.Moderation.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.RestrictedOrdered)]
[TestCategory(OnlineTestFeatureCategories.Moderation)]
[TestCategory(OnlineTestTierCategories.Restricted)]
[TestCategory(OnlineTestStageCategories.ModerationRestricted)]
[TestCategory(OnlineTestCapabilityCategories.Moderation)]
[TestSubject(typeof(TiebaClient))]
public sealed class ModerationScenarioTests : OnlineRestrictedExecutionTestBase
{
    private const int ReplyLookupPageSize = 30;
    private const int ReplyLookupMaxPages = 5;
    private const int ReplyLookupAttempts = 6;
    private static readonly TimeSpan ReplyLookupDelay = TimeSpan.FromSeconds(2);

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDelPostAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsRecoverAsync)]
    public Task DeleteReplyAndRecoverAsync_DedicatedRestrictedReply_UsesCompensationAudit()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation reply delete lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveModerationContextAsync(
                    scope,
                    client,
                    nameof(DeleteReplyAndRecoverAsync_DedicatedRestrictedReply_UsesCompensationAudit));
                var reply = await FindReplyAsync(client, context.ThreadId, context.ReplyId);

                if (reply is null)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(DeleteReplyAndRecoverAsync_DedicatedRestrictedReply_UsesCompensationAudit)}: restricted moderation reply asset must be visible in the dedicated thread before delete coverage can run. Confirm {OnlineTestEnvironmentVariables.RestrictedAssetsModerationReplyId} belongs to a disposable reply in the thread referenced by {OnlineTestEnvironmentVariables.RestrictedAssetsModerationThreadId}.");
                }

                var deleted = await client.Threads.DelPostAsync(context.ForumName, context.ThreadId, context.ReplyId);
                Assert.IsTrue(deleted, "Expected the restricted moderation reply asset to accept a temporary delete mutation.");

                var deletedArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.ModerationRestricted,
                    "post",
                    $"{context.ThreadId}:{context.ReplyId}",
                    $"restricted moderation delete for dedicated reply {context.ReplyId}");
                scope.Compensation.Register(
                    deletedArtifact,
                    "recover restricted moderation reply",
                    "reply recovered",
                    cancellationToken => RecoverDeletedReplyAsync(client, context, cancellationToken));

                var deletedReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: false);
                Assert.IsNull(deletedReply, "Expected the dedicated restricted reply asset to disappear after delete moderation coverage runs.");

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded, "Expected the restricted moderation scenario to reconcile the deleted reply through compensation.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("reply recovered", audit.CompensationResults[0].CompensationOutcome);
                Assert.AreEqual(OnlineTestStageCategories.ModerationRestricted, audit.CompensationResults[0].StageCategory);

                var restoredReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: true);
                Assert.IsNotNull(restoredReply, "Expected the dedicated restricted reply asset to be restored once compensation completed.");
                Assert.AreEqual(context.ReplyId, restoredReply.Pid);
                Assert.AreEqual(context.ThreadId, restoredReply.Tid);

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(context.ReplyId.ToString(), auditDisplay);
                Assert.Contains("reply recovered", auditDisplay);
                Assert.Contains("unreconciled: none", auditDisplay);
            });
    }

    private static TiebaClient CreateClient(OnlineExecutionScope scope)
    {
        var options = new TiebaOptions
        {
            Bduss = scope.Restricted.Account.Bduss,
            Stoken = scope.Restricted.Account.Stoken,
            TransportMode = TiebaTransportMode.Http
        };

        return new TiebaClient(options);
    }

    private static async Task<ModerationContext> ResolveModerationContextAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        var forumName = RequireModerationForumName(scope, operationName);
        var threadId = RequireModerationThreadId(scope, operationName);
        var replyId = RequireModerationReplyId(scope, operationName);

        var snapshot = await client.Threads.GetPostsAsync(threadId, 1, 1, PostSortType.Desc);
        Assert.IsNotNull(snapshot);
        Assert.IsNotNull(snapshot.Thread);
        Assert.AreEqual(threadId, snapshot.Thread.Tid);

        return new ModerationContext(forumName, threadId, replyId);
    }

    private static string RequireModerationForumName(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Restricted.Assets.ModerationForumName))
            return scope.Restricted.Assets.ModerationForumName;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted moderation execution requires a dedicated forum asset. Set {OnlineTestEnvironmentVariables.RestrictedAssetsModerationForumName} before attempting reply moderation.");
        return string.Empty;
    }

    private static long RequireModerationThreadId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Restricted.Assets.ModerationThreadId is > 0)
            return scope.Restricted.Assets.ModerationThreadId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted moderation execution requires a dedicated thread asset. Set {OnlineTestEnvironmentVariables.RestrictedAssetsModerationThreadId} before attempting reply moderation.");
        return default;
    }

    private static long RequireModerationReplyId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Restricted.Assets.ModerationReplyId is > 0)
            return scope.Restricted.Assets.ModerationReplyId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted moderation execution requires a dedicated reply asset. Set {OnlineTestEnvironmentVariables.RestrictedAssetsModerationReplyId} before attempting reply moderation.");
        return default;
    }

    private static async Task<Post?> FindReplyAsync(TiebaClient client, long threadId, long replyId)
    {
        for (var page = 1; page <= ReplyLookupMaxPages; page++)
        {
            var posts = await client.Threads.GetPostsAsync(threadId, page, ReplyLookupPageSize, PostSortType.Desc);
            var match = posts.Objs.FirstOrDefault(post => post.Pid == replyId);
            if (match is not null)
                return match;

            if (!posts.HasMore)
                break;
        }

        return null;
    }

    private static async Task<Post?> WaitForReplyStateAsync(TiebaClient client, long threadId, long replyId, bool shouldExist)
    {
        for (var attempt = 0; attempt < ReplyLookupAttempts; attempt++)
        {
            var reply = await FindReplyAsync(client, threadId, replyId);
            if ((reply is not null) == shouldExist)
                return reply;

            if (attempt < ReplyLookupAttempts - 1)
                await Task.Delay(ReplyLookupDelay);
        }

        return shouldExist ? null : await FindReplyAsync(client, threadId, replyId);
    }

    private static async ValueTask RecoverDeletedReplyAsync(
        TiebaClient client,
        ModerationContext context,
        CancellationToken cancellationToken)
    {
        var recovered = await client.Threads.RecoverAsync(context.ForumName, context.ThreadId, context.ReplyId, false, cancellationToken);
        if (!recovered)
            throw new InvalidOperationException($"Expected to recover restricted moderation reply {context.ReplyId} in thread {context.ThreadId}.");
    }

    private sealed record ModerationContext(string ForumName, long ThreadId, long ReplyId);
}
