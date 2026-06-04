using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoMapper
{
    internal static UserInfo? FromTbData(PostInfoList? dataRes)
    {
        if (dataRes == null) return null;

        return new UserInfo
        {
            UserId = dataRes.UserId,
            Portrait = NormalizePortrait(dataRes.UserPortrait),
            UserName = dataRes.UserName,
            NickNameNew = dataRes.NameShow
        };
    }

    internal static UserInfo FromTbData(JObject data)
    {
        return new UserInfo
        {
            UserId = data.GetValue("id")?.Value<long>() ?? 0,
            Portrait = NormalizePortrait(data.GetValue("portrait")?.Value<string>() ?? string.Empty),
            UserName = data.GetValue("name")?.Value<string>() ?? string.Empty,
            NickNameNew = data.GetValue("name_show")?.Value<string>() ?? string.Empty
        };
    }

    internal static UserInfo? FromTbData(User? data)
    {
        if (data == null) return null;

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

    internal static UserInfo FromTbData(
        GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.MsgInfo.Types.UserInfo? data)
    {
        if (data == null) return new UserInfo();

        return new UserInfo
        {
            UserId = data.UserId,
            Portrait = NormalizePortrait(data.Portrait ?? string.Empty),
            UserName = data.UserName
        };
    }

    private static string NormalizePortrait(string portrait)
    {
        return UserProtoMapping.NormalizePortrait(portrait);
    }
}
