using AioTieba4DotNet.Models.Messages;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class WsMsgGroupMapper
{
    internal static WsMsgGroup FromTbData(GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg data)
    {
        var groupId = data.GroupInfo.GroupId;
        var groupType = data.GroupInfo.GroupType;
        return new WsMsgGroup
        {
            GroupId = groupId,
            GroupType = groupType,
            Messages = data.MsgList.Select(message => WsMessageMapper.FromTbData(groupId, groupType, message))
                .ToList()
        };
    }
}
