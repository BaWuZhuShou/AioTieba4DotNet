using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.SearchExact;

internal sealed class SearchExact(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<ExactSearches> RequestAsync(string fname, string query, int pn, int rn,
        ForumSearchType searchType,
        bool onlyThread, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("_client_version", Const.MainVersion),
            new("kw", fname),
            new("only_thread", onlyThread ? "1" : "0"),
            new("pn", pn.ToString()),
            new("rn", rn.ToString()),
            new("sm", ((int)searchType).ToString()),
            new("word", query)
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/s/searchpost").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ExactSearchesMapper.FromTbData(ParseBody(result));
    }
}
