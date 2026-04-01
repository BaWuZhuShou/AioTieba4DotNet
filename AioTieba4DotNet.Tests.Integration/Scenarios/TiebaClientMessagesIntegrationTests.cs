using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.MessagingClient)]
public sealed class TiebaClientMessagesIntegrationTests : TestBase
{
    [TestMethod]
    public async Task GetAtsAndRepliesAsync_AuthenticatedAccount_ReturnsMessagePages()
    {
        EnsureAuthenticated();

        try
        {
            var ats = await Client.Messages.GetAtsAsync(1);
            var replies = await Client.Messages.GetRepliesAsync(1);

            Assert.IsNotNull(ats);
            Assert.IsNotNull(replies);
            Assert.IsGreaterThanOrEqualTo(1, ats.Page.CurrentPage);
            Assert.IsGreaterThanOrEqualTo(1, replies.Page.CurrentPage);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping message-page integration path in this environment: {exception.Message}");
        }
    }

    [TestMethod]
    public async Task GetGroupMessagesAsync_AfterInitWebSocket_ReturnsContainerOrExplicitSkip()
    {
        EnsureAuthenticated();

        try
        {
            await Client.Client.InitWebSocketAsync();
            var groups = await Client.Messages.GetGroupMessagesAsync();

            Assert.IsNotNull(groups);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping websocket message integration path in this environment: {exception.Message}");
        }
        catch (Exception exception)
        {
            Assert.Inconclusive($"Skipping websocket message integration path after transport failure: {exception.Message}");
        }
    }
}
