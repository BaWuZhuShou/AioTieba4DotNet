using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.DelPost;

[RequireBduss]
public class DelPost(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, long tid, long pid)
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("fid", fid.ToString()),
            new("tid", tid.ToString()),
            new("pid", pid.ToString()),
            new("tbs", HttpCore.Account!.Tbs!),
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/post/del").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
