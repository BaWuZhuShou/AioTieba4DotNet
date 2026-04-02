using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     表示回收站条目列表。
/// </summary>
public sealed class Recovers : Containers<Recover>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Recovers" /> class.
    /// </summary>
    /// <param name="objs">A recover entry collection.</param>
    /// <param name="page">A page model.</param>
    public Recovers(List<Recover> objs, RecoverPage page) : base(objs)
    {
        ArgumentNullException.ThrowIfNull(page);
        Page = page;
    }

    /// <summary>
    ///     获取分页信息。
    /// </summary>
    /// <value>A page model.</value>
    public RecoverPage Page { get; }

    /// <summary>
    ///     获取一个值，该值指示是否还有下一页。
    /// </summary>
    /// <value><see langword="true" /> if another page exists; otherwise, <see langword="false" />.</value>
    public bool HasMore => Page.HasMore;
}
