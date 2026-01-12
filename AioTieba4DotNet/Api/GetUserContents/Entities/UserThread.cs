using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

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
    public int Fid { get; init; }

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

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes">Protobuf 帖子列表信息数据</param>
    /// <returns>用户历史发布主题帖实体</returns>
    internal static UserThread FromTbData(PostInfoList dataRes)
    {
        return new UserThread
        {
            Contents = Content.FromTbData(dataRes),
            Title = dataRes.Title,
            Fid = (int)dataRes.ForumId,
            Fname = dataRes.ForumName,
            Tid = (long)dataRes.ThreadId,
            Pid = (long)dataRes.PostId,
            Type = (int)dataRes.ThreadType,
            VoteInfo = VoteInfo.FromTbData(dataRes.PollInfo),
            ViewNum = dataRes.FreqNum,
            ReplyNum = (int)dataRes.ReplyNum,
            ShareNum = dataRes.ShareNum,
            Agree = dataRes.Agree?.AgreeNum ?? 0,
            Disagree = dataRes.Agree?.DisagreeNum ?? 0,
            CreateTime = (int)dataRes.CreateTime
        };
    }
}
