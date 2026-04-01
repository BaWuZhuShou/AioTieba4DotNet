using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.AddBawuBlacklist;

[RequireBduss]
[PythonApi("aiotieba.api.add_bawu_blacklist")]
internal sealed class AddBawuBlacklist(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
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
            new("tbs", HttpCore.Account!.Tbs!),
            new("user_id", userId.ToString()),
            new("word", fname),
            new("ie", "utf-8")
        };

        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/bawu2/platform/addBlack").Uri;
        var result = await HttpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
