using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetStatistics;

internal sealed class GetStatistics(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<ForumStatistics> RequestAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("_client_version", Const.MainVersion),
            new("forum_id", fid.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/forum/getforumdata").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        var body = ParseBody(result);
        return ForumStatisticsMapper.FromTbData(body.GetValue("data") as JArray ?? new JArray());
    }
}
