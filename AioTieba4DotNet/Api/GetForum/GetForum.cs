using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetForum.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetForum;

public class GetForum(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static Forum ParseBody(string body)
    {
        var o = JsonApiBase.ParseBody(body);

        var forumDict = o.GetValue("forum")?.ToObject<Dictionary<string, object>>();
        if (forumDict == null)
        {
            throw new TieBaServerException(-1, "无法获取到贴吧数据!");
        }

        return Forum.FromTbData(forumDict);
    }

    public async Task<Forum> RequestAsync(string fname)
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("kw", fname)
        };
        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/c/f/frs/frsBottom").Uri;

        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}

