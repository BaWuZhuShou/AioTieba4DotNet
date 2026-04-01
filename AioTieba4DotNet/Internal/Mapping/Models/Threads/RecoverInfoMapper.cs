using AioTieba4DotNet.Models.Threads;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class RecoverInfoMapper
{
    internal static RecoverInfo FromTbData(JObject? data)
    {
        if (data is null)
        {
            return new RecoverInfo
            {
                Content = new Models.Contents.Content(),
                User = new RecoverUser()
            };
        }

        var threadInfo = data.GetValue("thread_info") as JObject;
        return new RecoverInfo
        {
            Content = RecoverContentMapper.FromTbData(threadInfo),
            Title = threadInfo?.GetValue("title")?.Value<string>() ?? string.Empty,
            Tid = threadInfo?.GetValue("thread_id")?.Value<long>() ?? 0,
            Pid = threadInfo?.GetValue("post_id")?.Value<long>() ?? 0,
            User = RecoverUserMapper.FromTbData(data.GetValue("user_info") as JObject, "show_nickname")
        };
    }
}
