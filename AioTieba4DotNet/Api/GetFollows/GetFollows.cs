using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetFollows;

/// <summary>
///     获取用户关注列表的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[PythonApi("aiotieba.api.get_follows")]
public class GetFollows(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserList ParseBody(string body)
    {
        var resJson = JsonApiBase.ParseBody(body);

        var followList = resJson.GetValue("follow_list")?.ToObject<JArray>() ?? [];
        var objs = followList.Select(m => UserInfo.FromTbData((JObject)m)).ToList();

        var pn = resJson.GetValue("pn")?.ToObject<int>() ?? 0;
        var totalCount = resJson.GetValue("total_follow_num")?.ToObject<int>() ?? 0;
        var hasMore = resJson.GetValue("has_more")?.ToObject<int>() == 1;

        var page = new PageT { CurrentPage = pn, TotalCount = totalCount, HasMore = hasMore, HasPrevious = pn > 1 };

        return new UserList(objs, page);
    }

    /// <summary>
    ///     发送获取用户关注列表请求
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <returns>关注的用户列表</returns>
    public async Task<UserList> RequestAsync(long userId, int pn)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? ""),
            new("_client_version", Const.MainVersion),
            new("pn", pn.ToString()),
            new("uid", userId.ToString())
        };

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/follow/followList").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
