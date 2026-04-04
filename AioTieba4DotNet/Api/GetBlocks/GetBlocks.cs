using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetBlocks;

internal sealed class GetBlocks(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static Blocks ParseResponse(string body)
    {
        var responseJson = ParseBody(body, "no", "error");
        return BlocksMapper.FromTbData(responseJson);
    }

    public async Task<Blocks> RequestAsync(ulong fid, string userName, int pn,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("fn", "-"),
            new("fid", fid.ToString()),
            new("word", userName),
            new("is_ajax", "1"),
            new("pn", pn.ToString())
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/bawublock").Uri;
        var result = await httpCore.SendWebGetAsync(requestUri, parameters, cancellationToken);
        return ParseResponse(result);
    }
}
