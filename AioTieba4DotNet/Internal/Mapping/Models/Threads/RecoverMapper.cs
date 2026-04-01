using AioTieba4DotNet.Models.Threads;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class RecoverMapper
{
    internal static Recover FromTbData(JObject? data)
    {
        if (data is null)
            return new Recover { User = new RecoverUser() };

        var threadInfo = data.GetValue("thread_info") as JObject;
        var postInfo = data.GetValue("post_info") as JObject;
        var target = postInfo is { HasValues: true } ? postInfo : threadInfo;

        return new Recover
        {
            Text = target?.GetValue("abstract")?.Value<string>() ?? string.Empty,
            Tid = threadInfo?.GetValue("tid")?.Value<long>() ?? 0,
            Pid = postInfo?.GetValue("pid")?.Value<long>() ?? 0,
            User = RecoverUserMapper.FromTbData(target, "user_nickname"),
            OperatorShowName = data.SelectToken("op_info.name")?.Value<string>() ?? string.Empty,
            OperatorTime = data.SelectToken("op_info.time")?.Value<int>() ?? 0,
            IsFloor = ReadBoolean(data.GetValue("is_foor")),
            IsHide = ReadBoolean(data.GetValue("is_frs_mask"))
        };
    }

    private static bool ReadBoolean(JToken? token)
    {
        return token?.Type switch
        {
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.Integer => token.Value<int>() != 0,
            JTokenType.String => token.Value<string>() switch
            {
                "1" => true,
                "0" => false,
                var value when bool.TryParse(value, out var parsed) => parsed,
                _ => false
            },
            _ => false
        };
    }
}
