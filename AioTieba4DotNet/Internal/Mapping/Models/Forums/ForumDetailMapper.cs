using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ForumDetailMapper
{
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
}
