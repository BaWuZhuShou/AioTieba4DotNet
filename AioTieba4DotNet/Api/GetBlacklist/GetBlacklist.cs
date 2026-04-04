using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetBlacklist;

internal class GetBlacklist(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static BlacklistUsers ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);
        return BlacklistUsersMapper.FromTbData(resJson);
    }

    public async Task<BlacklistUsers> RequestAsync(CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty), new("_client_version", Const.MainVersion)
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/user/userBlackPage").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
