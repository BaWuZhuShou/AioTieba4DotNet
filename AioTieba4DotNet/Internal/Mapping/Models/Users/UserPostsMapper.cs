using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserPostsMapper
{
    internal static UserPosts FromTbData(PostInfoList dataRes)

    {
        var fid = (long)dataRes.ForumId;

        var tid = (long)dataRes.ThreadId;

        var user = UserInfoMapper.FromTbData(dataRes);

        List<UserPost> objs = [];

        foreach (var userPost in dataRes.Content.Select(UserPostMapper.FromTbData))

        {
            userPost.Fid = fid;

            userPost.Tid = tid;

            userPost.User = user;

            objs.Add(userPost);
        }


        return new UserPosts(objs, fid, tid);
    }
}
