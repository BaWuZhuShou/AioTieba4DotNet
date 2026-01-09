using AioTieba4DotNet.Abstractions;
using System.Text;
using AioTieba4DotNet.Api.GetUInfoUserJson.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoUserJson;

public class GetUInfoUserJson(ITiebaHttpCore httpCore) : BaseApiRequest<string, UserInfoJson>
{
    public override UserInfoJson ParseBody(string body)
    {
        var o = JObject.Parse(body);
        // var code = o.GetValue("no")?.ToObject<int>();
        // if (code != null && code != 0)
        // {
        //     throw new TieBaServerException(code ?? -1, o.GetValue("error")?.ToObject<string>() ?? string.Empty);
        // }

        var data = o.GetValue("creator")?.ToObject<JObject>();
        return data == null ? throw new TieBaServerException(-1, "无法获取到用户数据!") : UserInfoJson.FromTbData(data);
    }

    public override async Task<UserInfoJson> RequestAsync(string username)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("un", username),
            new("ie", "utf-8"),
        };
        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/i/sys/user_json").Uri;
        var responseMessage = await httpCore.PackWebGetRequestAsync(requestUri, data);
        var responseBytes = await responseMessage.Content.ReadAsByteArrayAsync();
        if (responseBytes.Length == 0)
        {
            throw new TieBaServerException(-1, "无法获取到用户数据!");
        }
        // 使用 GBK 编码将字节数组转换为字符串
        var responseString = Encoding.GetEncoding("UTF-8", EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback).GetString(responseBytes);
        return ParseBody(responseString);
    }
}

