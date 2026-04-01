namespace AioTieba4DotNet.Models.Messages;

/// <summary>
///     表示一个 websocket 消息组摘要。
/// </summary>
public sealed class WsMsgGroupInfo
{
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
    ///     获取服务器记录的最新消息 id。
    /// </summary>
    /// <value>A last message id.</value>
    public long LastMessageId { get; init; }
}
