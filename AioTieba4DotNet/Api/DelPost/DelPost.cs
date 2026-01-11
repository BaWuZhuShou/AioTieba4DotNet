using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.DelPost;

/// <summary>
///     删除回复的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.del_post")]
public class DelPost(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    /// <summary>
    ///     发送删除回复请求
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(ulong fid, long tid, long pid)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("_client_version", Const.MainVersion),
            new("fid", fid.ToString()),
            new("tid", tid.ToString()),
            new("pid", pid.ToString()),
            new("tbs", HttpCore.Account!.Tbs!)
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/post/del").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
