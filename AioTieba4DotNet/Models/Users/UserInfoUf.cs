using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示吧内用户信息快照。
/// </summary>
public class UserInfoUf : UserInfo
{
    /// <summary>
    ///     Gets a value that indicates whether the current account follows this user.
    /// </summary>
    /// <value><see langword="true"/> if the current account follows this user; otherwise, <see langword="false"/>.</value>
    public bool IsLike { get; init; }
}
