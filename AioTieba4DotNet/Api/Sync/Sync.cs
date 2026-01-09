using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Sync;

public class Sync(ITiebaHttpCore httpCore) : BaseApiRequest<ValueTuple<string, string>>
{
    public override ValueTuple<string,string> ParseBody(string body)
    {
        var o = JObject.Parse(body);
        var code = o.GetValue("error_code")?.ToObject<int>();
        if (code != null && code != 0)
        {
            throw new TieBaServerException(code ?? -1, o.GetValue("error_msg")?.ToObject<string>() ?? string.Empty);
        }

        var clientId = o.GetValue("client")!.ToObject<JObject>()!.GetValue("client_id")!.ToObject<string>()!;
        var sampleId = o.GetValue("wl_config")!.ToObject<JObject>()!.GetValue("sample_id")!.ToObject<string>()!;

        return (clientId, sampleId);
    }

    public override async Task<ValueTuple<string,string>> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", httpCore!.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("cuid", httpCore.Account.CuidGalaxy2)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/sync").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}

