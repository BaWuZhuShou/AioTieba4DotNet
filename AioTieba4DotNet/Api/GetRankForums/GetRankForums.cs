using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetRankForums;

internal sealed class GetRankForums(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<RankForums> RequestAsync(string fname, int pn, ForumRankType rankType,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("kw", fname), new("type", ((int)rankType).ToString()), new("pn", pn.ToString()), new("ie", "utf-8")
        };

        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/sign/index").Uri;
        var result = await HttpCore.SendWebGetAsync(requestUri, data, cancellationToken);
        return RankForumsMapper.FromHtml(result);
    }
}
