using AioTieba4DotNet.Models.Users;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoJsonMapper
{
    internal static UserInfoJson FromTbData(JObject data)
    {
        var portrait = data["portrait"]?.ToString() ?? string.Empty;
        if (portrait.Contains('?')) portrait = portrait[..^13];

        return new UserInfoJson
        {
            UserId = data["id"]?.ToObject<long>() ?? 0,
            Portrait = portrait,
            UserName = data["name"]?.ToString() ?? string.Empty,
            NickNameNew = data["name_show"]?.ToString() ?? string.Empty
        };
    }
}
