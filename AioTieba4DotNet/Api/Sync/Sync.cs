using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Sync;

public class Sync(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static (string ClientId, string SampleId) ParseBody(string body)
    {
        var o = JsonApiBase.ParseBody(body);

        var clientId = o.GetValue("client")!.ToObject<JObject>()!.GetValue("client_id")!.ToObject<string>()!;
        var sampleId = o.GetValue("wl_config")!.ToObject<JObject>()!.GetValue("sample_id")!.ToObject<string>()!;

        return (clientId, sampleId);
    }

    public async Task<(string ClientId, string SampleId)> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("cuid", HttpCore.Account.CuidGalaxy2)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/sync").Uri;
        var responseMessage = await HttpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}

