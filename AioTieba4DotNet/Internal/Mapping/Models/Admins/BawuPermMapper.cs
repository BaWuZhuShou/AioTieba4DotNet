using AioTieba4DotNet.Models.Admins;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BawuPermMapper
{
    internal static BawuPerm FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var permissions = BawuPermType.None;
        var permissionSetting = data.GetValue("perm_setting") as JObject;
        if (permissionSetting is not null)
        {
            foreach (var category in new[] { "category_user", "category_thread" })
            {
                if (permissionSetting.GetValue(category) is not JArray entries)
                    continue;

                foreach (var entry in entries.OfType<JObject>())
                {
                    if (!IsEnabled(entry.GetValue("switch")))
                        continue;

                    permissions |= entry.GetValue("perm")?.Value<int>() switch
                    {
                        2 => BawuPermType.RecoverAppeal,
                        3 => BawuPermType.Recover,
                        4 => BawuPermType.Unblock,
                        5 => BawuPermType.UnblockAppeal,
                        _ => BawuPermType.None
                    };
                }
            }
        }

        return new BawuPerm { Permissions = permissions };
    }

    private static bool IsEnabled(JToken? token) => token?.Type switch
    {
        JTokenType.Boolean => token.Value<bool>(),
        JTokenType.Integer => token.Value<int>() != 0,
        JTokenType.String => !string.IsNullOrWhiteSpace(token.Value<string>()) && token.Value<string>() != "0",
        _ => false
    };
}
