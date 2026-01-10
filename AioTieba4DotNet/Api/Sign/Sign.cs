using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Sign;

public class Sign(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(string fname, ulong fid)
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("fid", fid.ToString()),
            new("kw", fname),
            new("tbs", HttpCore.Account!.Tbs!),
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/forum/sign").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        ParseBody(result);
        return true;
    }
}
