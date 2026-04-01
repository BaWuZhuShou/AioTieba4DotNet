using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetBawuPerm;

[RequireBduss]
[PythonApi("aiotieba.api.get_bawu_perm")]
internal sealed class GetBawuPerm(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static BawuPerm ParseResponse(string body)
    {
        var responseJson = ParseBody(body, "no", "error");
        return BawuPermMapper.FromTbData(responseJson.GetValue("data") as JObject ?? new JObject());
    }

    public async Task<BawuPerm> RequestAsync(ulong fid, string portrait, CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("forum_id", fid.ToString()),
            new("portrait", portrait)
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/getAuthToolPerm").Uri;
        var result = await httpCore.SendWebGetAsync(requestUri, parameters, cancellationToken);
        return ParseResponse(result);
    }
}
