using System.Collections.Generic;
using System.Linq;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Api.DelPosts;

[RequireBduss]
[PythonApi("aiotieba.api.del_posts")]
internal class DelPosts(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, long tid, IReadOnlyList<long> pids, bool block,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("forum_id", fid.ToString()),
            new("post_ids", string.Join(',', pids.Select(pid => pid.ToString()))),
            new("tbs", HttpCore.Account!.Tbs!),
            new("thread_id", tid.ToString()),
            new("type", block ? "2" : "1")
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/multiDelPost").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
