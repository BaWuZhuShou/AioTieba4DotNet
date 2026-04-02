using System.Net;
using System.Text.RegularExpressions;
using AioTieba4DotNet.Models.Admins;

namespace AioTieba4DotNet.Internal.Mapping;

internal static partial class BawuPostLogsMapper
{
    internal static BawuPostLogs FromTbData(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var logs = AdminHtmlParsing.ExtractTableRows(html)
            .Select(MapRow)
            .OfType<BawuPostLog>()
            .ToList();

        var (currentPage, totalPage, totalCount, hasMore, hasPrevious) = AdminHtmlParsing.ParseCommonPage(html);
        return new BawuPostLogs(logs,
            new BawuPostLogPage
            {
                CurrentPage = currentPage,
                TotalPage = totalPage,
                TotalCount = totalCount,
                HasMore = hasMore,
                HasPrevious = hasPrevious
            });
    }

    private static BawuPostLog? MapRow(string rowHtml)
    {
        var cells = AdminHtmlParsing.ExtractTableCells(rowHtml);
        if (cells.Count < 4)
            return null;

        var leftCell = cells[0];
        var postMetaMatch = PostMetaRegex().Match(leftCell);
        var contentMatch = ContentBlockRegex().Match(leftCell);
        if (!postMetaMatch.Success || !contentMatch.Success)
            return null;

        var href = contentMatch.Groups["href"].Value;
        var title = AdminHtmlParsing.NormalizeText(contentMatch.Groups["titleInner"].Value);

        var text = AdminHtmlParsing.NormalizeText(contentMatch.Groups["text"].Value);
        if (text.Length > 12)
            text = text[12..];

        var tid = ParseThreadId(href);
        var pid = ParsePostId(href);
        if (pid == tid || !title.StartsWith("回复：", StringComparison.Ordinal))
        {
            pid = 0;
            text = string.IsNullOrEmpty(text) ? title : $"{title}\n{text}";
        }
        else
        {
            title = title[3..];
        }

        var medias = MediaRegex().Matches(leftCell).Select(static match =>
        {
            var src = WebUtility.HtmlDecode(match.Groups["src"].Value);
            return new BawuPostLogMedia
            {
                Src = src,
                OriginSrc = WebUtility.HtmlDecode(match.Groups["href"].Value),
                Hash = AdminHtmlParsing.ExtractImageHash(src)
            };
        }).ToList();

        return new BawuPostLog
        {
            Text = text,
            Title = title,
            Medias = medias,
            Tid = tid,
            Pid = pid,
            OperationType = AdminHtmlParsing.NormalizeText(cells[1]),
            PostPortrait = AdminHtmlParsing.ExtractPortraitFromHomeHref(postMetaMatch.Groups["portraitHref"].Value),
            PostTime =
                AdminHtmlParsing.ParseYearlessDateTime(
                    AdminHtmlParsing.NormalizeText(postMetaMatch.Groups["postTime"].Value)),
            OperatorUserName = AdminHtmlParsing.NormalizeText(cells[2]),
            OperationTime = AdminHtmlParsing.ParseFullDateTime(AdminHtmlParsing.NormalizeText(cells[3]))
        };
    }

    private static long ParseThreadId(string href)
    {
        var startIndex = href.IndexOf("/p/", StringComparison.Ordinal);
        if (startIndex < 0)
            return 0;

        var tidPart = href[(startIndex + 3)..];
        var endIndex = tidPart.IndexOfAny(['?', '#']);
        if (endIndex >= 0)
            tidPart = tidPart[..endIndex];

        return long.TryParse(tidPart, out var tid) ? tid : 0;
    }

    private static long ParsePostId(string href)
    {
        var hashIndex = href.LastIndexOf('#');
        if (hashIndex < 0)
            return 0;

        var pidPart = href[(hashIndex + 1)..];
        return long.TryParse(pidPart, out var pid) ? pid : 0;
    }

    [GeneratedRegex(
        "<div[^>]*class=(['\"])[^'\"]*post_meta[^'\"]*\\1[^>]*>.*?<a[^>]*href=(['\"])(?<portraitHref>.*?)\\2[^>]*>.*?</a>.*?<time[^>]*>(?<postTime>.*?)</time>.*?</div>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PostMetaRegex();

    [GeneratedRegex(
        "<h1[^>]*>\\s*<a[^>]*href=(['\"])(?<href>.*?)\\1(?:[^>]*title=(['\"])(?<titleAttr>.*?)\\3)?[^>]*>(?<titleInner>.*?)</a>\\s*</h1>\\s*<div[^>]*>(?<text>.*?)</div>(?<media>.*)$",
        RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ContentBlockRegex();

    [GeneratedRegex(
        "<a[^>]*href=(?:['\"])(?<href>.*?)(?:['\"])[^>]*>.*?<img[^>]*original=(?:['\"])(?<src>.*?)(?:['\"])",
        RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex MediaRegex();
}
