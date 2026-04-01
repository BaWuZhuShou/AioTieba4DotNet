using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     表示 aiotieba `get_self_follow_forums_v1` 返回的当前账号关注贴吧列表。
/// </summary>
public class SelfFollowForumsV1 : Containers<SelfFollowForumV1>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">V1 关注贴吧列表</param>
    /// <param name="page">分页信息</param>
    public SelfFollowForumsV1(List<SelfFollowForumV1> objs, SelfFollowForumsV1Page page) : base(objs)
    {
        ArgumentNullException.ThrowIfNull(page);
        Page = page;
    }

    /// <summary>
    ///     分页信息
    /// </summary>
    public SelfFollowForumsV1Page Page { get; }

    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore => Page.HasMore;
}
