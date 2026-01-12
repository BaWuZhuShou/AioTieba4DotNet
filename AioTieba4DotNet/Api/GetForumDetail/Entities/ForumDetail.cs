namespace AioTieba4DotNet.Api.GetForumDetail.Entities;

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
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="data">Protobuf 吧详情数据</param>
    /// <returns>吧详情信息实体</returns>
    internal static ForumDetail FromTbData(GetForumDetailResIdl.Types.DataRes data)
    {
        var forumInfo = data.ForumInfo;
        return new ForumDetail
        {
            Fid = forumInfo.ForumId,
            Fname = forumInfo.ForumName,
            Category = forumInfo.Lv1Name,
            SmallAvatar = forumInfo.Avatar,
            OriginAvatar = forumInfo.AvatarOrigin,
            Slogan = forumInfo.Slogan,
            MemberNum = forumInfo.MemberCount,
            PostNum = forumInfo.ThreadCount,
            HasBaWu = data.ElectionTab is { NewStrategyText: "已有吧主" }
        };
    }

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
