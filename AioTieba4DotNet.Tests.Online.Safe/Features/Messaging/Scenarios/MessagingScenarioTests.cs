#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Safe.Features.Messaging.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.Messaging)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.Messaging)]
[TestSubject(typeof(TiebaClient))]
public sealed class MessagingScenarioTests : OnlineSafeExecutionTestBase
{
    [TestMethod]
    [TestCategory(OnlineTestApiCategories.MessagesGetAtsAsync)]
    public Task GetAtsAsync_AuthenticatedAccount_ReturnsInboxPageShape()
    {
        return ExecuteSafeAsync(
            "messaging @ inbox read sample",
            async scope =>
            {
                using var client = CreateClient(scope, TiebaTransportMode.Http);
                var ats = await client.Messages.GetAtsAsync(1);

                Assert.IsNotNull(ats);
                Assert.IsNotNull(ats.Page);
                Assert.IsGreaterThanOrEqualTo(1, ats.Page.CurrentPage);
            },
            OnlineExecutionCapability.Messaging);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.MessagesGetRepliesAsync)]
    public Task GetRepliesAsync_AuthenticatedAccount_ReturnsInboxPageShapeOrExplicitEndpointGate()
    {
        return ExecuteSafeAsync(
            "messaging reply inbox read sample",
            async scope =>
            {
                using var client = CreateClient(scope, TiebaTransportMode.Http);
                var replies = await RunReplyInboxOrInconclusiveAsync(() => client.Messages.GetRepliesAsync(1));

                Assert.IsNotNull(replies);
                Assert.IsNotNull(replies.Page);
                Assert.IsGreaterThanOrEqualTo(1, replies.Page.CurrentPage);
            },
            OnlineExecutionCapability.Messaging);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.MessagesGetGroupMessagesAsync)]
    public Task GetGroupMessagesAsync_InitializedWebSocket_ReturnsMessageGroupContainerOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "messaging websocket group read sample",
            async scope =>
            {
                using var client = CreateClient(scope, TiebaTransportMode.Auto);

                var groups = await RunWebSocketMessagingOrInconclusiveAsync(async () =>
                {
                    await client.Client.InitWebSocketAsync();
                    return await client.Messages.GetGroupMessagesAsync();
                });

                Assert.IsNotNull(groups);
                foreach (var group in groups)
                {
                    Assert.IsPositive(group.GroupId);
                    Assert.IsNotNull(group.Messages);
                }
            },
            OnlineExecutionCapability.Messaging);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetPanelInfoAsync)]
    [TestCategory(OnlineTestApiCategories.MessagesSendMessageAsync)]
    public Task SendMessageAsync_DedicatedRecipient_UsesCompensatingFollowUpAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "messaging private-message compensation cycle",
            async scope =>
            {
                var recipient = RequireMessageRecipient(scope,
                    nameof(SendMessageAsync_DedicatedRecipient_UsesCompensatingFollowUpAndPublishesCompensationAudit));

                using var client = CreateClient(scope, TiebaTransportMode.Http);
                var recipientInfo = await client.Users.GetPanelInfoAsync(recipient);

                Assert.IsNotNull(recipientInfo);
                Assert.IsPositive(recipientInfo.UserId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(recipientInfo.Portrait));

                var messageMarker = CreateMessageMarker("safemsg");
                var compensationMarker = CreateMessageMarker("safemsgundo");
                var sentMessageId = await client.Messages.SendMessageAsync(recipientInfo.UserId, messageMarker);

                Assert.IsPositive(sentMessageId);

                var sentMessageArtifact = scope.Compensation.RecordCreatedArtifact(
                    OnlineTestStageCategories.Messaging,
                    "private-message",
                    sentMessageId,
                    $"safe private message '{messageMarker}' to dedicated recipient '{recipient}'");
                scope.Compensation.Register(
                    sentMessageArtifact,
                    "send private-message compensation notice",
                    "compensation notice sent",
                    cancellationToken => SendCompensationMessageAsync(
                        client,
                        recipientInfo.UserId,
                        compensationMarker,
                        sentMessageId,
                        cancellationToken));

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded,
                    "Expected the Messaging safe scenario to reconcile the private-message mutation via a compensating follow-up message.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("compensation notice sent", audit.CompensationResults[0].CompensationOutcome);

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(messageMarker, auditDisplay);
                Assert.Contains("unreconciled: none", auditDisplay);
            },
            OnlineExecutionCapability.Messaging);
    }

    [TestMethod]
    public Task ChatroomMutationAsset_DedicatedChatroomAndForumRequiredBeforeSend()
    {
        return ExecuteSafeAsync(
            "messaging chatroom asset gate",
            async scope =>
            {
                var chatroomId = RequireChatroomId(scope,
                    nameof(ChatroomMutationAsset_DedicatedChatroomAndForumRequiredBeforeSend));

                using var client = CreateClient(scope, TiebaTransportMode.Http);
                var forum = await ResolveDedicatedForumAsync(scope, client,
                    nameof(ChatroomMutationAsset_DedicatedChatroomAndForumRequiredBeforeSend));

                Assert.IsPositive(chatroomId);
                Assert.IsNotNull(forum);
                Assert.IsPositive(forum.Fid);
                Assert.IsFalse(string.IsNullOrWhiteSpace(forum.Fname));
            });
    }

    private static TiebaClient CreateClient(OnlineExecutionScope scope, TiebaTransportMode transportMode)
    {
        var options = new TiebaOptions
        {
            Bduss = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Bduss : null,
            Stoken = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Stoken : null,
            TransportMode = transportMode
        };

        return new TiebaClient(options);
    }

    private static string RequireMessageRecipient(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.MessageRecipient))
            return scope.Safe.Assets.MessageRecipient;

        Assert.Inconclusive(
            $"Skipping {operationName}: safe private-message mutation requires a dedicated recipient asset. Set {OnlineTestEnvironmentVariables.SafeAssetsMessageRecipient} before sending any message.");
        return string.Empty;
    }

    private static long RequireChatroomId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.ChatroomId is > 0)
            return scope.Safe.Assets.ChatroomId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: safe chatroom mutation requires a dedicated chatroom asset. Set {OnlineTestEnvironmentVariables.SafeAssetsChatroomId} before sending any chatroom message.");
        return default;
    }

    private static async Task<Forum> ResolveDedicatedForumAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumQuery))
        {
            var resolvedForum = await client.Forums.GetForumAsync(scope.Safe.Assets.ForumQuery);
            Assert.IsNotNull(resolvedForum);
            return resolvedForum;
        }

        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumName))
        {
            var resolvedForum = await client.Forums.GetForumAsync(scope.Safe.Assets.ForumName);
            Assert.IsNotNull(resolvedForum);
            return resolvedForum;
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: safe chatroom mutation requires a dedicated forum asset. Set {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} before chatroom sends.");
        return null!;
    }

    private static async Task<T> RunReplyInboxOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (IsReplyInboxCapabilityGate(exception))
        {
            Assert.Inconclusive(
                $"Skipping messaging reply inbox read in this environment: the configured safe account does not currently satisfy the reply inbox endpoint capability gate ({exception.Message}).");
            throw;
        }
    }

    private static bool IsReplyInboxCapabilityGate(TieBaServerException exception)
    {
        return exception.Code is 1 or 110000
               && (exception.Message.Contains("用户未登录或登录失败", StringComparison.Ordinal)
                   || exception.Message.Contains("请先登录", StringComparison.Ordinal));
    }

    private static async Task<T> RunWebSocketMessagingOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (IsWebSocketMessagingCapabilityGate(exception))
        {
            Assert.Inconclusive(
                $"Skipping websocket messaging read in this environment: the configured safe account does not currently satisfy the websocket messaging capability gate ({exception.Message}).");
            throw;
        }
        catch (TiebaTransportException exception) when (IsWebSocketTransportEnvironmentGate(exception))
        {
            Assert.Inconclusive(
                $"Skipping websocket messaging read in this environment: websocket transport is not currently available ({exception.Message}).");
            throw;
        }
    }

    private static bool IsWebSocketMessagingCapabilityGate(TieBaServerException exception)
    {
        return exception.Code is 1 or 110000
               && (exception.Message.Contains("用户未登录或登录失败", StringComparison.Ordinal)
                   || exception.Message.Contains("请先登录", StringComparison.Ordinal));
    }

    private static bool IsWebSocketTransportEnvironmentGate(TiebaTransportException exception)
    {
        return exception.Message.Contains("WebSocket connect/handshake failed before the request pipeline became available.", StringComparison.Ordinal)
               || exception.Message.Contains("WebSocket request", StringComparison.Ordinal)
               || exception.Message.Contains("The WebSocket receive loop failed", StringComparison.Ordinal)
               || exception.Message.Contains("The WebSocket heartbeat loop failed", StringComparison.Ordinal);
    }

    private static async ValueTask SendCompensationMessageAsync(
        TiebaClient client,
        long recipientUserId,
        string compensationMarker,
        long originalMessageId,
        CancellationToken cancellationToken)
    {
        var compensationMessageId = await client.Messages.SendMessageAsync(
            recipientUserId,
            $"{compensationMarker} acknowledge safe message {originalMessageId}",
            cancellationToken);
        if (compensationMessageId <= 0)
        {
            throw new InvalidOperationException(
                $"Expected a positive message id when sending the compensation follow-up for private message {originalMessageId}.");
        }
    }

    private static string CreateMessageMarker(string prefix)
    {
        return $"{prefix}{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}";
    }
}
