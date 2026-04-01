using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.SignGrowth;

[RequireBduss]
[PythonApi("aiotieba.api.sign_growth")]
internal sealed class SignGrowth(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(string actType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actType);

        var data = new List<KeyValuePair<string, string>>
        {
            new("tbs", HttpCore.Account?.Tbs ?? string.Empty),
            new("act_type", actType),
            new("cuid", "-")
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/usergrowth/commitUGTaskInfo").Uri;
        var result = await HttpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        ParseBody(result, "no", "error");
        return true;
    }
}
