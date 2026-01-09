using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.FollowUser;

public class FollowUser(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(string portrait)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? ""),
            new("portrait", portrait),
            new("tbs", HttpCore.Account?.Tbs ?? "")
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/user/follow").Uri;
        var responseMessage = await HttpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        ParseBody(result);
        return true;
    }
}
