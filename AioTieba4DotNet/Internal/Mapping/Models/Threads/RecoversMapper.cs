using AioTieba4DotNet.Models.Threads;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class RecoversMapper
{
    internal static Recovers FromTbData(JObject? data)
    {
        if (data is null)
            return new Recovers([], new RecoverPage());

        var container = data.GetValue("data") as JObject;
        var objs = container?.GetValue("thread_list") is JArray recovers
            ? recovers.OfType<JObject>().Select(RecoverMapper.FromTbData).ToList()
            : [];

        var page = RecoverPageMapper.FromTbData(container?.GetValue("page") as JObject);
        return new Recovers(objs, page);
    }
}
