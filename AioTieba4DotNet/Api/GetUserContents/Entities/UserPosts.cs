using AioTieba4DotNet.Api.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

/// <summary>
/// 用户历史回复信息列表
/// </summary>
public class UserPosts : Containers<UserPost>
{
    public UserPosts(List<UserPost> objs, int fid, int tid) : base(objs)
    {
        Fid = fid;
        Tid = tid;
    }

    public UserPosts(IEnumerable<UserPost>? collection, int fid, int tid) : base(collection)
    {
        Fid = fid;
        Tid = tid;
    }

    /// <summary>
    /// 所在吧id
    /// </summary>
    public int Fid { get; init; }

    /// <summary>
    /// 所在主题帖id
    /// </summary>
    public int Tid { get; init; }

    /// <summary>
    /// Creates an instance of <see cref="UserPosts"/> from a given <see cref="PostInfoList"/> object.
    /// </summary>
    /// <param name="dataRes">The <see cref="PostInfoList"/> containing data to construct the <see cref="UserPosts"/> object.</param>
    /// <returns>A new instance of <see cref="UserPosts"/> populated with data derived from the provided <see cref="PostInfoList"/>.</returns>
    public static UserPosts FromTbData(PostInfoList dataRes)
    {
        var fid = (int)dataRes.ForumId;
        var tid = (int)dataRes.ThreadId;
        List<UserPost> objs = [];
        foreach (var postInfoContent in dataRes.Content)
        {
            var userPost = UserPost.FromTbData(postInfoContent);
            userPost.Fid = fid;
            userPost.Tid = tid;
            objs.Add(userPost);
        }

        return new UserPosts(objs, fid, tid);
    }
}