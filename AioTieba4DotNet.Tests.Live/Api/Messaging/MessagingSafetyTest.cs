using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Messaging;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.MessagingClient)]
public sealed class MessagingSafetyTest : TestBase
{
    [TestMethod]
    public void SafeMessageRecipientFixture_IsExplicitlyRequired_ForLiveMessagingActions()
    {
        var recipient = RequireSafeMessageRecipientFixture(
            nameof(SafeMessageRecipientFixture_IsExplicitlyRequired_ForLiveMessagingActions));

        Assert.IsFalse(string.IsNullOrWhiteSpace(recipient));
    }

    [TestMethod]
    public void SafeChatroomIdFixture_IsExplicitlyRequired_ForLiveChatroomActions()
    {
        var chatroomId = RequireSafeChatroomIdFixture(
            nameof(SafeChatroomIdFixture_IsExplicitlyRequired_ForLiveChatroomActions));

        Assert.IsGreaterThan(0, chatroomId);
    }
}
