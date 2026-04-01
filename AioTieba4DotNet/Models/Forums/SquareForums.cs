using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     吧广场贴吧信息
/// </summary>
public sealed class SquareForum
{
    /// <summary>
    ///     吧 ID
    /// </summary>
    public ulong Fid { get; init; }

    /// <summary>
    ///     吧名
    /// </summary>
    public string Fname { get; init; } = string.Empty;

    /// <summary>
    ///     吧会员数
    /// </summary>
    public int MemberNum { get; init; }

    /// <summary>
    ///     发帖量
    /// </summary>
    public int PostNum { get; init; }

    /// <summary>
    ///     是否已关注
    /// </summary>
    public bool IsFollowed { get; init; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SquareForum other && Fid == other.Fid;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Fid.GetHashCode();
    }
}

/// <summary>
///     吧广场分页信息
/// </summary>
public sealed class SquareForumsPage
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
    ///     总页数
    /// </summary>
    public int TotalPage { get; init; }

    /// <summary>
    ///     总条数
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    ///     是否有下一页
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    ///     是否有上一页
    /// </summary>
    public bool HasPrevious { get; init; }
}

/// <summary>
///     吧广场列表
/// </summary>
public sealed class SquareForums(List<SquareForum> objs, SquareForumsPage page) : Containers<SquareForum>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public SquareForumsPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
