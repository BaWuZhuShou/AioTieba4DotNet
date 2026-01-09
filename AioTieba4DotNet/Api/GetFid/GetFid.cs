using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetFid;

public class GetFid(ITiebaHttpCore httpCore)
{
    public  ulong ParseBody(string body)
    {
        var o = JObject.Parse(body);
        var code = o.GetValue("no")?.ToObject<int>();
        if (code != null && code != 0)
        {
            throw new TieBaServerException(code ?? -1, o.GetValue("error")?.ToObject<string>() ?? string.Empty);
        }

        var fid = o.GetValue("data")!.ToObject<JObject>()!.GetValue("fid")!.ToObject<ulong>();
        if (fid == 0)
        {
            throw new TieBaServerException(-1, "fid is 0!");
        }

        return fid;
    }

    public  async Task<ulong> RequestAsync(string fname)
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("fname", fname),
            new("ie", "utf-8")
        };
        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/f/commit/share/fnameShareApi").Uri;

        var responseMessage = await httpCore.PackWebGetRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}

