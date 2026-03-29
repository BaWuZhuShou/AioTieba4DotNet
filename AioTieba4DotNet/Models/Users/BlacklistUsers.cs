using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

public class BlacklistUsers : Containers<BlacklistUser>
{
    public BlacklistUsers(List<BlacklistUser> objs) : base(objs)
    {
    }
}
