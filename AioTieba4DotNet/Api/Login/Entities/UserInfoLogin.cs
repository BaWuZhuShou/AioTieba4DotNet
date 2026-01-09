using AioTieba4DotNet.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Login.Entities;

public class UserInfoLogin : UserInfo
{
    public static UserInfoLogin FromTbData(JObject data)
    {
        return new UserInfoLogin
        {
            UserId = data.GetValue("id")!.Value<long>(),
            Portrait = data.GetValue("portrait")!.Value<string>()!,
            UserName = data.GetValue("name")!.Value<string>()!,
        };
    }
}
