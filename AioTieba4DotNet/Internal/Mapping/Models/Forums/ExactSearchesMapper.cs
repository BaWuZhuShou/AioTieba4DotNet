using AioTieba4DotNet.Models.Forums;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ExactSearchesMapper
{
    internal static ExactSearches FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var objs = data.GetValue("post_list") is JArray posts
            ? posts.OfType<JObject>().Select(FromSearchItemTbData).ToList()
            : [];

        var pageToken = data.GetValue("page") as JObject ?? new JObject();
        var page = new ExactSearchesPage
        {
            PageSize = pageToken.GetValue("page_size")?.Value<int>() ?? 0,
            CurrentPage = pageToken.GetValue("current_page")?.Value<int>() ?? 0,
            TotalPage = pageToken.GetValue("total_page")?.Value<int>() ?? 0,
            TotalCount = pageToken.GetValue("total_count")?.Value<int>() ?? 0,
            HasMore = pageToken.GetValue("has_more")?.Value<int>() == 1,
            HasPrevious = pageToken.GetValue("has_prev")?.Value<int>() == 1
        };

        return new ExactSearches(objs, page);
    }

    private static ExactSearch FromSearchItemTbData(JObject data)
    {
        var author = data.GetValue("author") as JObject;
        return new ExactSearch
        {
            Text = data.GetValue("content")?.Value<string>() ?? string.Empty,
            Title = data.GetValue("title")?.Value<string>() ?? string.Empty,
            Fname = data.GetValue("fname")?.Value<string>() ?? string.Empty,
            Tid = data.GetValue("tid")?.Value<long>() ?? 0,
            Pid = data.GetValue("pid")?.Value<long>() ?? 0,
            ShowName = author?.GetValue("name_show")?.Value<string>() ?? string.Empty,
            IsComment = data.GetValue("is_floor")?.Value<int>() == 1,
            CreateTime = data.GetValue("time")?.Value<int>() ?? 0
        };
    }
}
