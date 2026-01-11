namespace AioTieba4DotNet.Enums;

/// <summary>
///     帖子评论权限
///     Note:
///     ALL 允许所有人\n
///     FANS 仅允许我的粉丝\n
///     FOLLOW 仅允许我的关注
/// </summary>
public enum PrivReply
{
    /// <summary>
    ///     允许所有人
    /// </summary>
    All = 1,

    /// <summary>
    ///     仅允许我的粉丝
    /// </summary>
    Fans = 5,

    /// <summary>
    ///     仅允许我的关注
    /// </summary>
    Follow = 6
}
