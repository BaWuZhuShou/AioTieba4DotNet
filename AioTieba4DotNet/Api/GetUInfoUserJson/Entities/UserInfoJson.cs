using AioTieba4DotNet.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoUserJson.Entities;

public class UserInfoJson : UserInfo
{
    public static UserInfoJson FromTbData(JObject data)
    {
        return new UserInfoJson
        {
            UserName = "",
            UserId = data["id"]!.ToObject<long>(),
            Portrait = data["portrait"]!.ToString(),
        };
    }
}
