using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetBawuBlacklist;

internal sealed class GetBawuBlacklist(ITiebaHttpCore httpCore)
{
    public async Task<BawuBlacklistUsers> RequestAsync(string fname, int pn,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string>> { new("word", fname), new("pn", pn.ToString()) };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/bawu2/platform/listBlackUser").Uri;
        var result = await httpCore.SendWebGetAsync(requestUri, parameters, cancellationToken);
        return BawuBlacklistUsersMapper.FromTbData(result);
    }
}
