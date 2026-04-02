using AioTieba4DotNet.Models.Shared;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoGuInfoWebMapper
{
    internal static UserInfo FromTbData(JObject data)
    {
        var userIdToken = data.GetValue("uid");
        var userId = userIdToken?.Value<long>() ?? 0;
        var userName = data.GetValue("uname")?.Value<string>() ?? string.Empty;
        if (userName == userId.ToString())
            userName = string.Empty;

        var portrait = data.GetValue("portrait")?.Value<string>() ?? string.Empty;
        if (portrait.Contains('?', StringComparison.Ordinal))
            portrait = portrait[..portrait.IndexOf('?', StringComparison.Ordinal)];

        return new UserInfo
        {
            UserId = userId,
            Portrait = portrait,
            UserName = userName,
            NickNameNew = data.GetValue("show_nickname")?.Value<string>() ?? string.Empty
        };
    }
}
