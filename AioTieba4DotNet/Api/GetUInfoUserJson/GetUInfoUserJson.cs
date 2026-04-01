using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet;
using AioTieba4DotNet.Internal.Mapping;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoUserJson;

/// <summary>
///     通过用户名获取用户信息 JSON 的 API (Web端接口，常用于获取用户 uid)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[PythonApi("aiotieba.api.get_uinfo_user_json")]
internal class GetUInfoUserJson(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserInfoJson ParseBody(string body)
    {
        var o = JObject.Parse(body);
        var data = o.GetValue("creator")?.ToObject<JObject>();
        return data == null ? throw new TieBaServerException(-1, "无法获取到用户数据!") : UserInfoJsonMapper.FromTbData(data);
    }

    /// <summary>
    ///     发送获取用户信息 JSON 请求
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息 JSON 实体</returns>
    public async Task<UserInfoJson> RequestAsync(string username, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>> { new("un", username), new("ie", "utf-8") };
        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/i/sys/user_json").Uri;
        var responseString = await HttpCore.SendWebGetAsync(requestUri, data, cancellationToken);
        if (string.IsNullOrEmpty(responseString)) throw new TieBaServerException(-1, "无法获取到用户数据!");

        return ParseBody(responseString);
    }
}
