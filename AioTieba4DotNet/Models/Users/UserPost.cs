using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

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
    public long Fid { get; set; }

    /// <summary>
    ///     所在主题帖id
    /// </summary>
    public long Tid { get; set; }

    /// <summary>
    ///     回复id
    /// </summary>
    public long Pid { get; set; }

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
    ///     转换为字符串
    /// </summary>
    /// <returns>回复摘要</returns>
    public override string ToString()
    {
        return
            $"{nameof(Contents)}: {Contents}, {nameof(Fid)}: {Fid}, {nameof(Tid)}: {Tid}, {nameof(Pid)}: {Pid}, {nameof(IsComment)}: {IsComment}, {nameof(User)}: {User}, {nameof(CreateTime)}: {CreateTime}";
    }
}
