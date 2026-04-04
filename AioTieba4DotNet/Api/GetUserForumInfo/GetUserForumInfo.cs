using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUserForumInfo;

internal sealed class GetUserForumInfo(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<UserForumInfo> RequestAsync(ulong fid, string portrait,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("_client_version", Const.MainVersion),
            new("forum_id", fid.ToString()),
            new("friend_portrait", portrait)
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/forum/getUserForumLevelInfo").Uri;
        var response = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(response);
    }

    private static UserForumInfo ParseResponse(string body)
    {
        var responseJson = JObject.Parse(body);
        var errorCode = responseJson.GetValue("error_code")?.Value<int>() ?? 0;
        var errorMessage = responseJson.GetValue("error_msg")?.Value<string>()
                           ?? responseJson.GetValue("error")?.Value<string>()
                           ?? responseJson.GetValue("errmsg")?.Value<string>();
        ApiResponseValidator.CheckError(errorCode, errorMessage);

        var data = responseJson.GetValue("data") as JObject ?? new JObject();
        return UserForumInfoMapper.FromTbData(data);
    }
}
