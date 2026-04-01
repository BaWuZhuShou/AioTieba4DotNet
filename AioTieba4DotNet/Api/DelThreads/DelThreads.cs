using System.Collections.Generic;
using System.Linq;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Api.DelThreads;

[RequireBduss]
[PythonApi("aiotieba.api.del_threads")]
internal class DelThreads(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, IReadOnlyList<long> tids, bool block,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("forum_id", fid.ToString()),
            new("tbs", HttpCore.Account!.Tbs!),
            new("thread_ids", string.Join(',', tids.Select(tid => tid.ToString()))),
            new("type", block ? "2" : "1")
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/multiDelThread").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
