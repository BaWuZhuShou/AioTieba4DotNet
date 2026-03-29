namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     页信息
/// </summary>
public class PageT
{
    /// <summary>
    ///     页大小
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    ///     当前页码
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    ///     总页码
    /// </summary>
    public int TotalPage { get; init; }

    /// <summary>
    ///     总计数
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    ///     是否有后继页
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    ///     是否有前驱页
    /// </summary>
    public bool HasPrevious { get; init; }
    /// <summary>
    ///     格式设置
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{nameof(PageSize)}: {PageSize}, {nameof(CurrentPage)}: {CurrentPage}, {nameof(TotalPage)}: {TotalPage}, {nameof(TotalCount)}: {TotalCount}, {nameof(HasMore)}: {HasMore}, {nameof(HasPrevious)}: {HasPrevious}";
    }
}
