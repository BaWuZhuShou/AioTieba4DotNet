using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.Good;

[RequireBduss]
[PythonApi("aiotieba.api.good")]
internal class Good(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(string fname, ulong fid, long tid, int cid,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("cid", cid.ToString()),
            new("fid", fid.ToString()),
            new("ntn", "set"),
            new("tbs", HttpCore.Account!.Tbs!),
            new("word", fname),
            new("z", tid.ToString())
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/commitgood").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
