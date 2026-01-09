using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Agree;


public class Agree(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(long tid, long pid, bool isComment, bool isDisagree, bool isUndo)
    {
        int objType;
        if (pid == 0)
        {
            objType = 3;
        }
        else
        {
            objType = isComment ? 2 : 1;
        }

        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? ""),
            new("_client_version", Const.MainVersion),
            new("agree_type", isDisagree ? "5" : "2"),
            new("cuid", HttpCore.Account?.CuidGalaxy2 ?? ""),
            new("obj_type", objType.ToString()),
            new("op_type", isUndo ? "1" : "0"),
            new("post_id", pid.ToString()),
            new("tbs", HttpCore.Account?.Tbs ?? ""),
            new("thread_id", tid.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/agree/opAgree").Uri;
        var responseMessage = await HttpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        ParseBody(result);
        return true;
    }
}
