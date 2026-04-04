using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.DelBawuBlacklist;

internal sealed class DelBawuBlacklist(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResponse(string body)
    {
        _ = ParseBody(body, "errno", "errmsg");
        return true;
    }

    public async Task<bool> RequestAsync(string fname, long userId, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("word", fname),
            new("tbs", HttpCore.Account!.Tbs!),
            new("list[]", userId.ToString()),
            new("ie", "utf-8")
        };

        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/bawu2/platform/cancelBlack").Uri;
        var result = await HttpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
