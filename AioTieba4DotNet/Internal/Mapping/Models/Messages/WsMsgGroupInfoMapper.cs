using AioTieba4DotNet.Models.Messages;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class WsMsgGroupInfoMapper
{
    internal static WsMsgGroupInfo FromTbData(UpdateClientInfoResIdl.Types.DataRes.Types.GroupInfo data)
    {
        return new WsMsgGroupInfo
        {
            GroupId = data.GroupId, GroupType = data.GroupType, LastMessageId = data.LastMsgId
        };
    }
}
