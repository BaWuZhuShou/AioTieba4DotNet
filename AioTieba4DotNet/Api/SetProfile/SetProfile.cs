using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.SetProfile;

[RequireBduss]
[PythonApi("aiotieba.api.set_profile")]
internal sealed class SetProfile(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(string nickName, string sign, Gender gender,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("intro", sign),
            new("nick_name", nickName),
            new("sex", ((int)gender).ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/profile/modify").Uri;
        var response = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        ParseBody(response);
        return true;
    }
}
