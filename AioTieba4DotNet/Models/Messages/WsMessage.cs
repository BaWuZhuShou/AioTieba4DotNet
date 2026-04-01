using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Messages;

/// <summary>
///     表示一条 websocket 私信消息。
/// </summary>
public sealed class WsMessage
{
    /// <summary>
    ///     获取消息所属的消息组 id。
    /// </summary>
    /// <value>A message group id.</value>
    public long GroupId { get; init; }

    /// <summary>
    ///     获取消息所属的消息组类型。
    /// </summary>
    /// <value>A message group type.</value>
    public int GroupType { get; init; }

    public GroupType GroupTypeValue => Enum.IsDefined(typeof(GroupType), GroupType)
        ? (GroupType)GroupType
        : global::AioTieba4DotNet.Models.Messages.GroupType.Unknown;

    /// <summary>
    ///     获取消息 id。
    /// </summary>
    /// <value>A message id.</value>
    public long MsgId { get; init; }

    /// <summary>
    ///     获取消息类型。
    /// </summary>
    /// <value>A message type.</value>
    public int MsgType { get; init; }

    public MsgType MsgTypeValue => Enum.IsDefined(typeof(MsgType), MsgType)
        ? (MsgType)MsgType
        : global::AioTieba4DotNet.Models.Messages.MsgType.Unknown;

    /// <summary>
    ///     获取文本内容。
    /// </summary>
    /// <value>A message text.</value>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    ///     获取发送者信息。
    /// </summary>
    /// <value>A sender model.</value>
    public required UserInfo User { get; init; }

    /// <summary>
    ///     获取发送时间。
    /// </summary>
    /// <value>A Unix timestamp in seconds.</value>
    public int CreateTime { get; init; }
}
