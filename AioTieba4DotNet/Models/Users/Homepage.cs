using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示用户主页帖子列表与资料页快照。
/// </summary>
public class Homepage : Containers<UserThread>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Homepage"/> class.
    /// </summary>
    /// <param name="objs">A homepage thread collection.</param>
    /// <param name="user">A profile snapshot.</param>
    public Homepage(List<UserThread> objs, UserInfoPf user) : base(objs)
    {
        User = user;
    }

    /// <summary>
    ///     Gets the profile snapshot for the homepage owner.
    /// </summary>
    /// <value>A profile snapshot.</value>
    public UserInfoPf User { get; init; }
}
