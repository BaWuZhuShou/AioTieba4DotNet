namespace AioTieba4DotNet.Api.Entities;

/// <summary>
///     投票选项实体
/// </summary>
public class VoteOption
{
    /// <summary>
    ///     该选项的投票数
    /// </summary>
    public long VoteNum { get; set; }

    /// <summary>
    ///     选项文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="pollOption">Protobuf 投票选项数据</param>
    /// <returns>投票选项实体</returns>
    public static VoteOption FromTbData(PollInfo.Types.PollOption pollOption)
    {
        return new VoteOption { VoteNum = pollOption.Num, Text = pollOption.Text };
    }
}
