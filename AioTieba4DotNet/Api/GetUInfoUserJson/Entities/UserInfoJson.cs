using AioTieba4DotNet.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoUserJson.Entities;

public class UserInfoJson : UserInfo
{
    public static UserInfoJson FromTbData(JObject data)
    {
        var portrait = data["portrait"]?.ToString() ?? "";
        if (portrait.Contains('?'))
        {
            portrait = portrait[..^13];
        }

        return new UserInfoJson
        {
            UserId = data["id"]?.ToObject<long>() ?? 0,
            Portrait = portrait,
            UserName = data["name"]?.ToString() ?? "",
            NickNameNew = data["name_show"]?.ToString() ?? "",
        };
    }
}
