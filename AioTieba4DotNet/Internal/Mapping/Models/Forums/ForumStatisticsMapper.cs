using AioTieba4DotNet.Models.Forums;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ForumStatisticsMapper
{
    internal static ForumStatistics FromTbData(JArray data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new ForumStatistics
        {
            View = Extract(data, 0),
            Thread = Extract(data, 1),
            NewMember = Extract(data, 2),
            Post = Extract(data, 3),
            SignRatio = Extract(data, 4),
            AvgTime = Extract(data, 5),
            AvgTimes = Extract(data, 6),
            Recommend = Extract(data, 7)
        };
    }

    private static IReadOnlyList<int> Extract(JArray data, int index)
    {
        if (index >= data.Count || data[index] is not JObject group)
            return [];

        var values = group.GetValue("group") is JArray groups && groups.Count > 1
            ? groups[1] is JObject secondGroup
                ? secondGroup["values"] as JArray
                : null
            : null;

        return values?.Select(static token => token?["value"]?.Value<int>() ?? 0).ToList() ?? [];
    }
}
