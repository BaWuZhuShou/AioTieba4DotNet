using AioTieba4DotNet.Models.Threads;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class RecoverUserMapper
{
    internal static RecoverUser FromTbData(JObject? data, string nicknameFieldName)
    {
        if (data is null)
            return new RecoverUser();

        return new RecoverUser
        {
            Portrait = NormalizePortrait(data.GetValue("portrait")?.Value<string>() ?? string.Empty),
            UserName = data.GetValue("user_name")?.Value<string>() ?? string.Empty,
            NickNameNew = data.GetValue(nicknameFieldName)?.Value<string>() ?? string.Empty
        };
    }

    private static string NormalizePortrait(string portrait)
    {
        var queryIndex = portrait.IndexOf('?', StringComparison.Ordinal);
        return queryIndex >= 0 ? portrait[..queryIndex] : portrait;
    }
}
