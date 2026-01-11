using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetFid;

/// <summary>
///     获取吧 ID (fid) 的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[PythonApi("aiotieba.api.get_fid")]
public class GetFid(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static ulong ParseBody(string body)
    {
        var o = ParseBody(body, "no", "error");

        var fid = o.GetValue("data")!.ToObject<JObject>()!.GetValue("fid")!.ToObject<ulong>();
        if (fid == 0) throw new TieBaServerException(-1, "fid is 0!");

        return fid;
    }

    /// <summary>
    ///     发送获取吧 ID 请求
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>吧 ID (fid)</returns>
    public async Task<ulong> RequestAsync(string fname)
    {
        var data = new List<KeyValuePair<string, string>> { new("fname", fname), new("ie", "utf-8") };
        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/f/commit/share/fnameShareApi").Uri;

        var result = await HttpCore.SendWebGetAsync(requestUri, data);
        return ParseBody(result);
    }
}
