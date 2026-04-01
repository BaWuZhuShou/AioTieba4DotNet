using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Messages;

/// <summary>
///     表示一个 websocket 消息组列表。
/// </summary>
public sealed class WsMsgGroups(List<WsMsgGroup> objs) : Containers<WsMsgGroup>(objs)
{
}
