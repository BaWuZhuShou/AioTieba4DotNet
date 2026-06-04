using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoTUidMapper
{
    internal static UserInfo FromTbData(User data)
    {
        return new UserInfo
        {
            UserId = data.Id,
            Portrait = UserProtoMapping.NormalizePortrait(data.Portrait),
            UserName = data.Name ?? string.Empty,
            NickNameNew = data.NameShow ?? string.Empty,
            TiebaUid = UserProtoMapping.ParseTiebaUid(data.TiebaUid),
            GLevel = (int)(data.UserGrowth?.LevelId ?? 0),
            Gender = (Gender)data.Gender,
            Age = UserProtoMapping.ParseTbAge(data.TbAge),
            PostNum = data.PostNum,
            FanNum = data.FansNum,
            FollowNum = data.ConcernNum,
            ForumNum = data.MyLikeNum,
            Sign = data.Intro ?? string.Empty,
            Ip = data.IpAddress ?? string.Empty,
            Icons = UserProtoMapping.MapIconNames(data),
            IsVip = UserProtoMapping.MapIsVip(data),
            IsGod = UserProtoMapping.MapIsGod(data),
            PrivLike = UserProtoMapping.MapPrivLike(data.PrivSets),
            PrivReply = UserProtoMapping.MapPrivReply(data.PrivSets)
        };
    }
}
