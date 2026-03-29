using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class AtMessagesMapper
{
    internal static AtMessages FromTbData(JObject data)
    {
        var objs = data.GetValue("at_list") is JArray atList
            ? atList.OfType<JObject>().Select(AtMessageMapper.FromTbData).ToList()
            : [];
        var page = PageTMapper.FromTbData(data.GetValue("page") as JObject);

        return new AtMessages(objs, page);
    }
}
