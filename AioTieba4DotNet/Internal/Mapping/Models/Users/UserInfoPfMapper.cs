using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoPfMapper
{
    internal static UserInfoPf FromTbData(ProfileResIdl.Types.DataRes dataProto)

    {
        var userProto = dataProto.User;

        if (userProto == null) throw new InvalidOperationException("Profile response data.User is null.");


        var antiStatProto = dataProto.AntiStat;

        return new UserInfoPf
        {
            UserId = userProto.Id,
            Portrait = UserProtoMapping.NormalizePortrait(userProto.Portrait),
            UserName = userProto.Name ?? string.Empty,
            NickNameNew = userProto.NameShow ?? string.Empty,
            TiebaUid = UserProtoMapping.ParseTiebaUid(userProto.TiebaUid),
            GLevel = userProto.UserGrowth != null ? (int)userProto.UserGrowth.LevelId : 0,
            Gender = (Gender)userProto.Gender,
            Age = UserProtoMapping.ParseTbAge(userProto.TbAge),
            PostNum = userProto.PostNum,
            AgreeNum = dataProto.UserAgreeInfo != null ? (int)dataProto.UserAgreeInfo.TotalAgreeNum : 0,
            FanNum = userProto.FansNum,
            FollowNum = userProto.ConcernNum,
            ForumNum = userProto.MyLikeNum,
            Sign = userProto.Intro ?? string.Empty,
            Ip = userProto.IpAddress ?? string.Empty,
            Icons = UserProtoMapping.MapIconNames(userProto),
            VImage = VirtualImagePfMapper.FromTbData(userProto.VirtualImageInfo),
            IsVip = UserProtoMapping.MapIsVip(userProto),
            IsGod = UserProtoMapping.MapIsGod(userProto),
            IsBlocked =
                antiStatProto != null && antiStatProto.BlockStat != 0 && antiStatProto.HideStat != 0 &&
                antiStatProto.DaysTofree > 30,
            PrivLike = UserProtoMapping.MapPrivLike(userProto.PrivSets),
            PrivReply = UserProtoMapping.MapPrivReply(userProto.PrivSets)
        };
    }
}
