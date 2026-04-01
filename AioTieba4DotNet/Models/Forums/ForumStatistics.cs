namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     贴吧后台统计信息，时间顺序为从旧到新
/// </summary>
public sealed class ForumStatistics
{
    /// <summary>
    ///     浏览量
    /// </summary>
    public IReadOnlyList<int> View { get; init; } = [];

    /// <summary>
    ///     主题帖数
    /// </summary>
    public IReadOnlyList<int> Thread { get; init; } = [];

    /// <summary>
    ///     新增吧会员数
    /// </summary>
    public IReadOnlyList<int> NewMember { get; init; } = [];

    /// <summary>
    ///     回复数
    /// </summary>
    public IReadOnlyList<int> Post { get; init; } = [];

    /// <summary>
    ///     签到率
    /// </summary>
    public IReadOnlyList<int> SignRatio { get; init; } = [];

    /// <summary>
    ///     人均浏览时长
    /// </summary>
    public IReadOnlyList<int> AvgTime { get; init; } = [];

    /// <summary>
    ///     人均进吧次数
    /// </summary>
    public IReadOnlyList<int> AvgTimes { get; init; } = [];

    /// <summary>
    ///     首页推荐数
    /// </summary>
    public IReadOnlyList<int> Recommend { get; init; } = [];
}
