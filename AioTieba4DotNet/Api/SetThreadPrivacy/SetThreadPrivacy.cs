using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Api.SetThreadPrivacy;

[RequireBduss]
[PythonApi("aiotieba.api.set_thread_privacy")]
internal class SetThreadPrivacy(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResult(string body)
    {
        ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, long tid, long pid, bool isPrivate,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("forum_id", fid.ToString()),
            new("is_hide", isPrivate ? "1" : "0"),
            new("post_id", pid.ToString()),
            new("thread_id", tid.ToString())
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/thread/setPrivacy").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResult(result);
    }
}
