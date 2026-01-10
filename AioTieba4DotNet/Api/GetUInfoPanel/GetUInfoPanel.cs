using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetUInfoPanel.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoPanel;

public class GetUInfoPanel(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserInfoPanel ParseBody(string body)
    {
        var o = JsonApiBase.ParseBody(body, "no", "error");

        var data = o.GetValue("data")?.ToObject<JObject>();
        if (data == null)
        {
            throw new TieBaServerException(-1, "无法获取到用户数据!");
        }

        return UserInfoPanel.FromTbData(data);
    }

    public async Task<UserInfoPanel> RequestAsync(string nameOrPortrait)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            Utils.IsPortrait(nameOrPortrait)
                ? new KeyValuePair<string, string>("id", nameOrPortrait)
                : new KeyValuePair<string, string>("un", nameOrPortrait)
        };
        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/home/get/panel").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}

