using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class CommentMapper
{
    internal static Comment FromTbData(SubPostList? dataProto)

    {
        if (dataProto == null) return new Comment { Content = ContentMapper.FromTbData((IEnumerable<PbContent>?)null) };


        var content = ContentMapper.FromTbData(dataProto.Content);


        long replyToId = 0;

        if (content.Frags.Count >= 2 &&
            content.Frags[0] is FragText { Text: "回复 " } &&
            dataProto.Content.Count >= 2)

        {
            replyToId = dataProto.Content[1].Uid;

            var f0 = content.Frags[0];

            var f1 = content.Frags[1];

            if (f0 is FragText t0) content.Texts.Remove(t0);

            if (f1 is FragText t1) content.Texts.Remove(t1);

            if (f1 is FragAt a1) content.Ats.Remove(a1);

            content.Frags.RemoveRange(0, 2);


            if (content.Frags.Count > 0 && content.Frags[0] is FragText firstFragText &&
                firstFragText.Text.StartsWith(" :"))

            {
                var trimmedText = firstFragText.Text[2..];

                if (string.IsNullOrEmpty(trimmedText))

                {
                    content.Frags.RemoveAt(0);

                    content.Texts.Remove(firstFragText);
                }

                else

                {
                    var newFirstText = new FragText { Text = trimmedText };

                    content.Frags[0] = newFirstText;

                    var indexInTexts = content.Texts.IndexOf(firstFragText);

                    if (indexInTexts != -1) content.Texts[indexInTexts] = newFirstText;
                }
            }
        }


        return new Comment
        {
            Content = content,
            Pid = dataProto.Id,
            User = UserInfoTMapper.FromTbData(dataProto.Author),
            AuthorId = dataProto.AuthorId,
            ReplyToId = replyToId,
            Agree = dataProto.Agree?.AgreeNum ?? 0,
            Disagree = dataProto.Agree?.DisagreeNum ?? 0,
            CreateTime = dataProto.Time
        };
    }
}
