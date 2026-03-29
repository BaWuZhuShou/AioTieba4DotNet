using AioTieba4DotNet.Models.Users;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class AtMessageMapper
{
    internal static AtMessage FromTbData(JObject data)
    {
        return new AtMessage
        {
            Content = data.GetValue("content")?.Value<string>() ?? string.Empty,
            Fname = data.GetValue("fname")?.Value<string>() ?? string.Empty,
            ThreadId = data.GetValue("thread_id")?.Value<long>() ?? 0,
            PostId = data.GetValue("post_id")?.Value<long>() ?? 0,
            Replyer = data.GetValue("replyer") is JObject replyer ? UserInfoMapper.FromTbData(replyer) : null,
            IsFloor = data.GetValue("is_floor")?.Value<int>() == 1,
            IsFirstPost = data.GetValue("is_first_post")?.Value<int>() == 1,
            Time = data.GetValue("time")?.Value<long>() ?? 0
        };
    }
}
