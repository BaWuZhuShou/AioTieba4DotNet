using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.AddBaWu;

internal sealed class AddBaWu(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResponse(string body)
    {
        _ = ParseBody(body, "no", "error");
        return true;
    }

    public async Task<bool> RequestAsync(long fid, string userName, string bawuType,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("fn", "-"),
            new("fid", fid.ToString()),
            new("team_un", userName),
            new("type", bawuType),
            new("tbs", HttpCore.Account!.Tbs!)
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/bawuteamadd").Uri;
        var result = await HttpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
