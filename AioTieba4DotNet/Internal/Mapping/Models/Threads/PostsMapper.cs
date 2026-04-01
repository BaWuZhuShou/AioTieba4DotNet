using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class PostsMapper
{
    internal static Posts FromTbData(PbPageResIdl.Types.DataRes dataRes)

    {
        var forum = ForumTMapper.FromTbData(dataRes.Forum);

        var thread = ThreadMapper.FromTbData(dataRes.Thread);

        thread.Fname = forum.Fname;

        thread.Fid = forum.Fid;


        var users = dataRes.UserList.ToDictionary(u => u.Id, UserInfoTMapper.FromTbData);

        if (users.TryGetValue(thread.AuthorId, out var threadAuthor)) thread.User = threadAuthor;


        var posts = dataRes.PostList.Select(PostMapper.FromTbData).ToList();

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


        return new Posts { Page = PageTMapper.FromTbData(dataRes.Page), Forum = forum, Thread = thread, Objs = posts };
    }
}
