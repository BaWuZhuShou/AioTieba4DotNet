namespace AioTieba4DotNet.Models.Messages;

/// <summary>
///     表示一个 websocket 消息组。
/// </summary>
public sealed class WsMsgGroup
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

    public GroupType GroupTypeValue => Enum.IsDefined(typeof(GroupType), GroupType)
        ? (GroupType)GroupType
        : global::AioTieba4DotNet.Models.Messages.GroupType.Unknown;

    /// <summary>
    ///     获取消息列表。
    /// </summary>
    /// <value>A message collection.</value>
    public IReadOnlyList<WsMessage> Messages { get; init; } = [];
}
