using AioTieba4DotNet.Models.Messages;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class WsNotifyMapper
{
    internal static WsNotify FromTbData(PushNotifyResIdl.Types.PusherMsg data)
    {
        var createTime = long.TryParse(data.Data.Et, out var parsedCreateTime) ? parsedCreateTime : 0;
        return new WsNotify
        {
            NoteType = data.Data.Type,
            GroupId = data.Data.GroupId,
            GroupType = data.Data.GroupType,
            MsgId = data.Data.MsgId,
            CreateTime = createTime
        };
    }
}
