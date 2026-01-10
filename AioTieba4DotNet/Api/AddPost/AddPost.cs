using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.AddPost;

public class AddPost(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private new static long ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);

        return resJson["data"]?["pid"]?.ToObject<long>() ?? 0;
    }

    public async Task<long> RequestAsync(string fname, ulong fid, long tid, List<IFrag> contents, long quoteId = 0, uint floor = 0)
    {
        var contentJson = JsonConvert.SerializeObject(contents.Select(c => c.ToDict()));

        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("anonymous", "0"),
            new("content", contentJson),
            new("fid", fid.ToString()),
            new("fname", fname),
            new("tid", tid.ToString()),
            new("is_ad", "0"),
            new("new_vcode", "1"),
            new("tbs", HttpCore.Account!.Tbs!),
            new("vcode_tag", "12"),
        };

        if (quoteId != 0)
        {
            data.Add(new KeyValuePair<string, string>("quote_id", quoteId.ToString()));
            if (floor != 0)
            {
                data.Add(new KeyValuePair<string, string>("floor_num", floor.ToString()));
            }
        }

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/post/add").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
