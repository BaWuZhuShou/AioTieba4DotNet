using AioTieba4DotNet.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Login.Entities;

public class UserInfoLogin : BaseUser
{
    /// <summary>
    /// user_id
    /// </summary>
    public long UserId { get; init; }

    public static UserInfoLogin FromTbData(JObject data)
    {
        return new UserInfoLogin()
        {
            UserId = data.GetValue("id")!.Value<int>(),
            Portrait = data.GetValue("portrait")!.Value<string>()!,
            UserName = data.GetValue("name")!.Value<string>()!,
        };
    }

    public override string ToString()
    {
        return $"{nameof(UserId)}: {UserId}, {nameof(Portrait)}: {Portrait}, {nameof(UserName)}: {UserName}";
    }

    protected bool Equals(UserInfoLogin other)
    {
        return UserId == other.UserId;
    }
}