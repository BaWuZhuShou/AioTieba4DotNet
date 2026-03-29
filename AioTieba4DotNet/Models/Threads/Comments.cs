namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     楼中楼评论列表
/// </summary>
public class Comments
{
    /// <summary>
    ///     页码信息
    /// </summary>
    public required PageT Page { get; init; }

    /// <summary>
    ///     吧信息
    /// </summary>
    public required ForumT Forum { get; init; }

    /// <summary>
    ///     主题帖信息
    /// </summary>
    public required Thread Thread { get; init; }

    /// <summary>
    ///     楼层信息
    /// </summary>
    public required Post Post { get; init; }

    /// <summary>
    ///     评论列表
    /// </summary>
    public required List<Comment> Objs { get; init; }

    /// <summary>
    ///     是否还有更多
    /// </summary>
    public bool HasMore => Page.HasMore;
}
