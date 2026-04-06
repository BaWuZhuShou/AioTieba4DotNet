using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserPostMapper
{
    internal static UserPost FromTbData(PostInfoList.Types.PostInfoContent dataRes)

    {
        var contents = ContentMapper.FromTbData(dataRes);


        return new UserPost
        {
            Contents = contents,
            Pid = checked((long)dataRes.PostId),
            IsComment = dataRes.PostType != 0,
            CreateTime = (int)dataRes.CreateTime
        };
    }
}
