using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     首页推荐屏蔽贴吧列表
/// </summary>
public class DislikeForums : Containers<DislikeForum>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">屏蔽贴吧列表</param>
    /// <param name="page">分页信息</param>
    public DislikeForums(List<DislikeForum> objs, DislikeForumsPage page) : base(objs)
    {
        ArgumentNullException.ThrowIfNull(page);
        Page = page;
    }

    /// <summary>
    ///     分页信息
    /// </summary>
    public DislikeForumsPage Page { get; }

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
