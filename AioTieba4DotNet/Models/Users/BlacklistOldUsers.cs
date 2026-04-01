using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示旧版黑名单用户列表。
/// </summary>
public class BlacklistOldUsers : Containers<BlacklistOldUser>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlacklistOldUsers"/> class.
    /// </summary>
    /// <param name="objs">A blacklist user collection.</param>
    /// <param name="page">A page snapshot.</param>
    public BlacklistOldUsers(List<BlacklistOldUser> objs, PageT page) : base(objs)
    {
        Page = page;
    }

    /// <summary>
    ///     Gets the page metadata.
    /// </summary>
    /// <value>A page snapshot.</value>
    public PageT Page { get; init; }
}
