using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;

public class UserInfoPf : UserInfoGuInfoApp
{
    public required VirtualImagePf VImage { get; init; }

    public static UserInfoPf FromTbData(ProfileResIdl.Types.DataRes dataProto)
    {
        var userProto = dataProto.User;
        var antiStatProto = dataProto.AntiStat;
        var portrait = userProto.Portrait;
        if (portrait.Contains('?'))
        {
            portrait = portrait[..^13];
        }

        return new UserInfoPf
        {
            UserId = userProto.Id,
            Portrait = portrait,
            UserName = userProto.Name,
            NickNameNew = userProto.NameShow,
            TiebaUid = string.IsNullOrEmpty(userProto.TiebaUid) ? 0 : long.Parse(userProto.TiebaUid),
            GLevel = (int)userProto.UserGrowth.LevelId,
            Gender = (Gender)userProto.Gender,
            Age = string.IsNullOrEmpty(userProto.TbAge) ? 0 : float.Parse(userProto.TbAge),
            PostNum = userProto.PostNum,
            AgreeNum = (int)dataProto.UserAgreeInfo.TotalAgreeNum,
            FanNum = userProto.FansNum,
            FollowNum = userProto.ConcernNum,
            ForumNum = userProto.MyLikeNum,
            Sign = userProto.Intro,
            Ip = userProto.IpAddress,
            Icons = userProto.Iconinfo.Where(i => !string.IsNullOrEmpty(i.Name)).Select(i => i.Name).ToList(),
            VImage = VirtualImagePf.FromTbData(userProto.VirtualImageInfo),
            IsVip = userProto.NewTshowIcon.Count != 0,
            IsGod = userProto.NewGodData.Status == 1,
            IsBlocked = antiStatProto.BlockStat != 0 && antiStatProto.HideStat != 0 && antiStatProto.DaysTofree > 30,
            PrivLike = userProto.PrivSets.Like != 0 ? (PrivLike)userProto.PrivSets.Like : PrivLike.Public,
            PrivReply = userProto.PrivSets.Reply != 0 ? (PrivReply)userProto.PrivSets.Reply : PrivReply.All,
        };
    }
}
