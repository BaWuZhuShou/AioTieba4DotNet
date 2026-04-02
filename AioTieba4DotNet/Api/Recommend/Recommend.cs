using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.Recommend;

[RequireBduss]
[PythonApi("aiotieba.api.recommend")]
internal class Recommend(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        var result = ParseBody(body);
        if (result["data"]?["is_push_success"]?.ToObject<int?>() != 1)
            throw new TieBaServerException(1, result["data"]?["msg"]?.ToObject<string>() ?? "Recommend failed.");

        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, long tid, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss), new("forum_id", fid.ToString()), new("thread_id", tid.ToString())
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80,
            "/c/c/bawu/pushRecomToPersonalized").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
