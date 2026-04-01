namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     当前账号在某吧的等级信息
/// </summary>
public sealed class ForumLevelInfo
{
    /// <summary>
    ///     等级名称
    /// </summary>
    public string LevelName { get; init; } = string.Empty;

    /// <summary>
    ///     等级值
    /// </summary>
    public int UserLevel { get; init; }

    /// <summary>
    ///     是否已关注
    /// </summary>
    public bool IsLike { get; init; }
}
