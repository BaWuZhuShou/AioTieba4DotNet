namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示贴吧等级排行榜中的用户信息。
/// </summary>
public class RankUser
{
    /// <summary>
    ///     Gets the user name.
    /// </summary>
    /// <value>A user name.</value>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the level value.
    /// </summary>
    /// <value>A level value.</value>
    public int Level { get; init; }

    /// <summary>
    ///     Gets the experience value.
    /// </summary>
    /// <value>An experience value.</value>
    public int Exp { get; init; }

    /// <summary>
    ///     Gets a value that indicates whether the user is a VIP.
    /// </summary>
    /// <value><see langword="true"/> if the user is a VIP; otherwise, <see langword="false"/>.</value>
    public bool IsVip { get; init; }
}
