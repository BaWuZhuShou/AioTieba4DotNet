using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.UnfollowUser;

[RequireBduss]
public class UnfollowUser(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(string portrait)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? ""),
            new("portrait", portrait),
            new("tbs", HttpCore.Account?.Tbs ?? "")
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/user/unfollow").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        ParseBody(result);
        return true;
    }
}
