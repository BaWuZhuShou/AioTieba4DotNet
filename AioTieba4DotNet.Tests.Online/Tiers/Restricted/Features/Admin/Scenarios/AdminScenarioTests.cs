#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Tiers.Restricted.Features.Admin.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.RestrictedOrdered)]
[TestCategory(OnlineTestFeatureCategories.Admin)]
[TestCategory(OnlineTestTierCategories.Restricted)]
[TestCategory(OnlineTestStageCategories.AdminRestricted)]
[TestCategory(OnlineTestCapabilityCategories.Admin)]
[TestSubject(typeof(TiebaClient))]
public sealed class AdminScenarioTests : OnlineRestrictedExecutionTestBase
{
    private const int BlockLookupAttempts = 6;
    private static readonly TimeSpan BlockLookupDelay = TimeSpan.FromSeconds(2);

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsGetBawuBlacklistAsync)]
    public Task GetBawuBlacklistAsyncDedicatedRestrictedForumReturnsPageShape()
    {
        return ExecuteRestrictedAsync(
            "restricted admin blacklist read",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumName = RequireAdminForumName(scope, nameof(GetBawuBlacklistAsyncDedicatedRestrictedForumReturnsPageShape));
                var blacklist = await client.Admins.GetBawuBlacklistAsync(forumName);

                Assert.IsNotNull(blacklist);
                Assert.IsNotNull(blacklist.Page);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsGetBawuInfoAsync)]
    public Task GetBawuInfoAsyncDedicatedRestrictedForumReturnsAdminSurfaceShape()
    {
        return ExecuteRestrictedAsync(
            "restricted admin info read",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumName = RequireAdminForumName(scope, nameof(GetBawuInfoAsyncDedicatedRestrictedForumReturnsAdminSurfaceShape));
                var bawuInfo = await client.Admins.GetBawuInfoAsync(forumName);

                Assert.IsNotNull(bawuInfo);
                Assert.IsNotNull(bawuInfo.All);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsGetBawuPermAsync)]
    public Task GetBawuPermAsyncDedicatedRestrictedPortraitReturnsPermissionShape()
    {
        return ExecuteRestrictedAsync(
            "restricted admin permission read",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumName = RequireAdminForumName(scope, nameof(GetBawuPermAsyncDedicatedRestrictedPortraitReturnsPermissionShape));
                var portrait = RequireAdminPortrait(scope, nameof(GetBawuPermAsyncDedicatedRestrictedPortraitReturnsPermissionShape));
                var permissions = await client.Admins.GetBawuPermAsync(forumName, portrait);

                Assert.IsNotNull(permissions);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsGetBawuPostLogsAsync)]
    public Task GetBawuPostLogsAsyncDedicatedRestrictedForumReturnsPageShape()
    {
        return ExecuteRestrictedAsync(
            "restricted admin post-log read",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumName = RequireAdminForumName(scope, nameof(GetBawuPostLogsAsyncDedicatedRestrictedForumReturnsPageShape));
                var postLogs = await client.Admins.GetBawuPostLogsAsync(forumName);

                Assert.IsNotNull(postLogs);
                Assert.IsNotNull(postLogs.Page);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsGetBawuUserLogsAsync)]
    public Task GetBawuUserLogsAsyncDedicatedRestrictedForumReturnsPageShape()
    {
        return ExecuteRestrictedAsync(
            "restricted admin user-log read",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumName = RequireAdminForumName(scope, nameof(GetBawuUserLogsAsyncDedicatedRestrictedForumReturnsPageShape));
                var userLogs = await client.Admins.GetBawuUserLogsAsync(forumName);

                Assert.IsNotNull(userLogs);
                Assert.IsNotNull(userLogs.Page);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsGetUnblockAppealsAsync)]
    public Task GetUnblockAppealsAsyncDedicatedRestrictedForumReturnsCollectionShape()
    {
        return ExecuteRestrictedAsync(
            "restricted admin appeals read",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var forumName = RequireAdminForumName(scope, nameof(GetUnblockAppealsAsyncDedicatedRestrictedForumReturnsCollectionShape));
                var appeals = await client.Admins.GetUnblockAppealsAsync(forumName);

                Assert.IsNotNull(appeals);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsGetBlocksAsync)]
    public Task GetBlocksAsyncDedicatedRestrictedForumReturnsPageShape()
    {
        return ExecuteRestrictedAsync(
            "restricted admin block-list read",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = ResolveAdminContext(scope, nameof(GetBlocksAsyncDedicatedRestrictedForumReturnsPageShape));
                var blocks = await client.Admins.GetBlocksAsync(context.ForumName, context.UserName, 1);

                Assert.IsNotNull(blocks);
                Assert.IsNotNull(blocks.Page);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsBlockAsync)]
    public Task BlockAsyncDedicatedRestrictedTargetUsesCompensationAudit()
    {
        return ExecuteRestrictedAsync(
            "restricted admin block mutation lifecycle",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = ResolveAdminContext(scope, nameof(BlockAsyncDedicatedRestrictedTargetUsesCompensationAudit));
                var existingBlock = await WaitForBlockedUserStateAsync(client, context, shouldExist: true, expectStableBaseline: true);

                if (existingBlock is not null)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(BlockAsyncDedicatedRestrictedTargetUsesCompensationAudit)}: restricted admin target '{context.UserName}' in '{context.ForumName}' is not starting from the clean baseline required for this direct block coverage run. Clear the live fixture before rerunning.");
                }

                var reason = $"restricted-admin-contract-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
                bool blocked;
                try
                {
                    blocked = await client.Admins.BlockAsync(context.ForumName, context.Portrait, 1, reason);
                }
                catch (TieBaServerException exception) when (exception.Code == 1211068)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(BlockAsyncDedicatedRestrictedTargetUsesCompensationAudit)}: the restricted admin fixture is still converging on the requested live state, so this run cannot observe the direct block path within the bounded verification window.");
                    return;
                }

                Assert.IsTrue(blocked, "Expected the restricted admin target to accept a temporary block mutation.");

                try
                {
                    RegisterBlockReconciliationCompensation(
                        scope,
                        client,
                        context,
                        $"restricted admin temporary block for {context.UserName}");

                    var blockEntry = await WaitForBlockedUserStateAsync(client, context, shouldExist: true, expectStableBaseline: true);
                    if (blockEntry is null)
                    {
                        Assert.Inconclusive(
                            $"Skipping {nameof(BlockAsyncDedicatedRestrictedTargetUsesCompensationAudit)}: BlockAsync accepted the mutation, but the dedicated restricted fixture did not surface the resulting live state inside the bounded verification window for this run.");
                    }

                    Assert.AreEqual(context.UserId, blockEntry.UserId);
                    Assert.AreEqual(context.UserName, blockEntry.UserName);
                }
                finally
                {
                    await scope.Compensation.ExecuteAsync();
                }

                AssertSingleAdminAudit(scope, context.UserId.ToString(), "block reconciled");

                var clearedBlock = await WaitForBlockedUserStateAsync(client, context, shouldExist: false, expectStableBaseline: true);
                Assert.IsNull(clearedBlock, "Expected the restricted admin target block to be removed once compensation completed.");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.AdminsUnblockAsync)]
    public Task UnblockAsyncDedicatedRestrictedTargetRestoresSetupBlockAndPublishesCompensationAudit()
    {
        return ExecuteRestrictedAsync(
            "restricted admin unblock mutation lifecycle",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = ResolveAdminContext(scope, nameof(UnblockAsyncDedicatedRestrictedTargetRestoresSetupBlockAndPublishesCompensationAudit));
                var existingBlock = await WaitForBlockedUserStateAsync(client, context, shouldExist: true, expectStableBaseline: true);

                if (existingBlock is not null)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(UnblockAsyncDedicatedRestrictedTargetRestoresSetupBlockAndPublishesCompensationAudit)}: restricted admin target '{context.UserName}' in '{context.ForumName}' is not starting from the clean baseline required for this direct unblock coverage run. Clear the live fixture before rerunning.");
                }

                var setupReason = $"restricted-admin-setup-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
                bool setupBlocked;
                try
                {
                    setupBlocked = await client.Admins.BlockAsync(context.ForumName, context.Portrait, 1, setupReason);
                }
                catch (TieBaServerException exception) when (exception.Code == 1211068)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(UnblockAsyncDedicatedRestrictedTargetRestoresSetupBlockAndPublishesCompensationAudit)}: the restricted admin fixture is still converging on the setup state required for this direct unblock coverage run, so the bounded verification window cannot proceed yet.");
                    return;
                }

                Assert.IsTrue(setupBlocked, "Expected the restricted admin target to accept the setup block mutation before unblock coverage runs.");

                try
                {
                    RegisterBlockReconciliationCompensation(
                        scope,
                        client,
                        context,
                        $"setup block for {context.UserName} before restricted admin unblock coverage");

                    var setupBlockEntry = await WaitForBlockedUserStateAsync(client, context, shouldExist: true, expectStableBaseline: true);
                    if (setupBlockEntry is null)
                    {
                        Assert.Inconclusive(
                            $"Skipping {nameof(UnblockAsyncDedicatedRestrictedTargetRestoresSetupBlockAndPublishesCompensationAudit)}: the restricted admin fixture did not surface the setup state inside the bounded verification window for this run, so the direct unblock assertion cannot proceed truthfully.");
                    }

                    var unblocked = await client.Admins.UnblockAsync(context.ForumName, context.UserId);
                    Assert.IsTrue(unblocked, "Expected the restricted admin target to accept a direct unblock mutation.");
                }
                finally
                {
                    await scope.Compensation.ExecuteAsync();
                }

                AssertSingleAdminAudit(scope, context.UserId.ToString(), "block reconciled");

                var clearedBlock = await WaitForBlockedUserStateAsync(client, context, shouldExist: false, expectStableBaseline: true);
                Assert.IsNull(clearedBlock, "Expected the restricted admin target block to remain cleared after direct unblock and idempotent compensation.");
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

    private static AdminContext ResolveAdminContext(OnlineExecutionScope scope, string operationName)
    {
        var forumName = RequireAdminForumName(scope, operationName);
        var userName = RequireAdminUserName(scope, operationName);
        var userId = RequireAdminUserId(scope, operationName);
        var portrait = RequireAdminPortrait(scope, operationName);
        return new AdminContext(forumName, userName, userId, portrait);
    }

    private static string RequireAdminForumName(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Restricted.Assets.ModerationForumName))
            return scope.Restricted.Assets.ModerationForumName;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted admin execution requires the shared restricted forum asset. Set {OnlineTestEnvironmentVariables.RestrictedAssetsModerationForumName} before attempting admin mutations.");
        return string.Empty;
    }

    private static string RequireAdminUserName(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Restricted.Assets.AdminUserName))
            return scope.Restricted.Assets.AdminUserName;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted admin execution requires a dedicated target user name. Set {OnlineTestEnvironmentVariables.RestrictedAssetsAdminUserName} before attempting admin mutations.");
        return string.Empty;
    }

    private static long RequireAdminUserId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Restricted.Assets.AdminUserId is > 0)
            return scope.Restricted.Assets.AdminUserId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted admin execution requires a dedicated target user id. Set {OnlineTestEnvironmentVariables.RestrictedAssetsAdminUserId} before attempting admin mutations.");
        return default;
    }

    private static string RequireAdminPortrait(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Restricted.Assets.AdminPortrait))
            return scope.Restricted.Assets.AdminPortrait;

        Assert.Inconclusive(
            $"Skipping {operationName}: restricted admin execution requires a dedicated target portrait. Set {OnlineTestEnvironmentVariables.RestrictedAssetsAdminPortrait} before attempting admin mutations.");
        return string.Empty;
    }

    private static async Task<Block?> WaitForBlockedUserStateAsync(
        TiebaClient client,
        AdminContext context,
        bool shouldExist,
        bool expectStableBaseline)
    {
        Block? lastObservedBlock = null;

        for (var attempt = 0; attempt < BlockLookupAttempts; attempt++)
        {
            var blocks = await client.Admins.GetBlocksAsync(context.ForumName, context.UserName, 1);
            var block = blocks.Objs.FirstOrDefault(candidate => candidate.UserId == context.UserId);
            lastObservedBlock = block;
            if ((block is not null) == shouldExist)
                return block;

            if (!expectStableBaseline && attempt == 0)
                return block;

            if (attempt < BlockLookupAttempts - 1)
                await Task.Delay(BlockLookupDelay);
        }

        return lastObservedBlock;
    }

    private static void RegisterBlockReconciliationCompensation(
        OnlineExecutionScope scope,
        TiebaClient client,
        AdminContext context,
        string description)
    {
        var blockedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.AdminRestricted,
            "block",
            context.UserId,
            description);
        scope.Compensation.Register(
            blockedArtifact,
            "reconcile restricted admin block",
            "block reconciled",
            cancellationToken => EnsureTargetUnblockedAsync(client, context, cancellationToken));
    }

    private static void AssertSingleAdminAudit(OnlineExecutionScope scope, string auditMarker, string expectedOutcome)
    {
        var audit = scope.Compensation.GetLastAudit();
        Assert.IsNotNull(audit);
        Assert.IsTrue(audit.Succeeded, "Expected the restricted admin scenario to reconcile the temporary block through compensation.");
        Assert.HasCount(1, audit.RecordedArtifacts);
        Assert.HasCount(1, audit.CompensationResults);
        Assert.IsEmpty(audit.UnreconciledArtifacts);
        Assert.AreEqual(expectedOutcome, audit.CompensationResults[0].CompensationOutcome);
        Assert.AreEqual(OnlineTestStageCategories.AdminRestricted, audit.CompensationResults[0].StageCategory);

        var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains(auditMarker, auditDisplay);
        Assert.Contains(expectedOutcome, auditDisplay);
        Assert.Contains("unreconciled: none", auditDisplay);
    }

    private static async ValueTask EnsureTargetUnblockedAsync(
        TiebaClient client,
        AdminContext context,
        CancellationToken cancellationToken)
    {
        var unblocked = await client.Admins.UnblockAsync(context.ForumName, context.UserId, cancellationToken);
        if (unblocked)
            return;

        var clearedBlock = await WaitForBlockedUserStateAsync(client, context, shouldExist: false, expectStableBaseline: true);
        if (clearedBlock is null)
            return;

        throw new InvalidOperationException($"Expected to reconcile restricted admin target block for {context.UserName} ({context.UserId}).");
    }

    private sealed record AdminContext(string ForumName, string UserName, long UserId, string Portrait);
}
