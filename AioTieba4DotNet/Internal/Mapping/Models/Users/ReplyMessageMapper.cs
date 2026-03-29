using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ReplyMessageMapper
{
    internal static ReplyMessage FromTbData(ReplyMeResIdl.Types.DataRes.Types.ReplyList data)
    {
        return new ReplyMessage
        {
            Content = data.Content,
            Fname = data.Fname,
            ThreadId = data.ThreadId,
            QuotePostId = data.QuotePid,
            PostId = data.PostId,
            Replyer = UserInfoMapper.FromTbData(data.Replyer),
            QuoteUser = UserInfoMapper.FromTbData(data.QuoteUser),
            ThreadAuthorUser = UserInfoMapper.FromTbData(data.ThreadAuthorUser),
            IsFloor = data.IsFloor == 1,
            Time = data.Time
        };
    }
}
