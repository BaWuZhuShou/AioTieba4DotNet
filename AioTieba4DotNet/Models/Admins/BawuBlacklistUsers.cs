using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Admins;

/// <summary>
///     吧务黑名单用户
/// </summary>
public sealed class BawuBlacklistUser
{
    /// <summary>
    ///     用户 ID
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    ///     portrait
    /// </summary>
    public string Portrait { get; init; } = string.Empty;

    /// <summary>
    ///     用户名
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    ///     日志名称
    /// </summary>
    public string LogName => !string.IsNullOrEmpty(UserName) ? UserName :
        !string.IsNullOrEmpty(Portrait) ? Portrait : UserId.ToString();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is BawuBlacklistUser other && UserId == other.UserId;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return UserId.GetHashCode();
    }
}

/// <summary>
///     吧务黑名单分页信息
/// </summary>
public sealed class BawuBlacklistPage
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
///     吧务黑名单列表
/// </summary>
public sealed class BawuBlacklistUsers(List<BawuBlacklistUser> objs, BawuBlacklistPage page)
    : Containers<BawuBlacklistUser>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public BawuBlacklistPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
