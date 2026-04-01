using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetRankUsers;

[PythonApi("aiotieba.api.get_rank_users")]
internal sealed class GetRankUsers(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<RankUsers> RequestAsync(string fname, int pn, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("kw", fname),
            new("pn", pn.ToString()),
            new("ie", "utf-8")
        };

        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/f/like/furank").Uri;
        var response = await HttpCore.SendWebGetAsync(requestUri, data, cancellationToken);
        return RankUsersMapper.FromHtml(response);
    }
}
