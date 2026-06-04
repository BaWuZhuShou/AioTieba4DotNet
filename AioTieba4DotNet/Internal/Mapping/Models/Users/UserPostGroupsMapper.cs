using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserPostGroupsMapper
{
    internal static UserPostGroups FromTbData(UserPostResIdl.Types.DataRes dataRes)

    {
        List<UserPosts> objs = [];

        objs.AddRange(dataRes.PostList.Select(UserPostsMapper.FromTbData));

        return new UserPostGroups(objs);
    }
}
