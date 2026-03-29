using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     用户历史回复信息列表
/// </summary>
public class UserPosts : Containers<UserPost>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">用户历史回复列表</param>
    /// <param name="fid">吧 ID</param>
    /// <param name="tid">主题帖 ID</param>
    public UserPosts(List<UserPost> objs, long fid, long tid) : base(objs)
    {
        Fid = fid;
        Tid = tid;
    }

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="collection">用户历史回复集合</param>
    /// <param name="fid">吧 ID</param>
    /// <param name="tid">主题帖 ID</param>
    public UserPosts(IEnumerable<UserPost>? collection, long fid, long tid) : base(collection)
    {
        Fid = fid;
        Tid = tid;
    }

    /// <summary>
    ///     所在吧id
    /// </summary>
    public long Fid { get; init; }

    /// <summary>
    ///     所在主题帖id
    /// </summary>
    public long Tid { get; init; }
}
