using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetSelfFollowForums;

internal sealed class GetSelfFollowForums(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static SelfFollowForums ParseResponse(string body)
    {
        var data = ParseBody(body);
        return SelfFollowForumsMapper.FromTbData(data);
    }

    public async Task<SelfFollowForums> RequestAsync(int pn, int rn, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("tbs", HttpCore.Account?.Tbs ?? string.Empty),
            new("sort_type", "3"),
            new("call_from", "3"),
            new("page_no", pn.ToString()),
            new("res_num", rn.ToString())
        };

        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/c/f/forum/forumGuide").Uri;
        var result = await HttpCore.SendAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("Subapp-Type", "hybrid");
            request.Content = new FormUrlEncodedContent(data);
            return request;
        }, cancellationToken: cancellationToken);

        return ParseResponse(result);
    }
}
