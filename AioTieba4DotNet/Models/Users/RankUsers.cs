using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示贴吧等级排行榜用户列表。
/// </summary>
public class RankUsers : Containers<RankUser>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RankUsers"/> class.
    /// </summary>
    /// <param name="objs">A ranked user collection.</param>
    /// <param name="page">A page snapshot.</param>
    public RankUsers(List<RankUser> objs, PageT page) : base(objs)
    {
        Page = page;
    }

    /// <summary>
    ///     Gets the page metadata.
    /// </summary>
    /// <value>A page snapshot.</value>
    public PageT Page { get; init; }
}
