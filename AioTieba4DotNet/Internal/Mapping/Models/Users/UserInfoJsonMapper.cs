using AioTieba4DotNet.Models.Shared;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoJsonMapper
{
    internal static UserInfo FromTbData(JObject data)
    {
        var portrait = UserProtoMapping.NormalizePortrait(data["portrait"]?.ToString());

        return new UserInfo
        {
            UserId = data["id"]?.ToObject<long>() ?? 0,
            Portrait = portrait,
            UserName = data["name"]?.ToString() ?? string.Empty,
            NickNameNew = data["name_show"]?.ToString() ?? string.Empty
        };
    }
}
