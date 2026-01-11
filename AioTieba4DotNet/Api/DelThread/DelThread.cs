using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.DelThread;

[RequireBduss]
public class DelThread(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, long tid)
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("fid", fid.ToString()),
            new("tid", tid.ToString()),
            new("tbs", HttpCore.Account!.Tbs!),
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/thread/del").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
