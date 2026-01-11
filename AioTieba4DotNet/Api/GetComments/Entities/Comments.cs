using AioTieba4DotNet.Api.GetThreadPosts.Entities;
using AioTieba4DotNet.Api.GetThreads.Entities;
using Thread = AioTieba4DotNet.Api.GetThreads.Entities.Thread;

namespace AioTieba4DotNet.Api.GetComments.Entities;

/// <summary>
///     楼中楼评论列表
/// </summary>
public class Comments
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
    ///     楼层信息
    /// </summary>
    public required GetThreadPosts.Entities.Post Post { get; init; }

    /// <summary>
    ///     评论列表
    /// </summary>
    public required List<Comment> Objs { get; init; }

    /// <summary>
    ///     是否还有更多
    /// </summary>
    public bool HasMore => Page.HasMore;

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes">Protobuf 楼层响应数据</param>
    /// <returns>楼中楼评论列表实体</returns>
    public static Comments FromTbData(PbFloorResIdl.Types.DataRes? dataRes)
    {
        if (dataRes == null)
            return new Comments
            {
                Page = new PageT(),
                Forum = new ForumT(),
                Thread = Thread.FromTbData(null),
                Post = GetThreadPosts.Entities.Post.FromTbData(null),
                Objs = []
            };

        var forum = ForumT.FromTbData(dataRes.Forum);
        var thread = Thread.FromTbData(dataRes.Thread);
        thread.Fname = forum.Fname;
        thread.Fid = forum.Fid;

        var post = GetThreadPosts.Entities.Post.FromTbData(dataRes.Post);
        post.Fname = forum.Fname;
        post.Fid = forum.Fid;
        post.Tid = thread.Tid;

        var comments = dataRes.SubpostList.Select(Comment.FromTbData).ToList();
        foreach (var comment in comments)
        {
            comment.Fname = forum.Fname;
            comment.Fid = forum.Fid;
            comment.Tid = thread.Tid;
            comment.Ppid = post.Pid;
            comment.Floor = post.Floor;
            comment.IsThreadAuthor = comment.AuthorId == thread.AuthorId;
        }

        return new Comments
        {
            Page = PageT.FromTbData(dataRes.Page),
            Forum = forum,
            Thread = thread,
            Post = post,
            Objs = comments
        };
    }
}
