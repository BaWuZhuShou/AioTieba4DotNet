using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.RemoveFan;

[RequireBduss]
[PythonApi("aiotieba.api.remove_fan")]
internal class RemoveFan(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(long userId, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("fans_uid", userId.ToString()),
            new("tbs", HttpCore.Account?.Tbs ?? string.Empty)
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/user/removeFans").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        ParseBody(result);
        return true;
    }
}
