using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.SetBawuPerm;

[RequireBduss]
[PythonApi("aiotieba.api.set_bawu_perm")]
internal sealed class SetBawuPerm(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static readonly (BawuPermType Permission, int Id)[] PermissionOrder =
    [
        (BawuPermType.Unblock, 4),
        (BawuPermType.UnblockAppeal, 5),
        (BawuPermType.Recover, 3),
        (BawuPermType.RecoverAppeal, 2)
    ];

    private static bool ParseResponse(string body)
    {
        _ = ParseBody(body, "no", "error");
        return true;
    }

    private static string PackPermissionSettings(BawuPermType permissions)
    {
        var permissionSettings = new JArray(
            PermissionOrder.Select(entry => new JObject
            {
                ["switch"] = (permissions & entry.Permission) != 0 ? 1 : 0,
                ["perm"] = entry.Id
            }));

        return permissionSettings.ToString(Formatting.None);
    }

    public async Task<bool> RequestAsync(ulong fid, string portrait, BawuPermType permissions,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("forum_id", fid.ToString()),
            new("auth_user_portrait", portrait),
            new("perm_setting", PackPermissionSettings(permissions))
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/setAuthToolPerm").Uri;
        var result = await httpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
