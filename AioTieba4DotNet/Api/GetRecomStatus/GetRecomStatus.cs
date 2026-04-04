using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetRecomStatus;

internal sealed class GetRecomStatus(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<RecomStatus> RequestAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("_client_version", Const.MainVersion),
            new("forum_id", fid.ToString()),
            new("pn", "1"),
            new("rn", "0")
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/bawu/getRecomThreadList").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return RecomStatusMapper.FromTbData(ParseBody(result));
    }
}
