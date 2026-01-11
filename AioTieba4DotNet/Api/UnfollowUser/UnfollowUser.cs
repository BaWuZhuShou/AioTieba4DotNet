using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.UnfollowUser;

/// <summary>
///     取消关注用户的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.unfollow_user")]
public class UnfollowUser(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    /// <summary>
    ///     发送取消关注用户请求
    /// </summary>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(string portrait)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? ""),
            new("portrait", portrait),
            new("tbs", HttpCore.Account?.Tbs ?? "")
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/user/unfollow").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        ParseBody(result);
        return true;
    }
}
