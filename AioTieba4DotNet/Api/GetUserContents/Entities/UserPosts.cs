using AioTieba4DotNet.Api.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

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
    public UserPosts(List<UserPost> objs, int fid, int tid) : base(objs)
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
    public UserPosts(IEnumerable<UserPost>? collection, int fid, int tid) : base(collection)
    {
        Fid = fid;
        Tid = tid;
    }

    /// <summary>
    ///     所在吧id
    /// </summary>
    public int Fid { get; init; }

    /// <summary>
    ///     所在主题帖id
    /// </summary>
    public int Tid { get; init; }

    /// <summary>
    ///     Creates an instance of <see cref="UserPosts" /> from a given <see cref="PostInfoList" /> object.
    /// </summary>
    /// <param name="dataRes">The <see cref="PostInfoList" /> containing data to construct the <see cref="UserPosts" /> object.</param>
    /// <returns>
    ///     A new instance of <see cref="UserPosts" /> populated with data derived from the provided
    ///     <see cref="PostInfoList" />.
    /// </returns>
    internal static UserPosts FromTbData(PostInfoList dataRes)
    {
        var fid = (int)dataRes.ForumId;
        var tid = (int)dataRes.ThreadId;
        List<UserPost> objs = [];
        foreach (var userPost in dataRes.Content.Select(UserPost.FromTbData))
        {
            userPost.Fid = fid;
            userPost.Tid = tid;
            objs.Add(userPost);
        }

        return new UserPosts(objs, fid, tid);
    }
}
