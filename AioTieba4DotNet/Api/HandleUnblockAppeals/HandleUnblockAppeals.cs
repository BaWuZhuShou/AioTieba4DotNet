using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.HandleUnblockAppeals;

internal sealed class HandleUnblockAppeals(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseResponse(string body)
    {
        _ = ParseBody(body, "no", "error");
        return true;
    }

    public async Task<bool> RequestAsync(ulong fid, IReadOnlyList<long> appealIds, bool refuse,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>> { new("fn", "-"), new("fid", fid.ToString()) };

        for (var i = 0; i < appealIds.Count; i++)
            data.Add(new KeyValuePair<string, string>($"appeal_list[{i}]", appealIds[i].ToString()));

        data.Add(new KeyValuePair<string, string>("refuse_reason", "_"));
        data.Add(new KeyValuePair<string, string>("status", refuse ? "2" : "1"));
        data.Add(new KeyValuePair<string, string>("tbs", httpCore.Account!.Tbs!));

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/multiAppealhandle").Uri;
        var result = await httpCore.SendWebFormAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
