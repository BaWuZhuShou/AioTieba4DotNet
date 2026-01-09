using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.UnlikeForum;

public class UnlikeForum(ITiebaHttpCore httpCore)
{
    private static bool ParseBody(string body)
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue("error_code")?.ToObject<int>();
        if (code != null && code != 0)
        {
            throw new TieBaServerException(code.Value, resJson.GetValue("error_msg")?.ToObject<string>() ?? string.Empty);
        }

        var error = resJson["error"];
        if (error != null)
        {
            var errno = error.Value<int>("errno");
            if (errno != 0)
            {
                throw new TieBaServerException(errno, error.Value<string>("errmsg") ?? string.Empty);
            }
        }

        return true;
    }

    public async Task<bool> RequestAsync(ulong fid)
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", httpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("fid", fid.ToString()),
            new("tbs", httpCore.Account!.Tbs!),
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/forum/unlike").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}
