using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

/// <summary>
///     用户历史回复信息
/// </summary>
public class UserPost
{
    /// <summary>
    ///     正文内容碎片列表
    /// </summary>
    public Content Contents { get; init; } = null!;

    /// <summary>
    ///     所在吧id
    /// </summary>
    public int Fid { get; set; }

    /// <summary>
    ///     所在主题帖id
    /// </summary>
    public int Tid { get; set; }

    /// <summary>
    ///     回复id
    /// </summary>
    public int Pid { get; set; }

    /// <summary>
    ///     是否为楼中楼
    /// </summary>
    public bool IsComment { get; set; }

    /// <summary>
    ///     用户数据
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    ///     创建时间 10位时间戳 以秒为单位
    /// </summary>
    public int CreateTime { get; set; }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes">Protobuf 帖子内容信息数据</param>
    /// <returns>用户历史回复实体</returns>
    public static UserPost FromTbData(PostInfoList.Types.PostInfoContent dataRes)
    {
        var contents = Content.FromTbData(dataRes);

        return new UserPost
        {
            Contents = contents,
            Pid = (int)dataRes.PostId,
            IsComment = dataRes.PostType != 0,
            CreateTime = (int)dataRes.CreateTime
        };
    }

    /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>回复摘要</returns>
    public override string ToString()
    {
        return
            $"{nameof(Contents)}: {Contents}, {nameof(Fid)}: {Fid}, {nameof(Tid)}: {Tid}, {nameof(Pid)}: {Pid}, {nameof(IsComment)}: {IsComment}, {nameof(User)}: {User}, {nameof(CreateTime)}: {CreateTime}";
    }
}
