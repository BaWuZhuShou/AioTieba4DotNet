using AioTieba4DotNet.Models.Threads;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class PageTMapper
{
    internal static PageT FromTbData(Page? page)

    {
        if (page == null) return new PageT();


        return new PageT
        {
            PageSize = page.PageSize,
            CurrentPage = page.CurrentPage,
            TotalPage = page.TotalPage,
            TotalCount = page.TotalCount,
            HasMore = page.HasMore == 1,
            HasPrevious = page.HasPrev == 1
        };
    }

    internal static PageT FromTbData(JObject? page)
    {
        if (page == null) return new PageT();

        return new PageT
        {
            PageSize = page.GetValue("page_size")?.Value<int>() ?? 0,
            CurrentPage = page.GetValue("current_page")?.Value<int>() ?? 0,
            TotalPage = page.GetValue("total_page")?.Value<int>() ?? 0,
            TotalCount = page.GetValue("total_count")?.Value<int>() ?? 0,
            HasMore = page.GetValue("has_more")?.Value<int>() == 1,
            HasPrevious = page.GetValue("has_prev")?.Value<int>() == 1
        };
    }
}
