#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.PushNotify;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Tests.Infrastructure;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public sealed class MessagesModuleBehaviorTests
{
    [TestMethod]
    public async Task MessagesModule_DelegatesGetAtsToInternalProtocol()
    {
        var expected = new AtMessages([], new Models.Threads.PageT { CurrentPage = 2 });
        var protocol = new RecordingMessagesProtocol
        {
            AtMessagesResult = expected
        };
        var module = new MessagesModule(protocol);

        var actual = await module.GetAtsAsync(2);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(2, protocol.LastAtsPn);
    }

    [TestMethod]
    public async Task MessagesModule_DelegatesGetRepliesToInternalProtocol()
    {
        var expected = new ReplyMessages([], new Models.Threads.PageT { CurrentPage = 3 });
        var protocol = new RecordingMessagesProtocol
        {
            ReplyMessagesResult = expected
        };
        var module = new MessagesModule(protocol);

        var actual = await module.GetRepliesAsync(3);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(3, protocol.LastRepliesPn);
    }

    [TestMethod]
    public async Task MessagesModule_DelegatesSendMessageToInternalProtocol()
    {
        var protocol = new RecordingMessagesProtocol();
        var module = new MessagesModule(protocol);

        var messageId = await module.SendMessageAsync(123, "hello");

        Assert.AreEqual(778899L, messageId);
        Assert.AreEqual(123L, protocol.LastSendUserId);
        Assert.AreEqual("hello", protocol.LastSendContent);
    }

    [TestMethod]
    public async Task MessagesModule_DelegatesChatroomSendToInternalProtocol()
    {
        var protocol = new RecordingMessagesProtocol();
        var module = new MessagesModule(protocol);

        var accepted = await module.SendChatroomMessageAsync(456, 789, "group hi", [11, 22], 10005);

        Assert.IsTrue(accepted);
        Assert.AreEqual(456L, protocol.LastChatroomId);
        Assert.AreEqual(789UL, protocol.LastForumId);
        CollectionAssert.AreEqual(new long[] { 11, 22 }, protocol.LastAtUserIds!.ToArray());
        Assert.AreEqual(10005, protocol.LastRobotCode);
    }

    [TestMethod]
    public void MessagesModule_ParsesPushNotificationsExplicitly()
    {
        var module = new MessagesModule(new RecordingMessagesProtocol());
        var payload = BuildPushPayload();

        var notifications = module.ParsePushNotifications(payload);

        Assert.AreEqual(1, notifications.Count);
        Assert.AreEqual(202006, notifications[0].NoteType);
        Assert.AreEqual(12345L, notifications[0].GroupId);
        Assert.AreEqual(6, notifications[0].GroupType);
        Assert.AreEqual(998877L, notifications[0].MsgId);
        Assert.AreEqual(1711111111L, notifications[0].CreateTime);
    }

    [TestMethod]
    public void MessagesModule_PublicContracts_FreezeCanonicalReadNames()
    {
        var messagesSource = RepositorySourceTextAssert.ReadRepositoryFiles(
            "AioTieba4DotNet/Contracts/IMessagesModule.cs",
            "AioTieba4DotNet/Protocols/IMessagesProtocol.cs",
            "AioTieba4DotNet/Modules/MessagesModule.cs");

        RepositorySourceTextAssert.ContainsAll(
            messagesSource,
            "GetAtsAsync",
            "GetRepliesAsync",
            "SendMessageAsync",
            "SetMessageReadAsync",
            "ParsePushNotifications");
    }

    private static byte[] BuildPushPayload()
    {
        var response = new PushNotifyResIdl();
        response.MultiMsg.Add(new PushNotifyResIdl.Types.PusherMsg
        {
            Data = new PushNotifyResIdl.Types.PusherMsg.Types.PusherMsgInfo
            {
                GroupId = 12345,
                MsgId = 998877,
                Type = 202006,
                Et = "1711111111",
                GroupType = 6
            }
        });
        return response.ToByteArray();
    }

    private sealed class RecordingMessagesProtocol : IMessagesProtocol
    {
        public AtMessages AtMessagesResult { get; init; } = new([], new Models.Threads.PageT());

        public ReplyMessages ReplyMessagesResult { get; init; } = new([], new Models.Threads.PageT());

        public int? LastAtsPn { get; private set; }

        public int? LastRepliesPn { get; private set; }

        public long? LastSendUserId { get; private set; }

        public string? LastSendContent { get; private set; }

        public long? LastChatroomId { get; private set; }

        public ulong? LastForumId { get; private set; }

        public IReadOnlyList<long>? LastAtUserIds { get; private set; }

        public int? LastRobotCode { get; private set; }

        public Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default)
        {
            LastAtsPn = pn;
            return Task.FromResult(AtMessagesResult);
        }

        public Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default)
        {
            LastRepliesPn = pn;
            return Task.FromResult(ReplyMessagesResult);
        }

        public Task<WsMsgGroups> GetGroupMessagesAsync(int getType, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WsMsgGroups([]));

        public Task<WsMsgGroups> GetGroupMessagesAsync(IReadOnlyList<long> groupIds, int getType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new WsMsgGroups([]));

        public Task<long> SendMessageAsync(long userId, string content, CancellationToken cancellationToken = default)
        {
            LastSendUserId = userId;
            LastSendContent = content;
            return Task.FromResult(778899L);
        }

        public Task<long> SendMessageAsync(string portraitOrUserName, string content,
            CancellationToken cancellationToken = default) => Task.FromResult(778899L);

        public Task<bool> SendChatroomMessageAsync(long chatroomId, ulong forumId, string text,
            IReadOnlyList<long>? atUserIds = null, int robotCode = -1, CancellationToken cancellationToken = default)
        {
            LastChatroomId = chatroomId;
            LastForumId = forumId;
            LastAtUserIds = atUserIds;
            LastRobotCode = robotCode;
            return Task.FromResult(true);
        }

        public Task<bool> SetMessageReadAsync(WsMessage message, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public IReadOnlyList<WsNotify> ParsePushNotifications(byte[] payload) => PushNotify.ParseBody(payload);
    }
}
