namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     分页版当前账号关注贴吧分页信息
/// </summary>
public class SelfFollowForumsV1Page
{
    /// <summary>
    ///     当前页码
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    ///     总页数
    /// </summary>
    public int TotalPage { get; init; }

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    ///     是否有上一页
    /// </summary>
    public bool HasPrevious { get; init; }
}
