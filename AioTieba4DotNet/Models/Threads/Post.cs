using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     楼层信息
/// </summary>
public class Post
{
    /// <summary>
    ///     正文内容碎片列表
    /// </summary>
    public required Content Content { get; init; }

    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text => string.IsNullOrEmpty(Sign) ? Content.Text : $"{Content.Text}\n{Sign}";

    /// <summary>
    ///     小尾巴文本内容
    /// </summary>
    public string Sign { get; init; } = "";

    /// <summary>
    ///     所在吧id
    /// </summary>
    public long Fid { get; set; }

    /// <summary>
    ///     所在贴吧名
    /// </summary>
    public string Fname { get; set; } = "";

    /// <summary>
    ///     所在主题帖id
    /// </summary>
    public long Tid { get; set; }

    /// <summary>
    ///     楼层id
    /// </summary>
    public long Pid { get; init; }

    /// <summary>
    ///     发布者的用户信息
    /// </summary>
    public UserInfoT? User { get; set; }

    /// <summary>
    ///     发布者的user_id
    /// </summary>
    public long AuthorId { get; init; }

    /// <summary>
    ///     楼层数
    /// </summary>
    public uint Floor { get; init; }

    /// <summary>
    ///     楼中楼列表
    /// </summary>
    public List<Comment> Comments { get; init; } = [];

    /// <summary>
    ///     楼中楼总数
    /// </summary>
    public uint ReplyNum { get; init; }

    /// <summary>
    ///     点赞数
    /// </summary>
    public long Agree { get; init; }

    /// <summary>
    ///     点踩数
    /// </summary>
    public long Disagree { get; init; }

    /// <summary>
    ///     创建时间 10位时间戳 以秒为单位
    /// </summary>
    public uint CreateTime { get; init; }

    /// <summary>
    ///     是否楼主
    /// </summary>
    public bool IsThreadAuthor { get; set; }
}
