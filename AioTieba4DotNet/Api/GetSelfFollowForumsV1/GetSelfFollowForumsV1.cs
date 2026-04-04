using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetSelfFollowForumsV1;

internal sealed class GetSelfFollowForumsV1(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static SelfFollowForumsV1 ParseResponse(string body)
    {
        var data = ParseBody(body, "errno", "errmsg");
        var legacyLikeForum = data.GetValue("data")?["like_forum"] as JObject
                              ?? throw new TieBaServerException(-1, "Unable to parse self follow forums v1 data.");

        return SelfFollowForumsV1Mapper.FromTbData(legacyLikeForum);
    }

    public async Task<SelfFollowForumsV1> RequestAsync(int pn, int rn, CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string>> { new("pn", pn.ToString()), new("rn", rn.ToString()) };

        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/mg/o/getForumHome").Uri;
        var result = await HttpCore.SendWebGetAsync(requestUri, parameters, cancellationToken);
        return ParseResponse(result);
    }
}
