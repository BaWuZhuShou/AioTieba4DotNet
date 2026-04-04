using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetBawuUserlogs;

internal sealed class GetBawuUserlogs(ITiebaHttpCore httpCore)
{
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification =
            "The admin user-log query intentionally keeps the upstream Tieba filter options explicit for parity and call-site clarity.")]
    public async Task<BawuUserLogs> RequestAsync(string fname, int pn, string searchValue, BawuSearchType searchType,
        DateTimeOffset? startTime, DateTimeOffset? endTime, int operationType,
        CancellationToken cancellationToken = default)
    {
        var parameters = BuildParameters(fname, pn, searchValue, searchType, startTime, endTime, operationType);
        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/bawu2/platform/listUserLog").Uri;
        var result = await httpCore.SendWebGetAsync(requestUri, parameters, cancellationToken);
        return BawuUserLogsMapper.FromTbData(result);
    }

    private static List<KeyValuePair<string, string>> BuildParameters(string fname, int pn, string searchValue,
        BawuSearchType searchType, DateTimeOffset? startTime, DateTimeOffset? endTime, int operationType)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("word", fname), new("pn", pn.ToString()), new("ie", "utf-8")
        };

        if (operationType != 0)
            parameters.Add(new KeyValuePair<string, string>("op_type", operationType.ToString()));

        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            parameters.Add(new KeyValuePair<string, string>("svalue", searchValue));
            parameters.Add(new KeyValuePair<string, string>("stype",
                searchType == BawuSearchType.User ? "post_uname" : "op_uname"));
        }

        if (startTime.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string>("end",
                (endTime ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString()));
            parameters.Add(new KeyValuePair<string, string>("begin", startTime.Value.ToUnixTimeSeconds().ToString()));
        }

        return parameters;
    }
}
