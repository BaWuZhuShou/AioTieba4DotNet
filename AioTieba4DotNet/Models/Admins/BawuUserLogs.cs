using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Admins;

/// <summary>
///     吧务用户管理日志查询选项
/// </summary>
public sealed class BawuUserLogQueryOptions
{
    /// <summary>
    ///     页码
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    ///     搜索文本
    /// </summary>
    public string SearchValue { get; init; } = string.Empty;

    /// <summary>
    ///     搜索类型
    /// </summary>
    public BawuSearchType SearchType { get; init; } = BawuSearchType.User;

    /// <summary>
    ///     起始时间
    /// </summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    ///     结束时间；为 <see langword="null"/> 且指定起始时间时将自动使用当前时间
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    ///     操作类型；0 表示不筛选
    /// </summary>
    public int OperationType { get; init; }
}

/// <summary>
///     吧务用户管理日志项
/// </summary>
public sealed class BawuUserLog
{
    /// <summary>
    ///     操作类型文案
    /// </summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>
    ///     作用天数；无天数时为 0
    /// </summary>
    public int OperationDurationDays { get; init; }

    /// <summary>
    ///     被操作用户 portrait
    /// </summary>
    public string UserPortrait { get; init; } = string.Empty;

    /// <summary>
    ///     操作人用户名
    /// </summary>
    public string OperatorUserName { get; init; } = string.Empty;

    /// <summary>
    ///     操作时间
    /// </summary>
    public DateTime OperationTime { get; init; }
}

/// <summary>
///     吧务用户管理日志分页信息
/// </summary>
public sealed class BawuUserLogPage
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
///     吧务用户管理日志列表
/// </summary>
public sealed class BawuUserLogs(List<BawuUserLog> objs, BawuUserLogPage page)
    : Containers<BawuUserLog>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public BawuUserLogPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
