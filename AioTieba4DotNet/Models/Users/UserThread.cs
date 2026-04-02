using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     用户历史发布主题帖信息
/// </summary>
public class UserThread
{
    /// <summary>
    ///     正文内容碎片列表
    /// </summary>
    public Content Contents { get; init; } = new();

    /// <summary>
    ///     标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    ///     吧 ID
    /// </summary>
    public long Fid { get; init; }

    /// <summary>
    ///     吧名
    /// </summary>
    public string Fname { get; init; } = string.Empty;

    /// <summary>
    ///     主题帖 ID
    /// </summary>
    public long Tid { get; init; }

    /// <summary>
    ///     首楼 ID
    /// </summary>
    public long Pid { get; init; }

    /// <summary>
    ///     用户信息
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    ///     帖子类型
    /// </summary>
    public int Type { get; init; }

    /// <summary>
    ///     投票信息
    /// </summary>
    public VoteInfo? VoteInfo { get; init; }

    /// <summary>
    ///     浏览量
    /// </summary>
    public int ViewNum { get; init; }

    /// <summary>
    ///     回复数
    /// </summary>
    public int ReplyNum { get; init; }

    /// <summary>
    ///     分享数
    /// </summary>
    public int ShareNum { get; init; }

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
    public int CreateTime { get; init; }

    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text => string.IsNullOrEmpty(Title) ? Contents.Text : $"{Title}\n{Contents.Text}";

    /// <summary>
    ///     是否为求助帖
    /// </summary>
    public bool IsHelp => Type == 71;
}
