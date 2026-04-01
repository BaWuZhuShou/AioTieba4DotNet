using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     表示一条回收站内容的正文详情。
/// </summary>
public sealed class RecoverInfo
{
    /// <summary>
    ///     获取正文内容碎片列表。
    /// </summary>
    /// <value>A content model.</value>
    public required Content Content { get; init; }

    /// <summary>
    ///     获取标题。
    /// </summary>
    /// <value>A title.</value>
    public string Title { get; init; } = string.Empty;

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
    ///     获取拼接后的可读文本。
    /// </summary>
    /// <value>A composed text body.</value>
    public string Text => string.IsNullOrEmpty(Title) ? Content.Text : $"{Title}\n{Content.Text}";
}
