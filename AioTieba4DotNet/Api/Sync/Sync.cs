using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Sync;

/// <summary>
/// 同步客户端状态的 API (用于获取 ClientId 和 SampleId)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.sync")]
public class Sync(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static (string ClientId, string SampleId) ParseBody(string body)
    {
        var o = JsonApiBase.ParseBody(body);

        var clientId = o.GetValue("client")!.ToObject<JObject>()!.GetValue("client_id")!.ToObject<string>()!;
        var sampleId = o.GetValue("wl_config")!.ToObject<JObject>()!.GetValue("sample_id")!.ToObject<string>()!;

        return (clientId, sampleId);
    }

    /// <summary>
    /// 发送同步状态请求
    /// </summary>
    /// <returns>包含 ClientId 和 SampleId 的元组</returns>
    public async Task<(string ClientId, string SampleId)> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("cuid", HttpCore.Account.CuidGalaxy2)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/sync").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
