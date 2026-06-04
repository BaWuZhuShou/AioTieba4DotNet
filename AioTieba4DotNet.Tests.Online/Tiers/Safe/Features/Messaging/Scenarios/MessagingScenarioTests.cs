#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Tiers.Safe.Features.Messaging.Scenarios;

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
    public Task GetAtsAsyncAuthenticatedAccountReturnsInboxPageShape()
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
    public Task GetRepliesAsyncAuthenticatedAccountReturnsInboxPageShapeOrExplicitEndpointGate()
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
    public Task GetGroupMessagesAsyncInitializedWebSocketReturnsMessageGroupContainerOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "messaging websocket group read sample",
            async scope =>
            {
                using var client = CreateClient(scope, TiebaTransportMode.Auto);

                var groups = await RunWebSocketMessagingOrInconclusiveAsync(() => client.Messages.GetGroupMessagesAsync());

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
    [TestCategory(OnlineTestApiCategories.MessagesGetGroupMessagesAsync)]
    public Task GetGroupMessagesAsyncDedicatedGroupIdReturnsSelectedMessageGroupsOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "messaging websocket explicit-group read sample",
            async scope =>
            {
                using var client = CreateClient(scope, TiebaTransportMode.Auto);
                var operationName = nameof(GetGroupMessagesAsyncDedicatedGroupIdReturnsSelectedMessageGroupsOrExplicitSkip);
                var groupId = RequireChatroomId(scope, operationName);

                var selectedGroups = await RunWebSocketMessagingOrInconclusiveAsync(() => client.Messages.GetGroupMessagesAsync([groupId], 1));

                Assert.IsNotNull(selectedGroups);
                if (!selectedGroups.Any())
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated websocket group '{groupId}' currently returned no message groups, so explicit-group messaging coverage cannot prove a non-empty selection in this environment.");
                }

                foreach (var group in selectedGroups)
                {
                    Assert.IsPositive(group.GroupId);
                    Assert.IsNotNull(group.Messages);
                }
            },
            OnlineExecutionCapability.Messaging);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.UsersGetPanelInfoAsync)]
    public Task GetPanelInfoAsyncDedicatedRecipientReturnsStableIdentity()
    {
        return ExecuteSafeAsync(
            "messaging recipient panel identity sample",
            async scope =>
            {
                using var client = CreateClient(scope, TiebaTransportMode.Http);
                var recipient = RequireMessageRecipient(scope, nameof(GetPanelInfoAsyncDedicatedRecipientReturnsStableIdentity));
                var recipientInfo = await client.Users.GetPanelInfoAsync(recipient);

                AssertRecipientInfoShape(recipientInfo);
            },
            OnlineExecutionCapability.Messaging);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.MessagesSendMessageAsync)]
    public Task SendMessageAsyncDedicatedRecipientStringUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "messaging private-message recipient overload",
            async scope =>
            {
                var operationName = nameof(SendMessageAsyncDedicatedRecipientStringUsesCompensationAudit);
                using var client = CreateClient(scope, TiebaTransportMode.Http);
                var recipient = RequireMessageRecipient(scope, operationName);
                var messageMarker = CreateMessageMarker("safemsgname");
                var compensationMarker = CreateMessageMarker("safemsgundoname");
                long sentMessageId;
                try
                {
                    sentMessageId = await client.Messages.SendMessageAsync(recipient, messageMarker);
                }
                catch (TiebaProtocolException exception) when (IsMessageRecipientResolutionGate(exception))
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: safe string-recipient messaging requires {OnlineTestEnvironmentVariables.SafeAssetsMessageRecipient} to resolve to a valid private-message user id in this environment ({exception.Message}).");
                    return;
                }

                Assert.IsPositive(sentMessageId);

                var sentArtifact = scope.Compensation.RecordCreatedArtifact(
                    OnlineTestStageCategories.Messaging,
                    "private-message",
                    sentMessageId,
                    $"safe private message '{messageMarker}' to dedicated recipient '{recipient}' via portrait-or-username overload");
                scope.Compensation.Register(
                    sentArtifact,
                    "send private-message compensation notice for recipient overload",
                    "compensation notice sent",
                    cancellationToken => SendStringCompensationMessageAsync(
                        client,
                        recipient,
                        compensationMarker,
                        sentMessageId,
                        cancellationToken));

                await scope.Compensation.ExecuteAsync();
                AssertSingleMessageCompensationAudit(scope, messageMarker, "compensation notice sent");
            },
            OnlineExecutionCapability.Messaging);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.MessagesSendMessageAsync)]
    public Task SendMessageAsyncDedicatedRecipientUserIdUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "messaging private-message user-id overload",
            async scope =>
            {
                var operationName = nameof(SendMessageAsyncDedicatedRecipientUserIdUsesCompensationAudit);
                using var client = CreateClient(scope, TiebaTransportMode.Http);
                var recipientUserId = RequireMessageRecipientUserId(scope, operationName);
                var messageMarker = CreateMessageMarker("safemsgid");
                var compensationMarker = CreateMessageMarker("safemsgundoid");
                var sentMessageId = await client.Messages.SendMessageAsync(recipientUserId, messageMarker);

                Assert.IsPositive(sentMessageId);

                var sentArtifact = scope.Compensation.RecordCreatedArtifact(
                    OnlineTestStageCategories.Messaging,
                    "private-message",
                    sentMessageId,
                    $"safe private message '{messageMarker}' to dedicated recipient user id '{recipientUserId}' via numeric user-id overload");
                scope.Compensation.Register(
                    sentArtifact,
                    "send private-message compensation notice for user-id overload",
                    "compensation notice sent",
                    cancellationToken => SendNumericCompensationMessageAsync(
                        client,
                        recipientUserId,
                        compensationMarker,
                        sentMessageId,
                        cancellationToken));

                await scope.Compensation.ExecuteAsync();
                AssertSingleMessageCompensationAudit(scope, messageMarker, "compensation notice sent");
            },
            OnlineExecutionCapability.Messaging);
    }

    internal static TiebaClient CreateClient(OnlineExecutionScope scope, TiebaTransportMode transportMode)
    {
        var options = new TiebaOptions
        {
            Bduss = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Bduss : null,
            Stoken = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Stoken : null,
            TransportMode = transportMode
        };

        return new TiebaClient(options);
    }

    private static void AssertRecipientInfoShape(UserInfo recipientInfo)
    {
        Assert.IsNotNull(recipientInfo);
        Assert.IsFalse(
            recipientInfo.UserId <= 0
            && string.IsNullOrWhiteSpace(recipientInfo.Portrait)
            && string.IsNullOrWhiteSpace(recipientInfo.UserName),
            "Expected messaging panel info to expose at least one stable recipient identity signal.");
    }

    private static void AssertSingleMessageCompensationAudit(
        OnlineExecutionScope scope,
        string messageMarker,
        string expectedOutcome)
    {
        var audit = scope.Compensation.GetLastAudit();
        Assert.IsNotNull(audit);
        Assert.IsTrue(audit.Succeeded,
            "Expected the Messaging safe scenario to reconcile the private-message mutation via a compensating follow-up message.");
        Assert.HasCount(1, audit.RecordedArtifacts);
        Assert.HasCount(1, audit.CompensationResults);
        Assert.IsEmpty(audit.UnreconciledArtifacts);
        Assert.AreEqual(expectedOutcome, audit.CompensationResults[0].CompensationOutcome);

        var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains(messageMarker, auditDisplay);
        Assert.Contains("unreconciled: none", auditDisplay);
    }

    private static string RequireMessageRecipient(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.MessageRecipient))
            return scope.Safe.Assets.MessageRecipient;

        Assert.Inconclusive(
            $"Skipping {operationName}: safe private-message mutation requires a dedicated recipient asset. Set {OnlineTestEnvironmentVariables.SafeAssetsMessageRecipient} before sending any message.");
        return string.Empty;
    }

    private static long RequireMessageRecipientUserId(OnlineExecutionScope scope, string operationName)
    {
        var recipient = RequireMessageRecipient(scope, operationName);
        if (long.TryParse(recipient, out var recipientUserId) && recipientUserId > 0)
            return recipientUserId;

        Assert.Inconclusive(
            $"Skipping {operationName}: numeric private-message overload coverage requires {OnlineTestEnvironmentVariables.SafeAssetsMessageRecipient} to be a positive user id string so the test stays attributable to Messages.SendMessageAsync(long, ...). ");
        return default;
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

    internal static async Task<T> RunWebSocketMessagingOrInconclusiveAsync<T>(Func<Task<T>> action)
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

    private static bool IsMessageRecipientResolutionGate(TiebaProtocolException exception)
    {
        return exception.Message.Contains("Unable to resolve a valid user id", StringComparison.Ordinal);
    }

    private static async ValueTask SendStringCompensationMessageAsync(
        TiebaClient client,
        string recipient,
        string compensationMarker,
        long originalMessageId,
        CancellationToken cancellationToken)
    {
        var compensationMessageId = await client.Messages.SendMessageAsync(
            recipient,
            $"{compensationMarker} acknowledge safe message {originalMessageId}",
            cancellationToken);
        if (compensationMessageId <= 0)
        {
            throw new InvalidOperationException(
                $"Expected a positive message id when sending the compensation follow-up for private message {originalMessageId}.");
        }
    }

    private static async ValueTask SendNumericCompensationMessageAsync(
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
