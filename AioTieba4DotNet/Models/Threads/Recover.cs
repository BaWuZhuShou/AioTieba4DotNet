namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     表示一条待恢复内容摘要。
/// </summary>
public sealed class Recover
{
    /// <summary>
    ///     获取文本摘要。
    /// </summary>
    /// <value>A text summary.</value>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    ///     获取所属主题帖 id。
    /// </summary>
    /// <value>A thread id.</value>
    public long Tid { get; init; }

    /// <summary>
    ///     获取回复 id。
    /// </summary>
    /// <value>A post id, or <c>0</c> for a recovered thread entry.</value>
    public long Pid { get; init; }

    /// <summary>
    ///     获取作者信息。
    /// </summary>
    /// <value>An author model.</value>
    public required RecoverUser User { get; init; }

    /// <summary>
    ///     获取操作人的显示名称。
    /// </summary>
    /// <value>An operator display name.</value>
    public string OperatorShowName { get; init; } = string.Empty;

    /// <summary>
    ///     获取操作时间。
    /// </summary>
    /// <value>A Unix timestamp in seconds.</value>
    public int OperatorTime { get; init; }

    /// <summary>
    ///     获取一个值，该值指示该条目是否为楼中楼内容。
    /// </summary>
    /// <value><see langword="true" /> if the entry is a floor reply; otherwise, <see langword="false" />.</value>
    public bool IsFloor { get; init; }

    /// <summary>
    ///     获取一个值，该值指示该条目是否来自屏蔽恢复。
    /// </summary>
    /// <value><see langword="true" /> if the entry is a hidden-thread recovery item; otherwise, <see langword="false" />.</value>
    public bool IsHide { get; init; }
}
