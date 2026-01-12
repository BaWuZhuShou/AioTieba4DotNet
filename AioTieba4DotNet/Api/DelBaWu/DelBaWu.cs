using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.DelBawu;

/// <summary>
///     移除吧务的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.del_bawu")]
internal class DelBaWu(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    /// <summary>
    ///     发送移除吧务请求
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <param name="baWuType">吧务类型 (如 manager, moderator)</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(int fid, string portrait, string baWuType)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("fn", "-"),
            new("fid", fid.ToString()),
            new("team_un", "-"),
            new("team_uid", portrait),
            new("bawu_type", baWuType)
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/bawuteamclear").Uri;
        var result = await HttpCore.SendWebFormAsync(requestUri, data);

        return ParseBody(result);
    }
}
