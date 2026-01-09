using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

public class UserThread
{
    public Content Contents { get; init; } = new();
    public string Title { get; init; } = string.Empty;

    public int Fid { get; init; }
    public string Fname { get; init; } = string.Empty;
    public long Tid { get; init; }
    public long Pid { get; init; }
    public UserInfo? User { get; set; }

    public int Type { get; init; }

    public VoteInfo? VoteInfo { get; init; }
    public int ViewNum { get; init; }
    public int ReplyNum { get; init; }
    public int ShareNum { get; init; }
    public long Agree { get; init; }
    public long Disagree { get; init; }
    public int CreateTime { get; init; }

    public string Text => string.IsNullOrEmpty(Title) ? Contents.Text : $"{Title}\n{Contents.Text}";

    public bool IsHelp => Type == 71;

    public static UserThread FromTbData(PostInfoList dataRes)
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
