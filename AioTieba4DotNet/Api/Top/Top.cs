using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.Top;

[RequireBduss]
[PythonApi("aiotieba.api.top")]
internal class Top(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(string fname, ulong fid, long tid, bool isVip, bool isSet,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("fid", fid.ToString()),
            new("is_member_top", isVip ? "1" : "0"),
            new("ntn", isSet ? "set" : string.Empty),
            new("tbs", HttpCore.Account!.Tbs!),
            new("word", fname),
            new("z", tid.ToString())
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/committop").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
