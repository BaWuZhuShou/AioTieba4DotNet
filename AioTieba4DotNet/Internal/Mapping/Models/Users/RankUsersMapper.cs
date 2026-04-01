using System.Net;
using System.Text.RegularExpressions;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static partial class RankUsersMapper
{
    private static readonly Regex RowRegex = CreateRowRegex();
    private static readonly Regex CellRegex = CreateCellRegex();
    private static readonly Regex LevelRegex = CreateLevelRegex();
    private static readonly Regex TagRegex = CreateTagRegex();
    private static readonly Regex PageRegex = CreatePageRegex();

    internal static RankUsers FromHtml(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var users = RowRegex.Matches(body)
            .Select(static match => ParseRow(match.Groups["row"].Value))
            .Where(static user => user is not null)
            .Select(static user => user!)
            .ToList();

        return new RankUsers(users, ParsePage(body));
    }

    private static RankUser? ParseRow(string row)
    {
        var cells = CellRegex.Matches(row)
            .Select(static match => match.Groups["content"].Value)
            .ToList();

        if (cells.Count < 4)
            return null;

        var levelMatch = LevelRegex.Match(cells[2]);
        return new RankUser
        {
            UserName = StripTags(cells[1]),
            IsVip = cells[1].Contains("drl_item_vip", StringComparison.OrdinalIgnoreCase),
            Level = ParseInt(levelMatch.Groups["level"].Value),
            Exp = ParseInt(StripTags(cells[3]))
        };
    }

    private static PageT ParsePage(string body)
    {
        var pageMatch = PageRegex.Match(body);
        if (!pageMatch.Success)
            return new PageT { CurrentPage = 1, TotalPage = 1, HasMore = false, HasPrevious = false };

        var pageJson = JObject.Parse(WebUtility.HtmlDecode(pageMatch.Groups["data"].Value));
        var currentPage = pageJson.GetValue("cur_page")?.Value<int>() ?? 1;
        var totalPage = pageJson.GetValue("total_num")?.Value<int>() ?? Math.Max(currentPage, 1);

        return new PageT
        {
            CurrentPage = currentPage,
            TotalPage = totalPage,
            HasMore = currentPage < totalPage,
            HasPrevious = currentPage > 1
        };
    }

    private static string StripTags(string value)
    {
        return WebUtility.HtmlDecode(TagRegex.Replace(value, string.Empty)).Trim();
    }

    private static int ParseInt(string value, int defaultValue = 0)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    [GeneratedRegex("<tr[^>]*class=\"[^\"]*(?:drl_list_item|drl_list_item_self)[^\"]*\"[^>]*>(?<row>.*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateRowRegex();

    [GeneratedRegex("<td[^>]*>(?<content>.*?)</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateCellRegex();

    [GeneratedRegex("bg_lv(?<level>\\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateLevelRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex CreateTagRegex();

    [GeneratedRegex("<ul[^>]*class=\"[^\"]*p_rank_pager[^\"]*\"[^>]*data-field=(['\"])(?<data>.*?)\\1",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreatePageRegex();
}
