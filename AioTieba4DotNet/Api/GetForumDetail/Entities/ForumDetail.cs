namespace AioTieba4DotNet.Api.GetForumDetail.Entities;

public class ForumDetail
{
    public ulong Fid { get; set; } = 0;
    public string Fname { get; set; } = "";
    public string Category { get; set; } = "";
    public string SmallAvatar { get; set; } = "";
    public string OriginAvatar { get; set; } = "";
    public string Slogan { get; set; } = "";
    public uint MemberNum { get; set; }
    public uint PostNum { get; set; }
    public bool HasBaWu { get; set; }

    public static ForumDetail FromTbData(GetForumDetailResIdl.Types.DataRes data)
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

    public override string ToString()
    {
        return
            $"{nameof(Fid)}: {Fid}, {nameof(Fname)}: {Fname}, {nameof(Category)}: {Category}, {nameof(SmallAvatar)}: {SmallAvatar}, {nameof(OriginAvatar)}: {OriginAvatar}, {nameof(Slogan)}: {Slogan}, {nameof(MemberNum)}: {MemberNum}, {nameof(PostNum)}: {PostNum}, {nameof(HasBaWu)}: {HasBaWu}";
    }
}
