namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     回复列表
/// </summary>
public class Posts
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
    ///     回复列表
    /// </summary>
    public required List<Post> Objs { get; init; }

    /// <summary>
    ///     是否还有更多
    /// </summary>
    public bool HasMore => Page.HasMore;
}
