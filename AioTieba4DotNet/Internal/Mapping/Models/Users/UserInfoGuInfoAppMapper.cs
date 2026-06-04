using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoGuInfoAppMapper
{
    internal static UserInfo FromTbData(User dataProto)
    {
        return new UserInfo
        {
            UserId = dataProto.Id,
            Portrait = UserProtoMapping.NormalizePortrait(dataProto.Portrait),
            UserName = dataProto.Name ?? string.Empty,
            NickNameNew = dataProto.NameShow ?? string.Empty,
            TiebaUid = UserProtoMapping.ParseTiebaUid(dataProto.TiebaUid),
            GLevel = (int)(dataProto.UserGrowth?.LevelId ?? 0),
            Gender = (Gender)dataProto.Gender,
            Age = UserProtoMapping.ParseTbAge(dataProto.TbAge),
            PostNum = dataProto.PostNum,
            FanNum = dataProto.FansNum,
            FollowNum = dataProto.ConcernNum,
            ForumNum = dataProto.MyLikeNum,
            Sign = dataProto.Intro ?? string.Empty,
            Ip = dataProto.IpAddress ?? string.Empty,
            Icons = UserProtoMapping.MapIconNames(dataProto),
            IsVip = UserProtoMapping.MapIsVip(dataProto),
            IsGod = UserProtoMapping.MapIsGod(dataProto),
            PrivLike = UserProtoMapping.MapPrivLike(dataProto.PrivSets),
            PrivReply = UserProtoMapping.MapPrivReply(dataProto.PrivSets)
        };
    }
}
