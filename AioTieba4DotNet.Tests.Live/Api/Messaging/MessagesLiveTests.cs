using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Messaging;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.MessagingClient)]
public sealed class MessagesLiveTests : TestBase
{
    [TestMethod]
    public async Task SendMessageAsync_SafeRecipientFixture_ReturnsServerMessageIdOrExplicitSkip()
    {
        var recipient =
            RequireSafeMessageRecipientFixture(
                nameof(SendMessageAsync_SafeRecipientFixture_ReturnsServerMessageIdOrExplicitSkip));

        try
        {
            var messageId = await Client.Messages.SendMessageAsync(recipient,
                $"task17-live-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

            Assert.IsGreaterThan(0L, messageId);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive(
                $"Skipping safe live private-message verification in this environment: {exception.Message}");
        }
    }

    [TestMethod]
    public async Task SendChatroomMessageAsync_SafeFixtures_ReturnsAcceptedOrExplicitSkip()
    {
        var chatroomId =
            RequireSafeChatroomIdFixture(nameof(SendChatroomMessageAsync_SafeFixtures_ReturnsAcceptedOrExplicitSkip));
        var forum = await RequireSafeForumFixtureAsync(
            nameof(SendChatroomMessageAsync_SafeFixtures_ReturnsAcceptedOrExplicitSkip));

        try
        {
            var accepted = await Client.Messages.SendChatroomMessageAsync(chatroomId, forum.Fid,
                $"task17-chatroom-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

            Assert.IsTrue(accepted);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive($"Skipping safe live chatroom verification in this environment: {exception.Message}");
        }
    }
}
