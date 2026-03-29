using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserPostMapper
{
    internal static UserPost FromTbData(PostInfoList.Types.PostInfoContent dataRes)

        {

            var contents = AioTieba4DotNet.Internal.Mapping.ContentMapper.FromTbData(dataRes);



            return new UserPost

            {

                Contents = contents,

                Pid = (int)dataRes.PostId,

                IsComment = dataRes.PostType != 0,

                CreateTime = (int)dataRes.CreateTime

            };

        }
}
