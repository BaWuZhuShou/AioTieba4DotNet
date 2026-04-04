using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.UndislikeForum;

internal sealed class UndislikeForum(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("cuid", HttpCore.Account?.Cuid ?? string.Empty),
            new("forum_id", fid.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/excellent/submitCancelDislike").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        ParseBody(result);
        return true;
    }
}
