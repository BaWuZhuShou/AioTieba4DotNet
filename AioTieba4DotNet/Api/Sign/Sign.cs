using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.Sign;

/// <summary>
///     贴吧签到的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
internal class Sign(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static void ValidateSignedBonus(string body)
    {
        var response = ParseBody(body);
        var signBonusPoint = response["user_info"]?.Value<int?>("sign_bonus_point") ?? 0;
        if (signBonusPoint == 0)
            throw new TiebaProtocolException("sign_bonus_point is 0");
    }

    /// <summary>
    ///     发送贴吧签到请求
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(string fname, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("kw", fname),
            new("tbs", HttpCore.Account!.Tbs!)
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/forum/sign").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        ValidateSignedBonus(result);
        return true;
    }
}
