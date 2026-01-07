using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;

namespace AioTieba4DotNet.Api.GetThreads.Entities;

/// <summary>
/// 主题帖信息
/// </summary>
public class Thread
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
    /// 标题
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// 所在吧id
    /// </summary>
    public long Fid { get; set; } = 0;

    /// <summary>
    /// 所在贴吧名
    /// </summary>
    public string Fname { get; set; } = "";

    /// <summary>
    /// 主题帖tid
    /// </summary>
    public long Tid { get; init; }

    /// <summary>
    /// 首楼回复pid
    /// </summary>
    public long Pid { get; init; }

    /// <summary>
    /// 发布者的用户信息
    /// </summary>
    public UserInfoT? User { get; set; }

    /// <summary>
    ///  发布者的user_id
    /// </summary>
    public long AuthorId { get; init; }

    /// <summary>
    /// 帖子类型
    /// </summary>
    public int Type { get; init; }

    /// <summary>
    /// 帖子所在分区id
    /// </summary>
    public int TabId;

    /// <summary>
    /// 是否精品帖
    /// </summary>
    public bool IsGood { get; init; }

    /// <summary>
    /// 是否置顶帖
    /// </summary>
    public bool IsTop { get; init; }

    /// <summary>
    /// 是否分享帖
    /// </summary>
    public bool IsShare { get; init; }

    /// <summary>
    /// 是否被屏蔽
    /// </summary>
    public bool IsHide { get; init; }

    /// <summary>
    /// 是否为置顶话题
    /// </summary>
    public bool IsLivePost { get; init; }

    /// <summary>
    /// 是否为求助帖
    /// </summary>
    public bool IsHelp => Type == 71;

    /// <summary>
    /// 投票信息
    /// </summary>
    public VoteInfo? VoteInfo { get; set; }

    /// <summary>
    /// 转发来的原帖内容
    /// </summary>
    public ShareThread? ShareOrigin { get; init; }

    /// <summary>
    /// 虚拟形象
    /// </summary>
    public required VirtualImagePf VirtualImage { get; init; }

    /// <summary>
    /// 浏览量
    /// </summary>
    public int ViewNum { get; init; }

    /// <summary>
    /// 回复数
    /// </summary>
    public int ReplyNum { get; init; }

    /// <summary>
    /// 分享数
    /// </summary>
    public long ShareNum { get; init; }

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
    public int CreateTime { get; init; }

    /// <summary>
    /// 最后回复时间 10位时间戳 以秒为单位
    /// </summary>
    public int LastTime { get; init; }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="threadInfo"></param>
    /// <returns>Thread</returns>
    public static Thread FromTbData(ThreadInfo threadInfo)
    {
        return new Thread
        {
            Content = Content.FromTbData(threadInfo),
            Title = threadInfo.Title,
            Tid = threadInfo.Id,
            Pid = threadInfo.FirstPostId,
            AuthorId = threadInfo.AuthorId,
            VirtualImage = VirtualImagePf.FromTbData(threadInfo),
            Type = threadInfo.ThreadType,
            TabId = threadInfo.TabId,
            IsGood = threadInfo.IsGood == 1,
            IsTop = threadInfo.IsTop == 1,
            IsShare = threadInfo.IsShareThread == 1,
            IsHide = threadInfo.IsFrsMask == 1,
            IsLivePost = threadInfo.IsLivepost == 1,
            VoteInfo = threadInfo.PollInfo != null ? VoteInfo.FromTbData(threadInfo.PollInfo) : null,
            ShareOrigin = threadInfo is { IsShareThread: 1, OriginThreadInfo.Pid: 0 }
                ? ShareThread.FromTbData(threadInfo.OriginThreadInfo)
                : null,
            ViewNum = threadInfo.ViewNum,
            ReplyNum = threadInfo.ReplyNum,
            ShareNum = threadInfo.ShareNum,
            Agree = threadInfo.Agree.AgreeNum,
            Disagree = threadInfo.Agree.DisagreeNum,
            CreateTime = threadInfo.CreateTime,
            LastTime = threadInfo.LastTimeInt,
        };
    }

    /// <summary>
    /// 字符串输出
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{nameof(TabId)}: {TabId}, {nameof(Content)}: {Content}, {nameof(Text)}: {Text}, {nameof(Title)}: {Title}, {nameof(Fid)}: {Fid}, {nameof(Fname)}: {Fname}, {nameof(Tid)}: {Tid}, {nameof(Pid)}: {Pid}, {nameof(User)}: {User}, {nameof(VoteInfo)}: {VoteInfo}, {nameof(AuthorId)}: {AuthorId}, {nameof(VirtualImage)}: {VirtualImage}, {nameof(Type)}: {Type}, {nameof(IsGood)}: {IsGood}, {nameof(IsTop)}: {IsTop}, {nameof(IsShare)}: {IsShare}, {nameof(IsHide)}: {IsHide}, {nameof(IsLivePost)}: {IsLivePost},  {nameof(IsHelp)}: {IsHelp}, {nameof(ShareOrigin)}: {ShareOrigin}, {nameof(ViewNum)}: {ViewNum}, {nameof(ReplyNum)}: {ReplyNum}, {nameof(ShareNum)}: {ShareNum}, {nameof(Agree)}: {Agree}, {nameof(Disagree)}: {Disagree}, {nameof(CreateTime)}: {CreateTime}, {nameof(LastTime)}: {LastTime}";
    }
}