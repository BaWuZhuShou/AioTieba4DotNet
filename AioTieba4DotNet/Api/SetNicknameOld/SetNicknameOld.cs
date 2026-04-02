using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.SetNicknameOld;

[RequireBduss]
[PythonApi("aiotieba.api.set_nickname_old")]
internal sealed class SetNicknameOld(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(string nickName, CancellationToken cancellationToken = default)
    {
        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/submit/modifyNickname")
        {
            Query = $"nickname={Uri.EscapeDataString(nickName)}&tbs=1"
        }.Uri;

        var response = await HttpCore.SendWebFormAsync(requestUri, [], cancellationToken);
        ParseBody(response, "no", "error");
        return true;
    }
}
