using System.Net;
using System.Text.RegularExpressions;
using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static partial class RankForumsMapper
{
    private static readonly Regex RowRegex = CreateRowRegex();
    private static readonly Regex CellRegex = CreateCellRegex();
    private static readonly Regex PaginationRegex = CreatePaginationRegex();
    private static readonly Regex CurrentPageRegex = CreateCurrentPageRegex();
    private static readonly Regex TotalPageRegex = CreateTotalPageRegex();
    private static readonly Regex TagRegex = CreateTagRegex();

    internal static RankForums FromHtml(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var forums = RowRegex.Matches(body)
            .Select(static match => ParseRow(match.Groups["row"].Value))
            .Where(static forum => forum != null)
            .Select(static forum => forum!)
            .ToList();

        return new RankForums(forums, ParsePage(body));
    }

    private static RankForum? ParseRow(string row)
    {
        var cells = CellRegex.Matches(row)
            .Select(static match => new
            {
                ClassName = match.Groups["className"].Value,
                Content = match.Groups["content"].Value
            })
            .ToList();

        if (cells.Count < 5)
            return null;

        return new RankForum
        {
            Fname = StripTags(cells[1].Content),
            SignNum = ParseInt(StripTags(cells[2].Content)),
            MemberNum = ParseInt(StripTags(cells[3].Content)),
            HasBaWu = !cells[4].ClassName.Contains("no_bawu", StringComparison.OrdinalIgnoreCase)
                && !cells[4].Content.Contains("no_bawu", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static RankForumsPage ParsePage(string body)
    {
        var paginationMatch = PaginationRegex.Match(body);
        if (!paginationMatch.Success)
        {
            return new RankForumsPage
            {
                CurrentPage = 1,
                TotalPage = 1,
                HasMore = false,
                HasPrevious = false
            };
        }

        var content = paginationMatch.Groups["content"].Value;
        var current = ParseInt(CurrentPageRegex.Match(content).Groups["current"].Value, 1);
        var total = ParseInt(TotalPageRegex.Match(content).Groups["total"].Value, Math.Max(current, 1));

        return new RankForumsPage
        {
            CurrentPage = current,
            TotalPage = total,
            HasMore = current < total,
            HasPrevious = current > 1
        };
    }

    private static string StripTags(string value) => WebUtility.HtmlDecode(TagRegex.Replace(value, string.Empty)).Trim();

    private static int ParseInt(string value, int defaultValue = 0) => int.TryParse(value, out var parsed) ? parsed : defaultValue;

    [GeneratedRegex("<tr[^>]*class=\"[^\"]*j_rank_row[^\"]*\"[^>]*>(?<row>.*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateRowRegex();

    [GeneratedRegex("<td(?:(?:\\s+[^>]*class=\"(?<className>[^\"]*)\")|[^>])*>(?<content>.*?)</td>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateCellRegex();

    [GeneratedRegex("<div[^>]*class=\"[^\"]*pagination[^\"]*\"[^>]*>(?<content>.*?)</div>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreatePaginationRegex();

    [GeneratedRegex("<span[^>]*>(?<current>\\d+)</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateCurrentPageRegex();

    [GeneratedRegex("pn=(?<total>\\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateTotalPageRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex CreateTagRegex();
}
