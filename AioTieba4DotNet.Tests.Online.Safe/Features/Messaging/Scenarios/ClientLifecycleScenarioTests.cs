#nullable enable
using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
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
public sealed class ClientLifecycleScenarioTests : OnlineSafeExecutionTestBase
{
    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ClientInitWebSocketAsync)]
    public Task InitWebSocketAsyncMessagingCapableAccountWarmsTransportAndSupportsFollowUpReadOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "client lifecycle websocket warmup",
            async scope =>
            {
                using var client = MessagingScenarioTests.CreateClient(scope, TiebaTransportMode.Auto);

                var groups = await MessagingScenarioTests.RunWebSocketMessagingOrInconclusiveAsync(async () =>
                {
                    await client.Client.InitWebSocketAsync();
                    return await client.Messages.GetGroupMessagesAsync();
                });

                Assert.IsNotNull(groups);
            },
            OnlineExecutionCapability.Messaging);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ClientInitZIdAsync)]
    public Task InitZIdAsyncAuthenticatedAccountReturnsNonEmptyZIdOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "client lifecycle zid initialization",
            async scope =>
            {
                using var client = MessagingScenarioTests.CreateClient(scope, TiebaTransportMode.Http);
                var zId = await RunAuthenticatedClientLifecycleOrInconclusiveAsync(() => client.Client.InitZIdAsync());

                Assert.IsFalse(string.IsNullOrWhiteSpace(zId));
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ClientSyncAsync)]
    public Task SyncAsyncAuthenticatedAccountReturnsNonEmptyClientIdentifiersOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "client lifecycle sync",
            async scope =>
            {
                using var client = MessagingScenarioTests.CreateClient(scope, TiebaTransportMode.Http);
                var (clientId, sampleId) = await RunAuthenticatedClientLifecycleOrInconclusiveAsync(() => client.Client.SyncAsync());

                Assert.IsFalse(string.IsNullOrWhiteSpace(clientId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(sampleId));
            },
            OnlineExecutionCapability.Authenticated);
    }

    private static async Task<T> RunAuthenticatedClientLifecycleOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (exception.Code is 1 or 1130032 or 110000 or 110004)
        {
            Assert.Inconclusive($"Skipping client lifecycle read in this environment: {exception.Message}");
            throw;
        }
    }
}
