using AioTieba4DotNet.Models.Forums;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FollowForumsMapper
{
    internal static FollowForums FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var objs = new List<FollowForum>();
        if (data.GetValue("forum_list") is JObject forumList)
        {
            objs.AddRange(ReadForums(forumList["non-gconforum"] as JArray));
            objs.AddRange(ReadForums(forumList["gconforum"] as JArray));
        }

        var hasMore = data.GetValue("has_more")?.Value<int>() == 1;
        return new FollowForums(objs, hasMore);
    }

    private static IEnumerable<FollowForum> ReadForums(JArray? forumList)
    {
        return forumList?.OfType<JObject>().Select(static item => new FollowForum
        {
            Fid = item.GetValue("id")?.Value<ulong>() ?? 0,
            Fname = item.GetValue("name")?.Value<string>() ?? string.Empty,
            Level = item.GetValue("level_id")?.Value<int>() ?? 0,
            Exp = item.GetValue("cur_score")?.Value<int>() ?? 0
        }) ?? [];
    }
}
