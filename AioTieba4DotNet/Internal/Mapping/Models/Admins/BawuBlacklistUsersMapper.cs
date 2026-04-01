using System.Text.RegularExpressions;
using AioTieba4DotNet.Models.Admins;

namespace AioTieba4DotNet.Internal.Mapping;

internal static partial class BawuBlacklistUsersMapper
{
    internal static BawuBlacklistUsers FromTbData(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var users = AdminHtmlParsing.ExtractTableRows(html)
            .Select(MapRow)
            .Where(static user => user is not null)
            .Cast<BawuBlacklistUser>()
            .ToList();

        var (currentPage, totalPage, totalCount, hasMore, hasPrevious) = AdminHtmlParsing.ParseCommonPage(html);
        return new BawuBlacklistUsers(users, new BawuBlacklistPage
        {
            CurrentPage = currentPage,
            TotalPage = totalPage,
            TotalCount = totalCount,
            HasMore = hasMore,
            HasPrevious = hasPrevious
        });
    }

    private static BawuBlacklistUser? MapRow(string rowHtml)
    {
        var userName = AdminHtmlParsing.GetAttributeValue(rowHtml, "data-user-name");
        var userIdText = AdminHtmlParsing.GetAttributeValue(rowHtml, "data-user-id");
        var href = HomeHrefRegex().Match(rowHtml).Groups["href"].Value;
        if (string.IsNullOrWhiteSpace(userName) || !long.TryParse(userIdText, out var userId) ||
            string.IsNullOrWhiteSpace(href))
            return null;

        return new BawuBlacklistUser
        {
            UserName = AdminHtmlParsing.NormalizeText(userName),
            UserId = userId,
            Portrait = AdminHtmlParsing.ExtractPortraitFromHomeHref(href)
        };
    }

    [GeneratedRegex("<td[^>]*class=(['\"])[^'\"]*left_cell[^'\"]*\\1[^>]*>.*?<a[^>]*href=(['\"])(?<href>.*?)\\2", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex HomeHrefRegex();
}
