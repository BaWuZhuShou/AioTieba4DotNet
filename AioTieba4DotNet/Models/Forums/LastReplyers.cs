using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     最后回复相关用户信息
/// </summary>
public sealed class LastReplyerUser
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
    ///     旧版昵称
    /// </summary>
    public string NickNameOld { get; init; } = string.Empty;

    /// <summary>
    ///     昵称
    /// </summary>
    public string NickName => NickNameOld;

    /// <summary>
    ///     显示名称
    /// </summary>
    public string ShowName => !string.IsNullOrEmpty(NickNameOld) ? NickNameOld : UserName;

    /// <summary>
    ///     日志名称
    /// </summary>
    public string LogName => !string.IsNullOrEmpty(UserName)
        ? UserName
        : !string.IsNullOrEmpty(Portrait)
            ? $"{NickNameOld}/{Portrait}"
            : UserId.ToString();

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LastReplyerUser other && UserId == other.UserId;

    /// <inheritdoc />
    public override int GetHashCode() => UserId.GetHashCode();
}

/// <summary>
///     最后回复人信息
/// </summary>
public sealed class LastReplyer
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
    ///     昵称
    /// </summary>
    public string NickName => NickNameOld;

    /// <summary>
    ///     显示名称
    /// </summary>
    public string ShowName => !string.IsNullOrEmpty(NickNameOld) ? NickNameOld : UserName;

    /// <summary>
    ///     日志名称
    /// </summary>
    public string LogName => !string.IsNullOrEmpty(UserName) ? UserName : UserId.ToString();

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LastReplyer other && UserId == other.UserId;

    /// <inheritdoc />
    public override int GetHashCode() => UserId.GetHashCode();
}

/// <summary>
///     最后回复人接口页信息
/// </summary>
public sealed class LastReplyersPage
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
///     带最后回复人的主题帖信息
/// </summary>
public sealed class LastReplyerThread
{
    /// <summary>
    ///     标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    ///     所在吧 ID
    /// </summary>
    public ulong Fid { get; set; }

    /// <summary>
    ///     所在吧名
    /// </summary>
    public string Fname { get; set; } = string.Empty;

    /// <summary>
    ///     主题帖 ID
    /// </summary>
    public long Tid { get; init; }

    /// <summary>
    ///     首楼回复 ID
    /// </summary>
    public long Pid { get; init; }

    /// <summary>
    ///     作者信息
    /// </summary>
    public LastReplyerUser User { get; init; } = new();

    /// <summary>
    ///     最后回复人信息
    /// </summary>
    public LastReplyer LastReplyer { get; init; } = new();

    /// <summary>
    ///     是否精品帖
    /// </summary>
    public bool IsGood { get; init; }

    /// <summary>
    ///     是否置顶帖
    /// </summary>
    public bool IsTop { get; init; }

    /// <summary>
    ///     创建时间
    /// </summary>
    public int CreateTime { get; init; }

    /// <summary>
    ///     最后回复时间
    /// </summary>
    public int LastTime { get; init; }

    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text => Title;

    /// <summary>
    ///     作者 ID
    /// </summary>
    public long AuthorId => User.UserId;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LastReplyerThread other && Pid == other.Pid;

    /// <inheritdoc />
    public override int GetHashCode() => Pid.GetHashCode();
}

/// <summary>
///     带最后回复人的帖子列表
/// </summary>
public sealed class LastReplyers(List<LastReplyerThread> objs, LastReplyersPage page, Forum forum)
    : Containers<LastReplyerThread>(objs)
{
    /// <summary>
    ///     分页信息
    /// </summary>
    public LastReplyersPage Page { get; } = page ?? throw new ArgumentNullException(nameof(page));

    /// <summary>
    ///     所在贴吧
    /// </summary>
    public Forum Forum { get; } = forum ?? throw new ArgumentNullException(nameof(forum));

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
