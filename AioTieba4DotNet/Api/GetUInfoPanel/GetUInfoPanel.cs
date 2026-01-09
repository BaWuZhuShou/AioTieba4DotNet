using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetUInfoPanel.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoPanel;

public class GetUInfoPanel(ITiebaHttpCore httpCore) : BaseApiRequest<string, UserInfoPanel>
{
    public override UserInfoPanel ParseBody(string body)
    {
        var o = JObject.Parse(body);
        var code = o.GetValue("no")?.ToObject<int>();
        if (code != null && code != 0)
        {
            throw new TieBaServerException(code ?? -1, o.GetValue("error")?.ToObject<string>() ?? string.Empty);
        }
        var data = o.GetValue("data")?.ToObject<JObject>();
        if (data == null)
        {
            throw new TieBaServerException(-1, "无法获取到用户数据!");
        }

        return UserInfoPanel.FromTbData(data);
    }

    public override async Task<UserInfoPanel> RequestAsync(string nameOrPortrait)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            Utils.IsPortrait(nameOrPortrait)
                ? new KeyValuePair<string, string>("id", nameOrPortrait)
                : new KeyValuePair<string, string>("un", nameOrPortrait)
        };
        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/home/get/panel").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}

