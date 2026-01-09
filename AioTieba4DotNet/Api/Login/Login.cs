using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Login.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Login;

public class Login(ITiebaHttpCore httpCore) : BaseApiRequest<ValueTuple<UserInfoLogin, string>>
{
    public override ValueTuple<UserInfoLogin, string> ParseBody(string body)
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue("error_code")?.ToObject<int>();
        if (code != null && code != 0)
        {
            throw new TieBaServerException(code ?? -1, resJson.GetValue("error_msg")?.ToObject<string>() ?? string.Empty);
        }

        var userDict = resJson.GetValue("user")?.ToObject<JObject>()!;
        var user = UserInfoLogin.FromTbData(userDict);
        var tbs = resJson.GetValue("anti")?.ToObject<JObject>()!.GetValue("tbs")!.ToString()!;
        return (user, tbs);
    }

    public override async Task<ValueTuple<UserInfoLogin, string>> RequestAsync()
    {
        var data = new List<KeyValuePair<string, string>>()
        {
            new("_client_version", Const.MainVersion),
            new("bdusstoken", httpCore!.Account!.Bduss)
        };
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/s/login").Uri;
        var responseMessage = await httpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}

