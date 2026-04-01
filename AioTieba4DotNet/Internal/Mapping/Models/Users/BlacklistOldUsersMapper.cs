using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BlacklistOldUsersMapper
{
    internal static BlacklistOldUsers FromTbData(UserMuteQueryResIdl.Types.DataRes data)
    {
        var objs = data.MuteUser.Select(BlacklistOldUserMapper.FromTbData).ToList();
        var page = new PageT
        {
            CurrentPage = data.Page?.CurrentPage ?? 0,
            HasMore = data.Page?.HasMore == 1,
            HasPrevious = data.Page?.HasPrev == 1
        };

        return new BlacklistOldUsers(objs, page);
    }
}
