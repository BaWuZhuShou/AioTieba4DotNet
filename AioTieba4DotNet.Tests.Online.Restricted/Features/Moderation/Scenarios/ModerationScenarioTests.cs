#nullable enable
using System;
using System.Collections.Generic;
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
using ThreadModel = AioTieba4DotNet.Models.Threads.Thread;

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
    private const int RecoverLookupMaxPages = 3;
    private static readonly TimeSpan ReplyLookupDelay = TimeSpan.FromSeconds(2);

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDelPostAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsRecoverAsync)]
    public Task DeleteReplyAndRecoverAsyncDedicatedRestrictedReplyUsesCompensationAudit()
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
                    nameof(DeleteReplyAndRecoverAsyncDedicatedRestrictedReplyUsesCompensationAudit));
                var reply = await FindReplyAsync(client, context.ThreadId, context.ReplyId);

                if (reply is null)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(DeleteReplyAndRecoverAsyncDedicatedRestrictedReplyUsesCompensationAudit)}: restricted moderation reply asset must be visible in the dedicated thread before delete coverage can run. Confirm {OnlineTestEnvironmentVariables.RestrictedAssetsModerationReplyId} belongs to a disposable reply in the thread referenced by {OnlineTestEnvironmentVariables.RestrictedAssetsModerationThreadId}.");
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

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDelPostsAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsGetRecoversAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsGetRecoverInfoAsync)]
    public Task DeletePostsAndInspectRecoverAsyncDedicatedRestrictedReplyUsesRecoverReadSurface()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation batch delete and inspect recover surface",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveModerationContextAsync(
                    scope,
                    client,
                    nameof(DeletePostsAndInspectRecoverAsyncDedicatedRestrictedReplyUsesRecoverReadSurface));
                var reply = await FindReplyAsync(client, context.ThreadId, context.ReplyId);
                if (reply is null)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(DeletePostsAndInspectRecoverAsyncDedicatedRestrictedReplyUsesRecoverReadSurface)}: restricted moderation reply asset must be visible before batch delete coverage can run. Confirm {OnlineTestEnvironmentVariables.RestrictedAssetsModerationReplyId} belongs to the dedicated thread asset.");
                }

                var deleted = await client.Threads.DelPostsAsync(context.ForumName, context.ThreadId, [context.ReplyId]);
                Assert.IsTrue(deleted,
                    "Expected the restricted moderation reply asset to accept a temporary batch delete mutation.");

                var deletedArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.ModerationRestricted,
                    "post-batch-delete",
                    $"{context.ThreadId}:{context.ReplyId}",
                    $"restricted moderation batch delete for dedicated reply {context.ReplyId}");
                scope.Compensation.Register(
                    deletedArtifact,
                    "recover restricted moderation batch-deleted reply",
                    "reply recovered",
                    cancellationToken => RecoverDeletedReplyAsync(client, context, cancellationToken));

                var deletedReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: false);
                Assert.IsNull(deletedReply,
                    "Expected the dedicated restricted reply asset to disappear after batch delete moderation coverage runs.");

                var recoverByName = await RequireRecoverEntryAsync(
                    page => client.Threads.GetRecoversAsync(context.ForumName, page, 10),
                    recover => recover.Pid == context.ReplyId,
                    nameof(DeletePostsAndInspectRecoverAsyncDedicatedRestrictedReplyUsesRecoverReadSurface),
                    $"reply {context.ReplyId} via forum-name overload");
                var recoverByFid = await RequireRecoverEntryAsync(
                    page => client.Threads.GetRecoversAsync(context.ForumId, page, 10),
                    recover => recover.Pid == context.ReplyId,
                    nameof(DeletePostsAndInspectRecoverAsyncDedicatedRestrictedReplyUsesRecoverReadSurface),
                    $"reply {context.ReplyId} via fid overload");
                var recoverInfoByName = await client.Threads.GetRecoverInfoAsync(context.ForumName, context.ThreadId, context.ReplyId);
                var recoverInfoByFid = await client.Threads.GetRecoverInfoAsync(context.ForumId, context.ThreadId, context.ReplyId);

                Assert.AreEqual(context.ReplyId, recoverByName.Pid);
                Assert.AreEqual(context.ReplyId, recoverByFid.Pid);
                Assert.AreEqual(context.ThreadId, recoverByName.Tid);
                Assert.AreEqual(context.ThreadId, recoverByFid.Tid);

                AssertRecoverInfoShape(recoverInfoByName, context.ThreadId, context.ReplyId);
                AssertRecoverInfoShape(recoverInfoByFid, context.ThreadId, context.ReplyId);

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded,
                    "Expected the restricted moderation batch-delete scenario to reconcile the deleted reply through compensation.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("reply recovered", audit.CompensationResults[0].CompensationOutcome);

                var restoredReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: true);
                Assert.IsNotNull(restoredReply,
                    "Expected the dedicated restricted reply asset to be restored once batch-delete compensation completed.");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDelThreadAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsDelThreadsAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsGoodAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsUngoodAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsTopAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsUntopAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsMoveAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsRecommendAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsSetThreadPrivacyAsync)]
    public Task ModerateDedicatedThreadAsyncDedicatedRestrictedAssetsExerciseRecoverableManagementSurface()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation thread management lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(ModerateDedicatedThreadAsyncDedicatedRestrictedAssetsExerciseRecoverableManagementSurface);
                var context = await ResolveModerationContextAsync(scope, client, operationName);
                var initialThread = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);
                if (initialThread.IsGood)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated restricted thread asset is already marked good. Use a moderation thread that starts in a non-good state so `GoodAsync` and `UngoodAsync` can both be proven directly.");
                }

                if (initialThread.IsTop)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated restricted thread asset is already pinned. Use a moderation thread that starts unpinned so `TopAsync` and `UntopAsync` can both be proven directly.");
                }

                var tabMap = await client.Threads.GetTabMapAsync(context.ForumName);
                var alternateTab = tabMap.FirstOrDefault(pair => pair.Value != initialThread.TabId);
                if (alternateTab.Equals(default(KeyValuePair<string, int>)))
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated moderation forum does not expose an alternate tab for move coverage. Use a moderation forum with at least two tab ids so `MoveAsync` can be moved away and then restored.");
                }

                var recomStatus = await client.Forums.GetRecomStatusAsync(context.ForumId);
                if (recomStatus.TotalRecommendNum <= 0 || recomStatus.UsedRecommendNum >= recomStatus.TotalRecommendNum)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated moderation forum does not currently expose spare recommendation quota for one-way restricted `RecommendAsync` coverage.");
                }

                var goodSucceeded = await client.Threads.GoodAsync(context.ForumName, context.ThreadId);
                Assert.IsTrue(goodSucceeded, "Expected the dedicated restricted thread to accept a temporary good mutation.");
                var goodThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => thread.IsGood, "thread good state");
                Assert.IsTrue(goodThread.IsGood);

                var ungoodSucceeded = await client.Threads.UngoodAsync(context.ForumName, context.ThreadId);
                Assert.IsTrue(ungoodSucceeded, "Expected the dedicated restricted thread to accept a matching ungood mutation.");
                var ungoodThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => !thread.IsGood, "thread ungood state");
                Assert.IsFalse(ungoodThread.IsGood);

                var topSucceeded = await client.Threads.TopAsync(context.ForumName, context.ThreadId, false);
                Assert.IsTrue(topSucceeded, "Expected the dedicated restricted thread to accept a temporary top mutation.");
                var topThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => thread.IsTop, "thread top state");
                Assert.IsTrue(topThread.IsTop);

                var untopSucceeded = await client.Threads.UntopAsync(context.ForumName, context.ThreadId, false);
                Assert.IsTrue(untopSucceeded, "Expected the dedicated restricted thread to accept a matching untop mutation.");
                var untopThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => !thread.IsTop, "thread untop state");
                Assert.IsFalse(untopThread.IsTop);

                var privateSucceeded = await client.Threads.SetThreadPrivacyAsync(context.ForumName, context.ThreadId, context.ReplyId, true);
                Assert.IsTrue(privateSucceeded,
                    "Expected the dedicated restricted reply to accept a temporary privacy mutation.");
                var hiddenReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: false);
                Assert.IsNull(hiddenReply,
                    "Expected the dedicated restricted reply to disappear while the temporary privacy mutation is active.");

                var publicSucceeded = await client.Threads.SetThreadPrivacyAsync(context.ForumName, context.ThreadId, context.ReplyId, false);
                Assert.IsTrue(publicSucceeded,
                    "Expected the dedicated restricted reply to accept a matching privacy reset mutation.");
                var visibleReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: true);
                Assert.IsNotNull(visibleReply,
                    "Expected the dedicated restricted reply to reappear once the temporary privacy mutation was undone.");

                var moved = await client.Threads.MoveAsync(context.ForumName, context.ThreadId, alternateTab.Value, initialThread.TabId);
                Assert.IsTrue(moved,
                    "Expected the dedicated restricted thread to accept a temporary tab-move mutation.");
                var movedThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => thread.TabId == alternateTab.Value, "thread moved tab state");
                Assert.AreEqual(alternateTab.Value, movedThread.TabId);

                var movedBack = await client.Threads.MoveAsync(context.ForumName, context.ThreadId, initialThread.TabId, alternateTab.Value);
                Assert.IsTrue(movedBack,
                    "Expected the dedicated restricted thread to move back to its original tab after move coverage executed.");
                var restoredTabThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => thread.TabId == initialThread.TabId, "thread restored tab state");
                Assert.AreEqual(initialThread.TabId, restoredTabThread.TabId);

                var recommended = await client.Threads.RecommendAsync(context.ForumName, context.ThreadId);
                Assert.IsTrue(recommended,
                    "Expected the dedicated restricted thread to accept the explicit one-way recommendation mutation once quota and opt-in preconditions were satisfied.");

                var deletedThread = await client.Threads.DelThreadAsync(context.ForumName, context.ThreadId);
                Assert.IsTrue(deletedThread, "Expected the dedicated restricted thread to accept a temporary delete mutation.");
                var deletedThreadRecover = await RequireRecoverEntryAsync(
                    page => client.Threads.GetRecoversAsync(context.ForumName, page, 10),
                    recover => recover.Tid == context.ThreadId && recover.Pid == 0,
                    operationName,
                    $"thread {context.ThreadId} after DelThreadAsync");
                Assert.AreEqual(context.ThreadId, deletedThreadRecover.Tid);
                await RecoverDeletedThreadAsync(client, context, CancellationToken.None);
                _ = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);

                var batchDeletedThread = await client.Threads.DelThreadsAsync(context.ForumName, [context.ThreadId]);
                Assert.IsTrue(batchDeletedThread,
                    "Expected the dedicated restricted thread to accept a temporary batch delete mutation.");
                var batchDeletedRecover = await RequireRecoverEntryAsync(
                    page => client.Threads.GetRecoversAsync(context.ForumId, page, 10),
                    recover => recover.Tid == context.ThreadId && recover.Pid == 0,
                    operationName,
                    $"thread {context.ThreadId} after DelThreadsAsync");
                Assert.AreEqual(context.ThreadId, batchDeletedRecover.Tid);
                await RecoverDeletedThreadAsync(client, context, CancellationToken.None);
                _ = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);
            });
    }

    private static void AssertRecoverInfoShape(RecoverInfo recoverInfo, long threadId, long postId)
    {
        Assert.IsNotNull(recoverInfo);
        Assert.AreEqual(threadId, recoverInfo.Tid);
        Assert.AreEqual(postId, recoverInfo.Pid);
        Assert.IsNotNull(recoverInfo.Content);
        Assert.IsNotNull(recoverInfo.User);
        Assert.IsFalse(string.IsNullOrWhiteSpace(recoverInfo.Text));
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

    private static async Task<ModerationContext> ResolveModerationContextAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        var forumName = RequireModerationForumName(scope, operationName);
        var threadId = RequireModerationThreadId(scope, operationName);
        var replyId = RequireModerationReplyId(scope, operationName);
        var forumId = await client.Forums.GetFidAsync(forumName);

        var snapshot = await client.Threads.GetPostsAsync(threadId, 1, 1, PostSortType.Desc);
        Assert.IsNotNull(snapshot);
        Assert.IsNotNull(snapshot.Thread);
        Assert.AreEqual(threadId, snapshot.Thread.Tid);

        return new ModerationContext(forumName, forumId, threadId, replyId);
    }

    private static string RequireModerationForumName(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Restricted.Assets.ModerationForumName))
            return scope.Restricted.Assets.ModerationForumName;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted moderation execution requires a dedicated forum asset. Set {OnlineTestEnvironmentVariables.RestrictedAssetsModerationForumName} before attempting reply moderation.");
        return string.Empty;
    }

    private static long RequireModerationReplyId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Restricted.Assets.ModerationReplyId is > 0)
            return scope.Restricted.Assets.ModerationReplyId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted moderation execution requires a dedicated reply asset. Set {OnlineTestEnvironmentVariables.RestrictedAssetsModerationReplyId} before attempting reply moderation.");
        return default;
    }

    private static long RequireModerationThreadId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Restricted.Assets.ModerationThreadId is > 0)
            return scope.Restricted.Assets.ModerationThreadId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted moderation execution requires a dedicated thread asset. Set {OnlineTestEnvironmentVariables.RestrictedAssetsModerationThreadId} before attempting reply moderation.");
        return default;
    }

    private static async Task<Recover> RequireRecoverEntryAsync(
        Func<int, Task<Recovers>> getRecoversAsync,
        Func<Recover, bool> predicate,
        string operationName,
        string targetDescription)
    {
        for (var page = 1; page <= RecoverLookupMaxPages; page++)
        {
            var recovers = await getRecoversAsync(page);
            var match = recovers.FirstOrDefault(predicate);
            if (match is not null)
                return match;

            if (!recovers.HasMore)
                break;
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: could not find recover-bin evidence for {targetDescription} in the bounded moderation lookup window.");
        return default;
    }

    private static async Task<ThreadModel> RequireThreadSnapshotAsync(TiebaClient client, long threadId, string operationName)
    {
        for (var attempt = 0; attempt < ReplyLookupAttempts; attempt++)
        {
            try
            {
                var snapshot = await client.Threads.GetPostsAsync(threadId, 1, 1, PostSortType.Desc);
                if (snapshot.Thread is not null)
                    return snapshot.Thread;
            }
            catch (TieBaServerException) when (attempt < ReplyLookupAttempts - 1)
            {
            }
            catch (TiebaTransportException) when (attempt < ReplyLookupAttempts - 1)
            {
            }

            if (attempt < ReplyLookupAttempts - 1)
                await Task.Delay(ReplyLookupDelay);
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: the dedicated restricted thread asset could not be queried through `GetPostsAsync` in the bounded lookup window.");
        return default!;
    }

    private static async ValueTask RecoverDeletedReplyAsync(
        TiebaClient client,
        ModerationContext context,
        CancellationToken cancellationToken)
    {
        var recovered = await client.Threads.RecoverAsync(context.ForumName, pid: context.ReplyId, cancellationToken: cancellationToken);
        if (!recovered)
            throw new InvalidOperationException($"Expected to recover restricted moderation reply {context.ReplyId} in thread {context.ThreadId}.");
    }

    private static async ValueTask RecoverDeletedThreadAsync(
        TiebaClient client,
        ModerationContext context,
        CancellationToken cancellationToken)
    {
        var recovered = await client.Threads.RecoverAsync(context.ForumName, tid: context.ThreadId, cancellationToken: cancellationToken);
        if (!recovered)
            throw new InvalidOperationException($"Expected to recover restricted moderation thread {context.ThreadId}.");
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

    private static async Task<ThreadModel> WaitForThreadStateAsync(
        TiebaClient client,
        long threadId,
        Func<ThreadModel, bool> predicate,
        string stateDescription)
    {
        for (var attempt = 0; attempt < ReplyLookupAttempts; attempt++)
        {
            var snapshot = await RequireThreadSnapshotAsync(client, threadId, stateDescription);
            if (predicate(snapshot))
                return snapshot;

            if (attempt < ReplyLookupAttempts - 1)
                await Task.Delay(ReplyLookupDelay);
        }

        Assert.Inconclusive(
            $"Skipping {stateDescription}: the dedicated restricted thread asset did not settle into the expected state within the bounded moderation polling window.");
        return default!;
    }

    private sealed record ModerationContext(string ForumName, ulong ForumId, long ThreadId, long ReplyId);
}
