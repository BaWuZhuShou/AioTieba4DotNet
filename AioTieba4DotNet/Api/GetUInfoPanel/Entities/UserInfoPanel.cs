using AioTieba4DotNet.Core;
using AioTieba4DotNet.Entities;
using AioTieba4DotNet.Enums;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoPanel.Entities;

/// <summary>
///     用户信息 (面板接口)
/// </summary>
public class UserInfoPanel : UserInfo
{
    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="data">JSON 数据对象</param>
    /// <returns>用户信息实体</returns>
    public new static UserInfoPanel FromTbData(JObject data)
    {
        var portrait = data.GetValue("portrait")?.ToObject<string>() ?? "";
        if (portrait.Contains('?')) portrait = portrait[..^13];

        var isVip = false;
        var vipInfoToken = data.GetValue("vipInfo");
        JObject? vipInfoObject = null;
        if (vipInfoToken is { Type: JTokenType.Object }) vipInfoObject = vipInfoToken.ToObject<JObject>();

        var vStatus = vipInfoObject?.GetValue("v_status")?.ToObject<int>() ?? 0;
        float age = 0;
        if (data.GetValue("tb_age")?.ToObject<string>() != "-") age = data.GetValue("tb_age")!.ToObject<float>();

        if (vStatus == 3) isVip = true;

        return new UserInfoPanel
        {
            Portrait = portrait,
            UserName = data["name"]?.ToObject<string>() ?? "",
            NickNameNew = data["show_nickname"]?.ToObject<string>() ?? "",
            NickNameOld = data["name_show"]?.ToObject<string>() ?? "",
            Gender = (data["gender"]?.ToObject<string>() ?? "") == "male" ? Gender.Male : Gender.Female,
            Age = age,
            IsVip = isVip,
            PostNum = Utils.TbNumToInt(data["post_num"]?.ToObject<string>() ?? "0"),
            FanNum = Utils.TbNumToInt(data["followed_count"]?.ToObject<string>() ?? "0")
        };
    }
}
