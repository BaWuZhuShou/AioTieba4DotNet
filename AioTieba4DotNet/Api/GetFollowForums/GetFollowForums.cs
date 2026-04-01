using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetFollowForums;

[PythonApi("aiotieba.api.get_follow_forums")]
internal sealed class GetFollowForums(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static FollowForums ParseResponse(string body)
    {
        var data = ParseBody(body);
        return FollowForumsMapper.FromTbData(data);
    }

    public async Task<FollowForums> RequestAsync(long userId, int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("_client_version", Const.MainVersion),
            new("friend_uid", userId.ToString()),
            new("page_no", pn.ToString()),
            new("page_size", rn.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/forum/like").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
