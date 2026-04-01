using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示旧版黑名单中的用户信息。
/// </summary>
public class BlacklistOldUser : UserInfo
{
    /// <summary>
    ///     Gets the timestamp at which the mute expires.
    /// </summary>
    /// <value>An expiration timestamp in seconds.</value>
    public int UntilTime { get; init; }
}
