using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Api.GetThreads.Entities;

namespace AioTieba4DotNet.Api.GetThreadPosts.Entities;

/// <summary>
/// 楼中楼信息
/// </summary>
public class Comment
{
    /// <summary>
    /// 正文内容碎片列表
    /// </summary>
    public required Content Content { get; init; }

    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text => Content.Text;

    /// <summary>
    /// 所在吧id
    /// </summary>
    public long Fid { get; set; }

    /// <summary>
    /// 所在贴吧名
    /// </summary>
    public string Fname { get; set; } = "";

    /// <summary>
    /// 所在主题帖id
    /// </summary>
    public long Tid { get; set; }

    /// <summary>
    /// 所在楼层id
    /// </summary>
    public long Ppid { get; set; }

    /// <summary>
    /// 楼中楼id
    /// </summary>
    public long Pid { get; init; }

    /// <summary>
    /// 发布者的用户信息
    /// </summary>
    public UserInfoT? User { get; set; }

    /// <summary>
    /// 发布者的user_id
    /// </summary>
    public long AuthorId { get; init; }

    /// <summary>
    /// 被回复者的user_id
    /// </summary>
    public long ReplyToId { get; init; }

    /// <summary>
    /// 所在楼层数
    /// </summary>
    public uint Floor { get; set; }

    /// <summary>
    /// 点赞数
    /// </summary>
    public long Agree { get; init; }

    /// <summary>
    /// 点踩数
    /// </summary>
    public long Disagree { get; init; }

    /// <summary>
    /// 创建时间 10位时间戳 以秒为单位
    /// </summary>
    public uint CreateTime { get; init; }

    /// <summary>
    /// 是否楼主
    /// </summary>
    public bool IsThreadAuthor { get; set; }

    public static Comment FromTbData(SubPostList? dataProto)
    {
        if (dataProto == null)
        {
            return new Comment
            {
                Content = Content.FromTbData((IEnumerable<PbContent>?)null)
            };
        }
        var content = Content.FromTbData(dataProto.Content);
        
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
            
            if (content.Frags.Count > 0 && content.Frags[0] is FragText firstFragText && firstFragText.Text.StartsWith(" :"))
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
                    if (indexInTexts != -1)
                    {
                        content.Texts[indexInTexts] = newFirstText;
                    }
                }
            }
        }

        return new Comment
        {
            Content = content,
            Pid = dataProto.Id,
            User = UserInfoT.FromTbData(dataProto.Author),
            AuthorId = dataProto.AuthorId,
            ReplyToId = replyToId,
            Agree = dataProto.Agree?.AgreeNum ?? 0,
            Disagree = dataProto.Agree?.DisagreeNum ?? 0,
            CreateTime = dataProto.Time
        };
    }
}
