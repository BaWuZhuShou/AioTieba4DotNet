using AioTieba4DotNet.Api.GetThreads.Entities;
using Thread = AioTieba4DotNet.Api.GetThreads.Entities.Thread;

namespace AioTieba4DotNet.Api.GetThreadPosts.Entities;

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

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes">Protobuf 帖子页面响应数据</param>
    /// <returns>回复列表实体</returns>
    internal static Posts FromTbData(PbPageResIdl.Types.DataRes dataRes)
    {
        var forum = ForumT.FromTbData(dataRes.Forum);
        var thread = Thread.FromTbData(dataRes.Thread);
        thread.Fname = forum.Fname;
        thread.Fid = forum.Fid;

        var users = dataRes.UserList.ToDictionary(u => u.Id, UserInfoT.FromTbData);
        if (users.TryGetValue(thread.AuthorId, out var threadAuthor)) thread.User = threadAuthor;

        var posts = dataRes.PostList.Select(Post.FromTbData).ToList();
        foreach (var post in posts)
        {
            post.Fname = forum.Fname;
            post.Fid = forum.Fid;
            post.Tid = thread.Tid;
            post.IsThreadAuthor = post.AuthorId == thread.AuthorId;
            if (users.TryGetValue(post.AuthorId, out var postAuthor)) post.User = postAuthor;

            foreach (var comment in post.Comments)
            {
                comment.Fname = forum.Fname;
                comment.Fid = forum.Fid;
                comment.Tid = thread.Tid;
                comment.Ppid = post.Pid;
                comment.Floor = post.Floor;
                comment.IsThreadAuthor = comment.AuthorId == thread.AuthorId;
                if (users.TryGetValue(comment.AuthorId, out var commentAuthor)) comment.User = commentAuthor;
            }
        }

        return new Posts { Page = PageT.FromTbData(dataRes.Page), Forum = forum, Thread = thread, Objs = posts };
    }
}
