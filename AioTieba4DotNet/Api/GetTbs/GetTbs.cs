using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetTbs;

[RequireBduss]
public class GetTbs(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private new static string ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);

        return resJson.GetValue("tbs")?.ToString() ?? "";
    }

    public async Task<string> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? ""),
            new("_client_version", Const.MainVersion)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/tbs").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        var tbs = ParseBody(result);

        if (HttpCore.Account != null)
        {
            HttpCore.Account.Tbs = tbs;
        }

        return tbs;
    }
}
