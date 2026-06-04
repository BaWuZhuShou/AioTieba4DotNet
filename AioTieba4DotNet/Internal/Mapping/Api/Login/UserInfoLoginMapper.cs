using AioTieba4DotNet.Api.Login.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoLoginMapper
{
    internal static UserInfoLogin FromTbData(JObject data)
    {
        return new UserInfoLogin
        {
            UserId = data.GetValue("id")?.Value<long>() ?? 0,
            Portrait = NormalizePortrait(data.GetValue("portrait")?.Value<string>() ?? string.Empty),
            UserName = data.GetValue("name")?.Value<string>() ?? string.Empty
        };
    }

    private static string NormalizePortrait(string portrait)
    {
        return UserProtoMapping.NormalizePortrait(portrait);
    }
}
