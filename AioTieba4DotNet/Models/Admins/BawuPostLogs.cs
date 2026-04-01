using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Admins;

/// <summary>
///     吧务删帖日志查询选项
/// </summary>
public sealed class BawuPostLogQueryOptions
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
///     吧务删帖日志媒体
/// </summary>
public sealed class BawuPostLogMedia
{
    /// <summary>
    ///     小图链接
    /// </summary>
    public string Src { get; init; } = string.Empty;

    /// <summary>
    ///     原图链接
    /// </summary>
    public string OriginSrc { get; init; } = string.Empty;

    /// <summary>
    ///     图床 hash
    /// </summary>
    public string Hash { get; init; } = string.Empty;
}

/// <summary>
///     吧务删帖日志项
/// </summary>
public sealed class BawuPostLog
{
    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    ///     主题帖标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    ///     媒体列表
    /// </summary>
    public IReadOnlyList<BawuPostLogMedia> Medias { get; init; } = [];

    /// <summary>
    ///     主题帖 ID
    /// </summary>
    public long Tid { get; init; }

    /// <summary>
    ///     回复 ID；主题帖时为 0
    /// </summary>
    public long Pid { get; init; }

    /// <summary>
    ///     操作类型文案
    /// </summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>
    ///     发帖用户 portrait
    /// </summary>
    public string PostPortrait { get; init; } = string.Empty;

    /// <summary>
    ///     发帖时间（无年份，按 upstream 约定使用 1904 年）
    /// </summary>
    public DateTime PostTime { get; init; }

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
///     吧务删帖日志分页信息
/// </summary>
public sealed class BawuPostLogPage
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
///     吧务删帖日志列表
/// </summary>
public sealed class BawuPostLogs(List<BawuPostLog> objs, BawuPostLogPage page)
    : Containers<BawuPostLog>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public BawuPostLogPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
