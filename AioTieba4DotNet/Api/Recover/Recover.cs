using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Api.Recover;

[RequireBduss]
[PythonApi("aiotieba.api.recover")]
internal class Recover(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        ParseBody(body, "no", "error");
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, long tid, long pid, bool isHide,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("tbs", HttpCore.Account!.Tbs!),
            new("fn", "-"),
            new("fid", fid.ToString()),
            new("tid_list[]", tid.ToString()),
            new("pid_list[]", pid.ToString()),
            new("type_list[]", pid > 0 ? "1" : "0"),
            new("is_frs_mask_list[]", isHide ? "1" : "0")
        };

        var requestUri = new UriBuilder(Const.AppSecureScheme, Const.WebBaseHost, 443, "/mo/q/bawurecoverthread").Uri;
        var result = await HttpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
