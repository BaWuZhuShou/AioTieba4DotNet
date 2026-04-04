using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetAts;

internal class GetAts(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static AtMessages ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);
        return AtMessagesMapper.FromTbData(resJson);
    }

    public async Task<AtMessages> RequestAsync(int pn, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("_client_version", Const.MainVersion),
            new("pn", pn.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/feed/atme").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
