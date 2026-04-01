using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Api.DelThread;

/// <summary>
///     删除主题帖的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.del_thread")]
internal class DelThread(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

/// <summary>
///     发送删除主题帖请求
/// </summary>
/// <param name="fid">吧 ID</param>
/// <param name="tid">主题帖 ID</param>
/// <param name="isHide">是否隐藏主题帖</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(ulong fid, long tid, bool isHide = false,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("fid", fid.ToString()),
            new("is_frs_mask", isHide ? "1" : "0"),
            new("tbs", HttpCore.Account!.Tbs!),
            new("z", tid.ToString())
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/delthread").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
