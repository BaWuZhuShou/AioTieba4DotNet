using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     用户关注贴吧列表
/// </summary>
public class FollowForums : Containers<FollowForum>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">关注贴吧列表</param>
    /// <param name="hasMore">是否还有下一页</param>
    public FollowForums(List<FollowForum> objs, bool hasMore) : base(objs)
    {
        HasMore = hasMore;
    }

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore { get; }
}
