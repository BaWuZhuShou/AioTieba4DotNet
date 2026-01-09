using AioTieba4DotNet.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Login.Entities;

public class UserInfoLogin : UserInfo
{
    public static UserInfoLogin FromTbData(JObject data)
    {
        var portrait = data.GetValue("portrait")?.Value<string>() ?? "";
        if (portrait.Contains('?'))
        {
            portrait = portrait[..^13];
        }

        return new UserInfoLogin
        {
            UserId = data.GetValue("id")?.Value<long>() ?? 0,
            Portrait = portrait,
            UserName = data.GetValue("name")?.Value<string>() ?? "",
        };
    }
}
