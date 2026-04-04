using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetSelfInfoMoIndex;

internal class GetSelfInfoMoIndex(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserInfo ParseBody(string body)
    {
        var resJson = ParseBody(body, "no", "error");
        var data = resJson.GetValue("data") as JObject ?? new JObject();
        return UserInfoSelfMoIndexMapper.FromTbData(data);
    }

    public async Task<UserInfo> RequestAsync(CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>> { new("need_user", "1") };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/newmoindex").Uri;
        var result = await HttpCore.SendWebGetAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
