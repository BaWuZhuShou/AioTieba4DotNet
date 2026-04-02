using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetUnblockAppeals;

[RequireBduss]
[PythonApi("aiotieba.api.get_unblock_appeals")]
internal sealed class GetUnblockAppeals(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static Appeals ParseResponse(string body)
    {
        var responseJson = ParseBody(body, "no", "error");
        return AppealsMapper.FromTbData(responseJson);
    }

    public async Task<Appeals> RequestAsync(ulong fid, int pn, int rn, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("fn", "-"),
            new("fid", fid.ToString()),
            new("pn", pn.ToString()),
            new("rn", rn.ToString()),
            new("tbs", httpCore.Account!.Tbs!)
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/getBawuAppealList").Uri;
        var result = await httpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
