using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Admins;

/// <summary>
///     封禁记录项
/// </summary>
public sealed class Block
{
    /// <summary>
    ///     用户 ID
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    ///     用户名
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    ///     旧版昵称
    /// </summary>
    public string NickNameOld { get; init; } = string.Empty;

    /// <summary>
    ///     封禁天数
    /// </summary>
    public int Day { get; init; }

    /// <summary>
    ///     昵称
    /// </summary>
    public string NickName => NickNameOld;

    /// <summary>
    ///     显示名称
    /// </summary>
    public string ShowName => !string.IsNullOrEmpty(NickNameOld) ? NickNameOld : UserName;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Block other && UserId == other.UserId;

    /// <inheritdoc />
    public override int GetHashCode() => UserId.GetHashCode();
}

/// <summary>
///     封禁列表分页信息
/// </summary>
public sealed class BlocksPage
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
///     封禁列表
/// </summary>
public sealed class Blocks(List<Block> objs, BlocksPage page) : Containers<Block>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public BlocksPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
