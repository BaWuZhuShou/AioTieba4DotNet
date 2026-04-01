using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class HomepageMapper
{
    internal static Homepage FromTbData(ProfileResIdl.Types.DataRes data)
    {
        var user = UserInfoPfMapper.FromTbData(data);
        var threads = data.PostList.Select(UserThreadMapper.FromTbData).ToList();
        foreach (var thread in threads)
            thread.User = user;

        return new Homepage(threads, user);
    }
}
