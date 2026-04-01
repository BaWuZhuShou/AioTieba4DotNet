namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     大吧主推荐配额状态
/// </summary>
public sealed class RecomStatus
{
    /// <summary>
    ///     本月总推荐配额
    /// </summary>
    public int TotalRecommendNum { get; init; }

    /// <summary>
    ///     本月已使用配额
    /// </summary>
    public int UsedRecommendNum { get; init; }
}
