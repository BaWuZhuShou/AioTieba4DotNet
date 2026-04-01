using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Api.DelPost;

/// <summary>
///     删除回复的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.del_post")]
internal class DelPost(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    /// <summary>
    ///     发送删除回复请求
    /// </summary>
/// <param name="fid">吧 ID</param>
/// <param name="tid">主题帖 ID</param>
/// <param name="pid">回复 ID</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(ulong fid, long tid, long pid, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("fid", fid.ToString()),
            new("pid", pid.ToString()),
            new("tbs", HttpCore.Account!.Tbs!),
            new("z", tid.ToString())
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/delpost").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
