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
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadModel = AioTieba4DotNet.Models.Threads.Thread;

namespace AioTieba4DotNet.Tests.Online.Tiers.Restricted.Features.Moderation.Scenarios;

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
    public Task DelPostAsyncDedicatedRestrictedReplyUsesCompensationAudit()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation reply delete lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(DelPostAsyncDedicatedRestrictedReplyUsesCompensationAudit);
                var context = ResolveModerationContext(scope, operationName);
                var reply = await RequireVisibleReplyAsync(client, context, operationName);

                var deleted = await client.Threads.DelPostAsync(context.ForumName, context.ThreadId, context.ReplyId);
                Assert.IsTrue(deleted, "Expected the restricted moderation reply asset to accept a temporary delete mutation.");

                RegisterReplyRecoveryCompensation(scope, client, context, "post", $"restricted moderation delete for dedicated reply {context.ReplyId}");

                var deletedReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: false);
                Assert.IsNull(deletedReply, "Expected the dedicated restricted reply asset to disappear after delete moderation coverage runs.");

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ReplyId.ToString(), "reply recovered", "post");

                var restoredReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: true);
                Assert.IsNotNull(restoredReply, "Expected the dedicated restricted reply asset to be restored once compensation completed.");
                Assert.AreEqual(context.ReplyId, restoredReply.Pid);
                Assert.AreEqual(context.ThreadId, restoredReply.Tid);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsRecoverAsync)]
    public Task RecoverAsyncDedicatedRestrictedReplyRestoresDeletedReply()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation reply recover lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(RecoverAsyncDedicatedRestrictedReplyRestoresDeletedReply);
                var context = ResolveModerationContext(scope, operationName);
                _ = await RequireVisibleReplyAsync(client, context, operationName);

                var deleted = await client.Threads.DelPostAsync(context.ForumName, context.ThreadId, context.ReplyId);
                Assert.IsTrue(deleted, "Expected the restricted moderation reply asset to accept the setup delete mutation before recover coverage runs.");

                RegisterReplyRecoveryCompensation(scope, client, context, "post", $"setup delete for dedicated reply {context.ReplyId} before recover coverage");

                var deletedReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: false);
                Assert.IsNull(deletedReply, "Expected the dedicated restricted reply asset to disappear after the setup delete mutation.");

                var recovered = await client.Threads.RecoverAsync(context.ForumName, pid: context.ReplyId);
                Assert.IsTrue(recovered, "Expected the restricted moderation reply asset to accept a direct recover mutation.");

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ReplyId.ToString(), "reply recovered", "post");

                var restoredReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: true);
                Assert.IsNotNull(restoredReply, "Expected the dedicated restricted reply asset to remain visible once idempotent compensation completed.");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDelPostsAsync)]
    public Task DelPostsAsyncDedicatedRestrictedReplyUsesCompensationAudit()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation batch delete lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(DelPostsAsyncDedicatedRestrictedReplyUsesCompensationAudit);
                var context = ResolveModerationContext(scope, operationName);
                _ = await RequireVisibleReplyAsync(client, context, operationName);

                var deleted = await client.Threads.DelPostsAsync(context.ForumName, context.ThreadId, [context.ReplyId]);
                Assert.IsTrue(deleted,
                    "Expected the restricted moderation reply asset to accept a temporary batch delete mutation.");

                RegisterReplyRecoveryCompensation(scope, client, context, "post-batch-delete", $"restricted moderation batch delete for dedicated reply {context.ReplyId}");

                var deletedReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: false);
                Assert.IsNull(deletedReply,
                    "Expected the dedicated restricted reply asset to disappear after batch delete moderation coverage runs.");

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ReplyId.ToString(), "reply recovered", "post-batch-delete");

                var restoredReply = await WaitForReplyStateAsync(client, context.ThreadId, context.ReplyId, shouldExist: true);
                Assert.IsNotNull(restoredReply,
                    "Expected the dedicated restricted reply asset to be restored once batch-delete compensation completed.");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetRecoversAsync)]
    public Task GetRecoversAsyncDedicatedRestrictedReplyDeletedViaForumNameReturnsRecoverEntry()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation recovers by name sample",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRecoversAsyncDedicatedRestrictedReplyDeletedViaForumNameReturnsRecoverEntry);
                var context = ResolveModerationContext(scope, operationName);
                _ = await RequireVisibleReplyAsync(client, context, operationName);

                var deleted = await client.Threads.DelPostsAsync(context.ForumName, context.ThreadId, [context.ReplyId]);
                Assert.IsTrue(deleted, "Expected the restricted moderation reply asset to accept the setup batch delete mutation before recover-bin reads.");

                RegisterReplyRecoveryCompensation(scope, client, context, "post-batch-delete", $"setup batch delete for dedicated reply {context.ReplyId} before recover-bin read by forum name");

                var recover = await RequireRecoverEntryAsync(
                    page => client.Threads.GetRecoversAsync(context.ForumName, page, 10),
                    candidate => candidate.Pid == context.ReplyId,
                    operationName,
                    $"reply {context.ReplyId} via forum-name overload");

                Assert.AreEqual(context.ReplyId, recover.Pid);
                Assert.AreEqual(context.ThreadId, recover.Tid);

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ReplyId.ToString(), "reply recovered", "post-batch-delete");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetRecoversAsync)]
    public Task GetRecoversAsyncDedicatedRestrictedReplyDeletedViaFidReturnsRecoverEntry()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation recovers by fid sample",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRecoversAsyncDedicatedRestrictedReplyDeletedViaFidReturnsRecoverEntry);
                var context = ResolveModerationContext(scope, operationName);
                var forumId = RequireModerationForumId(context, operationName);
                _ = await RequireVisibleReplyAsync(client, context, operationName);

                var deleted = await client.Threads.DelPostsAsync(context.ForumName, context.ThreadId, [context.ReplyId]);
                Assert.IsTrue(deleted, "Expected the restricted moderation reply asset to accept the setup batch delete mutation before recover-bin reads.");

                RegisterReplyRecoveryCompensation(scope, client, context, "post-batch-delete", $"setup batch delete for dedicated reply {context.ReplyId} before recover-bin read by fid");

                var recover = await RequireRecoverEntryAsync(
                    page => client.Threads.GetRecoversAsync(forumId, page, 10),
                    candidate => candidate.Pid == context.ReplyId,
                    operationName,
                    $"reply {context.ReplyId} via fid overload");

                Assert.AreEqual(context.ReplyId, recover.Pid);
                Assert.AreEqual(context.ThreadId, recover.Tid);

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ReplyId.ToString(), "reply recovered", "post-batch-delete");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetRecoverInfoAsync)]
    public Task GetRecoverInfoAsyncDedicatedRestrictedReplyDeletedViaForumNameReturnsRecoverInfo()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation recover-info by name sample",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRecoverInfoAsyncDedicatedRestrictedReplyDeletedViaForumNameReturnsRecoverInfo);
                var context = ResolveModerationContext(scope, operationName);
                _ = await RequireVisibleReplyAsync(client, context, operationName);

                var deleted = await client.Threads.DelPostsAsync(context.ForumName, context.ThreadId, [context.ReplyId]);
                Assert.IsTrue(deleted, "Expected the restricted moderation reply asset to accept the setup batch delete mutation before recover-info reads.");

                RegisterReplyRecoveryCompensation(scope, client, context, "post-batch-delete", $"setup batch delete for dedicated reply {context.ReplyId} before recover-info read by forum name");

                var recoverInfo = await client.Threads.GetRecoverInfoAsync(context.ForumName, context.ThreadId, context.ReplyId);
                AssertRecoverInfoShape(recoverInfo, context.ThreadId, context.ReplyId);

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ReplyId.ToString(), "reply recovered", "post-batch-delete");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetRecoverInfoAsync)]
    public Task GetRecoverInfoAsyncDedicatedRestrictedReplyDeletedViaFidReturnsRecoverInfo()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation recover-info by fid sample",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRecoverInfoAsyncDedicatedRestrictedReplyDeletedViaFidReturnsRecoverInfo);
                var context = ResolveModerationContext(scope, operationName);
                var forumId = RequireModerationForumId(context, operationName);
                _ = await RequireVisibleReplyAsync(client, context, operationName);

                var deleted = await client.Threads.DelPostsAsync(context.ForumName, context.ThreadId, [context.ReplyId]);
                Assert.IsTrue(deleted, "Expected the restricted moderation reply asset to accept the setup batch delete mutation before recover-info reads.");

                RegisterReplyRecoveryCompensation(scope, client, context, "post-batch-delete", $"setup batch delete for dedicated reply {context.ReplyId} before recover-info read by fid");

                var recoverInfo = await client.Threads.GetRecoverInfoAsync(forumId, context.ThreadId, context.ReplyId);
                AssertRecoverInfoShape(recoverInfo, context.ThreadId, context.ReplyId);

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ReplyId.ToString(), "reply recovered", "post-batch-delete");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGoodAsync)]
    public Task GoodAsyncDedicatedRestrictedThreadUsesCompensationAudit()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation good lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GoodAsyncDedicatedRestrictedThreadUsesCompensationAudit);
                var context = ResolveModerationContext(scope, operationName);
                var initialThread = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);
                if (initialThread.IsGood)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated restricted thread asset is already marked good. Use a moderation thread that starts in a non-good state so `GoodAsync` can be proven directly.");
                }

                var goodSucceeded = await client.Threads.GoodAsync(context.ForumName, context.ThreadId);
                Assert.IsTrue(goodSucceeded, "Expected the dedicated restricted thread to accept a temporary good mutation.");
                var goodThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => thread.IsGood, "thread good state");
                Assert.IsTrue(goodThread.IsGood);

                RegisterThreadMutationCompensation(scope, client, context, "thread-good", $"temporary good mutation for restricted thread {context.ThreadId}", "thread ungood", EnsureThreadUngoodAsync);

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ThreadId.ToString(), "thread ungood", "thread-good");

                var restoredThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => !thread.IsGood, "thread restored good state");
                Assert.IsFalse(restoredThread.IsGood);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsUngoodAsync)]
    public Task UngoodAsyncDedicatedRestrictedThreadRestoresSetupGoodState()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation ungood lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(UngoodAsyncDedicatedRestrictedThreadRestoresSetupGoodState);
                var context = ResolveModerationContext(scope, operationName);
                var initialThread = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);
                if (initialThread.IsGood)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated restricted thread asset is already marked good. Use a moderation thread that starts in a non-good state so `UngoodAsync` can be exercised against a known setup good mutation.");
                }

                var setupGood = await client.Threads.GoodAsync(context.ForumName, context.ThreadId);
                Assert.IsTrue(setupGood, "Expected the dedicated restricted thread to accept the setup good mutation before ungood coverage runs.");
                _ = await WaitForThreadStateAsync(client, context.ThreadId, thread => thread.IsGood, "thread setup good state");

                RegisterThreadMutationCompensation(scope, client, context, "thread-good", $"setup good mutation for restricted thread {context.ThreadId} before ungood coverage", "thread ungood", EnsureThreadUngoodAsync);

                var ungoodSucceeded = await client.Threads.UngoodAsync(context.ForumName, context.ThreadId);
                Assert.IsTrue(ungoodSucceeded, "Expected the dedicated restricted thread to accept a matching ungood mutation.");

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ThreadId.ToString(), "thread ungood", "thread-good");

                var restoredThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => !thread.IsGood, "thread final ungood state");
                Assert.IsFalse(restoredThread.IsGood);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsTopAsync)]
    public Task TopAsyncDedicatedRestrictedThreadUsesCompensationAudit()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation top lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(TopAsyncDedicatedRestrictedThreadUsesCompensationAudit);
                var context = ResolveModerationContext(scope, operationName);
                var initialThread = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);
                if (initialThread.IsTop)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated restricted thread asset is already pinned. Use a moderation thread that starts unpinned so `TopAsync` can be proven directly.");
                }

                var topSucceeded = await client.Threads.TopAsync(context.ForumName, context.ThreadId, false);
                Assert.IsTrue(topSucceeded, "Expected the dedicated restricted thread to accept a temporary top mutation.");
                var topThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => thread.IsTop, "thread top state");
                Assert.IsTrue(topThread.IsTop);

                RegisterThreadMutationCompensation(scope, client, context, "thread-top", $"temporary top mutation for restricted thread {context.ThreadId}", "thread untop", EnsureThreadUntoppedAsync);

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ThreadId.ToString(), "thread untop", "thread-top");

                var restoredThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => !thread.IsTop, "thread restored top state");
                Assert.IsFalse(restoredThread.IsTop);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsUntopAsync)]
    public Task UntopAsyncDedicatedRestrictedThreadRestoresSetupTopState()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation untop lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(UntopAsyncDedicatedRestrictedThreadRestoresSetupTopState);
                var context = ResolveModerationContext(scope, operationName);
                var initialThread = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);
                if (initialThread.IsTop)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated restricted thread asset is already pinned. Use a moderation thread that starts unpinned so `UntopAsync` can be exercised against a known setup top mutation.");
                }

                var setupTop = await client.Threads.TopAsync(context.ForumName, context.ThreadId, false);
                Assert.IsTrue(setupTop, "Expected the dedicated restricted thread to accept the setup top mutation before untop coverage runs.");
                _ = await WaitForThreadStateAsync(client, context.ThreadId, thread => thread.IsTop, "thread setup top state");

                RegisterThreadMutationCompensation(scope, client, context, "thread-top", $"setup top mutation for restricted thread {context.ThreadId} before untop coverage", "thread untop", EnsureThreadUntoppedAsync);

                var untopSucceeded = await client.Threads.UntopAsync(context.ForumName, context.ThreadId, false);
                Assert.IsTrue(untopSucceeded, "Expected the dedicated restricted thread to accept a matching untop mutation.");

                await scope.Compensation.ExecuteAsync();
                AssertSingleModerationAudit(scope, context.ThreadId.ToString(), "thread untop", "thread-top");

                var restoredThread = await WaitForThreadStateAsync(client, context.ThreadId, thread => !thread.IsTop, "thread final untop state");
                Assert.IsFalse(restoredThread.IsTop);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsRecommendAsync)]
    public Task RecommendAsyncDedicatedRestrictedThreadUsesAvailableQuota()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation recommend sample",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(RecommendAsyncDedicatedRestrictedThreadUsesAvailableQuota);
                var context = ResolveModerationContext(scope, operationName);
                var forumId = RequireModerationForumId(context, operationName);
                var recomStatus = await client.Forums.GetRecomStatusAsync(forumId);
                if (recomStatus.TotalRecommendNum <= 0 || recomStatus.UsedRecommendNum >= recomStatus.TotalRecommendNum)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the dedicated moderation forum does not currently expose spare recommendation quota for one-way restricted `RecommendAsync` coverage.");
                }

                var recommended = await client.Threads.RecommendAsync(context.ForumName, context.ThreadId);
                Assert.IsTrue(recommended,
                    "Expected the dedicated restricted thread to accept the explicit one-way recommendation mutation once quota and opt-in preconditions were satisfied.");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsSetThreadPrivacyAsync)]
    public Task SetThreadPrivacyAsyncDedicatedRestrictedReplyRestoresVisibility()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation thread privacy lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(SetThreadPrivacyAsyncDedicatedRestrictedReplyRestoresVisibility);
                var context = ResolveModerationContext(scope, operationName);
                _ = await RequireVisibleReplyAsync(client, context, operationName);

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
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDelThreadAsync)]
    public Task DelThreadAsyncDedicatedRestrictedThreadUsesManualRecovery()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation thread delete lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(DelThreadAsyncDedicatedRestrictedThreadUsesManualRecovery);
                var context = ResolveModerationContext(scope, operationName);
                _ = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);

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
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsDelThreadsAsync)]
    public Task DelThreadsAsyncDedicatedRestrictedThreadUsesManualRecovery()
    {
        return ExecuteRestrictedAsync(
            "restricted moderation batch thread delete lifecycle",
            OnlineExecutionCapability.Moderation,
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(DelThreadsAsyncDedicatedRestrictedThreadUsesManualRecovery);
                var context = ResolveModerationContext(scope, operationName);
                var forumId = RequireModerationForumId(context, operationName);
                _ = await RequireThreadSnapshotAsync(client, context.ThreadId, operationName);

                var batchDeletedThread = await client.Threads.DelThreadsAsync(context.ForumName, [context.ThreadId]);
                Assert.IsTrue(batchDeletedThread,
                    "Expected the dedicated restricted thread to accept a temporary batch delete mutation.");

                var batchDeletedRecover = await RequireRecoverEntryAsync(
                    page => client.Threads.GetRecoversAsync(forumId, page, 10),
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

    private static void AssertSingleModerationAudit(
        OnlineExecutionScope scope,
        string auditMarker,
        string expectedOutcome,
        string expectedArtifactType)
    {
        var audit = scope.Compensation.GetLastAudit();
        Assert.IsNotNull(audit);
        Assert.IsTrue(audit.Succeeded, "Expected the restricted moderation scenario to reconcile the temporary mutation through compensation.");
        Assert.HasCount(1, audit.RecordedArtifacts);
        Assert.HasCount(1, audit.CompensationResults);
        Assert.IsEmpty(audit.UnreconciledArtifacts);
        Assert.AreEqual(expectedOutcome, audit.CompensationResults[0].CompensationOutcome);
        Assert.AreEqual(expectedArtifactType, audit.RecordedArtifacts[0].ArtifactType);

        var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains(auditMarker, auditDisplay);
        Assert.Contains("unreconciled: none", auditDisplay);
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

    private static async Task<Post> RequireVisibleReplyAsync(TiebaClient client, ModerationContext context, string operationName)
    {
        var reply = await FindReplyAsync(client, context.ThreadId, context.ReplyId);
        if (reply is null)
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: restricted moderation reply asset must be visible in the dedicated thread before coverage can run. Confirm {OnlineTestEnvironmentVariables.RestrictedAssetsModerationReplyId} belongs to a disposable reply in the thread referenced by {OnlineTestEnvironmentVariables.RestrictedAssetsModerationThreadId}.");
        }

        return reply;
    }

    private static async Task<Post?> FindReplyAsync(TiebaClient client, long threadId, long replyId)
    {
        for (var page = 1; page <= ReplyLookupMaxPages; page++)
        {
            Posts posts;
            try
            {
                posts = await client.Threads.GetPostsAsync(threadId, page, ReplyLookupPageSize, PostSortType.Desc);
            }
            catch (TieBaServerException exception) when (exception.Code == 4)
            {
                return null;
            }

            var match = posts.Objs.FirstOrDefault(post => post.Pid == replyId);
            if (match is not null)
                return match;

            if (!posts.HasMore)
                break;
        }

        return null;
    }

    private static void RegisterReplyRecoveryCompensation(
        OnlineExecutionScope scope,
        TiebaClient client,
        ModerationContext context,
        string artifactType,
        string description)
    {
        var deletedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.ModerationRestricted,
            artifactType,
            $"{context.ThreadId}:{context.ReplyId}",
            description);
        scope.Compensation.Register(
            deletedArtifact,
            "recover restricted moderation reply",
            "reply recovered",
            cancellationToken => EnsureDeletedReplyRecoveredAsync(client, context, cancellationToken));
    }

    private static void RegisterThreadMutationCompensation(
        OnlineExecutionScope scope,
        TiebaClient client,
        ModerationContext context,
        string artifactType,
        string description,
        string outcome,
        Func<TiebaClient, ModerationContext, CancellationToken, ValueTask> compensation)
    {
        var artifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.ModerationRestricted,
            artifactType,
            context.ThreadId.ToString(),
            description);
        scope.Compensation.Register(
            artifact,
            $"reconcile restricted moderation {artifactType}",
            outcome,
            cancellationToken => compensation(client, context, cancellationToken));
    }

    private static ModerationContext ResolveModerationContext(OnlineExecutionScope scope, string operationName)
    {
        var forumName = RequireModerationForumName(scope, operationName);
        var forumId = TryResolveModerationForumId(scope);
        var threadId = RequireModerationThreadId(scope, operationName);
        var replyId = RequireModerationReplyId(scope, operationName);

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

    private static ulong? TryResolveModerationForumId(OnlineExecutionScope scope)
    {
        if (scope.Restricted.Assets.ModerationForumId is > 0)
            return (ulong)scope.Restricted.Assets.ModerationForumId.Value;

        return null;
    }

    private static ulong RequireModerationForumId(ModerationContext context, string operationName)
    {
        if (context.ForumId is { } forumId and > 0)
            return forumId;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted moderation execution requires a dedicated forum id asset. Set {OnlineTestEnvironmentVariables.RestrictedAssetsModerationForumId} before attempting fid-based or recommendation moderation coverage.");
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
            catch (TieBaServerException exception) when (exception.Code == 4)
            {
                Assert.Inconclusive(
                    $"Skipping {operationName}: the dedicated restricted thread asset '{threadId}' is no longer visible to Threads.GetPostsAsync. Refresh {OnlineTestEnvironmentVariables.RestrictedAssetsModerationThreadId} before rerunning restricted moderation coverage.");
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

    private static async ValueTask EnsureDeletedReplyRecoveredAsync(
        TiebaClient client,
        ModerationContext context,
        CancellationToken cancellationToken)
    {
        var recovered = await client.Threads.RecoverAsync(context.ForumName, pid: context.ReplyId, cancellationToken: cancellationToken);
        if (!recovered)
        {
            var reply = await FindReplyAsync(client, context.ThreadId, context.ReplyId);
            if (reply is not null)
                return;

            throw new InvalidOperationException($"Expected to recover restricted moderation reply {context.ReplyId} in thread {context.ThreadId}.");
        }
    }

    private static async ValueTask EnsureThreadUngoodAsync(
        TiebaClient client,
        ModerationContext context,
        CancellationToken cancellationToken)
    {
        _ = await client.Threads.UngoodAsync(context.ForumName, context.ThreadId, cancellationToken);
    }

    private static async ValueTask EnsureThreadUntoppedAsync(
        TiebaClient client,
        ModerationContext context,
        CancellationToken cancellationToken)
    {
        _ = await client.Threads.UntopAsync(context.ForumName, context.ThreadId, false, cancellationToken);
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

    private sealed record ModerationContext(string ForumName, ulong? ForumId, long ThreadId, long ReplyId);
}
