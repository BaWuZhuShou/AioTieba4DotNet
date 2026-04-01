namespace AioTieba4DotNet.Models.Messages;

/// <summary>
///     表示一条 websocket push 通知。
/// </summary>
public sealed class WsNotify
{
    /// <summary>
    ///     获取通知类型。
    /// </summary>
    /// <value>A notification type.</value>
    public int NoteType { get; init; }

    /// <summary>
    ///     获取消息组 id。
    /// </summary>
    /// <value>A message group id.</value>
    public long GroupId { get; init; }

    /// <summary>
    ///     获取消息组类型。
    /// </summary>
    /// <value>A message group type.</value>
    public int GroupType { get; init; }

    /// <summary>
    ///     获取消息 id。
    /// </summary>
    /// <value>A message id.</value>
    public long MsgId { get; init; }

    /// <summary>
    ///     获取推送时间。
    /// </summary>
    /// <value>A Unix timestamp in seconds.</value>
    public long CreateTime { get; init; }
}
