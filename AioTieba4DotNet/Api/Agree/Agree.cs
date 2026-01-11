using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Agree;

/// <summary>
/// 点赞/点踩 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.agree")]
public class Agree(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    /// <summary>
    /// 发送点赞/点踩请求
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID (0 表示对主题帖点赞)</param>
    /// <param name="isComment">是否为楼中楼回复</param>
    /// <param name="isDisagree">是否为点踩</param>
    /// <param name="isUndo">是否为取消操作</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(long tid, long pid, bool isComment, bool isDisagree, bool isUndo)
    {
        int objType;
        if (pid == 0)
            objType = 3;
        else
            objType = isComment ? 2 : 1;

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
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        ParseBody(result);
        return true;
    }
}
