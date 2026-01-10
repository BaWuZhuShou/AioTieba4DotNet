using AioTieba4DotNet.Abstractions;
using System.Text;
using AioTieba4DotNet.Api.GetUInfoUserJson.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoUserJson;

public class GetUInfoUserJson(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserInfoJson ParseBody(string body)
    {
        var o = JObject.Parse(body);
        var data = o.GetValue("creator")?.ToObject<JObject>();
        return data == null ? throw new TieBaServerException(-1, "无法获取到用户数据!") : UserInfoJson.FromTbData(data);
    }

    public async Task<UserInfoJson> RequestAsync(string username)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("un", username),
            new("ie", "utf-8"),
        };
        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/i/sys/user_json").Uri;
        var responseString = await HttpCore.SendWebGetAsync(requestUri, data);
        if (string.IsNullOrEmpty(responseString))
        {
            throw new TieBaServerException(-1, "无法获取到用户数据!");
        }
        return ParseBody(responseString);
    }
}

