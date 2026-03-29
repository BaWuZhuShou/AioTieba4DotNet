using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserPostsMapper
{
    internal static UserPosts FromTbData(PostInfoList dataRes)

        {

            var fid = (long)dataRes.ForumId;

            var tid = (long)dataRes.ThreadId;

            List<UserPost> objs = [];

            foreach (var userPost in dataRes.Content.Select(AioTieba4DotNet.Internal.Mapping.UserPostMapper.FromTbData))

            {

                userPost.Fid = fid;

                userPost.Tid = tid;

                objs.Add(userPost);

            }



            return new UserPosts(objs, fid, tid);

        }
}
