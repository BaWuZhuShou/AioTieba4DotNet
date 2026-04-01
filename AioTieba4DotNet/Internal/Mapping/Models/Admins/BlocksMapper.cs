using System.Text.RegularExpressions;
using AioTieba4DotNet.Models.Admins;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static partial class BlocksMapper
{
    internal static Blocks FromTbData(JObject data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var payload = data.GetValue("data") as JObject ?? new JObject();
        var content = payload.GetValue("content")?.Value<string>() ?? string.Empty;
        var blocks = BlockRegex().Matches(content).Select(static match => new Block
        {
            UserId = long.Parse(match.Groups["userId"].Value),
            UserName = match.Groups["userName"].Value,
            NickNameOld = match.Groups["nickName"].Value,
            Day = int.Parse(match.Groups["day"].Value)
        }).ToList();

        var pageMap = payload.GetValue("page") as JObject ?? new JObject();
        var currentPage = pageMap.GetValue("pn")?.Value<int>() ?? 0;
        return new Blocks(blocks, new BlocksPage
        {
            PageSize = pageMap.GetValue("size")?.Value<int>() ?? 0,
            CurrentPage = currentPage,
            TotalPage = pageMap.GetValue("total_page")?.Value<int>() ?? 0,
            TotalCount = pageMap.GetValue("total_count")?.Value<int>() ?? 0,
            HasMore = ToBoolean(pageMap.GetValue("have_next")),
            HasPrevious = currentPage > 1
        });
    }

    private static bool ToBoolean(JToken? token) => token?.Type switch
    {
        JTokenType.Boolean => token.Value<bool>(),
        JTokenType.Integer => token.Value<int>() != 0,
        _ => false
    };

    [GeneratedRegex("<li[^>]*>.*?<a[^>]*attr-uid=(['\"])(?<userId>\\d+)\\1[^>]*attr-un=(['\"])(?<userName>.*?)\\2[^>]*attr-nn=(['\"])(?<nickName>.*?)\\3[^>]*attr-blockday=(['\"])(?<day>\\d+)\\4", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BlockRegex();
}
