using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     吧会员用户信息
/// </summary>
public sealed class MemberUser
{
    /// <summary>
    ///     用户名
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    ///     portrait
    /// </summary>
    public string Portrait { get; init; } = string.Empty;

    /// <summary>
    ///     等级
    /// </summary>
    public int Level { get; init; }
}

/// <summary>
///     吧会员列表分页信息
/// </summary>
public sealed class MemberUsersPage
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
    ///     是否有下一页
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    ///     是否有上一页
    /// </summary>
    public bool HasPrevious { get; init; }
}

/// <summary>
///     吧会员用户列表
/// </summary>
public sealed class MemberUsers(List<MemberUser> objs, MemberUsersPage page) : Containers<MemberUser>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public MemberUsersPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
