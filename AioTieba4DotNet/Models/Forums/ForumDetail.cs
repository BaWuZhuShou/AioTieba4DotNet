namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     吧详情信息
/// </summary>
public class ForumDetail
{
    /// <summary>
    ///     吧 ID
    /// </summary>
    public ulong Fid { get; set; }

    /// <summary>
    ///     吧名
    /// </summary>
    public string Fname { get; set; } = "";

    /// <summary>
    ///     分类
    /// </summary>
    public string Category { get; set; } = "";

    /// <summary>
    ///     小头像
    /// </summary>
    public string SmallAvatar { get; set; } = "";

    /// <summary>
    ///     原头像
    /// </summary>
    public string OriginAvatar { get; set; } = "";

    /// <summary>
    ///     吧标语
    /// </summary>
    public string Slogan { get; set; } = "";

    /// <summary>
    ///     会员数
    /// </summary>
    public uint MemberNum { get; set; }

    /// <summary>
    ///     发帖数
    /// </summary>
    public uint PostNum { get; set; }

    /// <summary>
    ///     是否有吧务
    /// </summary>
    public bool HasBaWu { get; set; }
    /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>吧详情摘要</returns>
    public override string ToString()
    {
        return
            $"{nameof(Fid)}: {Fid}, {nameof(Fname)}: {Fname}, {nameof(Category)}: {Category}, {nameof(SmallAvatar)}: {SmallAvatar}, {nameof(OriginAvatar)}: {OriginAvatar}, {nameof(Slogan)}: {Slogan}, {nameof(MemberNum)}: {MemberNum}, {nameof(PostNum)}: {PostNum}, {nameof(HasBaWu)}: {HasBaWu}";
    }
}
