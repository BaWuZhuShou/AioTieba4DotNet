using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetRecovers;

[RequireBduss]
[PythonApi("aiotieba.api.get_recovers")]
internal sealed class GetRecovers(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static Recovers ParseResponse(string body)
    {
        var responseJson = ParseBody(body, "no", "error");
        return RecoversMapper.FromTbData(responseJson);
    }

    public async Task<Recovers> RequestAsync(ulong fid, long? userId, int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("rn", rn.ToString()),
            new("forum_id", fid.ToString()),
            new("pn", pn.ToString()),
            new("type", "1"),
            new("sub_type", "1")
        };

        if (userId is > 0)
            parameters.Add(new("uid", userId.Value.ToString()));

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/manage/getRecoverList").Uri;
        var result = await HttpCore.SendWebGetAsync(requestUri, parameters, cancellationToken);
        return ParseResponse(result);
    }
}
