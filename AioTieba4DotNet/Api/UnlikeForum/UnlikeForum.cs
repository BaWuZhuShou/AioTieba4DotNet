using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.UnlikeForum;

/// <summary>
///     取消关注贴吧的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.unfollow_forum")]
internal class UnlikeForum(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
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
    ///     发送取消关注贴吧请求
    /// </summary>
    /// <param name="fid">吧 ID (fid)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("fid", fid.ToString()),
            new("tbs", HttpCore.Account!.Tbs!)
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/forum/unlike").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
