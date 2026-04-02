using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.FollowUser;

/// <summary>
///     关注用户的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.follow_user")]
internal class FollowUser(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    /// <summary>
    ///     发送关注用户请求
    /// </summary>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(string portrait, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? ""),
            new("portrait", portrait),
            new("tbs", HttpCore.Account?.Tbs ?? "")
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/user/follow").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        ParseBody(result);
        return true;
    }
}
