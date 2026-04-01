using AioTieba4DotNet.Api.GetForumLevel;
using AioTieba4DotNet.Api.GetGroupMsg;
using AioTieba4DotNet.Api.GetAts;
using AioTieba4DotNet.Api.GetReplys;
using AioTieba4DotNet.Api.InitWebSocket;
using AioTieba4DotNet.Api.PushNotify;
using AioTieba4DotNet.Api.SendMsg;
using AioTieba4DotNet.Api.SetMsgReaded;
using AioTieba4DotNet.Api.Sync;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.Chatrooms;

namespace AioTieba4DotNet.Protocols;

internal delegate Task<bool> SendChatroomMessageHandler(Account account, UserInfo selfInfo, ForumLevelInfo forumLevel,
    long chatroomId, ulong forumId, string text, IReadOnlyList<long>? atUserIds, int robotCode,
    CancellationToken cancellationToken);

internal sealed class MessagesProtocol(TiebaOperationDispatcher dispatcher, IUserProtocol users,
    SendChatroomMessageHandler? sendChatroomMessageAsync = null) : IMessagesProtocol
{
    private readonly MessageCursorStore _cursorStore = new();
    private readonly SendChatroomMessageHandler _sendChatroomMessageAsync =
        sendChatroomMessageAsync ?? DefaultSendChatroomMessageAsync;

    public async Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<AtMessages>(
                nameof(GetAtsAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetAts(session.HttpCore).RequestAsync(pn, ct)),
            cancellationToken);
    }

    public async Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageNumber(pn);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<ReplyMessages>(
                nameof(GetRepliesAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                (session, ct) => new GetReplys(session.HttpCore).RequestAsync(pn, ct)),
            cancellationToken);
    }

    private static Task<bool> DefaultSendChatroomMessageAsync(Account account, UserInfo selfInfo,
        ForumLevelInfo forumLevel, long chatroomId, ulong forumId, string text, IReadOnlyList<long>? atUserIds,
        int robotCode, CancellationToken cancellationToken)
        => new BlcpChatroomSender().SendMessageAsync(account, selfInfo, forumLevel, chatroomId, forumId, text,
            atUserIds, robotCode, cancellationToken);

    public async Task<WsMsgGroups> GetGroupMessagesAsync(int getType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateGetType(getType);

        await EnsureCursorStoreInitializedAsync(cancellationToken);
        var groupIds = _cursorStore.GetKnownGroupIds();
        if (groupIds.Count == 0)
            return new WsMsgGroups([]);

        return await GetGroupMessagesCoreAsync(groupIds, getType, cancellationToken);
    }

    public async Task<WsMsgGroups> GetGroupMessagesAsync(IReadOnlyList<long> groupIds, int getType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateGroupIds(groupIds);
        ValidateGetType(getType);

        await EnsureCursorStoreInitializedAsync(cancellationToken);
        return await GetGroupMessagesCoreAsync(groupIds, getType, cancellationToken);
    }

    public async Task<long> SendMessageAsync(long userId, string content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUserId(userId);
        ValidateRequiredText(nameof(content), content, "Message content must not be blank.");

        await EnsureCursorStoreInitializedAsync(cancellationToken);

        var messageId = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<long>(
                nameof(SendMessageAsync),
                TiebaOperationCapabilities.WebSocketOnly(requiresAuthentication: true),
                ExecuteWebSocketAsync: (session, ct) =>
                    new SendMsg(session.WsCore).RequestAsync(userId, content, _cursorStore.GetRecordId(), ct)),
            cancellationToken);

        if (_cursorStore.PrivateGroupId > 0)
            _cursorStore.Update(_cursorStore.PrivateGroupId, messageId);

        return messageId;
    }

    public async Task<long> SendMessageAsync(string portraitOrUserName, string content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequiredText(nameof(portraitOrUserName), portraitOrUserName,
            "Portrait or user name must not be blank.");

        var user = await users.GetPanelInfoAsync(portraitOrUserName, cancellationToken);
        if (user.UserId <= 0)
            throw new TiebaProtocolException(
                $"Unable to resolve a valid user id from '{portraitOrUserName}' for private messaging.");

        return await SendMessageAsync(user.UserId, content, cancellationToken);
    }

    public async Task<bool> SendChatroomMessageAsync(long chatroomId, ulong forumId, string text,
        IReadOnlyList<long>? atUserIds = null, int robotCode = -1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateChatroomId(chatroomId);
        ValidateForumId(forumId);
        ValidateRequiredText(nameof(text), text, "Chatroom message text must not be blank.");
        ValidateAtUserIds(atUserIds);

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SendChatroomMessageAsync),
                TiebaOperationCapabilities.HttpOnly(requiresAuthentication: true),
                async (session, ct) =>
                {
                    var account = session.RequireAuthenticatedAccount(nameof(SendChatroomMessageAsync));
                    var selfInfo = await users.GetSelfInfoAsync(ct);
                    var forumLevel = await new GetForumLevel(session.HttpCore).RequestAsync(forumId, ct);
                    if (string.IsNullOrWhiteSpace(account.SampleId) || string.IsNullOrWhiteSpace(account.ClientId))
                    {
                        var identifiers = await session.ExecuteClientSyncAsync(
                            nameof(SendChatroomMessageAsync),
                            innerCt => new Sync(session.HttpCore).RequestAsync(innerCt),
                            ct);
                        session.UpdateClientIdentifiers(identifiers.ClientId, identifiers.SampleId);
                    }

                    return await _sendChatroomMessageAsync(account, selfInfo, forumLevel, chatroomId, forumId,
                        text, atUserIds, robotCode, ct);
                }),
            cancellationToken);
    }

    public async Task<bool> SetMessageReadAsync(WsMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);
        ValidateUserId(message.User.UserId);
        ValidateMessageId(message.MsgId);

        await EnsureCursorStoreInitializedAsync(cancellationToken);

        var groupId = _cursorStore.PrivateGroupId > 0 ? _cursorStore.PrivateGroupId : message.GroupId;
        if (groupId <= 0)
            throw new TiebaProtocolException("Unable to determine the private-message group id for read-state updates.");

        return await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<bool>(
                nameof(SetMessageReadAsync),
                TiebaOperationCapabilities.WebSocketOnly(requiresAuthentication: true),
                ExecuteWebSocketAsync: (session, ct) =>
                    new SetMsgReaded(session.WsCore).RequestAsync(message.User.UserId, groupId, message.MsgId, ct)),
            cancellationToken);
    }

    public IReadOnlyList<WsNotify> ParsePushNotifications(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return PushNotify.ParseBody(payload);
    }

    private async Task EnsureCursorStoreInitializedAsync(CancellationToken cancellationToken)
    {
        await _cursorStore.EnsureInitializedAsync(
            async ct => await dispatcher.ExecuteAsync(
                new TiebaOperationDescriptor<IReadOnlyList<WsMsgGroupInfo>>(
                    "InitWebSocketAsync",
                    TiebaOperationCapabilities.WebSocketOnly(requiresAuthentication: true),
                    ExecuteWebSocketAsync: (session, innerCt) => new InitWebSocket(session.WsCore).RequestAsync(innerCt)),
                ct),
            cancellationToken);
    }

    private async Task<WsMsgGroups> GetGroupMessagesCoreAsync(IReadOnlyList<long> groupIds, int getType,
        CancellationToken cancellationToken)
    {
        var lastMessageIds = groupIds.Select(_cursorStore.GetLastMessageId).ToList();
        var groups = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<WsMsgGroups>(
                nameof(GetGroupMessagesAsync),
                TiebaOperationCapabilities.WebSocketOnly(requiresAuthentication: true),
                ExecuteWebSocketAsync: (session, ct) =>
                    new GetGroupMsg(session.WsCore).RequestAsync(groupIds, lastMessageIds, getType, ct)),
            cancellationToken);

        _cursorStore.Update(groups);
        return groups;
    }

    private static void ValidateUserId(long userId)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "User id must be positive.");
    }

    private static void ValidatePageNumber(int pn)
    {
        if (pn <= 0)
            throw new ArgumentOutOfRangeException(nameof(pn), pn, "Page number must be positive.");
    }

    private static void ValidateGroupIds(IReadOnlyList<long> groupIds)
    {
        ArgumentNullException.ThrowIfNull(groupIds);
        if (groupIds.Count == 0)
            throw new ArgumentException("At least one group id is required.", nameof(groupIds));

        foreach (var groupId in groupIds)
        {
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupIds), groupId, "Group ids must be positive.");
        }
    }

    private static void ValidateAtUserIds(IReadOnlyList<long>? atUserIds)
    {
        if (atUserIds is null) return;
        foreach (var userId in atUserIds)
        {
            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(atUserIds), userId, "Mentioned user ids must be positive.");
        }
    }

    private static void ValidateForumId(ulong forumId)
    {
        if (forumId == 0)
            throw new ArgumentOutOfRangeException(nameof(forumId), forumId, "Forum id must be positive.");
    }

    private static void ValidateChatroomId(long chatroomId)
    {
        if (chatroomId <= 0)
            throw new ArgumentOutOfRangeException(nameof(chatroomId), chatroomId, "Chatroom id must be positive.");
    }

    private static void ValidateGetType(int getType)
    {
        if (getType <= 0)
            throw new ArgumentOutOfRangeException(nameof(getType), getType, "Get type must be positive.");
    }

    private static void ValidateMessageId(long messageId)
    {
        if (messageId <= 0)
            throw new ArgumentOutOfRangeException(nameof(messageId), messageId, "Message id must be positive.");
    }

    private static void ValidateRequiredText(string paramName, string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message, paramName);
    }
}
