using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     黑名单用户列表
/// </summary>
public class BlacklistUsers : Containers<BlacklistUser>
{
    /// <summary>
    ///     初始化黑名单用户列表
    /// </summary>
    /// <param name="objs">黑名单用户集合</param>
    public BlacklistUsers(List<BlacklistUser> objs) : base(objs)
    {
    }
}
