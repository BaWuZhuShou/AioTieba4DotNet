using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserThreadsMapper
{
    internal static UserThreads FromTbData(UserPostResIdl.Types.DataRes dataRes)

        {

            List<UserThread> objs = [];

            objs.AddRange(dataRes.PostList.Select(AioTieba4DotNet.Internal.Mapping.UserThreadMapper.FromTbData));

            if (objs.Count == 0) return new UserThreads(objs);



            var user = UserInfoMapper.FromTbData(dataRes.PostList[0]);

            foreach (var uthread in objs) uthread.User = user;



            return new UserThreads(objs);

        }
}
