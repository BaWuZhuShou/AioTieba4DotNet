namespace AioTieba4DotNet.Models.Shared;

/// <summary>
///     投票信息
/// </summary>
public class VoteInfo
{
    /// <summary>
    ///     投票标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    ///     是否多选
    /// </summary>
    public bool IsMulti { get; init; }

    /// <summary>
    ///     选项列表
    /// </summary>
    public List<VoteOption> Options { get; init; } = [];

    /// <summary>
    ///     总投票数
    /// </summary>
    public long TotalVotes { get; init; }

    /// <summary>
    ///     总投票人数
    /// </summary>
    public long TotalUsers { get; init; }
    /// <summary>
    ///     格式设置
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var optionStr = Options.Aggregate("", (current, option) => current + (option + ", "));
        return
            $"{nameof(Title)}: {Title}, {nameof(IsMulti)}: {IsMulti}, {nameof(Options)}: {optionStr}, {nameof(TotalVotes)}: {TotalVotes}, {nameof(TotalUsers)}: {TotalUsers}";
    }
}
