#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Restricted.Features.Admin.Scenarios;

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
    [TestCategory(OnlineTestApiCategories.AdminsGetBawuInfoAsync)]
    [TestCategory(OnlineTestApiCategories.AdminsGetBlocksAsync)]
    [TestCategory(OnlineTestApiCategories.AdminsBlockAsync)]
    [TestCategory(OnlineTestApiCategories.AdminsUnblockAsync)]
    public Task BlockAndUnblockAsyncDedicatedRestrictedTargetUsesCompensationAudit()
    {
        return ExecuteRestrictedAsync(
            "restricted admin block lifecycle",
            OnlineExecutionCapability.Admin,
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveAdminContextAsync(
                    scope,
                    client,
                    nameof(BlockAndUnblockAsyncDedicatedRestrictedTargetUsesCompensationAudit));
                var existingBlock = await WaitForBlockedUserStateAsync(client, context, shouldExist: true, expectStableBaseline: true);

                if (existingBlock is not null)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(BlockAndUnblockAsyncDedicatedRestrictedTargetUsesCompensationAudit)}: restricted admin target '{context.UserName}' is already blocked in '{context.ForumName}'. Clear the fixture before rerunning so the scenario can prove a fresh mutation and compensation cycle.");
                }

                var reason = $"restricted-admin-contract-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
                var blocked = await client.Admins.BlockAsync(context.ForumName, context.Portrait, 1, reason);
                Assert.IsTrue(blocked, "Expected the restricted admin target to accept a temporary block mutation.");

                var blockedArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.AdminRestricted,
                    "block",
                    context.UserId,
                    $"restricted admin temporary block for {context.UserName}");
                scope.Compensation.Register(
                    blockedArtifact,
                    "unblock restricted admin target",
                    "block removed",
                    cancellationToken => UnblockTargetAsync(client, context, cancellationToken));

                var blockEntry = await WaitForBlockedUserStateAsync(client, context, shouldExist: true, expectStableBaseline: true);
                Assert.IsNotNull(blockEntry, "Expected the restricted admin target to appear in the block list after mutation.");
                Assert.AreEqual(context.UserId, blockEntry.UserId);
                Assert.AreEqual(context.UserName, blockEntry.UserName);

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded, "Expected the restricted admin scenario to reconcile the temporary block through compensation.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("block removed", audit.CompensationResults[0].CompensationOutcome);
                Assert.AreEqual(OnlineTestStageCategories.AdminRestricted, audit.CompensationResults[0].StageCategory);

                var clearedBlock = await WaitForBlockedUserStateAsync(client, context, shouldExist: false, expectStableBaseline: true);
                Assert.IsNull(clearedBlock, "Expected the restricted admin target block to be removed once compensation completed.");

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(context.UserId.ToString(), auditDisplay);
                Assert.Contains("block removed", auditDisplay);
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

    private static async Task<AdminContext> ResolveAdminContextAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        var forumName = RequireAdminForumName(scope, operationName);
        var userName = RequireAdminUserName(scope, operationName);
        var userId = RequireAdminUserId(scope, operationName);
        var portrait = RequireAdminPortrait(scope, operationName);

        var bawuInfo = await client.Admins.GetBawuInfoAsync(forumName);
        Assert.IsNotNull(bawuInfo);
        Assert.IsNotNull(bawuInfo.All);

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

    private static async ValueTask UnblockTargetAsync(
        TiebaClient client,
        AdminContext context,
        CancellationToken cancellationToken)
    {
        var unblocked = await client.Admins.UnblockAsync(context.ForumName, context.UserId, cancellationToken);
        if (!unblocked)
            throw new InvalidOperationException($"Expected to unblock restricted admin target {context.UserName} ({context.UserId}).");
    }

    private sealed record AdminContext(string ForumName, string UserName, long UserId, string Portrait);
}
