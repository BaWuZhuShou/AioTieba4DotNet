using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.DelBawu;

public class DelBaWu(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    public async Task<bool> RequestAsync(int fid, string portrait, string baWuType)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("fn", "-"),
            new("fid", fid.ToString()),
            new("team_un", "-"),
            new("team_uid", portrait),
            new("bawu_type", baWuType)
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/bawuteamclear").Uri;
        var responseMessage = await HttpCore.PackWebFormRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsStringAsync();

        return ParseBody(result);
    }
}

