using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.FollowUser;

public class FollowUser(ITiebaHttpCore httpCore)
{
    private static void ParseBody(string body)
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue("error_code")?.ToObject<int>() ?? 0;
        if (code != 0)
        {
            throw new TieBaServerException(code, resJson.GetValue("error_msg")?.ToObject<string>() ?? string.Empty);
        }
    }

    public async Task<bool> RequestAsync(string portrait)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", httpCore.Account?.Bduss ?? ""),
            new("portrait", portrait),
            new("tbs", httpCore.Account?.Tbs ?? "")
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/user/follow").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        ParseBody(result);
        return true;
    }
}
