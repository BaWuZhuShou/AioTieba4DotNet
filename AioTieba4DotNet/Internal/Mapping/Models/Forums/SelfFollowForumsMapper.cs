using AioTieba4DotNet.Models.Forums;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class SelfFollowForumsMapper
{
    internal static SelfFollowForums FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var objs = data.GetValue("like_forum") is JArray forumList
            ? forumList.OfType<JObject>().Select(static item => new SelfFollowForum
            {
                Fid = item.GetValue("forum_id")?.Value<ulong>() ?? 0,
                Fname = item.GetValue("forum_name")?.Value<string>() ?? string.Empty,
                Level = item.GetValue("level_id")?.Value<int>() ?? 0,
                IsSigned = item.GetValue("is_sign")?.Value<int>() == 1
            }).ToList()
            : [];

        var hasMore = data.GetValue("like_forum_has_more")?.Value<int>() == 1;
        return new SelfFollowForums(objs, hasMore);
    }
}
