using AioTieba4DotNet.Models.Threads;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class RecoverPageMapper
{
    internal static RecoverPage FromTbData(JObject? data)
    {
        if (data is null)
            return new RecoverPage();

        var currentPage = data.GetValue("pn")?.Value<int>() ?? 0;
        return new RecoverPage
        {
            PageSize = data.GetValue("rn")?.Value<int>() ?? 0,
            CurrentPage = currentPage,
            HasMore = data.GetValue("has_more")?.Value<int>() == 1,
            HasPrevious = currentPage > 1
        };
    }
}
