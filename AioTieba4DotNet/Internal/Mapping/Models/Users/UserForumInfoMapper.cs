using AioTieba4DotNet.Models.Users;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserForumInfoMapper
{
    internal static UserForumInfo FromTbData(JObject data)
    {
        var userForum = data.GetValue("user_forum_info") as JObject;
        var forum = data.GetValue("forum_info") as JObject;

        return new UserForumInfo
        {
            User = UserInfoUfMapper.FromTbData(data.GetValue("user_info") as JObject),
            IsFollow = (userForum?.GetValue("is_follow")?.Value<int>() ?? 0) != 0,
            FollowDays = userForum?.GetValue("follow_days")?.Value<int>() ?? 0,
            SignDays = userForum?.GetValue("sign_days")?.Value<int>() ?? 0,
            ThreadNum = userForum?.GetValue("thread_num")?.Value<int>() ?? 0,
            DayPostNum = userForum?.GetValue("day_post_num")?.Value<int>() ?? 0,
            MemberRank = userForum?.GetValue("member_no")?.Value<int>() ?? 0,
            DaySignRank = userForum?.GetValue("day_sign_no")?.Value<int>() ?? 0,
            Level = userForum?.GetValue("level_id")?.Value<int>() ?? 0,
            LevelName = userForum?.GetValue("level_name")?.Value<string>() ?? string.Empty,
            Exp = userForum?.GetValue("cur_score")?.Value<int>() ?? 0,
            LevelupExp = userForum?.GetValue("levelup_score")?.Value<int>() ?? 0,
            RoleName = userForum?.GetValue("role_name")?.Value<string>() ?? string.Empty,
            Identify = userForum?.GetValue("identify")?.Value<string>() ?? string.Empty,
            HighLightSignDays = userForum?.GetValue("high_light_sign_days")?.Value<int>() ?? 0,
            Fname = forum?.GetValue("forum_name")?.Value<string>() ?? string.Empty,
            SmallAvatar = forum?.GetValue("forum_avatar")?.Value<string>() ?? string.Empty
        };
    }
}
