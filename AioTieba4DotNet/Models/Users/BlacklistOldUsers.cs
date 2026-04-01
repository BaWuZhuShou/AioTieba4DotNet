using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示 aiotieba <c>get_blacklist_old</c> 返回的黑名单 <c>_old</c> 用户列表。
/// </summary>
public class BlacklistOldUsers : Containers<BlacklistOldUser>
{
    /// <summary>
    ///     初始化黑名单 <c>_old</c> 用户列表。
    /// </summary>
    /// <param name="objs">黑名单 <c>_old</c> 用户集合。</param>
    /// <param name="page">分页快照。</param>
    public BlacklistOldUsers(List<BlacklistOldUser> objs, PageT page) : base(objs)
    {
        Page = page;
    }

    /// <summary>
    ///     获取分页元数据。
    /// </summary>
    /// <value>分页快照。</value>
    public PageT Page { get; init; }
}
