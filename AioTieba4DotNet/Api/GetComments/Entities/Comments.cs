using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Api.GetThreadPosts.Entities;

namespace AioTieba4DotNet.Api.GetComments.Entities;

public class Comments
{
    public required PageT Page { get; init; }
    public required ForumT Forum { get; init; }
    public required AioTieba4DotNet.Api.GetThreads.Entities.Thread Thread { get; init; }
    public required AioTieba4DotNet.Api.GetThreadPosts.Entities.Post Post { get; init; }
    public required List<Comment> Objs { get; init; }

    public bool HasMore => Page.HasMore;

    public static Comments FromTbData(PbFloorResIdl.Types.DataRes? dataRes)
    {
        if (dataRes == null)
            return new Comments
            {
                Page = new PageT(),
                Forum = new ForumT(),
                Thread = AioTieba4DotNet.Api.GetThreads.Entities.Thread.FromTbData(null),
                Post = AioTieba4DotNet.Api.GetThreadPosts.Entities.Post.FromTbData(null),
                Objs = []
            };

        var forum = ForumT.FromTbData(dataRes.Forum);
        var thread = AioTieba4DotNet.Api.GetThreads.Entities.Thread.FromTbData(dataRes.Thread);
        thread.Fname = forum.Fname;
        thread.Fid = forum.Fid;

        var post = AioTieba4DotNet.Api.GetThreadPosts.Entities.Post.FromTbData(dataRes.Post);
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
