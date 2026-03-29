using AioTieba4DotNet.Models.Threads;
using PostModel = AioTieba4DotNet.Models.Threads.Post;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class CommentsMapper
{
    internal static Comments FromTbData(PbFloorResIdl.Types.DataRes? dataRes)

        {

            if (dataRes == null)

                return new Comments

                {

                    Page = new PageT(),

                    Forum = new ForumT(),

                    Thread = AioTieba4DotNet.Internal.Mapping.ThreadMapper.FromTbData(null),

                    Post = AioTieba4DotNet.Internal.Mapping.PostMapper.FromTbData(null),

                    Objs = []

                };



            var forum = AioTieba4DotNet.Internal.Mapping.ForumTMapper.FromTbData(dataRes.Forum);

            var thread = AioTieba4DotNet.Internal.Mapping.ThreadMapper.FromTbData(dataRes.Thread);

            thread.Fname = forum.Fname;

            thread.Fid = forum.Fid;



            var post = AioTieba4DotNet.Internal.Mapping.PostMapper.FromTbData(dataRes.Post);

            post.Fname = forum.Fname;

            post.Fid = forum.Fid;

            post.Tid = thread.Tid;



            var comments = dataRes.SubpostList.Select(AioTieba4DotNet.Internal.Mapping.CommentMapper.FromTbData).ToList();

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

                Page = AioTieba4DotNet.Internal.Mapping.PageTMapper.FromTbData(dataRes.Page),

                Forum = forum,

                Thread = thread,

                Post = post,

                Objs = comments

            };

        }
}
