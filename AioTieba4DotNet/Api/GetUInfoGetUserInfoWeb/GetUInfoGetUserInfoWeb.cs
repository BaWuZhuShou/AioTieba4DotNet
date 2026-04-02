using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoGetUserInfoWeb;

[PythonApi("aiotieba.api.get_uinfo_getUserInfo_web")]
internal sealed class GetUInfoGetUserInfoWeb(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<UserInfo> RequestAsync(int userId, CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string>> { new("chatUid", userId.ToString()) };
        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/im/pcmsg/query/getUserInfo").Uri;
        var response = await HttpCore.SendWebGetAsync(requestUri, parameters, cancellationToken);
        return ParseResponse(response);
    }

    private static UserInfo ParseResponse(string body)
    {
        var responseJson = ParseBody(body, "errno", "errmsg");
        var user = responseJson.GetValue("chatUser") as JObject
                   ?? throw new TieBaServerException(-1, "Unable to get user info from response.");
        return UserInfoGuInfoWebMapper.FromTbData(user);
    }
}
