using AioTieba4DotNet.Models.Forums;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class SelfFollowForumsV1Mapper
{
    internal static SelfFollowForumsV1 FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var objs = data.GetValue("list") is JArray forumList
            ? forumList.OfType<JObject>().Select(static item => new SelfFollowForumV1
            {
                Fid = item.GetValue("forum_id")?.Value<ulong>() ?? 0,
                Fname = item.GetValue("forum_name")?.Value<string>() ?? string.Empty,
                Level = item.GetValue("level_id")?.Value<int>() ?? 0
            }).ToList()
            : [];

        var page = data.GetValue("page") is JObject pageData
            ? new SelfFollowForumsV1Page
            {
                CurrentPage = pageData.GetValue("cur_page")?.Value<int>() ?? 0,
                TotalPage = pageData.GetValue("total_page")?.Value<int>() ?? 0,
                HasMore = (pageData.GetValue("cur_page")?.Value<int>() ?? 0) <
                          (pageData.GetValue("total_page")?.Value<int>() ?? 0),
                HasPrevious = (pageData.GetValue("cur_page")?.Value<int>() ?? 0) > 1
            }
            : new SelfFollowForumsV1Page();

        return new SelfFollowForumsV1(objs, page);
    }
}
