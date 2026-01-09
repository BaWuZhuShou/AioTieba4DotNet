using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetTbs;

public class GetTbs(ITiebaHttpCore httpCore)
{
    private static string ParseBody(string body)
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue("error_code")?.ToObject<int>();
        if (code != null && code != 0)
        {
            throw new TieBaServerException(code ?? -1, resJson.GetValue("error_msg")?.ToObject<string>() ?? string.Empty);
        }

        return resJson.GetValue("tbs")?.ToString() ?? "";
    }

    public async Task<string> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", httpCore.Account?.Bduss ?? ""),
            new("_client_version", Const.MainVersion)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/tbs").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        var tbs = ParseBody(result);

        if (httpCore.Account != null)
        {
            httpCore.Account.Tbs = tbs;
        }

        return tbs;
    }
}
