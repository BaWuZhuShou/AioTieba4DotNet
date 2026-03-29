using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Models.Shared;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoSelfMoIndexMapper
{
    internal static UserInfo FromTbData(JObject data)
    {
        var vipStatus = data.SelectToken("vipInfo.v_status")?.Value<int>() ?? 0;

        return new UserInfo
        {
            UserId = data.GetValue("id")?.Value<long>() ?? 0,
            Portrait = data.GetValue("portrait")?.Value<string>() ?? string.Empty,
            UserName = data.GetValue("name")?.Value<string>() ?? string.Empty,
            Gender = (Gender)(data.GetValue("user_sex")?.Value<int>() ?? 0),
            PostNum = data.GetValue("post_num")?.Value<int>() ?? 0,
            FanNum = data.GetValue("fans_num")?.Value<int>() ?? 0,
            FollowNum = data.GetValue("concern_num")?.Value<int>() ?? 0,
            ForumNum = data.GetValue("like_forum_num")?.Value<int>() ?? 0,
            Sign = data.GetValue("intro")?.Value<string>() ?? string.Empty,
            IsVip = vipStatus == 3
        };
    }
}
