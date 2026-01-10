using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetFid;

public class GetFid(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static ulong ParseBody(string body)
    {
        var o = JsonApiBase.ParseBody(body, "no", "error");

        var fid = o.GetValue("data")!.ToObject<JObject>()!.GetValue("fid")!.ToObject<ulong>();
        if (fid == 0)
        {
            throw new TieBaServerException(-1, "fid is 0!");
        }

        return fid;
    }

    public async Task<ulong> RequestAsync(string fname)
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("fname", fname),
            new("ie", "utf-8")
        };
        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/f/commit/share/fnameShareApi").Uri;

        var result = await HttpCore.SendWebGetAsync(requestUri, data);
        return ParseBody(result);
    }
}
