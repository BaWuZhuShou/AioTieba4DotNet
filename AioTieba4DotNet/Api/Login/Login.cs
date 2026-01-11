using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Login.Entities;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Login;

/// <summary>
/// 登录 API (用于获取用户信息和 TBS)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.login")]
internal class Login(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static (UserInfoLogin User, string Tbs) ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);

        var userDict = resJson.GetValue("user")?.ToObject<JObject>()!;
        var user = UserInfoLogin.FromTbData(userDict);
        var tbs = resJson.GetValue("anti")?.ToObject<JObject>()!.GetValue("tbs")!.ToString()!;
        return (user, tbs);
    }

    /// <summary>
    /// 发送登录请求
    /// </summary>
    /// <returns>包含用户信息和 TBS 的元组</returns>
    public async Task<(UserInfoLogin User, string Tbs)> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("_client_version", Const.MainVersion), new("bdusstoken", HttpCore.Account!.Bduss)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/login").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
