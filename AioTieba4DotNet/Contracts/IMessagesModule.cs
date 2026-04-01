using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Contracts;

/// <summary>
///     消息业务模块契约。
/// </summary>
public interface IMessagesModule
{
    /// <summary>
    ///     获取 @ 消息列表。
    /// </summary>
    /// <param name="pn">一个页码。</param>
    /// <param name="cancellationToken">一个取消令牌。</param>
    /// <returns>一个 @ 消息列表。</returns>
    Task<AtMessages> GetAtsAsync(int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取回复消息列表。
    /// </summary>
    /// <param name="pn">一个页码。</param>
    /// <param name="cancellationToken">一个取消令牌。</param>
    /// <returns>一个回复消息列表。</returns>
    Task<ReplyMessages> GetRepliesAsync(int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取当前已知消息组的消息。
    /// </summary>
    /// <param name="getType">一个获取类型。</param>
    /// <param name="cancellationToken">一个取消令牌。</param>
    /// <returns>一个 websocket 消息组列表。</returns>
    Task<WsMsgGroups> GetGroupMessagesAsync(int getType = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取指定消息组的消息。
    /// </summary>
    /// <param name="groupIds">一组消息组 id。</param>
    /// <param name="getType">一个获取类型。</param>
    /// <param name="cancellationToken">一个取消令牌。</param>
    /// <returns>一个 websocket 消息组列表。</returns>
    Task<WsMsgGroups> GetGroupMessagesAsync(IReadOnlyList<long> groupIds, int getType = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送私信。
    /// </summary>
    /// <param name="userId">一个用户 id。</param>
    /// <param name="content">一段消息内容。</param>
    /// <param name="cancellationToken">一个取消令牌。</param>
    /// <returns>一个消息 id。</returns>
    Task<long> SendMessageAsync(long userId, string content, CancellationToken cancellationToken = default);

    /// <summary>
    ///     向 portrait 或用户名对应的用户发送私信。
    /// </summary>
    /// <param name="portraitOrUserName">一个 portrait 或用户名。</param>
    /// <param name="content">一段消息内容。</param>
    /// <param name="cancellationToken">一个取消令牌。</param>
    /// <returns>一个消息 id。</returns>
    Task<long> SendMessageAsync(string portraitOrUserName, string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送吧群消息。
    /// </summary>
    /// <param name="chatroomId">一个聊天室 id。</param>
    /// <param name="forumId">一个吧 id。</param>
    /// <param name="text">一段消息内容。</param>
    /// <param name="atUserIds">一组需要 @ 的用户 id。</param>
    /// <param name="robotCode">一个机器人指令代码。</param>
    /// <param name="cancellationToken">一个取消令牌。</param>
    /// <returns><see langword="true" /> if the message is accepted; otherwise, <see langword="false" />.</returns>
    Task<bool> SendChatroomMessageAsync(long chatroomId, ulong forumId, string text,
        IReadOnlyList<long>? atUserIds = null, int robotCode = -1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     将一条私信标记为已读。
    /// </summary>
    /// <param name="message">一条 websocket 私信消息。</param>
    /// <param name="cancellationToken">一个取消令牌。</param>
    /// <returns><see langword="true" /> if the message is marked as read; otherwise, <see langword="false" />.</returns>
    Task<bool> SetMessageReadAsync(WsMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     解析 websocket push_notify 负载。
    /// </summary>
    /// <remarks>
    ///     Upstream 将 <c>push_notify</c> 作为 push 载荷解析器而不是常规请求 API。v3 公开层保留一个显式解析入口，
    ///     但不会额外发明事件总线或后台订阅框架。
    /// </remarks>
    /// <param name="payload">一个 websocket push 负载字节数组。</param>
    /// <returns>一组 push 通知。</returns>
    IReadOnlyList<WsNotify> ParsePushNotifications(byte[] payload);
}
