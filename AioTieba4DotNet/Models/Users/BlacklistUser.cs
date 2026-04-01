using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示 aiotieba <c>get_blacklist</c> 返回的黑名单用户项。
/// </summary>
public class BlacklistUser : UserInfo
{
    /// <summary>
    ///     是否屏蔽关注相关行为
    /// </summary>
    public bool BlockFollow { get; init; }

    /// <summary>
    ///     是否屏蔽互动相关行为
    /// </summary>
    public bool BlockInteract { get; init; }

    /// <summary>
    ///     是否屏蔽私聊相关行为
    /// </summary>
    public bool BlockChat { get; init; }
}
