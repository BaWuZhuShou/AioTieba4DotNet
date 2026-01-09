namespace AioTieba4DotNet.Enums;

/// <summary>
/// 帖子评论权限
/// Note:
///         ALL 允许所有人\n
///         FANS 仅允许我的粉丝\n
///         FOLLOW 仅允许我的关注
/// </summary>
public enum PrivReply
{
    All = 1,
    Fans = 5,
    Follow = 6
}