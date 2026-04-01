using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     吧签到排行项
/// </summary>
public sealed class RankForum
{
    /// <summary>
    ///     吧名
    /// </summary>
    public string Fname { get; init; } = string.Empty;

    /// <summary>
    ///     签到人数
    /// </summary>
    public int SignNum { get; init; }

    /// <summary>
    ///     总会员数
    /// </summary>
    public int MemberNum { get; init; }

    /// <summary>
    ///     是否有吧务
    /// </summary>
    public bool HasBaWu { get; init; }
}

/// <summary>
///     吧签到排行榜分页信息
/// </summary>
public sealed class RankForumsPage
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
///     吧签到排行榜
/// </summary>
public sealed class RankForums(List<RankForum> objs, RankForumsPage page) : Containers<RankForum>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public RankForumsPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
