using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetSelfInfoInitNickname;

internal class GetSelfInfoInitNickname(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserInfo ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);
        var userInfo = resJson.GetValue("user_info") as JObject ?? new JObject();
        return UserInfoSelfInitMapper.FromTbData(userInfo);
    }

    public async Task<UserInfo> RequestAsync(CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty), new("_client_version", Const.MainVersion)
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/s/initNickname").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
