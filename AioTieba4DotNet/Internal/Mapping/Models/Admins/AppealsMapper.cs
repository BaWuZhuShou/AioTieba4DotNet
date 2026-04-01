using AioTieba4DotNet.Models.Admins;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class AppealsMapper
{
    internal static Appeals FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var payload = data.GetValue("data") as JObject ?? new JObject();
        var appeals = payload.GetValue("appeal_list") is JArray appealList
            ? appealList.OfType<JObject>().Select(MapAppeal).ToList()
            : [];

        var hasMore = payload.GetValue("has_more")?.Type switch
        {
            JTokenType.Boolean => payload.GetValue("has_more")!.Value<bool>(),
            JTokenType.Integer => payload.GetValue("has_more")!.Value<int>() != 0,
            _ => false
        };

        return new Appeals(appeals, hasMore);
    }

    private static Appeal MapAppeal(JObject data)
    {
        var user = data.GetValue("user") as JObject ?? new JObject();
        var portrait = user.GetValue("portrait")?.Value<string>() ?? string.Empty;
        var suffixIndex = portrait.IndexOf('?', StringComparison.Ordinal);
        if (suffixIndex >= 0)
            portrait = portrait[..suffixIndex];

        return new Appeal
        {
            UserId = user.GetValue("id")?.Value<long>() ?? 0,
            Portrait = portrait,
            UserName = user.GetValue("name")?.Value<string>() ?? string.Empty,
            NickName = user.GetValue("name_show")?.Value<string>() ?? string.Empty,
            AppealId = long.TryParse(data.GetValue("appeal_id")?.Value<string>(), out var appealId) ? appealId : 0,
            AppealReason = data.GetValue("appeal_reason")?.Value<string>() ?? string.Empty,
            AppealTime =
                long.TryParse(data.GetValue("appeal_time")?.Value<string>(), out var appealTime) ? appealTime : 0,
            PunishReason = data.GetValue("punish_reason")?.Value<string>() ?? string.Empty,
            PunishTime =
                long.TryParse(data.GetValue("punish_start_time")?.Value<string>(), out var punishTime)
                    ? punishTime
                    : 0,
            PunishDay = data.GetValue("punish_day_num")?.Value<int>() ?? 0,
            OperatorName = data.GetValue("operate_man")?.Value<string>() ?? string.Empty
        };
    }
}
