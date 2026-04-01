using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserPostGroupsMapper
{
    internal static UserPostGroups FromTbData(UserPostResIdl.Types.DataRes dataRes)

        {

            List<UserPosts> objs = [];

            objs.AddRange(dataRes.PostList.Select(AioTieba4DotNet.Internal.Mapping.UserPostsMapper.FromTbData));

            if (objs.Count == 0) return new UserPostGroups(objs);

            var postInfoList = dataRes.PostList[0];

            var user = UserInfoMapper.FromTbData(postInfoList);

            foreach (var userPost in objs.SelectMany(obj => obj)) userPost.User = user;



            return new UserPostGroups(objs);

        }
}
