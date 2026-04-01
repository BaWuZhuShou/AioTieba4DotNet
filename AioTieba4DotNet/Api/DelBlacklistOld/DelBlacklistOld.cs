using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.DelBlacklistOld;

[RequireBduss]
[PythonApi("aiotieba.api.del_blacklist_old")]
internal sealed class DelBlacklistOld(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(long userId, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("mute_user", userId.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/user/userMuteDel").Uri;
        var response = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        ParseResponse(response);
        return true;
    }

    private static void ParseResponse(string body)
    {
        var responseJson = JObject.Parse(body);
        ApiResponseValidator.CheckError(responseJson.GetValue("error_code")?.Value<int>() ?? 0,
            responseJson.GetValue("error_msg")?.Value<string>());
        ApiResponseValidator.CheckError(responseJson.GetValue("errorno")?.Value<int>() ?? 0,
            responseJson.GetValue("errmsg")?.Value<string>());
    }
}
