using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.Unblock;

[RequireBduss]
[PythonApi("aiotieba.api.unblock")]
internal sealed class Unblock(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResponse(string body)
    {
        _ = ParseBody(body, "no", "error");
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, long userId, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("fn", "-"),
            new("fid", fid.ToString()),
            new("block_un", "-"),
            new("block_uid", userId.ToString()),
            new("tbs", httpCore.Account!.Tbs!)
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/bawublockclear").Uri;
        var result = await httpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
