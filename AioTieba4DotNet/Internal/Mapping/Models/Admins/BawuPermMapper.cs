using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Models.Admins;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BawuPermMapper
{
    [SuppressMessage("Critical Code Smell", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification =
            "The permission mapper mirrors the nested admin permission payload directly so each category and permission bit stays easy to compare with the upstream response shape.")]
    internal static BawuPerm FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var permissions = BawuPermType.None;
        var permissionSetting = data.GetValue("perm_setting") as JObject;
        if (permissionSetting is not null)
            foreach (var entries in new[] { "category_user", "category_thread" }
                         .Select(permissionSetting.GetValue)
                         .OfType<JArray>())
            {
                foreach (var entry in entries.OfType<JObject>().Where(static entry => IsEnabled(entry.GetValue("switch"))))
                {
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

        return new BawuPerm { Permissions = permissions };
    }

    private static bool IsEnabled(JToken? token)
    {
        return token?.Type switch
        {
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.Integer => token.Value<int>() != 0,
            JTokenType.String => !string.IsNullOrWhiteSpace(token.Value<string>()) && token.Value<string>() != "0",
            _ => false
        };
    }
}
