namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     首页推荐屏蔽贴吧分页信息
/// </summary>
public class DislikeForumsPage
{
    /// <summary>
    ///     当前页码
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    ///     是否有上一页
    /// </summary>
    public bool HasPrevious { get; init; }
}
