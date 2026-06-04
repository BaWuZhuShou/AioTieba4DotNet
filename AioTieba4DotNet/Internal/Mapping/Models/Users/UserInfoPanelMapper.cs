using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoPanelMapper
{
    internal static UserInfo FromTbData(JObject data)
    {
        var portrait = UserProtoMapping.NormalizePortrait(data.GetValue("portrait")?.ToObject<string>());

        var vipInfoToken = data.GetValue("vipInfo");
        JObject? vipInfoObject = null;
        if (vipInfoToken is { Type: JTokenType.Object }) vipInfoObject = vipInfoToken.ToObject<JObject>();

        var vStatus = vipInfoObject?.GetValue("v_status")?.ToObject<int>() ?? 0;
        var tbAge = data.GetValue("tb_age")?.ToObject<string>();
        var age = tbAge == "-" ? 0 : data.GetValue("tb_age")?.ToObject<float>() ?? 0;

        return new UserInfo
        {
            Portrait = portrait,
            UserName = data["name"]?.ToObject<string>() ?? string.Empty,
            NickNameNew = data["show_nickname"]?.ToObject<string>() ?? string.Empty,
            NickNameOld = data["name_show"]?.ToObject<string>() ?? string.Empty,
            Gender = (data["gender"]?.ToObject<string>() ?? string.Empty) == "male" ? Gender.Male : Gender.Female,
            Age = age,
            IsVip = vStatus == 3,
            PostNum = Utils.TbNumToInt(data["post_num"]?.ToObject<string>() ?? "0"),
            FanNum = Utils.TbNumToInt(data["followed_count"]?.ToObject<string>() ?? "0")
        };
    }
}
