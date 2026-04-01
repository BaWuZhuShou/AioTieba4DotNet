using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Protocols;

internal interface IMessagesProtocol
{
    Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default);

    Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default);

    Task<WsMsgGroups> GetGroupMessagesAsync(int getType, CancellationToken cancellationToken = default);

    Task<WsMsgGroups> GetGroupMessagesAsync(IReadOnlyList<long> groupIds, int getType,
        CancellationToken cancellationToken = default);

    Task<long> SendMessageAsync(long userId, string content, CancellationToken cancellationToken = default);

    Task<long> SendMessageAsync(string portraitOrUserName, string content,
        CancellationToken cancellationToken = default);

    Task<bool> SendChatroomMessageAsync(long chatroomId, ulong forumId, string text,
        IReadOnlyList<long>? atUserIds = null, int robotCode = -1, CancellationToken cancellationToken = default);

    Task<bool> SetMessageReadAsync(WsMessage message, CancellationToken cancellationToken = default);

    IReadOnlyList<WsNotify> ParsePushNotifications(byte[] payload);
}
