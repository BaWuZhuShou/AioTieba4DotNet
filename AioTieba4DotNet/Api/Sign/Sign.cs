using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Sign;

public class Sign(ITiebaHttpCore httpCore)
{
    private static bool ParseBody(string body)
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue("error_code")?.ToObject<int>();
        if (code != null && code != 0)
        {
            throw new TieBaServerException(code ?? -1, resJson.GetValue("error_msg")?.ToObject<string>() ?? string.Empty);
        }

        return true;
    }

    public async Task<bool> RequestAsync(string fname, ulong fid)
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", httpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("fid", fid.ToString()),
            new("kw", fname),
            new("tbs", httpCore.Account!.Tbs!),
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/forum/sign").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}
