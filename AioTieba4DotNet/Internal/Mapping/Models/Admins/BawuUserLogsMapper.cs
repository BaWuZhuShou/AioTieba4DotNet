using AioTieba4DotNet.Models.Admins;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BawuUserLogsMapper
{
    internal static BawuUserLogs FromTbData(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var logs = AdminHtmlParsing.ExtractTableRows(html)
            .Select(MapRow)
            .Where(static log => log is not null)
            .Cast<BawuUserLog>()
            .ToList();

        var (currentPage, totalPage, totalCount, hasMore, hasPrevious) = AdminHtmlParsing.ParseCommonPage(html);
        return new BawuUserLogs(logs,
            new BawuUserLogPage
            {
                CurrentPage = currentPage,
                TotalPage = totalPage,
                TotalCount = totalCount,
                HasMore = hasMore,
                HasPrevious = hasPrevious
            });
    }

    private static BawuUserLog? MapRow(string rowHtml)
    {
        var cells = AdminHtmlParsing.ExtractTableCells(rowHtml);
        if (cells.Count < 5)
            return null;

        var durationText = AdminHtmlParsing.NormalizeText(cells[2])
            .Replace(" ", string.Empty, StringComparison.Ordinal);
        var durationDays = durationText.EndsWith("天", StringComparison.Ordinal) &&
                           int.TryParse(durationText[..^1], out var parsedDuration)
            ? parsedDuration
            : 0;

        return new BawuUserLog
        {
            UserPortrait =
                AdminHtmlParsing.ExtractPortraitFromHomeHref(AdminHtmlParsing.GetAttributeValue(cells[0], "href")),
            OperationType = AdminHtmlParsing.NormalizeText(cells[1]),
            OperationDurationDays = durationDays,
            OperatorUserName = AdminHtmlParsing.NormalizeText(cells[3]),
            OperationTime = AdminHtmlParsing.ParseFullDateTime(AdminHtmlParsing.NormalizeText(cells[4]))
        };
    }
}
