using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;

/// <summary>
///     用户信息 (个人主页接口)
/// </summary>
public class UserInfoPf : UserInfoGuInfoApp
{
    /// <summary>
    ///     虚拟形象
    /// </summary>
    public required VirtualImagePf VImage { get; init; }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 响应数据</param>
    /// <returns>用户信息实体</returns>
    public static UserInfoPf FromTbData(ProfileResIdl.Types.DataRes dataProto)
    {
        var userProto = dataProto.User;
        if (userProto == null) throw new InvalidOperationException("Profile response data.User is null.");

        var antiStatProto = dataProto.AntiStat;
        var portrait = userProto.Portrait ?? string.Empty;
        if (portrait.Contains('?')) portrait = portrait[..^13];

        return new UserInfoPf
        {
            UserId = userProto.Id,
            Portrait = portrait,
            UserName = userProto.Name ?? string.Empty,
            NickNameNew = userProto.NameShow ?? string.Empty,
            TiebaUid = string.IsNullOrEmpty(userProto.TiebaUid) ? 0 : long.Parse(userProto.TiebaUid),
            GLevel = userProto.UserGrowth != null ? (int)userProto.UserGrowth.LevelId : 0,
            Gender = (Gender)userProto.Gender,
            Age = string.IsNullOrEmpty(userProto.TbAge) ? 0 : float.Parse(userProto.TbAge),
            PostNum = userProto.PostNum,
            AgreeNum = dataProto.UserAgreeInfo != null ? (int)dataProto.UserAgreeInfo.TotalAgreeNum : 0,
            FanNum = userProto.FansNum,
            FollowNum = userProto.ConcernNum,
            ForumNum = userProto.MyLikeNum,
            Sign = userProto.Intro ?? string.Empty,
            Ip = userProto.IpAddress ?? string.Empty,
            Icons =
                userProto.Iconinfo != null
                    ? userProto.Iconinfo.Where(i => !string.IsNullOrEmpty(i.Name)).Select(i => i.Name).ToList()
                    : [],
            VImage = VirtualImagePf.FromTbData(userProto.VirtualImageInfo),
            IsVip = userProto.NewTshowIcon != null && userProto.NewTshowIcon.Count != 0,
            IsGod = userProto.NewGodData != null && userProto.NewGodData.Status == 1,
            IsBlocked =
                antiStatProto != null && antiStatProto.BlockStat != 0 && antiStatProto.HideStat != 0 &&
                antiStatProto.DaysTofree > 30,
            PrivLike =
                userProto.PrivSets != null && userProto.PrivSets.Like != 0
                    ? (PrivLike)userProto.PrivSets.Like
                    : PrivLike.Public,
            PrivReply = userProto.PrivSets != null && userProto.PrivSets.Reply != 0
                ? (PrivReply)userProto.PrivSets.Reply
                : PrivReply.All
        };
    }
}
