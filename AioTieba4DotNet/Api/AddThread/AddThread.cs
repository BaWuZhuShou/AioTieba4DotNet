using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.AddThread;

public class AddThread(ITiebaHttpCore httpCore)
{
    private static long ParseBody(string body)
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue("error_code")?.ToObject<int>();
        if (code != null && code != 0)
        {
            throw new TieBaServerException(code ?? -1, resJson.GetValue("error_msg")?.ToObject<string>() ?? string.Empty);
        }

        return resJson["data"]?["tid"]?.ToObject<long>() ?? 0;
    }

    public async Task<long> RequestAsync(string fname, ulong fid, string title, List<IFrag> contents)
    {
        var contentJson = JsonConvert.SerializeObject(contents.Select(c => c.ToDict()));
        
        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", httpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("anonymous", "0"),
            new("content", contentJson),
            new("fid", fid.ToString()),
            new("forum_id", fid.ToString()),
            new("fname", fname),
            new("is_ad", "0"),
            new("is_feedback", "0"),
            new("is_new_list", "1"),
            new("new_vcode", "1"),
            new("stErrorNums", "0"),
            new("stMethodNum", "1"),
            new("stMode", "1"),
            new("stSize", "0"),
            new("stTime", "0"),
            new("tbs", httpCore.Account!.Tbs!),
            new("title", title),
            new("vcode_tag", "12"),
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/thread/add").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}
