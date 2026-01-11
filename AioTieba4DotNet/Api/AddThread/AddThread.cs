using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.AddThread;

/// <summary>
/// 发布主题帖 (Thread) 的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
/// <param name="mode">请求模式</param>
[RequireBduss]
[PythonApi("aiotieba.api.add_thread")]
public class AddThread(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaRequestMode mode = TiebaRequestMode.Http)
    : ApiWsBase<long>(httpCore, wsCore, mode)
{
    private static long ParseBody(string body)
    {
        var resJson = ApiBase.ParseBody(body);
        var data = resJson["data"];
        if (data != null)
        {
            var info = data["info"];
            if (info?["need_vcode"]?.ToObject<int>() > 0)
            {
                throw new TiebaException("Need verify code");
            }
        }

        return data?["tid"]?.ToObject<long>() ?? 0;
    }

    /// <summary>
    /// 发送发布主题帖请求
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="fid">吧 ID</param>
    /// <param name="title">帖子标题</param>
    /// <param name="contents">帖子内容碎片列表</param>
    /// <returns>新发布的帖子 ID (tid)</returns>
    public async Task<long> RequestAsync(string fname, ulong fid, string title, List<IFrag> contents)
    {
        return await ExecuteAsync(
            () => RequestHttpAsync(fname, fid, title, contents),
            null
        );
    }

    public async Task<long> RequestHttpAsync(string fname, ulong fid, string title, List<IFrag> contents)
    {
        var contentJson = JsonConvert.SerializeObject(contents.Select(c => c.ToDict()));
        var account = HttpCore.Account!;

        var data = new List<KeyValuePair<string, string>>()
        {
            new("BDUSS", account.Bduss),
            new("_client_id", account.ClientId ?? string.Empty),
            new("_client_type", "2"),
            new("_client_version", Const.PostVersion),
            new("_phone_imei", "000000000000000"),
            new("anonymous", "0"),
            new("c3_aid", account.C3Aid ?? string.Empty),
            new("content", contentJson),
            new("cuid", account.CuidGalaxy2),
            new("cuid_galaxy2", account.CuidGalaxy2),
            new("fid", fid.ToString()),
            new("forum_id", fid.ToString()),
            new("fname", fname),
            new("is_ad", "0"),
            new("is_feedback", "0"),
            new("is_new_list", "1"),
            new("model", "SM-G988N"),
            new("net_type", "1"),
            new("new_vcode", "1"),
            new("post_from", "3"),
            new("stErrorNums", "0"),
            new("stMethodNum", "1"),
            new("stMode", "1"),
            new("stSize", "0"),
            new("stTime", "0"),
            new("stoken", account.Stoken),
            new("tbs", account.Tbs!),
            new("title", title),
            new("vcode_tag", "12"),
            new("z_id", account.ZId ?? string.Empty),
        };

        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/thread/add").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
