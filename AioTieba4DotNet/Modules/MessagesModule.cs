using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Protocols;

namespace AioTieba4DotNet.Modules;

/// <summary>
///     消息业务模块默认实现。
/// </summary>
public sealed class MessagesModule : IMessagesModule
{
    private readonly IMessagesProtocol _protocol;

    internal MessagesModule(IMessagesProtocol protocol)
    {
        _protocol = protocol;
    }

    /// <inheritdoc />
    public Task<AtMessages> GetAtsAsync(int pn = 1, CancellationToken cancellationToken = default)
    {
        return _protocol.GetAtsAsync(pn, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReplyMessages> GetRepliesAsync(int pn = 1, CancellationToken cancellationToken = default)
    {
        return _protocol.GetRepliesAsync(pn, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WsMsgGroups> GetGroupMessagesAsync(int getType = 1, CancellationToken cancellationToken = default)
    {
        return _protocol.GetGroupMessagesAsync(getType, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WsMsgGroups> GetGroupMessagesAsync(IReadOnlyList<long> groupIds, int getType = 1,
        CancellationToken cancellationToken = default)
    {
        return _protocol.GetGroupMessagesAsync(groupIds, getType, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> SendMessageAsync(long userId, string content, CancellationToken cancellationToken = default)
    {
        return _protocol.SendMessageAsync(userId, content, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> SendMessageAsync(string portraitOrUserName, string content,
        CancellationToken cancellationToken = default)
    {
        return _protocol.SendMessageAsync(portraitOrUserName, content, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SendChatroomMessageAsync(long chatroomId, ulong forumId, string text,
        IReadOnlyList<long>? atUserIds = null, int robotCode = -1, CancellationToken cancellationToken = default)
    {
        return _protocol.SendChatroomMessageAsync(chatroomId, forumId, text, atUserIds, robotCode, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SetMessageReadAsync(WsMessage message, CancellationToken cancellationToken = default)
    {
        return _protocol.SetMessageReadAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<WsNotify> ParsePushNotifications(byte[] payload)
    {
        return _protocol.ParsePushNotifications(payload);
    }
}
