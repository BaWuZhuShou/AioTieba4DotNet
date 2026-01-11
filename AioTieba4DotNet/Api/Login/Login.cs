using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Login.Entities;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Login;

[RequireBduss]
public class Login(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static (UserInfoLogin User, string Tbs) ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);

        var userDict = resJson.GetValue("user")?.ToObject<JObject>()!;
        var user = UserInfoLogin.FromTbData(userDict);
        var tbs = resJson.GetValue("anti")?.ToObject<JObject>()!.GetValue("tbs")!.ToString()!;
        return (user, tbs);
    }

    public async Task<(UserInfoLogin User, string Tbs)> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("_client_version", Const.MainVersion),
            new("bdusstoken", HttpCore.Account!.Bduss)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/login").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}

