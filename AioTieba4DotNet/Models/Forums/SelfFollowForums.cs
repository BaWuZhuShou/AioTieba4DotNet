using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     当前账号关注贴吧列表
/// </summary>
public class SelfFollowForums : Containers<SelfFollowForum>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">关注贴吧列表</param>
    /// <param name="hasMore">是否还有下一页</param>
    public SelfFollowForums(List<SelfFollowForum> objs, bool hasMore) : base(objs)
    {
        HasMore = hasMore;
    }

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore { get; }
}
