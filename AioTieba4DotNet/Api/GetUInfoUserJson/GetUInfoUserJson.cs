using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using System.Text;
using AioTieba4DotNet.Api.GetUInfoUserJson.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoUserJson;

/// <summary>
/// 通过用户名获取用户信息 JSON 的 API (Web端接口，常用于获取用户 uid)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[PythonApi("aiotieba.api.get_uinfo_user_json")]
public class GetUInfoUserJson(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserInfoJson ParseBody(string body)
    {
        var o = JObject.Parse(body);
        var data = o.GetValue("creator")?.ToObject<JObject>();
        return data == null ? throw new TieBaServerException(-1, "无法获取到用户数据!") : UserInfoJson.FromTbData(data);
    }

    /// <summary>
    /// 发送获取用户信息 JSON 请求
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>用户信息 JSON 实体</returns>
    public async Task<UserInfoJson> RequestAsync(string username)
    {
        var data = new List<KeyValuePair<string, string>> { new("un", username), new("ie", "utf-8") };
        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/i/sys/user_json").Uri;
        var responseString = await HttpCore.SendWebGetAsync(requestUri, data);
        if (string.IsNullOrEmpty(responseString)) throw new TieBaServerException(-1, "无法获取到用户数据!");

        return ParseBody(responseString);
    }
}
