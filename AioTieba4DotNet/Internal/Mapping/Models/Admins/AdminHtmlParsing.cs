using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace AioTieba4DotNet.Internal.Mapping;

internal static partial class AdminHtmlParsing
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
    private static readonly Regex TagRegex = BuildTagRegex();
    private static readonly Regex TdRegex = BuildTdRegex();
    private static readonly Regex TrRegex = BuildTrRegex();
    private static readonly Regex BreadcrumbCountRegex = BuildBreadcrumbCountRegex();
    private static readonly Regex PaginationRegex = BuildPaginationRegex();
    private static readonly Regex ActivePageRegex = BuildActivePageRegex();
    private static readonly Regex TotalPageRegex = BuildTotalPageRegex();
    private static readonly Regex AttributeRegexTemplate = BuildAttributeRegexTemplate();
    private static readonly Regex ImageHashRegex = BuildImageHashRegex();
    private static readonly Regex WhitespaceRegex = BuildWhitespaceRegex();

    internal static string DecodeAndStrip(string html)
    {
        return WebUtility.HtmlDecode(TagRegex.Replace(html, string.Empty)).Trim();
    }

    internal static string NormalizeText(string html)
    {
        return WhitespaceRegex.Replace(DecodeAndStrip(html), " ").Trim();
    }

    internal static IReadOnlyList<string> ExtractTableRows(string html)
    {
        return TrRegex.Matches(html).Select(static match => match.Groups["content"].Value).ToArray();
    }

    internal static IReadOnlyList<string> ExtractTableCells(string rowHtml)
    {
        return TdRegex.Matches(rowHtml).Select(static match => match.Groups["content"].Value).ToArray();
    }

    internal static string GetAttributeValue(string html, string attributeName)
    {
        var pattern = AttributeRegexTemplate.ToString().Replace("__ATTRIBUTE__", Regex.Escape(attributeName));
        var match = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase, RegexTimeout).Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["value"].Value) : string.Empty;
    }

    internal static string ExtractPortraitFromHomeHref(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return string.Empty;

        var markerIndex = href.IndexOf("id=", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return string.Empty;

        var portrait = href[(markerIndex + 3)..];
        var fragmentIndex = portrait.IndexOf("#/", StringComparison.Ordinal);
        if (fragmentIndex >= 0)
            portrait = portrait[..fragmentIndex];

        var queryIndex = portrait.IndexOf('&');
        if (queryIndex >= 0)
            portrait = portrait[..queryIndex];

        return WebUtility.HtmlDecode(portrait);
    }

    internal static (int CurrentPage, int TotalPage, int TotalCount, bool HasMore, bool HasPrevious) ParseCommonPage(
        string html)
    {
        var totalCountMatch = BreadcrumbCountRegex.Match(html);
        var totalCount = totalCountMatch.Success
            ? int.Parse(totalCountMatch.Groups["count"].Value, CultureInfo.InvariantCulture)
            : 0;

        var paginationMatch = PaginationRegex.Match(html);
        if (!paginationMatch.Success)
            return totalCount == 0 ? (0, 0, 0, false, false) : (1, 1, totalCount, false, false);

        var paginationHtml = paginationMatch.Groups["content"].Value;
        var activePageMatch = ActivePageRegex.Match(paginationHtml);
        if (!activePageMatch.Success)
            return totalCount == 0 ? (0, 0, totalCount, false, false) : (1, 1, totalCount, false, false);

        var currentPage = int.Parse(activePageMatch.Groups["page"].Value, CultureInfo.InvariantCulture);
        var totalPageMatch = TotalPageRegex.Match(paginationHtml);
        var totalPage = totalPageMatch.Success
            ? int.Parse(totalPageMatch.Groups["page"].Value, CultureInfo.InvariantCulture)
            : currentPage;

        return (currentPage, totalPage, totalCount, currentPage < totalPage, currentPage > 1);
    }

    internal static DateTime ParseYearlessDateTime(string text)
    {
        var parsed = DateTime.ParseExact(text.Trim(), "MM-dd HH:mm", CultureInfo.InvariantCulture);
        return new DateTime(1904, parsed.Month, parsed.Day, parsed.Hour, parsed.Minute, 0, DateTimeKind.Unspecified);
    }

    internal static DateTime ParseFullDateTime(string text)
    {
        var normalized = WhitespaceRegex.Replace(text.Trim(), " ");
        if (DateTime.TryParseExact(normalized, ["yyyy-MM-ddHH:mm", "yyyy-MM-dd HH:mm"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        throw new FormatException($"Unsupported admin datetime format: '{text}'.");
    }

    internal static string ExtractImageHash(string src)
    {
        var match = ImageHashRegex.Match(src);
        return match.Success ? match.Groups["hash"].Value : string.Empty;
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildTagRegex();

    [GeneratedRegex("<td[^>]*>(?<content>.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildTdRegex();

    [GeneratedRegex("<tr[^>]*>(?<content>.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildTrRegex();

    [GeneratedRegex("<div[^>]*class=(['\"])[^'\"]*breadcrumbs[^'\"]*\\1[^>]*>.*?<em[^>]*>\\s*(?<count>\\d+)\\s*</em>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildBreadcrumbCountRegex();

    [GeneratedRegex("<div[^>]*class=(['\"])[^'\"]*tbui_pagination[^'\"]*\\1[^>]*>(?<content>.*?)</div>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildPaginationRegex();

    [GeneratedRegex(
        "<li[^>]*class=(['\"])[^'\"]*active[^'\"]*\\1[^>]*>\\s*(?:<a[^>]*>)?\\s*(?<page>\\d+)\\s*(?:</a>)?\\s*</li>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildActivePageRegex();

    [GeneratedRegex("\\((?<page>\\d+)\\)", RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildTotalPageRegex();

    [GeneratedRegex("\\b__ATTRIBUTE__\\s*=\\s*(['\"])(?<value>.*?)\\1",
        RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildAttributeRegexTemplate();

    [GeneratedRegex("(?<hash>[^/?#]+)\\.jpg(?:$|[?#])", RegexOptions.Singleline | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex BuildImageHashRegex();

    [GeneratedRegex("\\s+", RegexOptions.None, 1000)]
    private static partial Regex BuildWhitespaceRegex();
}
