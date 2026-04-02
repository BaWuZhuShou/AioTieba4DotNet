using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.Move;

[RequireBduss]
[PythonApi("aiotieba.api.move")]
internal class Move(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, long tid, int toTabId, int fromTabId,
        CancellationToken cancellationToken = default)
    {
        var threadPayload =
            $"[{{\"thread_id\":{tid},\"from_tab_id\":{fromTabId},\"to_tab_id\":{toTabId}}}]";

        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("forum_id", fid.ToString()),
            new("tbs", HttpCore.Account!.Tbs!),
            new("threads", threadPayload)
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/moveTabThread").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
