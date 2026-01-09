using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Entities;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetFollows;

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

        var page = new PageT
        {
            CurrentPage = pn,
            TotalCount = totalCount,
            HasMore = hasMore,
            HasPrevious = pn > 1
        };

        return new UserList(objs, page);
    }

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
        var responseMessage = await HttpCore.PackAppFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();
        return ParseBody(result);
    }
}
