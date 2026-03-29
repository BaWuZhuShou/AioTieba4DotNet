namespace AioTieba4DotNet.Models.Shared;

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
}
