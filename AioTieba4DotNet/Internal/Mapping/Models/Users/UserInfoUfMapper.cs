using AioTieba4DotNet.Models.Users;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoUfMapper
{
    internal static UserInfoUf FromTbData(JObject? data)
    {
        if (data is null)
            return new UserInfoUf();

        var portrait = data.GetValue("portrait")?.Value<string>() ?? string.Empty;
        if (portrait.Contains('?', StringComparison.Ordinal))
            portrait = portrait[..portrait.IndexOf('?', StringComparison.Ordinal)];

        return new UserInfoUf
        {
            UserId = data.GetValue("id")?.Value<long>() ?? 0,
            Portrait = portrait,
            NickNameNew = data.GetValue("name")?.Value<string>() ?? string.Empty,
            IsLike = (data.GetValue("is_like")?.Value<int>() ?? 0) != 0
        };
    }
}
