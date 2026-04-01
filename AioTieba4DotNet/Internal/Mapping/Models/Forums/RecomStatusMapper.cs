using AioTieba4DotNet.Models.Forums;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class RecomStatusMapper
{
    internal static RecomStatus FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new RecomStatus
        {
            TotalRecommendNum = data.GetValue("total_recommend_num")?.Value<int>() ?? 0,
            UsedRecommendNum = data.GetValue("used_recommend_num")?.Value<int>() ?? 0
        };
    }
}
