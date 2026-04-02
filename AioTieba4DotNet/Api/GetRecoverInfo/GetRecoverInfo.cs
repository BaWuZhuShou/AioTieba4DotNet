using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetRecoverInfo;

[RequireBduss]
[PythonApi("aiotieba.api.get_recover_info")]
internal sealed class GetRecoverInfo(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static RecoverInfo ParseResponse(string body)
    {
        var responseJson = ParseBody(body, "no", "error");
        return RecoverInfoMapper.FromTbData(responseJson.GetValue("data") as JObject);
    }

    public async Task<RecoverInfo> RequestAsync(ulong fid, long tid, long pid,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("forum_id", fid.ToString()),
            new("thread_id", tid.ToString()),
            new("post_id", pid.ToString()),
            new("type", "1"),
            new("sub_type", pid > 0 ? "2" : "1")
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/bawu/getRecoverInfo").Uri;
        var result = await HttpCore.SendWebGetAsync(requestUri, parameters, cancellationToken);
        return ParseResponse(result);
    }
}
