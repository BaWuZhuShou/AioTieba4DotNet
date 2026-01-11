using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetForum.Entities;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Api.GetForum;

/// <summary>
///     获取贴吧基础信息的 API (主要用于获取 Fid)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[PythonApi("aiotieba.api.get_forum")]
public class GetForum(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static Forum ParseBody(string body)
    {
        var o = JsonApiBase.ParseBody(body);

        var forumDict = o.GetValue("forum")?.ToObject<Dictionary<string, object>>();
        if (forumDict == null) throw new TieBaServerException(-1, "无法获取到贴吧数据!");

        return Forum.FromTbData(forumDict);
    }

    /// <summary>
    ///     发送获取贴吧基础信息请求
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <returns>贴吧基础信息</returns>
    public async Task<Forum> RequestAsync(string fname)
    {
        var data = new List<KeyValuePair<string, string>> { new("kw", fname) };
        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/c/f/frs/frsBottom").Uri;

        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
