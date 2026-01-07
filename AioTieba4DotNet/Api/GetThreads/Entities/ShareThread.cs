using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.Entities.Contents;

namespace AioTieba4DotNet.Api.GetThreads.Entities;

/// <summary>
/// 被分享的主题帖信息
/// </summary>
public class ShareThread
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text => Title != "" ? $"{Title}\n{Content.Texts}" : Content.Texts.ToString() ?? "";

    /// <summary>
    /// 正文内容碎片列表
    /// </summary>
    public required Content Content { get; init; }

    /// <summary>
    /// 标题内容
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// 发布者的user_id
    /// </summary>
    public long AuthorId { get; init; }

    /// <summary>
    /// 所在吧id
    /// </summary>
    public long Fid { get; init; }

    /// <summary>
    /// 所在贴吧名
    /// </summary>
    public string Fname { get; init; } = "";

    /// <summary>
    /// 主题帖tid
    /// </summary>
    public int Tid { get; init; }

    /// <summary>
    /// 首楼的回复id
    /// </summary>
    public long Pid { get; init; }

    /// <summary>
    /// 投票内容
    /// </summary>
    public VoteInfo? VoteInfo { get; init; }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="threadInfo"></param>
    /// <returns>ShareThread</returns>
    public static ShareThread FromTbData(ThreadInfo.Types.OriginThreadInfo threadInfo)
    {
        return new ShareThread
        {
            Content = Content.FromTbData(threadInfo),
            AuthorId = threadInfo.Content.Count != 0 ? threadInfo.Content.First().Uid : 0,
            Title = threadInfo.Title,
            Fid = threadInfo.Fid,
            Fname = threadInfo.Fname,
            Tid = threadInfo.Tid != null ? Convert.ToInt16(threadInfo.Tid) : 0,
            Pid = threadInfo.Pid,
            VoteInfo = threadInfo.PollInfo != null ? VoteInfo.FromTbData(threadInfo.PollInfo) : null,
        };
    }

    /// <summary>
    /// 格式设置
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{nameof(Text)}: {Text}, {nameof(Content)}: {Content}, {nameof(Title)}: {Title}, {nameof(AuthorId)}: {AuthorId}, {nameof(Fid)}: {Fid}, {nameof(Fname)}: {Fname}, {nameof(Tid)}: {Tid}, {nameof(Pid)}: {Pid}, {nameof(VoteInfo)}: {VoteInfo}";
    }
}