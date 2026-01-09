using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Api.DelBawu;

// 定义ClearBawuTeamRequest的参数类型
public class ClearBawuTeamParams
{
    public int Fid { get; set; }
    public string Portrait { get; set; } = "";
    public string BaWuType { get; set; } = "";
}

public class DelBaWu(ITiebaHttpCore httpCore) : BaseApiRequest<ClearBawuTeamParams, bool>
{
    public override bool ParseBody(string body)
    {
        // var jsonResponse = JsonConvert.DeserializeObject(body);
        // if (jsonResponse)
        // {
        //     // throw new TiebaServerError(jsonResponse["no"], jsonResponse["error"]);
        // }
        return true;
    }


    // 实现请求方法
    public override async Task<bool> RequestAsync(ClearBawuTeamParams requestParams)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("fn", "-"),
            new("fid", requestParams.Fid.ToString()),
            new("team_un", "-"),
            new("team_uid", requestParams.Portrait),
            new("bawu_type", requestParams.BaWuType)
        };

        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/mo/q/bawuteamclear").Uri;
        var request = await httpCore.PackWebFormRequestAsync(requestUri, data);

        // var body = await core.NetCore.SendRequestAsync(request);
        // ParseBody(body);

        return true;
    }
}

