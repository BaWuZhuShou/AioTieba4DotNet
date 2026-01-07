using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Api.GetThreads.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

/// <summary>
/// 用户历史回复信息
/// </summary>
public class UserPost
{
    /// <summary>
    /// 正文内容碎片列表
    /// </summary>
    public Content Contents { get; init; } = null!;

    /// <summary>
    /// 所在吧id
    /// </summary>
    public int Fid { get; set; }

    /// <summary>
    /// 所在主题帖id
    /// </summary>
    public int Tid { get; set; }

    /// <summary>
    /// 回复id
    /// </summary>
    public int Pid { get; set; }

    /// <summary>
    /// 是否为楼中楼
    /// </summary>
    public bool IsComment { get; set; }

    /// <summary>
    /// 用户数据
    /// </summary>
    public UserInfo User { get; set; } = null!;

    /// <summary>
    /// 创建时间 10位时间戳 以秒为单位
    /// </summary>
    public int CreateTime { get; set; }

    public static UserPost FromTbData(PostInfoList.Types.PostInfoContent dataRes)
    {
        var contents = Content.FromTbData(dataRes);

        return new UserPost()
        {
            Contents = contents,
            Pid = (int)dataRes.PostId,
            IsComment = dataRes.PostType != 0,
            CreateTime = (int)dataRes.CreateTime
        };
    }

    public override string ToString()
    {
        return
            $"{nameof(Contents)}: {Contents}, {nameof(Fid)}: {Fid}, {nameof(Tid)}: {Tid}, {nameof(Pid)}: {Pid}, {nameof(IsComment)}: {IsComment}, {nameof(User)}: {User}, {nameof(CreateTime)}: {CreateTime}";
    }
}