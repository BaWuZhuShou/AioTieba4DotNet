using AioTieba4DotNet.Models.Messages;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class WsMessageMapper
{
    internal static WsMessage FromTbData(long groupId, int groupType,
        GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.MsgInfo data)
    {
        return new WsMessage
        {
            GroupId = groupId,
            GroupType = groupType,
            MsgId = data.MsgId,
            MsgType = data.MsgType,
            Text = data.Content,
            User = UserInfoMapper.FromTbData(data.UserInfo),
            CreateTime = data.CreateTime
        };
    }
}
