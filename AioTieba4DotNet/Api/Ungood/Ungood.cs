using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.Ungood;

[RequireBduss]
[PythonApi("aiotieba.api.ungood")]
internal class Ungood(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(string fname, ulong fid, long tid,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("fid", fid.ToString()),
            new("tbs", HttpCore.Account!.Tbs!),
            new("word", fname),
            new("z", tid.ToString())
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/commitgood").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
