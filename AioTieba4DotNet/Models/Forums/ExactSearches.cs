using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     精确搜索结果
/// </summary>
public sealed class ExactSearch
{
    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    ///     标题内容
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    ///     所在贴吧名
    /// </summary>
    public string Fname { get; init; } = string.Empty;

    /// <summary>
    ///     所在主题帖 ID
    /// </summary>
    public long Tid { get; init; }

    /// <summary>
    ///     回复 ID
    /// </summary>
    public long Pid { get; init; }

    /// <summary>
    ///     发布者显示名称
    /// </summary>
    public string ShowName { get; init; } = string.Empty;

    /// <summary>
    ///     是否楼中楼
    /// </summary>
    public bool IsComment { get; init; }

    /// <summary>
    ///     创建时间
    /// </summary>
    public int CreateTime { get; init; }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ExactSearch other && Pid == other.Pid;

    /// <inheritdoc />
    public override int GetHashCode() => Pid.GetHashCode();
}

/// <summary>
///     精确搜索分页信息
/// </summary>
public sealed class ExactSearchesPage
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
///     精确搜索结果列表
/// </summary>
public sealed class ExactSearches(List<ExactSearch> objs, ExactSearchesPage page) : Containers<ExactSearch>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public ExactSearchesPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
