using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Agree;

public class Agree(ITiebaHttpCore httpCore)
{
    private static void ParseBody(string body)
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue("error_code")?.ToObject<int>() ?? 0;
        if (code != 0)
        {
            throw new TieBaServerException(code, resJson.GetValue("error_msg")?.ToObject<string>() ?? string.Empty);
        }
    }

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
            new("BDUSS", httpCore.Account?.Bduss ?? ""),
            new("_client_version", Const.MainVersion),
            new("agree_type", isDisagree ? "5" : "2"),
            new("cuid", httpCore.Account?.CuidGalaxy2 ?? ""),
            new("obj_type", objType.ToString()),
            new("op_type", isUndo ? "1" : "0"),
            new("post_id", pid.ToString()),
            new("tbs", httpCore.Account?.Tbs ?? ""),
            new("thread_id", tid.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/agree/opAgree").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        ParseBody(result);
        return true;
    }
}
