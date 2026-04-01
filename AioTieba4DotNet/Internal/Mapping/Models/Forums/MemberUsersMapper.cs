using System.Net;
using System.Text.RegularExpressions;
using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static partial class MemberUsersMapper
{
    private static readonly Regex PaginationRegex = CreatePaginationRegex();
    private static readonly Regex CurrentPageRegex = CreateCurrentPageRegex();
    private static readonly Regex TotalPageRegex = CreateTotalPageRegex();
    private static readonly Regex UserRegex = CreateUserRegex();

    internal static MemberUsers FromHtml(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var users = UserRegex.Matches(body)
            .Select(static match => new MemberUser
            {
                UserName = HtmlDecode(match.Groups["userName"].Value),
                Portrait = HtmlDecode(match.Groups["portrait"].Value),
                Level = ParseInt(match.Groups["level"].Value)
            })
            .ToList();

        var page = ParsePage(body);
        return new MemberUsers(users, page);
    }

    private static MemberUsersPage ParsePage(string body)
    {
        var paginationMatch = PaginationRegex.Match(body);
        if (!paginationMatch.Success)
        {
            return new MemberUsersPage
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

        return new MemberUsersPage
        {
            CurrentPage = current,
            TotalPage = total,
            HasMore = current < total,
            HasPrevious = current > 1
        };
    }

    private static string HtmlDecode(string value) => WebUtility.HtmlDecode(value).Trim();

    private static int ParseInt(string value, int defaultValue = 0) => int.TryParse(value, out var parsed) ? parsed : defaultValue;

    [GeneratedRegex("<div[^>]*class=\"[^\"]*tbui_pagination[^\"]*\"[^>]*>(?<content>.*?)</div>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreatePaginationRegex();

    [GeneratedRegex("<li[^>]*class=\"[^\"]*active[^\"]*\"[^>]*>(?<current>\\d+)</li>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateCurrentPageRegex();

    [GeneratedRegex("\\((?<total>\\d+)\\)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateTotalPageRegex();

    [GeneratedRegex(
        "<div[^>]*class=\"[^\"]*name_wrap[^\"]*\"[^>]*>.*?<a[^>]*title=\"(?<userName>[^\"]*)\"[^>]*href=\"[^\"]*id=(?<portrait>tb\\.[^\"&?]+)[^\"]*\"[^>]*>.*?<span[^>]*class=\"[^\"]*level_(?<level>\\d+)[^\"]*\"",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateUserRegex();
}
