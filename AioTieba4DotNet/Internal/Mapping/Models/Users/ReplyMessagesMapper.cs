using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ReplyMessagesMapper
{
    internal static ReplyMessages FromTbData(ReplyMeResIdl.Types.DataRes data)
    {
        var objs = data.ReplyList.Select(ReplyMessageMapper.FromTbData).ToList();
        return new ReplyMessages(objs, PageTMapper.FromTbData(data.Page));
    }
}
