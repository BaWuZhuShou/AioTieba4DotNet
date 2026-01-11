using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.Sign;

/// <summary>
///     贴吧签到的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.sign_forum")]
public class Sign(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    /// <summary>
    ///     发送贴吧签到请求
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="fid">吧 ID (fid)</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(string fname, ulong fid)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("fid", fid.ToString()),
            new("kw", fname),
            new("tbs", HttpCore.Account!.Tbs!)
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/forum/sign").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        ParseBody(result);
        return true;
    }
}
