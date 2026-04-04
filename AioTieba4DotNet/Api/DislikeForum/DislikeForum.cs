using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.DislikeForum;

internal sealed class DislikeForum(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("_client_version", Const.MainVersion),
            new("dislike", CreateDislikePayload(fid)),
            new("dislike_from", "homepage")
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/excellent/submitDislike").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        ParseBody(result);
        return true;
    }

    private static string CreateDislikePayload(ulong fid)
    {
        var payload = new JArray
        {
            new JObject
            {
                ["tid"] = 1,
                ["dislike_ids"] = 7,
                ["fid"] = fid,
                ["click_time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        return payload.ToString(Formatting.None);
    }
}
