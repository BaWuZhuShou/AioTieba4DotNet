using AioTieba4DotNet.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Api.GetThreads.Entities;

/// <summary>
///     用户信息
/// </summary>
public class UserInfoT : UserInfo
{
    /// <summary>
    ///     等级
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    ///     是否吧务
    /// </summary>
    public bool IsBawu { get; init; }

    public static UserInfoT? FromTbData(User? dataProto)
    {
        if (dataProto == null) return null;

        var portrait = dataProto.Portrait ?? "";
        if (portrait.Contains('?')) portrait = portrait[..^13];

        return new UserInfoT
        {
            UserId = dataProto.Id,
            Portrait = portrait,
            UserName = dataProto.Name,
            NickNameNew = dataProto.NameShow,
            Level = dataProto.LevelId,
            GLevel = (int)(dataProto.UserGrowth?.LevelId ?? 0),
            Gender = (Gender)dataProto.Gender,
            Icons = dataProto.Iconinfo?.Where(i => !string.IsNullOrEmpty(i.Name)).Select(i => i.Name).ToList() ?? [],
            IsBawu = dataProto.IsBawu == 1,
            IsVip = dataProto.NewTshowIcon.Count != 0,
            IsGod = dataProto.NewGodData is { Status: 1 },
            PrivLike = dataProto.PrivSets != null && dataProto.PrivSets.Like != 0
                ? (PrivLike)dataProto.PrivSets.Like
                : PrivLike.Public,
            PrivReply = dataProto.PrivSets != null && dataProto.PrivSets.Reply != 0
                ? (PrivReply)dataProto.PrivSets.Reply
                : PrivReply.All,
            Ip = dataProto.IpAddress
        };
    }
}
