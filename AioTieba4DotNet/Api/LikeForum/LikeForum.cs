using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.LikeForum;

/// <summary>
/// 关注贴吧的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.follow_forum")]
public class LikeForum(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);

        var error = resJson["error"];
        if (error != null)
        {
            var errno = error.Value<int>("errno");
            if (errno != 0) throw new TieBaServerException(errno, error.Value<string>("errmsg") ?? string.Empty);
        }

        return true;
    }

    /// <summary>
    /// 发送关注贴吧请求
    /// </summary>
    /// <param name="fid">吧 ID (fid)</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(ulong fid)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss), new("fid", fid.ToString()), new("tbs", HttpCore.Account!.Tbs!)
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/forum/like").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
