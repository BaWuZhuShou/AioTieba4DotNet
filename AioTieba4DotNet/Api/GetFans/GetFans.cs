using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Shared;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetFans;

[RequireBduss]
[PythonApi("aiotieba.api.get_fans")]
internal class GetFans(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserList ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);
        var objs = resJson.GetValue("user_list") is JArray users
            ? users.OfType<JObject>().Select(UserInfoMapper.FromTbData).ToList()
            : [];

        return new UserList(objs, PageTMapper.FromTbData(resJson.GetValue("page") as JObject));
    }

    public async Task<UserList> RequestAsync(long userId, int pn, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("_client_version", Const.MainVersion),
            new("pn", pn.ToString()),
            new("uid", userId.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/fans/page").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
