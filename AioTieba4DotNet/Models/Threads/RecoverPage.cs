namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     表示回收站列表分页信息。
/// </summary>
public sealed class RecoverPage
{
    /// <summary>
    ///     获取页大小。
    /// </summary>
    /// <value>A page size.</value>
    public int PageSize { get; init; }

    /// <summary>
    ///     获取当前页码。
    /// </summary>
    /// <value>A current page number.</value>
    public int CurrentPage { get; init; }

    /// <summary>
    ///     获取一个值，该值指示是否还有下一页。
    /// </summary>
    /// <value><see langword="true" /> if another page exists; otherwise, <see langword="false" />.</value>
    public bool HasMore { get; init; }

    /// <summary>
    ///     获取一个值，该值指示是否有上一页。
    /// </summary>
    /// <value><see langword="true" /> if a previous page exists; otherwise, <see langword="false" />.</value>
    public bool HasPrevious { get; init; }
}
