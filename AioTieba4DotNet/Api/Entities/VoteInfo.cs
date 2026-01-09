namespace AioTieba4DotNet.Api.Entities;

/// <summary>
/// 投票信息
/// </summary>
public class VoteInfo
{
    /// <summary>
    /// 投票标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 是否多选
    /// </summary>
    public bool IsMulti { get; init; }

    /// <summary>
    /// 选项列表
    /// </summary>
    public List<VoteOption> Options { get; init; } = [];

    /// <summary>
    /// 总投票数
    /// </summary>
    public long TotalVotes { get; init; }

    /// <summary>
    /// 总投票人数
    /// </summary>
    public long TotalUsers { get; init; }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="voteInfo"></param>
    /// <returns>VoteInfo</returns>
    public static VoteInfo? FromTbData(PollInfo? voteInfo)
    {
        if (voteInfo == null || string.IsNullOrEmpty(voteInfo.Title))
        {
            return null;
        }

        return new VoteInfo()
        {
            Title = voteInfo.Title,
            IsMulti = voteInfo.IsMulti == 1,
            Options = voteInfo.Options.Select(VoteOption.FromTbData).ToList(),
            TotalVotes = voteInfo.TotalPoll,
            TotalUsers = voteInfo.TotalNum
        };
    }

    /// <summary>
    /// 格式设置
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var optionStr = Options.Aggregate("", (current, option) => current + (option + ", "));
        return
            $"{nameof(Title)}: {Title}, {nameof(IsMulti)}: {IsMulti}, {nameof(Options)}: {optionStr}, {nameof(TotalVotes)}: {TotalVotes}, {nameof(TotalUsers)}: {TotalUsers}";
    }
}
