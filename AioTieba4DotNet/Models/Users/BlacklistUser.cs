using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

public class BlacklistUser : UserInfo
{
    public bool BlockFollow { get; init; }

    public bool BlockInteract { get; init; }

    public bool BlockChat { get; init; }
}
