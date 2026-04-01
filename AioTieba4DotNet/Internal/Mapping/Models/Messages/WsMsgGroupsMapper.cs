using AioTieba4DotNet.Models.Messages;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class WsMsgGroupsMapper
{
    internal static WsMsgGroups FromTbData(GetGroupMsgResIdl.Types.DataRes data)
    {
        return new WsMsgGroups(data.GroupInfo.Select(WsMsgGroupMapper.FromTbData).ToList());
    }
}
