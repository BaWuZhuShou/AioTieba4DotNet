using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoTMapper
{
    internal static UserInfoT? FromTbData(User? dataProto)

    {
        if (dataProto == null) return null;


        return new UserInfoT
        {
            UserId = dataProto.Id,
            Portrait = UserProtoMapping.NormalizePortrait(dataProto.Portrait),
            UserName = dataProto.Name ?? string.Empty,
            NickNameNew = dataProto.NameShow ?? string.Empty,
            TiebaUid = UserProtoMapping.ParseTiebaUid(dataProto.TiebaUid),
            Level = dataProto.LevelId,
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
            IsBawu = dataProto.IsBawu == 1,
            IsVip = UserProtoMapping.MapIsVip(dataProto),
            IsGod = UserProtoMapping.MapIsGod(dataProto),
            PrivLike = UserProtoMapping.MapPrivLike(dataProto.PrivSets),
            PrivReply = UserProtoMapping.MapPrivReply(dataProto.PrivSets)
        };
    }
}
