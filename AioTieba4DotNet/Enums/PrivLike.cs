namespace AioTieba4DotNet.Enums;

/// <summary>
///     关注吧列表的公开状态
///     Note:
///     PUBLIC 所有人可见\n
///     FRIEND 好友可见\n
///     HIDE 完全隐藏
/// </summary>
public enum PrivLike
{
    /// <summary>
    ///     所有人可见
    /// </summary>
    Public = 1,

    /// <summary>
    ///     好友可见
    /// </summary>
    Friend = 2,

    /// <summary>
    ///     完全隐藏
    /// </summary>
    Hide = 3
}
