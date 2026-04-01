using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Threads;
using CommentModel = AioTieba4DotNet.Models.Threads.Comment;
using PostModel = AioTieba4DotNet.Models.Threads.Post;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class PostMapper
{
    internal static PostModel FromTbData(Post? dataProto)

    {
        if (dataProto == null)

            return new PostModel
            {
                Content = ContentMapper.FromTbData(
                    (IEnumerable<PbContent>?)null)
            };


        var sign = dataProto.Signature != null
            ? string.Join("", dataProto.Signature.Content.Where(p => p.Type == 0).Select(p => p.Text))
            : "";

        return new PostModel
        {
            Content = ContentMapper.FromTbData(dataProto.Content),
            Sign = sign,
            Pid = dataProto.Id,
            User = UserInfoTMapper.FromTbData(dataProto.Author),
            AuthorId = dataProto.AuthorId,
            Floor = dataProto.Floor,
            ReplyNum = dataProto.SubPostNumber,
            Agree = dataProto.Agree?.AgreeNum ?? 0,
            Disagree = dataProto.Agree?.DisagreeNum ?? 0,
            CreateTime = dataProto.Time,
            Comments = dataProto.SubPostList?.SubPostList.Select(CommentMapper.FromTbData).ToList() ?? []
        };
    }
}
