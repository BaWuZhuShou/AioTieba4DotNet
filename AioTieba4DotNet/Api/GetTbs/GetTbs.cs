using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetTbs;

/// <summary>
/// 获取 TBS 校验码的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.get_tbs")]
public class GetTbs(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static string ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);

        return resJson.GetValue("tbs")?.ToString() ?? "";
    }

    /// <summary>
    /// 发送获取 TBS 校验码请求
    /// </summary>
    /// <returns>TBS 校验码</returns>
    public async Task<string> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? ""), new("_client_version", Const.MainVersion)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/tbs").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        var tbs = ParseBody(result);

        if (HttpCore.Account != null)
        {
            HttpCore.Account.Tbs = tbs;
        }

        return tbs;
    }
}
