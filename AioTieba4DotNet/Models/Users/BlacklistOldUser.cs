using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示 aiotieba <c>get_blacklist_old</c> 返回的黑名单 <c>_old</c> 用户信息。
/// </summary>
public class BlacklistOldUser : UserInfo
{
    /// <summary>
    ///     获取禁言结束时间戳。
    /// </summary>
    /// <value>以秒为单位的结束时间戳。</value>
    public int UntilTime { get; init; }
}
