#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.PushNotify;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public sealed class MessagesProtocolTests
{
    [TestMethod]
    public async Task MessageCursorStore_InitializesOrdersAndTracksPreviousMessageIds()
    {
        var store = new MessageCursorStore();

        store.Initialize([
            new WsMsgGroupInfo { GroupId = 99, GroupType = 1, LastMessageId = 7 },
            new WsMsgGroupInfo { GroupId = 88, GroupType = 6, LastMessageId = 5 },
            new WsMsgGroupInfo { GroupId = 0, GroupType = 6, LastMessageId = 11 }
        ]);
        store.Update(88, 12);
        store.Update(123, 25);
        store.Update(new WsMsgGroups([
            new WsMsgGroup { GroupId = 88, GroupType = 6, Messages = [CreateMessage(88, 100), CreateMessage(88, 120)] },
            new WsMsgGroup { GroupId = 123, GroupType = 1, Messages = [] }
        ]));

        CollectionAssert.AreEqual(new long[] { 88, 99, 123 }, (System.Collections.ICollection)store.GetKnownGroupIds());
        Assert.AreEqual(12L, store.GetLastMessageId(88));
        Assert.AreEqual(25L, store.GetLastMessageId(123));
    }

    [TestMethod]
    public async Task MessageCursorStore_EnsureInitializedAsync_LoadsOnlyOnce_AndGetRecordIdRequiresPrivateGroup()
    {
        var store = new MessageCursorStore();
        var loadCalls = 0;

        await store.EnsureInitializedAsync(_ =>
        {
            loadCalls++;
            return Task.FromResult<IReadOnlyList<WsMsgGroupInfo>>([
                new WsMsgGroupInfo { GroupId = 88, GroupType = 6, LastMessageId = 5 }
            ]);
        }, CancellationToken.None);
        await store.EnsureInitializedAsync(_ =>
        {
            loadCalls++;
            return Task.FromResult<IReadOnlyList<WsMsgGroupInfo>>([]);
        }, CancellationToken.None);

        Assert.AreEqual(1, loadCalls);
        Assert.AreEqual(501L, store.GetRecordId());

        var uninitialized = new MessageCursorStore();
        var exception = Throws<TiebaProtocolException>(() => uninitialized.GetRecordId());
        StringAssert.Contains(exception.Message, "private-message group id");
    }

    [TestMethod]
    public async Task MessagesProtocol_DelegatesAtAndReplyQueriesToUserProtocol()
    {
        var users = new RecordingUserProtocol();
        var protocol = CreateProtocol(new RecordingWsCore(), users);

        var ats = await protocol.GetAtsAsync(2);
        var replies = await protocol.GetRepliesAsync(3);

        Assert.AreEqual(2, users.LastAtPage);
        Assert.AreEqual(3, users.LastReplyPage);
        Assert.AreEqual(1, ats.Page.CurrentPage);
        Assert.AreEqual(1, replies.Page.CurrentPage);
    }

    [TestMethod]
    public async Task MessagesProtocol_GetGroupMessagesAsync_WithNoKnownGroups_ReturnsEmptyWithoutSecondRequest()
    {
        var wsCore = new RecordingWsCore();
        wsCore.Responses.Enqueue(CreateWsResponse(CreateInitResponse([]).ToByteArray()));
        var protocol = CreateProtocol(wsCore, new RecordingUserProtocol());

        var result = await protocol.GetGroupMessagesAsync(1);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(1, wsCore.Commands.Count);
        Assert.AreEqual(1001, wsCore.Commands[0]);
    }

    [TestMethod]
    public async Task MessagesProtocol_GetGroupMessagesAsync_PacksKnownGroupIdsAndMapsPayload()
    {
        var wsCore = new RecordingWsCore();
        wsCore.Responses.Enqueue(CreateWsResponse(CreateInitResponse([
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 88, GroupType = 6, LastMsgId = 5 },
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 99, GroupType = 1, LastMsgId = 7 }
        ]).ToByteArray()));
        wsCore.Responses.Enqueue(CreateWsResponse(CreateGroupMessageResponse(88, 6, 1234, "hello").ToByteArray()));
        var protocol = CreateProtocol(wsCore, new RecordingUserProtocol());

        var result = await protocol.GetGroupMessagesAsync(2);

        var request = global::GetGroupMsgReqIdl.Parser.ParseFrom(wsCore.Requests[1]);

        Assert.AreEqual(2, wsCore.Commands.Count);
        Assert.AreEqual(202003, wsCore.Commands[1]);
        Assert.AreEqual("2", request.Data.Gettype);
        Assert.AreEqual(2, request.Data.GroupMids.Count);
        Assert.AreEqual(88L, request.Data.GroupMids[0].GroupId);
        Assert.AreEqual(5L, request.Data.GroupMids[0].LastMsgId);
        Assert.AreEqual(99L, request.Data.GroupMids[1].GroupId);
        Assert.AreEqual(7L, request.Data.GroupMids[1].LastMsgId);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1234L, result[0].Messages[0].MsgId);
        Assert.AreEqual("hello", result[0].Messages[0].Text);
    }

    [TestMethod]
    public async Task MessagesProtocol_GetGroupMessagesAsync_WithExplicitGroupIds_UsesProvidedIds()
    {
        var wsCore = new RecordingWsCore();
        wsCore.Responses.Enqueue(CreateWsResponse(CreateInitResponse([
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 88, GroupType = 6, LastMsgId = 5 },
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 99, GroupType = 1, LastMsgId = 7 }
        ]).ToByteArray()));
        wsCore.Responses.Enqueue(CreateWsResponse(CreateGroupMessageResponse(88, 6, 1234, "hello").ToByteArray()));
        var protocol = CreateProtocol(wsCore, new RecordingUserProtocol());

        var result = await protocol.GetGroupMessagesAsync([88, 99], 2);

        var request = global::GetGroupMsgReqIdl.Parser.ParseFrom(wsCore.Requests[1]);

        Assert.AreEqual(2, wsCore.Commands.Count);
        Assert.AreEqual(2, request.Data.GroupMids.Count);
        Assert.AreEqual(88L, request.Data.GroupMids[0].GroupId);
        Assert.AreEqual(99L, request.Data.GroupMids[1].GroupId);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task MessagesProtocol_SendMessageAsync_ByUserId_InitializesCursorStoreAndUsesRecordId()
    {
        var wsCore = new RecordingWsCore();
        wsCore.Responses.Enqueue(CreateWsResponse(CreateInitResponse([
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 88, GroupType = 6, LastMsgId = 5 }
        ]).ToByteArray()));
        wsCore.Responses.Enqueue(CreateWsResponse(new global::CommitPersonalMsgResIdl
        {
            Error = new global::Error { Errorno = 0, Errmsg = string.Empty },
            Data = new global::CommitPersonalMsgResIdl.Types.DataRes { MsgId = 1234 }
        }.ToByteArray()));
        var protocol = CreateProtocol(wsCore, new RecordingUserProtocol());

        var messageId = await protocol.SendMessageAsync(42, "hello");

        var request = global::CommitPersonalMsgReqIdl.Parser.ParseFrom(wsCore.Requests[1]);

        Assert.AreEqual(1234L, messageId);
        Assert.AreEqual(205001, wsCore.Commands[1]);
        Assert.AreEqual(42L, request.Data.ToUid);
        Assert.AreEqual("hello", request.Data.Content);
        Assert.AreEqual(501L, request.Data.RecordId);
    }

    [TestMethod]
    public async Task MessagesProtocol_SendMessageAsync_ByPortrait_ResolvesUserBeforeSending()
    {
        var wsCore = new RecordingWsCore();
        wsCore.Responses.Enqueue(CreateWsResponse(CreateInitResponse([
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 88, GroupType = 6, LastMsgId = 5 }
        ]).ToByteArray()));
        wsCore.Responses.Enqueue(CreateWsResponse(new global::CommitPersonalMsgResIdl
        {
            Error = new global::Error { Errorno = 0, Errmsg = string.Empty },
            Data = new global::CommitPersonalMsgResIdl.Types.DataRes { MsgId = 6789 }
        }.ToByteArray()));
        var users = new RecordingUserProtocol
        {
            PanelInfo = new UserInfoPanel { UserId = 42, UserName = "resolved-user" }
        };
        var protocol = CreateProtocol(wsCore, users);

        var messageId = await protocol.SendMessageAsync("resolved-user", "hello");

        Assert.AreEqual("resolved-user", users.LastPanelInfoLookup);
        Assert.AreEqual(6789L, messageId);
    }

    [TestMethod]
    public async Task MessagesProtocol_SendChatroomMessageAsync_UsesExistingIdentifiers_AndForwardsArgumentsToSender()
    {
        var wsCore = new RecordingWsCore();
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateForumLevelResponse(userLevel: 9).ToByteArray()
        };
        var session = CreateSession(httpCore, wsCore);
        session.UpdateClientIdentifiers("client-existing", "sample-existing");
        var users = new RecordingUserProtocol
        {
            SelfInfo = new UserInfo { UserId = 42, UserName = "sender", NickNameNew = "Sender", Portrait = "tb.1.sender" }
        };

        Account? capturedAccount = null;
        UserInfo? capturedSelf = null;
        ForumLevelInfo? capturedForumLevel = null;
        long capturedChatroomId = 0;
        ulong capturedForumId = 0;
        string? capturedText = null;
        IReadOnlyList<long>? capturedAtUserIds = null;
        var capturedRobotCode = int.MinValue;
        var protocol = CreateProtocol(session, users,
            (account, selfInfo, forumLevel, chatroomId, forumId, text, atUserIds, robotCode, ct) =>
            {
                capturedAccount = account;
                capturedSelf = selfInfo;
                capturedForumLevel = forumLevel;
                capturedChatroomId = chatroomId;
                capturedForumId = forumId;
                capturedText = text;
                capturedAtUserIds = atUserIds;
                capturedRobotCode = robotCode;
                return Task.FromResult(true);
            });

        var result = await protocol.SendChatroomMessageAsync(12345, 7356044UL, "hello", [11L, 22L], 7);

        Assert.IsTrue(result);
        Assert.AreEqual(1, users.GetSelfInfoCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual(0, httpCore.SendAppFormCalls);
        Assert.AreEqual("sample-existing", capturedAccount?.SampleId);
        Assert.AreEqual("client-existing", capturedAccount?.ClientId);
        Assert.AreSame(users.SelfInfo, capturedSelf);
        Assert.AreEqual(9, capturedForumLevel?.UserLevel);
        Assert.AreEqual(12345L, capturedChatroomId);
        Assert.AreEqual(7356044UL, capturedForumId);
        Assert.AreEqual("hello", capturedText);
        CollectionAssert.AreEqual(new long[] { 11L, 22L }, capturedAtUserIds?.ToArray() ?? []);
        Assert.AreEqual(7, capturedRobotCode);
    }

    [TestMethod]
    public async Task MessagesProtocol_SendChatroomMessageAsync_SyncsMissingIdentifiers_BeforeInvokingSender()
    {
        var wsCore = new RecordingWsCore();
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","client":{"client_id":"client-synced"},"wl_config":{"sample_id":"sample-synced"}}
                              """,
            AppProtoResponse = CreateForumLevelResponse(userLevel: 12).ToByteArray()
        };
        var session = CreateSession(httpCore, wsCore);
        var users = new RecordingUserProtocol
        {
            SelfInfo = new UserInfo { UserId = 77, UserName = "self-user", Portrait = "tb.1.self" }
        };

        Account? capturedAccount = null;
        IReadOnlyList<long>? capturedAtUserIds = null;
        var capturedRobotCode = int.MinValue;
        var protocol = CreateProtocol(session, users,
            (account, selfInfo, forumLevel, chatroomId, forumId, text, atUserIds, robotCode, ct) =>
            {
                capturedAccount = account;
                capturedAtUserIds = atUserIds;
                capturedRobotCode = robotCode;
                Assert.AreSame(users.SelfInfo, selfInfo);
                Assert.AreEqual(12, forumLevel.UserLevel);
                Assert.AreEqual(54321L, chatroomId);
                Assert.AreEqual(81570UL, forumId);
                Assert.AreEqual("synced hello", text);
                return Task.FromResult(true);
            });

        var result = await protocol.SendChatroomMessageAsync(54321, 81570UL, "synced hello");

        Assert.IsTrue(result);
        Assert.AreEqual(1, httpCore.SendAppFormCalls);
        Assert.AreEqual(1, httpCore.SendAppProtoCalls);
        Assert.AreEqual("sample-synced", capturedAccount?.SampleId);
        Assert.AreEqual("client-synced", capturedAccount?.ClientId);
        Assert.IsNull(capturedAtUserIds);
        Assert.AreEqual(-1, capturedRobotCode);
    }

    [TestMethod]
    public async Task MessagesProtocol_DefaultChatroomSender_UsesBlcpSenderAndValidatesBeforeNetwork()
    {
        var method = typeof(MessagesProtocol).GetMethod("DefaultSendChatroomMessageAsync", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("DefaultSendChatroomMessageAsync not found.");

        var account = new Account(new string('b', 192), new string('s', 64));
        var selfInfo = new UserInfo { UserId = 42, UserName = "sender", Portrait = "tb.1.sender" };
        var forumLevel = new ForumLevelInfo { UserLevel = 9 };

        var exception = await ThrowsAsync<TiebaConfigurationException>(() =>
            (Task<bool>)method.Invoke(null, [account, selfInfo, forumLevel, 12345L, 7356044UL, "hello", null, 7, CancellationToken.None])!);

        StringAssert.Contains(exception.Message, nameof(Account.SampleId));
    }

    [TestMethod]
    public async Task MessagesProtocol_DefaultChatroomSender_IsSelectedWhenNoDelegateIsProvided()
    {
        var protocol = CreateProtocol(CreateSession(new RecordingHttpCore(), new RecordingWsCore()), new RecordingUserProtocol());
        var field = typeof(MessagesProtocol).GetField("_sendChatroomMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("_sendChatroomMessageAsync not found.");
        var handler = (SendChatroomMessageHandler)field.GetValue(protocol)!;

        var account = new Account(new string('b', 192), new string('s', 64));
        var selfInfo = new UserInfo { UserId = 42, UserName = "sender", Portrait = "tb.1.sender" };
        var forumLevel = new ForumLevelInfo { UserLevel = 9 };

        var exception = await ThrowsAsync<TiebaConfigurationException>(() =>
            handler(account, selfInfo, forumLevel, 12345L, 7356044UL, "hello", null, 7, CancellationToken.None));

        StringAssert.Contains(exception.Message, nameof(Account.SampleId));
    }

    [TestMethod]
    public async Task MessagesProtocol_SetMessageReadAsync_UsesPrivateGroupIdWhenAvailable()
    {
        var wsCore = new RecordingWsCore();
        wsCore.Responses.Enqueue(CreateWsResponse(CreateInitResponse([
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 88, GroupType = 6, LastMsgId = 5 }
        ]).ToByteArray()));
        wsCore.Responses.Enqueue(CreateWsResponse(new global::CommitReceivedPmsgResIdl
        {
            Error = new global::Error { Errorno = 0, Errmsg = string.Empty }
        }.ToByteArray()));
        var protocol = CreateProtocol(wsCore, new RecordingUserProtocol());

        var success = await protocol.SetMessageReadAsync(CreateMessage(groupId: 77, msgId: 1234, userId: 42));

        var request = global::CommitReceivedPmsgReqIdl.Parser.ParseFrom(wsCore.Requests[1]);

        Assert.IsTrue(success);
        Assert.AreEqual(205006, wsCore.Commands[1]);
        Assert.AreEqual(88L, request.Data.GroupId);
        Assert.AreEqual(42L, request.Data.ToUid);
        Assert.AreEqual(1234L, request.Data.MsgId);
        Assert.AreEqual(22, request.Data.MsgType);
    }

    [TestMethod]
    public void MessagesProtocol_ParsePushNotifications_UsesPushNotifyParser()
    {
        var protocol = CreateProtocol(new RecordingWsCore(), new RecordingUserProtocol());
        var payload = new global::PushNotifyResIdl();
        payload.MultiMsg.Add(new global::PushNotifyResIdl.Types.PusherMsg
        {
            Data = new global::PushNotifyResIdl.Types.PusherMsg.Types.PusherMsgInfo
            {
                GroupId = 12345,
                MsgId = 998877,
                Type = 202006,
                Et = "1711111111",
                GroupType = 6
            }
        });

        var notifications = protocol.ParsePushNotifications(payload.ToByteArray());

        Assert.AreEqual(1, notifications.Count);
        Assert.AreEqual(12345L, notifications[0].GroupId);
        Assert.AreEqual(998877L, notifications[0].MsgId);
    }

    [TestMethod]
    public async Task MessagesProtocol_ValidationPaths_ThrowBeforeNetworkCalls()
    {
        var wsCore = new RecordingWsCore();
        var users = new RecordingUserProtocol { PanelInfo = new UserInfoPanel { UserId = 0 } };
        var protocol = CreateProtocol(wsCore, users);

        await ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetGroupMessagesAsync(0));
        await ThrowsAsync<ArgumentNullException>(() => protocol.GetGroupMessagesAsync((IReadOnlyList<long>)null!, 1));
        await ThrowsAsync<ArgumentException>(() => protocol.GetGroupMessagesAsync([], 1));
        await ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.GetGroupMessagesAsync([0], 1));
        await ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.SendMessageAsync(0, "hello"));
        await ThrowsAsync<ArgumentException>(() => protocol.SendMessageAsync(42, " "));
        await ThrowsAsync<ArgumentException>(() => protocol.SendMessageAsync(" ", "hello"));
        await ThrowsAsync<TiebaProtocolException>(() => protocol.SendMessageAsync("missing-user", "hello"));
        await ThrowsAsync<ArgumentNullException>(() => protocol.SetMessageReadAsync(null!));
        await ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.SetMessageReadAsync(CreateMessage(groupId: 1, msgId: 0)));
        await ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.SendChatroomMessageAsync(0, 1, "hi"));
        await ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.SendChatroomMessageAsync(1, 0, "hi"));
        await ThrowsAsync<ArgumentException>(() => protocol.SendChatroomMessageAsync(1, 1, " "));
        await ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.SendChatroomMessageAsync(1, 1, "hi", [0]));

        Assert.AreEqual(0, wsCore.Commands.Count);
    }

    [TestMethod]
    public async Task MessagesProtocol_SetMessageReadAsync_ThrowsWhenNoPrivateOrMessageGroupIdIsAvailable()
    {
        var wsCore = new RecordingWsCore();
        wsCore.Responses.Enqueue(CreateWsResponse(CreateInitResponse([
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 99, GroupType = 1, LastMsgId = 5 }
        ]).ToByteArray()));
        var protocol = CreateProtocol(wsCore, new RecordingUserProtocol());

        var exception = await ThrowsAsync<TiebaProtocolException>(() =>
            protocol.SetMessageReadAsync(CreateMessage(groupId: 0, msgId: 1234, userId: 42)));

        StringAssert.Contains(exception.Message, "private-message group id");
    }

    [TestMethod]
    public async Task MessagesProtocol_SetMessageReadAsync_CoversPrivateGroupIdFallback()
    {
        var wsCore = new RecordingWsCore();
        wsCore.Responses.Enqueue(CreateWsResponse(CreateInitResponse([
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 88, GroupType = 6, LastMsgId = 5 },
            new global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo { GroupId = 99, GroupType = 1, LastMsgId = 7 }
        ]).ToByteArray()));
        wsCore.Responses.Enqueue(CreateWsResponse(new global::CommitReceivedPmsgResIdl
        {
            Error = new global::Error { Errorno = 0, Errmsg = string.Empty }
        }.ToByteArray()));
        var protocol = CreateProtocol(wsCore, new RecordingUserProtocol());

        var success = await protocol.SetMessageReadAsync(CreateMessage(groupId: 0, msgId: 1234, userId: 42));

        var request = global::CommitReceivedPmsgReqIdl.Parser.ParseFrom(wsCore.Requests[1]);

        Assert.IsTrue(success);
        Assert.AreEqual(205006, wsCore.Commands[1]);
        Assert.AreEqual(88L, request.Data.GroupId);
        Assert.AreEqual(42L, request.Data.ToUid);
        Assert.AreEqual(1234L, request.Data.MsgId);
        Assert.AreEqual(22, request.Data.MsgType);
    }

    private static MessagesProtocol CreateProtocol(RecordingWsCore wsCore, RecordingUserProtocol users)
    {
        return CreateProtocol(CreateSession(new RecordingHttpCore(), wsCore), users);
    }

    private static MessagesProtocol CreateProtocol(TiebaClientSession session, RecordingUserProtocol users,
        SendChatroomMessageHandler? sendChatroomMessageAsync = null)
        => new(new TiebaOperationDispatcher(session), users, sendChatroomMessageAsync);

    private static TiebaClientSession CreateSession(RecordingHttpCore httpCore, RecordingWsCore wsCore)
    {
        return new TiebaClientSession(
            new TiebaOptions
            {
                Bduss = new string('b', 192),
                Stoken = new string('s', 64),
                TransportMode = TiebaTransportMode.Auto
            },
            httpCore,
            wsCore);
    }

    private static global::UpdateClientInfoResIdl CreateInitResponse(
        IEnumerable<global::UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo> groups)
    {
        var response = new global::UpdateClientInfoResIdl
        {
            Error = new global::Error { Errorno = 0, Errmsg = string.Empty },
            Data = new global::UpdateClientInfoResIdl.Types.DataRes()
        };
        response.Data.GroupInfo.AddRange(groups);
        return response;
    }

    private static global::GetLevelInfoResIdl CreateForumLevelResponse(int userLevel)
    {
        return new global::GetLevelInfoResIdl
        {
            Error = new global::Error { Errorno = 0, Errmsg = string.Empty },
            Data = new global::GetLevelInfoResIdl.Types.DataRes
            {
                LevelName = $"Lv{userLevel}",
                UserLevel = userLevel,
                IsLike = 1
            }
        };
    }

    private static global::GetGroupMsgResIdl CreateGroupMessageResponse(long groupId, int groupType, long msgId,
        string text)
    {
        var response = new global::GetGroupMsgResIdl
        {
            Error = new global::Error { Errorno = 0, Errmsg = string.Empty },
            Data = new global::GetGroupMsgResIdl.Types.DataRes()
        };
        response.Data.GroupInfo.Add(new global::GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg
        {
            GroupInfo = new global::GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.GroupInfo
            {
                GroupId = groupId,
                GroupType = groupType
            },
            MsgList =
            {
                new global::GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.MsgInfo
                {
                    MsgId = msgId,
                    MsgType = 1,
                    Content = text,
                    CreateTime = 1711111111,
                    UserInfo = new global::GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.MsgInfo.Types.UserInfo
                    {
                        UserId = 42,
                        UserName = "sender",
                        Portrait = "tb.1.sender?012345678901"
                    }
                }
            }
        });
        return response;
    }

    private static WsMessage CreateMessage(long groupId, long msgId, long userId = 42) => new()
    {
        GroupId = groupId,
        GroupType = 6,
        MsgId = msgId,
        MsgType = 1,
        Text = "text",
        User = new UserInfo { UserId = userId, UserName = "sender" }
    };

    private static WSRes CreateWsResponse(byte[] payload) =>
        new() { Payload = new WSRes.Types.Payload { Data = ByteString.CopyFrom(payload) } };

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public Account? Account { get; private set; }
        public HttpClient HttpClient { get; } = new();
        public int SendAppFormCalls { get; private set; }
        public int SendAppProtoCalls { get; private set; }
        public Uri? LastAppFormUri { get; private set; }
        public Uri? LastAppProtoUri { get; private set; }
        public IReadOnlyList<KeyValuePair<string, string>>? LastAppFormData { get; private set; }
        public byte[]? LastAppProtoRequestData { get; private set; }
        public string AppFormResponse { get; set; } = string.Empty;
        public byte[] AppProtoResponse { get; set; } = [];

        public void SetAccount(Account newAccount) => Account = newAccount;

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            SendAppFormCalls++;
            LastAppFormUri = uri;
            LastAppFormData = data.ToArray();
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            SendAppProtoCalls++;
            LastAppProtoUri = uri;
            LastAppProtoRequestData = data.ToArray();
            return Task.FromResult(AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class RecordingWsCore : ITiebaWsCore
    {
        public Queue<WSRes> Responses { get; } = new();
        public List<int> Commands { get; } = [];
        public List<byte[]> Requests { get; } = [];
        public Account? Account { get; set; }

        public void SetAccount(Account newAccount) => Account = newAccount;

        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(cmd);
            Requests.Add(data);
            return Task.FromResult(Responses.Dequeue());
        }

        public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingUserProtocol : IUserProtocol
    {
        public int LastAtPage { get; private set; }
        public int LastReplyPage { get; private set; }
        public int GetSelfInfoCalls { get; private set; }
        public string? LastPanelInfoLookup { get; private set; }
        public UserInfoPanel PanelInfo { get; init; } = new() { UserId = 42, UserName = "resolved-user" };
        public UserInfo SelfInfo { get; init; } = new() { UserId = 42, UserName = "sender", Portrait = "tb.1.sender" };

        public Task<string> GetTbsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> BlockAsync(ulong fid, string portrait, int day, string reason, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> BlockAsync(string fname, string portrait, int day, string reason, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserList> GetFollowsAsync(long userId, int pn, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserList> GetFansAsync(long userId, int pn, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default)
        {
            LastPanelInfoLookup = nameOrPortrait;
            return Task.FromResult(PanelInfo);
        }
        public Task<UserInfoJson> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default)
        {
            GetSelfInfoCalls++;
            return Task.FromResult(SelfInfo);
        }
        public Task<UserInfo> GetSelfInfoInitNicknameAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SelfInfo);
        public Task<UserInfo> GetSelfInfoMoIndexAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SelfInfo);
        public Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LoginResult { User = SelfInfo });
        public Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default)
        {
            LastAtPage = pn;
            return Task.FromResult(new AtMessages([], new PageT { CurrentPage = 1 }));
        }
        public Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default)
        {
            LastReplyPage = pn;
            return Task.FromResult(new ReplyMessages([], new PageT { CurrentPage = 1 }));
        }
        public Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<BlacklistOldUsers> GetBlacklistLegacyAsync(int pn, int rn, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> SetBlacklistAsync(long userId, BlacklistType type, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AddBlacklistLegacyAsync(long userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RemoveBlacklistLegacyAsync(long userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserInfoGuInfoWeb> GetBasicInfoWebAsync(int userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserForumInfo> GetUserForumInfoAsync(ulong fid, string portrait, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserForumInfo> GetUserForumInfoAsync(string fname, string portrait, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RankUsers> GetRankUsersAsync(string fname, int pn, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Homepage> GetHomepageAsync(int userId, int pn, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> SetNicknameLegacyAsync(string nickName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> SetProfileAsync(string nickName, string sign, Gender gender, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserInfoTUid> GetUserByTiebaUidAsync(long tiebaUid, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserPostss> GetPostsAsync(int userId, uint pn, uint rn, string version, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UserThreads> GetThreadsAsync(int userId, uint pn, bool publicOnly, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
    }

    private static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
    }
}
