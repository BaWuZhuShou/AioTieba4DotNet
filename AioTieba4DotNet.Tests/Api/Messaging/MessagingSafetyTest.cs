using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Messaging;

[TestClass]
[TestCategory("Live")]
public class MessagingSafetyTest : TestBase
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

        Assert.IsTrue(chatroomId > 0);
    }
}
